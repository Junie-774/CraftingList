using CraftingList.Crafting;
using CraftingList.Utility;
using Dalamud.Interface.Colors;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Havok;
using System.Reflection;
using ImGuiScene;

namespace CraftingList.UI.CraftingListTab
{

    public class IngredientSummaryListing
    {
        public uint ItemId { get; set; }
        public string Name { get; set; } = "";
        public int Amount { get; set; }
        public bool HasMax { get; set; } // Used for aggregate listings for more than one entry. Allows keeping track of the amount needed by other entries
        public bool CanBeHQ { get; set; }
        public bool NeedsHQ { get; set; }
    }

    public class IngredientSummary
    {
        public IEnumerable<IngredientSummaryListing> Ingredients { get; set; } = new List<IngredientSummaryListing>();

        public List<List<IngredientSummaryListing>> IntermediateListings = new();

        private bool ExpectHQResults = false;

        public void UpdateIngredients()
        {
            var result = GetIngredientListFromEntryList(Service.Configuration.EntryList);
            Ingredients = result.Item1.OrderBy(i => i.ItemId);
            IntermediateListings = result.Item2;
        }

        public void DisplayListings()
        {
            foreach (var ingredient in Ingredients)
            {
                DisplayListing(ingredient);
            }
            ImGui.NewLine();


            ImGuiAddons.BeginGroupPanel("\"max\" entry expected yields", new Vector2(-1, 0));
            ImGui.Checkbox("Expect that results will be HQ if applicable?", ref ExpectHQResults);
            ImGui.NewLine();
            int j = 0;
            foreach (var entry in EntryListManager.Entries)
            {
                if (entry.NumCrafts != "max")
                    continue;

                TextureWrap? icon;
                if (Service.IconCache.TryGetIcon(Service.Recipes[entry.RecipeId].ItemResult.Value!.Icon, ExpectHQResults, out icon))
                {
                    ImGuiAddons.ScaledImageY(icon.ImGuiHandle, icon.Width, icon.Height, ImGui.GetFrameHeight());
                    ImGui.SameLine();
                }
                ImGui.Text($"{Service.Recipes[entry.RecipeId].ItemResult.Value!.Name}: ");
                ImGui.SameLine();

                var numCraftsAvailalable = GetNumCraftsPossible(entry, IntermediateListings[j]);
                var numSpacesForItem = GetNumItemThatCanFitInInventory((int) Service.Recipes[entry.RecipeId].ItemResult.Value!.RowId, ExpectHQResults);
                ImGui.Text(" " + Math.Min(numCraftsAvailalable, numSpacesForItem).ToString());


                j++;
            }
            ImGuiAddons.EndGroupPanel();
        }
        
        public static int GetNumCraftsPossible(CListEntry entry, List<IngredientSummaryListing> previouslyUsedMats)
        {
            if (entry.NumCrafts != "max")
                return CListEntry.GetCraftNum(entry.NumCrafts);

            var entryIngredients = GetIngredientListFromEntry(entry);
            var rawIngData = Service.Recipes[entry.RecipeId].UnkData5.Select(i => i.ItemIngredient).ToList();

            int leastNumCraftsPossible = int.MaxValue;


            foreach (var ingredient in entryIngredients)
            {
                var matchingListings = previouslyUsedMats.Where(l => l.Name == ingredient.Name);
                int previouslyUsedAmount = 0;
                if (matchingListings.Any())
                {
                    previouslyUsedAmount = matchingListings.First().Amount;
                }

                var numUsedPerCraft = ingredient.Amount;
                var numInInventory = SeInterface.GetItemCountInInventory(ingredient.ItemId, ingredient.NeedsHQ, false, false, 0);
                var numAvailable = Math.Max(numInInventory - previouslyUsedAmount, 0);
                var numCraftsPossible = numAvailable / numUsedPerCraft;

                if (numCraftsPossible < leastNumCraftsPossible)
                    leastNumCraftsPossible = numCraftsPossible;

            }

            return leastNumCraftsPossible;
        }
        public static void DisplayListing(IngredientSummaryListing ingredient)
        {
            int inInventory = SeInterface.GetItemCountInInventory(ingredient.ItemId, ingredient.NeedsHQ);// + SeInterface.GetItemCountInInevntory(ingredient.ItemId, true);
            if (inInventory >= ingredient.Amount)
            {
                ImGuiAddons.IconTextColored(ImGuiColors.HealerGreen, FontAwesomeIcon.Check);
                ImGui.SameLine();
            }
            else
            {
                ImGui.Dummy(new Vector2(17, 0));
                ImGui.SameLine();
            }
            ImGui.Text($"{ingredient.Name}: {ingredient.Amount}{(ingredient.HasMax ? " + max" : "")}");
            ImGui.SameLine();
            Vector4 color = ImGuiColors.DalamudGrey;

            if (inInventory < ingredient.Amount)
                color = ImGuiColors.DalamudRed;

            ImGui.TextColored(color, $" ({inInventory} in inventory) ");

            
        }

        
        public static List<IngredientSummaryListing> GetIngredientListFromRecipe(Recipe recipe)
        {

            var ingredients = recipe.UnkData5;

            List<IngredientSummaryListing> ingredientAmounts = new();


            for (int i = 0; i < ingredients.Length; i++)
            {
                var ingredient = ingredients[i];

                if (ingredient.ItemIngredient <= 0)
                    continue;

                var ingredientItem = Service.Items[ingredient.ItemIngredient];

                ingredientAmounts.Add(new IngredientSummaryListing
                {
                    ItemId = (uint)ingredient.ItemIngredient,
                    Name = ingredientItem.Name,
                    Amount = ingredient.AmountIngredient,
                    CanBeHQ = ingredientItem.CanBeHq
                });
            }

            return ingredientAmounts;
        }

        public static List<IngredientSummaryListing> GetIngredientListFromEntry(CListEntry entry)
        {
            List<IngredientSummaryListing> result = new();

            var recipe = Service.Recipes[entry.RecipeId];
            var entryIngredientList = GetIngredientListFromRecipe(recipe);

            for (int i = 0; i < entryIngredientList.Count; i++)
            {
                var copy = entryIngredientList[i];

                if (entryIngredientList[i].CanBeHQ)
                {
                    IngredientSummaryListing hqIngredientCopy = new()
                    {
                        ItemId = copy.ItemId,
                        Name = copy.Name,
                        Amount = copy.Amount,
                        HasMax = copy.HasMax,
                        CanBeHQ = copy.CanBeHQ,
                        NeedsHQ = copy.NeedsHQ,
                    };
                    hqIngredientCopy.Name = "" + hqIngredientCopy.Name;
                    hqIngredientCopy.NeedsHQ = true;
                    hqIngredientCopy.Amount = entry.HQSelection[i] * ((entry.NumCrafts.ToLower() == "max") ? 1 : CListEntry.GetCraftNum(entry.NumCrafts));

                    if (hqIngredientCopy.Amount > 0)
                        AddToList(result, hqIngredientCopy);

                    copy.Amount -= entry.HQSelection[i];

                }
                copy.Amount *= (entry.NumCrafts.ToLower() == "max") ? 1 : CListEntry.GetCraftNum(entry.NumCrafts);

                if (copy.Amount > 0)
                    AddToList(result, copy);

            }

            return result;
        }

        public static (List<IngredientSummaryListing>, List<List<IngredientSummaryListing>>) GetIngredientListFromEntryList(IEnumerable<CListEntry> list)
        {
            List<IngredientSummaryListing> result = new();
            List<List<IngredientSummaryListing>> intermediateListings = new();
            

            foreach (var entry in list)
            {

                var recipe = Service.Recipes[entry.RecipeId];
                var entryIngredientList = GetIngredientListFromRecipe(recipe);
                if (entry.NumCrafts.ToLower() == "max")
                {
                    intermediateListings.Add(result.ToList());
                    continue;
                }
                for (int i = 0; i < entryIngredientList.Count; i++)
                {
                    var copy = entryIngredientList[i];

                    if (entryIngredientList[i].CanBeHQ)
                    {
                        IngredientSummaryListing hqIngredientCopy = new()
                        {
                            ItemId = copy.ItemId,
                            Name = copy.Name,
                            Amount = copy.Amount,
                            HasMax = copy.HasMax,
                            CanBeHQ = copy.CanBeHQ,
                            NeedsHQ = copy.NeedsHQ,
                        };
                        hqIngredientCopy.Name = "" + hqIngredientCopy.Name;
                        hqIngredientCopy.NeedsHQ = true;
                        hqIngredientCopy.Amount = entry.HQSelection[i] * CListEntry.GetCraftNum(entry.NumCrafts);

                        if (hqIngredientCopy.Amount > 0)
                            AddToList(result, hqIngredientCopy);

                        copy.Amount -= entry.HQSelection[i];

                    }
                    copy.Amount *= CListEntry.GetCraftNum(entry.NumCrafts);

                    if (copy.Amount > 0)
                        AddToList(result, copy);

                }
 

            }
            return (result, intermediateListings);
        }

        private static void AddToDict(Dictionary<string, IngredientSummaryListing> dict, IngredientSummaryListing listing)
        {
            if (dict.TryGetValue(listing.Name, out IngredientSummaryListing? result))
            {
                result!.Amount = listing.Amount;
                result.HasMax |= listing.HasMax;
            }
            else
            {
                dict.Add(listing.Name, listing);
            }
        }

        private static void AddToList(List<IngredientSummaryListing> list, IngredientSummaryListing listing)
        {
            var matches = list.Where(l => l.Name == listing.Name);
            if (matches.Any())
            {
                matches.First().Amount += listing.Amount;
                matches.First().HasMax |= listing.HasMax;
            }
            else
                list.Add(listing);
        }

        public static unsafe int GetNumInventorySlotsFree()
        {
            var bag1 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1);
            var bag2 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2);
            var bag3 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3);
            var bag4 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4);

            int numFree = 0;

            for (int i = 0; i < bag1->Size; i++)
            {
                if (bag1->Items[i].ItemID == 0)
                    numFree++;
            }
            for (int i = 0; i < bag2->Size; i++)
            {

                if (bag2->Items[i].ItemID == 0)
                    numFree++;
            }

            for (int i = 0; i < bag3->Size; i++)
            {
                if (bag3->Items[i].ItemID == 0)
                    numFree++;
            }
            for (int i = 0; i < bag4->Size; i++)
            {
                if (bag4->Items[i].ItemID == 0)
                    numFree++;
            }

            return numFree;
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
                    if (bags[bag]->Items[i].ItemID == 0)
                    {
                        numFreeSlots++;
                    }
                    if (bags[bag]->Items[i].ItemID == itemId && ((bags[bag]->Items[i].Flags & InventoryItem.ItemFlags.HQ) != 0) == hq)
                    {
                        quantityHeld += (int) bags[bag]->Items[i].Quantity;
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
