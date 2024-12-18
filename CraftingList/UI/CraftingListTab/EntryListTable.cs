﻿using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule;

namespace CraftingList.UI.CraftingListTab
{
    public class EntryListTable
    {
        public IngredientSummary IngredientSummary = new();
        public List<TimeSpan> EntryTimeEstimations = new();
        public TimeSpan EstimatedTime;

        readonly private List<(int, Recipe)> filteredRecipes = new();
        readonly private List<(int, Recipe)> filteredFavorites = new();

        private CListEntry? draggedEntry = null;
        private readonly HashSet<int> entriesToRemove = new(); // We can't remove entries while iterating over them, so we add their id's to a set and remove all of them
                                                      // after iterating.
        readonly private CListEntry newEntry = new(-1, "1", "", false, CListEntry.EmptyHQSelection());
        private List<CListEntry> importEntries = new();

        private bool ShowNewEntryAsterisks = false;
        string recipeSearch = "";

        public Crafter crafter;

        public void Dispose()
        {
            IngredientSummary.Dispose();
        }

        public EntryListTable(Crafter crafter)
        {
            this.crafter = crafter;


            IngredientSummary.Update();
            EstimateTime();
            EntryListManager.ReassignIds();
            newEntry.EntryId = -1;
            FilterRecipes("");
        }

        public void DrawEntries(List<CListEntry> entries, string tableName)
        {
            ImGuiAddons.BeginGroupPanel($"Items", new Vector2(0, 0));

            if (ImGui.BeginTable($"##EntryList-{tableName}", 5, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg,
                new Vector2(ImGui.GetContentRegionAvail().X * .98f, // scale to prevent it from leaving the border.
                            ImGui.GetFrameHeight() * (entries.Count + 1))))
            {
                ImGui.TableSetupColumn($"Item##EntryList-{tableName}-Item", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"Amount##EntryList-{tableName}-Amount", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"Macro##EntryList-{tableName}-Macro", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"Duration##EntryList-{tableName}-Duration", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"##EntryList-{tableName}-Delete", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();

                int timeEstimation = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry.RecipeId < 0) continue;

                    ImGui.TableSetColumnIndex(0);
                    {
                        var icon = ImGuiAddons.LookupIcon(entry.Result().Icon);
                         if (icon != null)
                        {
                            ImGuiAddons.ScaledImageY(icon.ImGuiHandle, icon.Width, icon.Height, ImGui.GetFrameHeight());
                            ImGui.SameLine();
                        }
                    }

                    var expanded = ImGui.TreeNodeEx($"##{tableName}-Entry-Treenode-{entry.EntryId}", ImGuiTreeNodeFlags.None,
                        $"{entry.Result().Name}");
                    DragDropEntry(entry);


                    if (expanded)
                    {
                        ImGui.PushItemWidth(-1);
                        DrawEntry(entry);
                        ImGui.PopItemWidth();
                        ImGui.TreePop();
                    }

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(entry.NumCrafts);

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(entry.MacroName);

                    ImGui.TableSetColumnIndex(3);
                    lock (IngredientSummary.EntrySummaries)
                    {
                        if (entry.EntryId >= 0 && entry.EntryId < EntryTimeEstimations.Count)
                            ImGui.Text(FormatTime(EntryTimeEstimations[timeEstimation++]));
                    }

                    ImGui.TableSetColumnIndex(4);
                    if (ImGuiAddons.IconButton(FontAwesomeIcon.TrashAlt, "Remove Entry", $"{tableName}-{entry.Name}-{entry.EntryId}"))
                    {
                        entriesToRemove.Add(entry.EntryId);

                        continue;
                    }
                    
                    ImGui.TableNextRow();

                }
            }
            ImGui.EndTable();
            
            ImGuiAddons.EndGroupPanel();

            RemoveFlaggedEntries();
            
            if (crafter.CraftUpdateEvent)
            {
                if (!crafter.IsRunning())
                    //this.IngredientSummary.Resume();
                crafter.CraftUpdateEvent = false;
                IngredientSummary.Update();
                EstimateTime();
            }
        }

        public void DrawEntry(CListEntry entry)
        {
            if (ShowNewEntryAsterisks && entry.RecipeId < 0)
            {
                ImGui.TextColored(ImGuiColors.DPSRed, "*");
                ImGui.SameLine();
            }
            if (RecipeSelectionBox(entry))
            {

            }

            if(ShowNewEntryAsterisks && entry.MacroName == "")
            {
                ImGui.TextColored(ImGuiColors.DPSRed, "*");
                ImGui.SameLine();
            }
            if (MacroSelectionBox(entry))
            {
                
            }

            if (ShowNewEntryAsterisks && !CListEntry.IsValidNumCrafts(entry.NumCrafts))
            {
                ImGui.TextColored(ImGuiColors.DPSRed, "*");
                ImGui.SameLine();
            }
            ImGui.Text("Number of crafts: ");
            ImGui.SameLine();
            var numCrafts = entry.NumCrafts;
            ImGui.SetNextItemWidth(Math.Max(ImGui.CalcTextSize(" max ").X, ImGui.CalcTextSize(numCrafts).X) + 20);

            if (ImGui.InputText($"##NumCrafts-{entry.EntryId}", ref numCrafts, 50)
                && CListEntry.IsValidNumCrafts(numCrafts))
            {
                entry.NumCrafts = numCrafts;
                IngredientSummary.Update();
                EstimateTime();
                Service.Configuration.Save();
            }
            ImGuiAddons.TextTooltip("Number of crafts. Enter \"max\" to craft until you run out of materials or inventory space.");

            if (entry.MacroName != "<Quick Synth>") {
                ImGui.Checkbox($"Prioritize HQ ingredients?##-{entry.EntryId}", ref entry.PrioHQMats);
                if (!entry.PrioHQMats && ImGui.CollapsingHeader($"Specify HQ ingredients##{entry.EntryId}"))
                {
                    DrawIngredientsForEntry(entry);
                }
            }
        }

        public void DrawImportWindow()
        {
            if (ImGui.BeginPopup("ImportWindow"))
            {
                DrawEntries(importEntries, "ImportTable");

                ImGui.EndPopup();
            }
        }

        public void DrawNewEntry()
        {
            if (ImGui.BeginPopup("New Entry"))
            {
                ImGuiAddons.BeginGroupPanel("New Entry", new Vector2(-1, 0));

                ImGui.PushItemWidth(500);
                DrawEntry(newEntry);
                ImGui.PopItemWidth();


                ImGuiAddons.EndGroupPanel();

                if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add Entry", "NewEntry"))
                {
                    if (newEntry.RecipeId >= 0 &&
                        CListEntry.IsValidNumCrafts(newEntry.NumCrafts)
                        && (MacroManager.MacroNames.Contains(newEntry.MacroName) || newEntry.MacroName == "<Quick Synth>"))
                    {
                        newEntry.NumCrafts = newEntry.NumCrafts.ToLower();

                        EntryListManager.AddEntry(new CListEntry(newEntry.RecipeId, newEntry.NumCrafts, newEntry.MacroName, newEntry.PrioHQMats, newEntry.HQSelection));

                        IngredientSummary.Update();
                        EstimateTime();

                        if (Service.Configuration.RecentRecipeIds.Count > 10)
                            Service.Configuration.RecentRecipeIds.RemoveAt(Service.Configuration.RecentRecipeIds.Count - 1);

                        if (Service.Configuration.RecentRecipeIds.Contains(newEntry.RecipeId))
                            Service.Configuration.RecentRecipeIds.RemoveAll(x => x == newEntry.RecipeId);

                        Service.Configuration.RecentRecipeIds.Insert(0, newEntry.RecipeId);

                        newEntry.NumCrafts = "1";
                        newEntry.RecipeId = -1;
                        newEntry.Name = "";
                        newEntry.PrioHQMats = false;
                        newEntry.HQSelection = CListEntry.EmptyHQSelection();

                        Service.Configuration.Save();


                        ImGui.CloseCurrentPopup();

                        ShowNewEntryAsterisks = false;

                    }
                    else
                    {
                        Service.PluginLog.Error("New Entry is invalid.");
                        ShowNewEntryAsterisks = true;
                    }
                }

                ImGui.EndPopup();
            }
        }

        public void DrawIngredientsForEntry(CListEntry entry)
        {
            if (entry.RecipeId < 0)
            {
                ImGui.Text("Select an item to specify HQ ingredients.");
                return;
            }
            var recipe = entry.Recipe();

            var recipeIngredients = IngredientSummary.IngredientsFromRecipe(recipe);
            var maxString = GetLongest(recipeIngredients.Select(i => i.Item.Name.ToString()));
            bool hasHqItem = false;
            for (int i = 0; i < recipe.Ingredient.Count; i++)
            {
                if (recipe.Ingredient[i].Value.RowId <= 0)
                    continue;

                var item = recipe.Ingredient[i].Value;

                if (!item.CanBeHq)
                    continue;
                else
                    hasHqItem = true;

                {
                    var texture = ImGuiAddons.LookupIcon(item.Icon, true);
                    if (texture != null)
                    {
                        ImGuiAddons.ScaledImageY(texture.ImGuiHandle, texture.Width, texture.Height, ImGui.GetTextLineHeight());
                        ImGui.SameLine();
                    }
                }
                //if (Service.IconCache.TryGetIcon(item.Icon, true, out TextureWrap? texture))
                {
                    
                }

                if (HQMat(item.Name.ToString(),
                    recipe.AmountIngredient[i],
                    ref entry.HQSelection[i],
                    ImGui.CalcTextSize(maxString).X + 40))
                {
                    IngredientSummary.Update();
                }

            }

            if (!hasHqItem)
            {
                ImGui.Text("No HQ-able ingredients");
            }
        }
        public bool MacroSelectionBox(CListEntry entry)
        {
            if (ImGui.BeginCombo($"##macro-list-{entry.EntryId}", entry.MacroName.IsNullOrEmpty() ? "Select a macro..." : entry.MacroName, ImGuiComboFlags.HeightLargest))
            {
                if (entry.RecipeId >= 0 && entry.Recipe().CanQuickSynth)
                {
                    if (ImGui.Selectable($"<Quick Synth>##-{entry.EntryId}"))
                    {
                        EstimateTime();
                        entry.MacroName = "<Quick Synth>";
                        Service.Configuration.Save();
                    }
                }

                for (int i = 0; i < MacroManager.MacroNames.Count; i++)
                {

                    if (ImGui.Selectable($"{MacroManager.MacroNames[i]}##-{i}"))
                    {
                        EstimateTime();
                        entry.MacroName = MacroManager.MacroNames[i];
                        Service.Configuration.Save();
                    }
                }
                ImGui.EndCombo();
                return true;
            }
            return false;
        }
        public bool RecipeSelectionBox(CListEntry entry)
        {
            if (ImGui.BeginCombo($"##recipe-list-{entry.EntryId}", entry.RecipeId > -1 ? entry.Result().Name.ToString() ?? "???" : "Select a recipe...", ImGuiComboFlags.HeightLargest))
            {
                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                if (ImGui.BeginTabBar($"##Recipe-Select-Tabs-{entry.EntryId}")) {

                    bool recipesTab = ImGui.BeginTabItem($"Recipes##-{entry.EntryId}");
                    if (recipesTab)
                        ImGui.EndTabItem();

                    bool favoritesTab = ImGui.BeginTabItem($"Favorites##-{entry.EntryId}");
                    if (favoritesTab)
                        ImGui.EndTabItem();

                    if (ImGui.InputTextWithHint($"##recipe-search-{entry.EntryId}", "Search...", ref recipeSearch, 512U, ImGuiInputTextFlags.AutoSelectAll))
                        this.recipeSearch = recipeSearch.Trim().Replace("", string.Empty);
                    

                    if (recipesTab)
                    {
                        FilterRecipes(recipeSearch);
                        RecipeSelectionTable(entry, filteredRecipes);
                    }
                    if (favoritesTab)
                    {
                        FilterFavorites(recipeSearch);
                        RecipeSelectionTable(entry, filteredFavorites);
                    }

                }
                ImGui.EndTabBar();

                
                ImGui.EndCombo();

                return true;
            }

            return false;
        }

        public void RecipeSelectionTable(CListEntry entry, List<(int, Recipe)> recipes)
        {
            if (ImGui.BeginChild($"##recipe-list-child-{entry.EntryId}", new Vector2(0.0f, 250f) * ImGuiHelpers.GlobalScale, false, ImGuiWindowFlags.NoScrollbar)
                    && ImGui.BeginTable($"##recipe-list-table-{entry.EntryId}", 4, ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn($"##icon-{entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"Job##job-{entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"rLvl##rlvl-{entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"Name##name-{entry.EntryId}", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                ImGuiListClipperPtr guiListClipperPtr = ImGuiAddons.Clipper(recipes.Count);
                while (guiListClipperPtr.Step())
                {
                    for (int i = guiListClipperPtr.DisplayStart; i < guiListClipperPtr.DisplayEnd; i++)
                    {
                        ImGui.TableNextRow();
                        (int recipeNum, Recipe recipe) = recipes[i];
                        string jobStr = Service.Jobs[(int)recipe.CraftType.RowId + 8].Abbreviation.ToString();

                        ImGui.TableSetColumnIndex(0);
                        Vector2 cursorPos = ImGui.GetCursorPos();
                        if (ImGui.Selectable($"##recipe-{entry.EntryId}-{recipe.RowId}", recipeNum == entry.RecipeId, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            if (recipeNum != entry.RecipeId)
                                entry.HQSelection = CListEntry.EmptyHQSelection();

                            entry.RecipeId = recipeNum;
                            IngredientSummary.Update();
                            Service.Configuration.Save();
                            ImGui.CloseCurrentPopup();
                        }
                        if (!Service.Configuration.FavoriteRecipeIDs.Contains(recipeNum) && ImGui.BeginPopupContextItem($"##recipe-context-{i}"))
                        {
                            if (ImGuiAddons.IconButton(FontAwesomeIcon.Star, "Add to Favorites", $"##favorite-add-button-{i}"))
                            {
                                Service.Configuration.FavoriteRecipeIDs.Add(recipeNum);
                                Service.Configuration.Save();

                                FilterFavorites(recipeSearch);
                                ImGui.CloseCurrentPopup();
                            }
                            ImGui.EndPopup();
                        }
                        else if (ImGui.BeginPopupContextItem($"##recipe-context-{i}"))
                        {
                            if (ImGuiAddons.IconButton(FontAwesomeIcon.Trash, "Remove from Favorites", $"##favorite-remove-button-{i}"))
                            {
                                Service.Configuration.FavoriteRecipeIDs.Remove(recipeNum);
                                Service.Configuration.Save();

                                ImGui.CloseCurrentPopup();
                            }

                            ImGui.EndPopup();
                        }
                        
                        
                        {
                            var texture = ImGuiAddons.LookupIcon(recipe.ItemResult.Value!.Icon);
                            if (texture != null)
                            {
                                ImGui.SetCursorPos(cursorPos);
                                ImGuiAddons.ScaledImageY(texture.ImGuiHandle, texture.Width, texture.Height, ImGui.GetTextLineHeight());
                            }
                        }

                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text(jobStr);
                        ImGui.TableSetColumnIndex(2);
                        ImGui.Text($"{recipe.RecipeLevelTable.RowId}");
                        ImGui.TableSetColumnIndex(3);
                        ImGui.Text(recipe.ItemResult.Value!.Name.ToString());
                    }
                }
                ImGui.EndTable();

            }

            ImGui.EndChild();
        }
        public static void RemoveMacroName(string macroName)
        {

            foreach (var entry in Service.Configuration.EntryList)
            {
                if (entry.MacroName == macroName)
                    entry.MacroName = "";

            }
        }

        public static string GetLongest(IEnumerable<string> strings)
        {
            string max = "";
            foreach (var str in strings)
            {
                if (str.Length > max.Length)
                    max = str;
            }

            return max;
        }

        public bool HQMat(string name, int amount, ref int outInt, float size)
        {
            bool ret = false;
            ImGui.Text(name + ": ");
            ImGui.SameLine();
            //ImGui.SetCursorPosX(size);
            ImGui.SetNextItemWidth(25);
            if (ImGui.InputInt($"/{amount}##ingredient_{name}", ref outInt, 0))
            {
                ret = true;
                Service.Configuration.Save();
            }
            ImGui.SameLine();

            if (ImGui.Button($"-##hq-{name}", new Vector2(22, 22)))
            {
                outInt--;
                ret = true;
                IngredientSummary.Update();
                Service.Configuration.Save();

            }
            ImGui.SameLine();
            if (ImGui.Button($"+##hq-{name}", new Vector2(22, 22)))
            {
                outInt++;
                ret = true;
                IngredientSummary.Update();
                Service.Configuration.Save();

            }

            if (outInt > amount)
            {
                outInt = amount;
            }
            if (outInt < 0)
            {
                outInt = 0;
            }
            return ret;
        }

        public void EstimateTime()
        {
            int estimatedTime = 0;
            EntryTimeEstimations.Clear();
            lock (IngredientSummary.EntrySummaries)
            {
                int summaryIndex = 0;
                for (int i = 0; i < EntryListManager.Entries.Count; i++)
                {
                    if (EntryListManager.Entries[i].Complete)
                    {
                        i++;
                        continue;
                    }
                    var entryTime = TimeEstimation.EstimateEntryDurationMS(EntryListManager.Entries[i], IngredientSummary.EntrySummaries[summaryIndex]);
                    EntryTimeEstimations.Add(TimeSpan.FromMilliseconds(entryTime));
                    estimatedTime += entryTime;
                    summaryIndex++;
                }
            }
            EstimatedTime = TimeSpan.FromMilliseconds(estimatedTime);
        }

        public static void UpdateMacroNameInEntries(string oldName, string newName)
        {
            foreach (var entry in Service.Configuration.EntryList)
            {
                if (entry.MacroName == oldName)
                    entry.MacroName = newName;

            }
        }

        public void RemoveFlaggedEntries()
        {
            if (Service.Configuration.EntryList.RemoveAll(e => entriesToRemove.Contains(e.EntryId)) > 0)
            {
                EntryListManager.ReassignIds();
                IngredientSummary.Update();
                EstimateTime();
                Service.Configuration.Save();
            }
            entriesToRemove.Clear();
        }

        public static string FormatTime(TimeSpan time)
        {
            bool hasHours = time.Hours > 0;
            bool hasMinutes = time.Minutes > 0;
            bool hasSeconds = time.Seconds > 0;
            return $"~{(hasHours ? $"{time.Hours}h" : "")}{(hasHours && hasMinutes ? ", " : "")}" +
                $"{(hasMinutes ? $"{time.Minutes}m" : "")}{(hasMinutes && hasSeconds ? ", " : "")}" +
                $"{(hasSeconds ? $"{time.Seconds}s" : "")}";
        }

        private void DragDropEntry(CListEntry entry)
        {
            if (ImGui.BeginDragDropSource())
            {
                this.draggedEntry = entry;
                ImGui.Text(entry.Name);
                ImGui.SetDragDropPayload("CraftingMacroPayload", IntPtr.Zero, 0);
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("CraftingMacroPayload");

                bool nullPtr;
                unsafe
                {
                    nullPtr = payload.NativePtr == null;
                }

                if (!nullPtr && payload.IsDelivery() && draggedEntry != null)
                {
                    var copy = EntryListManager.Entries[draggedEntry.EntryId];
                    EntryListManager.Entries.RemoveAt(draggedEntry.EntryId);
                    EntryListManager.Entries.Insert(entry.EntryId, copy);
                    EntryListManager.ReassignIds();
                    Service.Configuration.Save();
                }

                ImGui.EndDragDropTarget();
            }

        }

        public static void RegenerateEntryIds(List<CListEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                EntryListManager.Entries[i].EntryId = i;
            }
        }
        private void FilterRecipes(string searchStr)
        {
            filteredRecipes.Clear();
            var words = searchStr.ToLowerInvariant().Split(' ');

            /*
            if (str.IsNullOrEmpty())
            {
                for (int i = 0; i < Service.Recipes.Count; i++)
                {
                    filteredRecipes.Add((i, Service.Recipes[i]));
                }
                return;
            }
            */
            for (int i = Service.Configuration.RecentRecipeIds.Count - 1; i >= 0; i--)
            {
                var recipeId = Service.Configuration.RecentRecipeIds[i];
                var recipeName = Service.Recipes[recipeId].ItemResult.Value!.Name.ToString().ToLowerInvariant();
                var isMatch = true;
                foreach (var word in words)
                {
                    if (!recipeName.Contains(word))
                        isMatch = false;

                }

                if (isMatch) {
                    filteredRecipes.Add((recipeId, Service.Recipes[recipeId]));
                }
            }

            for (int i = 0; i < Service.Recipes.Count; i++)
            {
                var recipeName = Service.Recipes[i].ItemResult.Value!.Name.ToString().ToLowerInvariant();
                var isMatch = true;
                foreach (var word in words)
                {
                    if (!recipeName.Contains(word))
                        isMatch = false;
                }
                if (isMatch
                    && !Service.Configuration.RecentRecipeIds.Contains(i))
                {

                    filteredRecipes.Add((i, Service.Recipes[i]));

                }
            }
        }

        private void FilterFavorites(string str)
        {
            filteredFavorites.Clear();

            str = str.ToLowerInvariant();

            for (int i = 0; i < Service.Recipes.Count; i++)
            {
                if (Service.Recipes[i].ItemResult.Value!.Name.ToString().ToLowerInvariant().Contains(str)
                    && Service.Configuration.FavoriteRecipeIDs.Contains(i))
                {
                    filteredFavorites.Add((i, Service.Recipes[i]));

                }
            }
        }
    }
}
