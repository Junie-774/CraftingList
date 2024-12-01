using CraftingList.Crafting;
using CraftingList.Utility;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CraftingList.UI.CraftingListTab
{
    public class EntryIngredientListing
    {
        public Item Item { get; set; } = Service.Items[0];
        public int NumUsed { get; set; }
        public bool IsHQ { get; set; }
        public int NumAvailable { get; set; }
    }

    public class EntryIngredientSummary
    {

        public CListEntry Entry = new(-1, "", "", false, CListEntry.EmptyHQSelection());

        public List<EntryIngredientListing> Ingredients { get; set; } = new();

        public bool CanCraft = false;
        public int NumCrafts = 0;
    }
    public class IngredientSummary : IDisposable
    {
        public List<EntryIngredientSummary> EntrySummaries = new();
        
        readonly Timer updateTimer = new(500);

        public IngredientSummary()
        {
            updateTimer.Elapsed += (sender, e) => Update();
            updateTimer.Start();
        }

        public void Dispose()
        {
            updateTimer.Dispose();
        }

        public void Pause()
        {
            updateTimer.Stop();
        }

        public void Resume()
        {
            updateTimer.Start();
        }

        public void Update()
        {
            List<EntryIngredientListing> intermediateListings = new();
            Dictionary<(Item, bool), int> inInventory = new();
            lock (EntrySummaries)
            {
                EntrySummaries.Clear();
                foreach (var entry in EntryListManager.Entries)
                {
                    if (entry.Complete) continue;
                    GetNumCraftsPossible(entry, intermediateListings, inInventory);
                    EntrySummaries.Add(EntryIngredients(entry, intermediateListings, inInventory));
                }
            }
        }

        public void DisplaySummaries()
        {
            lock (EntrySummaries)
            {
                if (ImGui.BeginTable("##IngredientSummary", 2, ImGuiTableFlags.None,
                    new Vector2(ImGui.GetContentRegionAvail().X * .98f, // scale to prevent it from leaving the border.
                            ImGui.GetFrameHeight() * (EntryListManager.Entries.Count + 1))))
                {
                    ImGui.TableSetupColumn("##IS-Check", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("##IS-Listing", ImGuiTableColumnFlags.WidthStretch);

                    
                    foreach (var summary in EntrySummaries)
                    {
                        if (summary.Entry.Complete)
                            continue;
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        if (!summary.CanCraft)
                            ImGuiAddons.IconTextColored(ImGuiColors.DPSRed, Dalamud.Interface.FontAwesomeIcon.ExclamationTriangle);
                        else
                            ImGuiAddons.IconTextColored(ImGuiColors.HealerGreen, Dalamud.Interface.FontAwesomeIcon.Check);
                        ImGui.SameLine();
                        ImGui.TableSetColumnIndex(1);
                        DisplayEntry(summary);
                    }


                    ImGui.EndTable();
                }

                if (ImGui.Checkbox("Include results as available ingredients in later crafts?", ref Service.Configuration.IncludeEntryResults)
                    || ImGui.Checkbox("Presume results are HQ?", ref Service.Configuration.PresumeEntryResultsHQ))
                {
                    Service.Configuration.Save();
                }
                

            }
        }

        public static List<EntryIngredientListing> IngredientsFromRecipe(Recipe recipe)
        {
            List<EntryIngredientListing> ingredientAmounts = new();

            var ingredients = recipe.Ingredient;


            for (int i = 0; i < ingredients.Count; i++)
            {
                var ingredient = ingredients[i];

                if (ingredient.Value.RowId <= 0)
                    continue;

                var newListing = new EntryIngredientListing
                {
                    Item = ingredient.Value,
                    NumUsed = recipe.AmountIngredient[i],
                    IsHQ = false,
                    NumAvailable = -1
                };


                ingredientAmounts.Add(newListing);
            }
            return ingredientAmounts;
        }

        public static List<EntryIngredientListing> GetBaseIngredientsFromEntry(CListEntry entry)
        {

            List<EntryIngredientListing> result = new();

            var recipe = entry.Recipe();
            var recipeIngredientList = IngredientsFromRecipe(recipe);

            for (int i = 0; i < recipeIngredientList.Count; i++)
            {
                var copy = recipeIngredientList[i];
                if (copy.Item.CanBeHq)
                {
                    if (entry.PrioHQMats)
                    {
                        result.Add(new()
                        {
                            Item = copy.Item,
                            NumUsed = copy.NumUsed,
                            IsHQ = true,
                            NumAvailable = -1,
                        });
                        result.Add(copy);
                        continue;
                    }
                    else {
                        EntryIngredientListing hqIngredientCopy = new()
                        {
                            Item = copy.Item,
                            NumUsed = entry.HQSelection[i] * Math.Max(CListEntry.GetCraftNum(entry.NumCrafts), 1),
                            IsHQ = true,
                            NumAvailable = -1
                        };

                        copy.NumUsed -= entry.HQSelection[i];

                        if (hqIngredientCopy.NumUsed != 0)
                            result.Add(hqIngredientCopy);

                    }

                }
                copy.NumUsed *= Math.Max(CListEntry.GetCraftNum(entry.NumCrafts), 1);

                if (copy.NumUsed != 0)
                    result.Add(copy);

            }
            return result;
        }

        public EntryIngredientSummary EntryIngredients(CListEntry entry, List<EntryIngredientListing> previouslyUsedIngredients, Dictionary<(Item, bool), int> itemsInInventory)
        {
            var entryIngredients = GetBaseIngredientsFromEntry(entry);
            var recipeIngredients = IngredientsFromRecipe(entry.Recipe());

            var nCrafts = Math.Min(GetNumCraftsPossible(entry, previouslyUsedIngredients, itemsInInventory),
                    GetNumItemThatCanFitInInventory((int) entry.Result().RowId, Service.Configuration.PresumeEntryResultsHQ));
            bool canCraft = nCrafts > 0;
            // PROBLEM: GetNumItemThatCanFitInInventory doesn't account for entry results
            foreach (var ingredient in entryIngredients)
            {

                EntryIngredientListing? prevUsedIngredient = previouslyUsedIngredients.Where(i => i.Item.RowId == ingredient.Item.RowId && i.IsHQ == ingredient.IsHQ).FirstOrDefault();
                int inInventory = itemsInInventory.TryGetValue((ingredient.Item, ingredient.IsHQ), out int var) ? var : SeInterface.GetItemCountInInventory(ingredient.Item.RowId, ingredient.IsHQ, false, false);
                int numPreviouslyUsed = prevUsedIngredient == null ? 0 : prevUsedIngredient.NumUsed;
                int numAvail = inInventory - numPreviouslyUsed;

                ingredient.NumAvailable = numAvail;
                if (entry.PrioHQMats)
                {
                    if (ingredient.IsHQ)
                    {
                        EntryIngredientListing nqIngredient = entryIngredients.Where(i => i.Item.RowId == ingredient.Item.RowId && i.IsHQ == false).First();
                        var totalUsed = nCrafts * ingredient.NumUsed;

                        ingredient.NumUsed = Math.Min(totalUsed, numAvail);
                        nqIngredient.NumUsed = Math.Max(0, totalUsed - ingredient.NumUsed);

                    }
                    else if (!ingredient.Item.CanBeHq && entry.NumCrafts.ToLower() == "max")
                    {
                        ingredient.NumUsed = Math.Max(ingredient.NumUsed, ingredient.NumUsed * nCrafts);

                    }
                }
                else if (entry.NumCrafts.ToLower() == "max")
                {
                    ingredient.NumUsed = Math.Max(ingredient.NumUsed, ingredient.NumUsed * nCrafts);
                }
                if (prevUsedIngredient == null)
                {
                    itemsInInventory.Add((ingredient.Item, ingredient.IsHQ), inInventory);

                    previouslyUsedIngredients.Add(new EntryIngredientListing
                    {
                        Item = ingredient.Item,
                        IsHQ = ingredient.IsHQ,
                        NumUsed = ingredient.NumUsed,
                        NumAvailable = -1,
                    });
                        
                }
                else
                {
                    prevUsedIngredient.NumUsed += ingredient.NumUsed;
                }

                if (ingredient.NumAvailable < ingredient.NumUsed)
                    canCraft = false;
                    
            }

            if (Service.Configuration.IncludeEntryResults && canCraft)
            {
                var matchingIngredients = previouslyUsedIngredients.Where(i => i.Item.RowId == entry.Result().RowId && i.IsHQ == Service.Configuration.PresumeEntryResultsHQ);
                if (matchingIngredients.Any())
                {
                    matchingIngredients.First().NumUsed -= nCrafts * entry.Recipe().AmountResult;
                }
                else
                {
                    previouslyUsedIngredients.Add(new EntryIngredientListing
                    {
                        Item = entry.Result(),
                        IsHQ = Service.Configuration.PresumeEntryResultsHQ,
                        NumUsed = -nCrafts * entry.Recipe().AmountResult,
                        NumAvailable = nCrafts * entry.Recipe().AmountResult
                    }) ;
                }
            }
            return new()
            {
                Entry = entry,
                Ingredients = entryIngredients,
                CanCraft = canCraft,
                NumCrafts = nCrafts,
            };
        }

        public static void DisplayEntry(EntryIngredientSummary entrySummary)
        {

            {
                var texture = ImGuiAddons.LookupIcon(entrySummary.Entry.Result().Icon);
                if (texture != null)
                {
                    ImGuiAddons.ScaledImageY(texture.ImGuiHandle, texture.Width, texture.Height, ImGui.GetFrameHeight());
                }
            }
            
            ImGui.SameLine();
            if (ImGui.TreeNodeEx($"##EntrySummary-{entrySummary.Entry.EntryId}", ImGuiTreeNodeFlags.CollapsingHeader, $"{entrySummary.Entry.Name}: {entrySummary.Entry.NumCrafts} {(entrySummary.Entry.NumCrafts.ToLower() == "max" ? $"({entrySummary.NumCrafts})" : "")}##-{entrySummary.Entry.EntryId}"))
            {
                if (ImGui.BeginTable($"##Ingredient-Summary-{entrySummary.Entry.EntryId}", 4, ImGuiTableFlags.BordersOuter
                    )) {

                    ImGui.TableSetupColumn($"##IS-Check-{entrySummary.Entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn($"Name##IS-Name-{entrySummary.Entry.EntryId}", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn($"Used##IS-Needed-{entrySummary.Entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn($" Available ##IS-Avail-{entrySummary.Entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);

                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();
                    foreach (var ingredient in entrySummary.Ingredients)
                    {
                        if (ingredient.NumUsed <= 0)
                            continue;
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        if (ingredient.NumUsed > ingredient.NumAvailable)
                            ImGuiAddons.IconTextColored(ImGuiColors.DPSRed, Dalamud.Interface.FontAwesomeIcon.ExclamationTriangle);
                        else
                            ImGuiAddons.IconTextColored(ImGuiColors.HealerGreen, Dalamud.Interface.FontAwesomeIcon.Check);

                        ImGui.TableSetColumnIndex(1);
                        {
                            var texture = ImGuiAddons.LookupIcon(ingredient.Item.Icon, ingredient.IsHQ, false);
                            if (texture != null)
                            {
                                ImGuiAddons.ScaledImageY(texture.ImGuiHandle, texture.Width, texture.Height, ImGui.GetFrameHeight());
                            }
                        }
                        ImGui.SameLine();
                        ImGui.Text($"{ingredient.Item.Name}{(ingredient.IsHQ ? "" : "")}");

                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text($"{ingredient.NumUsed}");

                        ImGui.TableSetColumnIndex(3);
                        ImGui.TextColored(ingredient.NumUsed > ingredient.NumAvailable ? ImGuiColors.DPSRed : ImGuiColors.HealerGreen, $"{ingredient.NumAvailable}");
                    }
                    ImGui.EndTable();
                }
                
            }
        }

        public static int GetNumCraftsPossible(CListEntry entry, List<EntryIngredientListing> previouslyUsedIngredients, Dictionary<(Item, bool), int> itemsInInventory)
        {
            if (entry.NumCrafts != "max")
                return CListEntry.GetCraftNum(entry.NumCrafts);

            var entryIngredients = GetBaseIngredientsFromEntry(entry);

            int leastNumCraftsPossible = int.MaxValue;


            foreach (var ingredient in entryIngredients)
            {
                EntryIngredientListing? prevUsedIngredient = previouslyUsedIngredients.Where(i => i.Item.RowId == ingredient.Item.RowId && i.IsHQ == ingredient.IsHQ).FirstOrDefault();
                int inInventory = itemsInInventory.TryGetValue((ingredient.Item, ingredient.IsHQ), out int var) ? var : SeInterface.GetItemCountInInventory(ingredient.Item.RowId, ingredient.IsHQ, false, false);
                int numPreviouslyUsed = prevUsedIngredient == null ? 0 : prevUsedIngredient.NumUsed;
                int numAvail = inInventory - numPreviouslyUsed;

                ingredient.NumAvailable = numAvail;
                if (entry.PrioHQMats)
                {
                    if (!ingredient.IsHQ)
                    {
                        int hqAvail = itemsInInventory.TryGetValue((ingredient.Item, true), out int nhq) ? nhq : SeInterface.GetItemCountInInventory(ingredient.Item.RowId, true, false, false);
                        numAvail += hqAvail;

                    }
                    else
                    {
                        continue;
                    }
                }
                var nCrafts = numAvail / ingredient.NumUsed;
                if (nCrafts < leastNumCraftsPossible)
                    leastNumCraftsPossible = nCrafts;

            }
            return leastNumCraftsPossible;
        }

        public unsafe static int GetNumItemThatCanFitInInventory(int itemId, bool hq)
        {
            if (itemId <= 0) return 0;

            InventoryContainer*[] bags = {
                InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1),
                InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2),
                InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3),
                InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4),
            };

            int numFreeSlots = 0;
            int quantityHeld = 0;
            for (int bag = 0; bag < 4; bag++)
            {
                for (int i = 0; i < bags[bag]->Size; i++)
                {
                    if (bags[bag]->Items[i].ItemId == 0)
                    {
                        numFreeSlots++;
                    }
                    if (bags[bag]->Items[i].ItemId == itemId && ((bags[bag]->Items[i].Flags & InventoryItem.ItemFlags.HighQuality) != 0) == hq)
                    {
                        quantityHeld += (int)bags[bag]->Items[i].Quantity;
                    }
                }
            }

            var item = Service.Items[itemId];
            int numInFreeSlots = (int)(numFreeSlots * item.StackSize);
            int numFreeInExistingStacks = (int)((quantityHeld % item.StackSize == 0) ? 0 : item.StackSize - (quantityHeld % item.StackSize));

            return numInFreeSlots + numFreeInExistingStacks;
        }
    }
}
