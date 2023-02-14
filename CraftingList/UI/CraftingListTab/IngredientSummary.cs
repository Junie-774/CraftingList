﻿using CraftingList.Crafting;
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
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Havok;
using System.Reflection;
using ImGuiScene;
using System.Timers;
using Dalamud.Logging;

namespace CraftingList.UI.CraftingListTab
{

    public class IngredientSummaryListing
    {
        public uint ItemId { get; set; }
        public string Name { get; set; } = "";
        public int NumCraftsNeeded { get; set; } // -1 represents "max"
        public bool HasMax { get; set; } // Used for aggregate listings for more than one entry. Allows keeping track of the amount needed by other entries
        public bool CanBeHQ { get; set; }
        public bool NeedsHQ { get; set; }
        public int PerCraft { get; set; }
        public int InInventory { get; set; }
    }

    public class IngredientSummary : IDisposable
    {
        public IEnumerable<IngredientSummaryListing> Ingredients { get; set; } = new List<IngredientSummaryListing>();

        public List<List<IngredientSummaryListing>> IntermediateListings = new();

        public bool ExpectHQResults = false;
        readonly Timer updateTimer = new(1000);

        public IngredientSummary()
        {
            updateTimer.Elapsed += (sender, e) => UpdateIngredients();
            updateTimer.Start();
        }

        public void Dispose()
        {
            updateTimer.Dispose();
        }

        public void UpdateIngredients()
        {
            var result = GetIngredientListFromEntryList(Service.Configuration.EntryList);
            Ingredients = result.Item1.OrderBy(i => i.ItemId).ThenBy(i => i.Name);
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

                if (Service.IconCache.TryGetIcon(Service.Recipes[entry.RecipeId].ItemResult.Value!.Icon, ExpectHQResults, out TextureWrap? icon))
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

        public static void DisplayListing(IngredientSummaryListing ingredient)
        {
            int inInventory = ingredient.InInventory;// + SeInterface.GetItemCountInInevntory(ingredient.ItemId, true);
            int amountNeeded = ingredient.PerCraft * Math.Max(0, ingredient.NumCraftsNeeded); // if the ingredient is only needed by a 'max' craft,
            if (inInventory >= amountNeeded && (!ingredient.HasMax || (inInventory > ingredient.PerCraft)))
            {
                ImGuiAddons.IconTextColored(ImGuiColors.HealerGreen, FontAwesomeIcon.Check);
                ImGui.SameLine();
            }
            else
            {
                ImGui.Dummy(new Vector2(17, 0));
                ImGui.SameLine();
            }
            ImGui.Text($"{ingredient.Name}: {(amountNeeded != 0 ? amountNeeded : string.Empty)}{(ingredient.HasMax ? (amountNeeded != 0 ? " + " : "") + "max" : "")}");
            ImGui.SameLine();
            Vector4 color = ImGuiColors.DalamudGrey;

            if (inInventory < amountNeeded || (ingredient.HasMax && (inInventory < ingredient.PerCraft)))
                color = ImGuiColors.DalamudRed;

            //PluginLog.Debug($"{ingredient.Name} | Amount: {ingredient.AmountNeeded} | PerCraft: {ingredient.PerCraft} | NumCrafts: {ingredient.NumCraftsNeeded}");
            ImGui.TextColored(color, $" ({inInventory} in inventory, {inInventory / ingredient.PerCraft} crafts) ");

        }

        public static int GetNumCraftsPossible(CListEntry entry, List<IngredientSummaryListing> previouslyUsedMats)
        {
            //PluginLog.Debug($"prev mats count: {previouslyUsedMats.Count}");
            if (entry.NumCrafts != "max")
                return CListEntry.GetCraftNum(entry.NumCrafts);

            var entryIngredients = GetIngredientListFromEntry(entry);
            var rawIngData = Service.Recipes[entry.RecipeId].UnkData5.Select(i => i.ItemIngredient).ToList();

            int leastNumCraftsPossible = int.MaxValue;


            foreach (var ingredient in entryIngredients)
            {
                //PluginLog.Debug($"{ingredient.Name} | PerCraft: {ingredient.PerCraft} | NumCrafts: {ingredient.NumCraftsNeeded}");
                var matchingListings = previouslyUsedMats.Where(l => l.Name == ingredient.Name);
                int previouslyUsedAmount = 0;
                if (matchingListings.Any())
                {

                    previouslyUsedAmount = matchingListings.First().PerCraft * matchingListings.First().NumCraftsNeeded;
                }

                var numUsedPerCraft = ingredient.PerCraft;
                var numInInventory = ingredient.InInventory;
                var numAvailable = Math.Max(numInInventory - previouslyUsedAmount, 0);
                var numCraftsPossible = numAvailable / numUsedPerCraft;
                //PluginLog.Debug($"Available: {numAvailable}");
                if (numCraftsPossible < leastNumCraftsPossible)
                    leastNumCraftsPossible = numCraftsPossible;

            }

            return leastNumCraftsPossible;
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
                    NumCraftsNeeded = 0,
                    CanBeHQ = ingredientItem.CanBeHq,
                    PerCraft = ingredient.AmountIngredient
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
                copy.InInventory = SeInterface.GetItemCountInInventory(copy.ItemId, false);
                copy.NumCraftsNeeded = CListEntry.GetCraftNum(entry.NumCrafts);

                if (copy.NumCraftsNeeded == -1)
                    copy.HasMax = true;


                if (entryIngredientList[i].CanBeHQ)
                {
                    IngredientSummaryListing hqIngredientCopy = new()
                    {
                        Name = copy.Name,
                        ItemId = copy.ItemId,
                        NumCraftsNeeded = copy.NumCraftsNeeded,
                        HasMax = copy.HasMax,
                        CanBeHQ = copy.CanBeHQ,
                        PerCraft = copy.PerCraft,
                    };
                    hqIngredientCopy.Name = "" + hqIngredientCopy.Name;
                    hqIngredientCopy.NeedsHQ = true;
                    hqIngredientCopy.PerCraft = entry.HQSelection[i];
                    hqIngredientCopy.InInventory = SeInterface.GetItemCountInInventory(hqIngredientCopy.ItemId, true);

                    if (hqIngredientCopy.NumCraftsNeeded != 0)
                        AddToList(result, hqIngredientCopy);

                    copy.PerCraft -= entry.HQSelection[i];

                }

                if (copy.NumCraftsNeeded != 0)
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

                var entryIngredients = GetIngredientListFromEntry(entry);

                if (entry.NumCrafts.ToLower() == "max")
                {
                    intermediateListings.Add(result.ToList());
                }
                AddListToList(result, entryIngredients);
            }
            return (result, intermediateListings);
        }

        private static void AddToList(List<IngredientSummaryListing> list, IngredientSummaryListing listing)
        {
            if (listing.NumCraftsNeeded == 0 || listing.PerCraft <= 0)
            {
                return;
            }
            var matches = list.Where(l => l.Name == listing.Name);
            if (matches.Any())
            {
                matches.First().NumCraftsNeeded += Math.Max(listing.NumCraftsNeeded, 0);
                matches.First().HasMax |= listing.HasMax;
            }
            else 
                list.Add(listing);
        }

        private static void AddListToList(List<IngredientSummaryListing> baseList, List<IngredientSummaryListing> toAdd)
        {
            for (int i = 0; i < toAdd.Count; i++)
            {
                AddToList(baseList, toAdd[i]);
            }
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
