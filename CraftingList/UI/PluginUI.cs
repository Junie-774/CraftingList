using CraftingList.Crafting;
using CraftingList.UI;
using CraftingList.Utility;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace CraftingList
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    unsafe class PluginUI : IDisposable
    {
        private List<ITab> Tabs;
        private readonly CraftingList plugin;



        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public PluginUI(CraftingList plugin, Configuration configuration)
        {
            this.Tabs = new List<ITab>()
            {
                new CraftingListTab(plugin),
                new MacroTab(plugin),
                new OptionsTab(plugin),
            };
            this.plugin = plugin;            
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawSettingsWindow();
        }
                    
        public void DrawCraftingList()
        {
            Tabs[0].Draw();

        }
        
        public void DrawExperimentalTab()
        {
            ImGui.Text("Wait durations (ms)");
            ImGui.PushItemWidth(ImGui.CalcTextSize("0000000").X);

            object box = plugin.Configuration.WaitDurations;
            foreach (var field in typeof(WaitDurationHelper).GetFields())
            {
                int toref = (int)(field.GetValue(box) ?? 2000);
                if (ImGui.InputInt(field.Name, ref toref, 0))
                {
                    field.SetValue(box, toref);
                    plugin.Configuration.WaitDurations = (WaitDurationHelper)box;
                }
            }
            ImGui.PopItemWidth();
        }
        public void DrawSettingsWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(500f, 375f), new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            if (ImGui.Begin("Crafting List", ref this.visible,
                 ImGuiWindowFlags.None))
            {
                ImGui.BeginTabBar("##ConfigTab");
                foreach (var tab in Tabs)
                {
                    if (ImGui.BeginTabItem(tab.Name))
                    {
                        tab.Draw();
                        ImGui.EndTabItem();
                    }
                }
                if (ImGui.BeginTabItem("Experimental"))
                {
                    DrawExperimentalTab();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();

            }
            plugin.Configuration.Save();
            ImGui.End();
        }

        public void OnConfigChange()
        {
            foreach (var tab in Tabs)
            {
                tab.OnConfigChange();
            }
        }


    }
}
