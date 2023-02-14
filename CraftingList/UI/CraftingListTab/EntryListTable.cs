using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.Utility;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CraftingList.UI.CraftingListTab
{
    public class EntryListTable
    {
        readonly private IEnumerable<Recipe?> craftableItems;
        readonly private List<string> craftableNames;
        public IngredientSummary IngredientSummary = new();
        public TimeEstimation TimeEstimator;

        public TimeSpan EstimatedTime;
        readonly private List<(int, Recipe)> filteredRecipes = new();

        private HashSet<int> entriesToRemove = new(); // We can't remove entries while iterating over them, so we add their id's to a set and remove all of them
                                                      // after iterating.

        readonly private CListEntry newEntry = new(-1, "", "", false, CListEntry.EmptyHQSelection());

        private Timer updateIngredientsTimer = new(500);

        string recipeSearch = "";

        public Crafter crafter;

        public EntryListTable(Crafter crafter)
        {
            this.TimeEstimator = new(IngredientSummary);
            this.crafter = crafter;
            craftableNames = new List<string>
            {
                ""
            };

            craftableItems = Service.DataManager.GetExcelSheet<Recipe>()!
                .Select(r => r).Where(r => r != null && r.ItemResult.Value != null && r.ItemResult.Value.Name != "");

            foreach (var item in craftableItems)
            {
                craftableNames.Add(item!.ItemResult.Value!.Name);
            }

            for (int index = 0; index < Service.Recipes.Count; index++)
            {
                filteredRecipes.Add((index, Service.Recipes[index]));
            }

            IngredientSummary.UpdateIngredients();
            EntryListManager.ReassignIds();
            newEntry.EntryId = -1;
        }

        public void DrawEntries()
        {
            ImGuiAddons.BeginGroupPanel("Crafting List", new Vector2(-1, -1));

            if (ImGui.BeginTable("##EntryList", 4, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg,
                new Vector2(ImGui.GetContentRegionAvail().X * .98f, // scale to prevent it from leaving the border.
                            ImGui.GetFrameHeight() * (EntryListManager.Entries.Count + 1))))
            {
                ImGui.TableSetupColumn($"Item##EntryList-Item", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"Amount##EntryList-Amount", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"Macro##EntryList-Macro", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn($"##EntryList-Delete", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();
                foreach (var entry in EntryListManager.Entries)
                {
                    if (entry.RecipeId < 0) continue;

                    ImGui.TableSetColumnIndex(0);
                    if (Service.IconCache.TryGetIcon(Service.Recipes[entry.RecipeId].ItemResult.Value!.Icon, false, out TextureWrap? icon))
                    {
                        ImGuiAddons.ScaledImageY(icon.ImGuiHandle, icon.Width, icon.Height, ImGui.GetFrameHeight());
                        ImGui.SameLine();
                    }

                    var expanded = ImGui.TreeNodeEx($"##treenode-{entry.EntryId}", ImGuiTreeNodeFlags.None,
                        $"{Service.Recipes[entry.RecipeId].ItemResult.Value!.Name}");


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

                    if (ImGuiAddons.IconButton(FontAwesomeIcon.TrashAlt, "Remove Entry", $"{entry.Name}-{entry.EntryId}"))
                    {
                        entriesToRemove.Add(entry.EntryId);

                        continue;
                    }
                    

                    ImGui.TableNextRow();

                }
            }
            ImGui.EndTable();
            if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add a new entry"))
            {
                ImGui.SetNextWindowSize(new Vector2(400, 0));

                ImGui.OpenPopup("New Entry");
            }
            ImGuiAddons.EndGroupPanel();

            RemoveFlaggedEntries();

            if (crafter.CraftUpdateEvent.WaitOne(0))
            {
                crafter.CraftUpdateEvent.Reset();
                IngredientSummary.UpdateIngredients();
                EstimateTime();
            }
        }

        public void DrawEntry(CListEntry entry)
        {
            if (RecipeSelectionBox(entry))
            {
                
            }

            if (MacroSelectionBox(entry))
            {
                
            }

            ImGui.Text("Number of crafts: ");
            ImGui.SameLine();
            var numCrafts = entry.NumCrafts;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("max").X + ImGui.CalcTextSize(numCrafts).X + 40);

            if (ImGui.InputText($"##NumCrafts-{entry.EntryId}", ref numCrafts, 50)
                && CListEntry.IsValidNumCrafts(numCrafts))
            {
                entry.NumCrafts = numCrafts;
                IngredientSummary.UpdateIngredients();
                EstimateTime();
                Service.Configuration.Save();
            }
            ImGuiAddons.TextTooltip("Number of crafts. Enter \"max\" to craft until you run out of materials or inventory space.");

            if (ImGui.CollapsingHeader($"Specify HQ ingredients##{entry.EntryId}"))
            {
                DrawIngredientsForEntry(entry);
            }

            ImGui.NewLine();
        }

        public void DrawNewEntry()
        {
            if (ImGui.BeginPopup("New Entry"))
            {
                ImGuiAddons.BeginGroupPanel("New Entry", new Vector2(-1, 0));

                ImGui.PushItemWidth(500);
                DrawEntry(newEntry);
                ImGui.PopItemWidth();

                if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add Entry", "NewEntry"))
                {
                    if (newEntry.RecipeId >= 0 &&
                        (newEntry.NumCrafts.ToLower() == "max" || int.TryParse(newEntry.NumCrafts, out _) && int.Parse(newEntry.NumCrafts) > 0)
                        && MacroManager.MacroNames.Contains(newEntry.MacroName))
                    {
                        newEntry.NumCrafts = newEntry.NumCrafts.ToLower();

                        EntryListManager.AddEntry(new CListEntry(newEntry.RecipeId, newEntry.NumCrafts, newEntry.MacroName, newEntry.SpecifiedHQ, newEntry.HQSelection));

                        IngredientSummary.UpdateIngredients();
                        EstimateTime();

                        newEntry.MacroName = "";
                        newEntry.NumCrafts = "";
                        newEntry.RecipeId = -1;
                        newEntry.HQSelection = CListEntry.EmptyHQSelection();
                        Service.Configuration.Save();
                        ImGui.CloseCurrentPopup();

                    }
                    else
                    {
                        PluginLog.Debug("BAD!");
                    }
                }

                ImGuiAddons.EndGroupPanel();
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
            var recipe = Service.Recipes[entry.RecipeId];

            var recipeIngredients = IngredientSummary.GetIngredientListFromRecipe(recipe);
            var maxString = GetLongest(recipeIngredients.Select(i => i.Name));
            bool hasHqItem = false;
            for (int i = 0; i < recipe.UnkData5.Length; i++)
            {
                //PluginLog.Debug($"{i}: {recipe.UnkData5[i].ItemIngredient}, {Service.Items[recipe.UnkData5[i].ItemIngredient].Name}");
                if (recipe.UnkData5[i].ItemIngredient <= 0)
                    continue;

                var item = Service.Items[recipe.UnkData5[i].ItemIngredient];

                if (!item.CanBeHq)
                    continue;
                else
                    hasHqItem = true;

                if (Service.IconCache.TryGetIcon(item.Icon, true, out TextureWrap? texture))
                {
                    ImGuiAddons.ScaledImageY(texture.ImGuiHandle, texture.Width, texture.Height, ImGui.GetTextLineHeight());
                    ImGui.SameLine();
                }

                if (HQMat(item.Name,
                    recipe.UnkData5[i].AmountIngredient,
                    ref entry.HQSelection[i],
                    ImGui.CalcTextSize(maxString).X + 40))
                {
                    IngredientSummary.UpdateIngredients();
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

                for (int i = 0; i < MacroManager.MacroNames.Count; i++)
                {
                    if (ImGui.Selectable($"{MacroManager.MacroNames[i]}##-{i}"))
                    {
                        EstimateTime();
                        Service.Configuration.Save();
                        entry.MacroName = MacroManager.MacroNames[i];
                    }
                }
                ImGui.EndCombo();
                return true;
            }
            return false;
        }
        public bool RecipeSelectionBox(CListEntry entry)
        {
            if (ImGui.BeginCombo($"##recipe-list-{entry.EntryId}", entry.RecipeId > -1 ? Service.Recipes[(int)entry.RecipeId].ItemResult.Value?.Name.RawString ?? "???" : "Select a recipe...", ImGuiComboFlags.HeightLargest))
            {
                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                if (ImGui.InputTextWithHint($"##recipe-search-{entry.EntryId}", "Search...", ref recipeSearch, 512U, ImGuiInputTextFlags.AutoSelectAll))
                {
                    this.recipeSearch = recipeSearch.Trim();
                    FilterRecipes(recipeSearch);
                }
                if (ImGui.BeginChild($"##recipe-list-child-{entry.EntryId}", new Vector2(0.0f, 250f) * ImGuiHelpers.GlobalScale, false, ImGuiWindowFlags.NoScrollbar)
                    && ImGui.BeginTable($"##recipe-list-table-{entry.EntryId}", 4, ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn($"##icon-{entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn($"Job##job-{entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn($"rLvl##rlvl-{entry.EntryId}", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn($"Name##name-{entry.EntryId}", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    ImGuiListClipperPtr guiListClipperPtr = ImGuiAddons.Clipper(filteredRecipes.Count);
                    while (guiListClipperPtr.Step())
                    {
                        for (int i = guiListClipperPtr.DisplayStart; i < guiListClipperPtr.DisplayEnd; i++)
                        {
                            ImGui.TableNextRow();
                            (int recipeNum, Recipe recipe) = filteredRecipes[i];
                            string jobStr = Service.Jobs[(int) recipe.CraftType.Row + 8].Abbreviation;

                            ImGui.TableSetColumnIndex(0);
                            Vector2 cursorPos = ImGui.GetCursorPos();
                            if (ImGui.Selectable($"##recipe-{entry.EntryId}-{recipe.RowId}", recipeNum == entry.RecipeId, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                if (recipeNum != entry.RecipeId)
                                    entry.HQSelection = CListEntry.EmptyHQSelection();

                                entry.RecipeId = recipeNum;
                                IngredientSummary.UpdateIngredients();
                                Service.Configuration.Save();
                                ImGui.CloseCurrentPopup();
                            }
                            TextureWrap? texture;
                            if (Service.IconCache.TryGetIcon(recipe.ItemResult.Value!.Icon, false, out texture))
                            {
                                ImGui.SetCursorPos(cursorPos);
                                ImGuiAddons.ScaledImageY(texture.ImGuiHandle, texture.Width, texture.Height, ImGui.GetTextLineHeight());
                            }

                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(jobStr);
                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text($"{recipe.RecipeLevelTable.Row}");
                            ImGui.TableSetColumnIndex(3);
                            ImGui.Text(recipe.ItemResult.Value!.Name.RawString);
                        }
                    }
                    ImGui.EndTable();

                }
                ImGui.EndChild();
                ImGui.EndCombo();
                return true;
            }

            return false;
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
            ImGui.Text(name + ": ");
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
                IngredientSummary.UpdateIngredients();
                EstimateTime();
                Service.Configuration.Save();

            }
            ImGui.SameLine();

            if (ImGui.Button($"+##hq-{name}", new Vector2(22, 22)))
            {
                outInt++;
                ret = true;
                IngredientSummary.UpdateIngredients();
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
            int j = 0;
            for (int i = 0; i < EntryListManager.Entries.Count; i++)
            {
                
                estimatedTime += TimeEstimator.EstimateEntryDurationMS(EntryListManager.Entries[i],
                    IngredientSummary.IntermediateListings.Count > j ? IngredientSummary.IntermediateListings[j] : new List<IngredientSummaryListing>());

                if (EntryListManager.Entries[i].NumCrafts.ToLower() == "max")
                    j++;
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
                IngredientSummary.UpdateIngredients();
                EstimateTime();
                Service.Configuration.Save();
            }
            entriesToRemove.Clear();
        }

        private void FilterRecipes(string str)
        {
            filteredRecipes.Clear();

            if (str.IsNullOrEmpty())
            {
                for (int i = 0; i < Service.Recipes.Count; i++)
                {
                    filteredRecipes.Add((i, Service.Recipes[i]));
                }
                return;
            }

            str = str.ToLowerInvariant();
            for (int i = 0; i < Service.Recipes.Count; i++)
            {
                if (Service.Recipes[i].ItemResult.Value!.Name.RawString.ToLowerInvariant().Contains(str))
                {
                    filteredRecipes.Add((i, Service.Recipes[i]));
                }
            }
        }
    }
}
