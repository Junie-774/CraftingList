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
            if (!DalamudApi.Configuration.AcknowledgedMacroChange)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(550f, 500f), new Vector2(550f, 500f));
                if (ImGui.Begin("Crafting List Update!!!", ref this.visible, ImGuiWindowFlags.NoResize))
                {
                    ImGui.SetWindowFontScale(1.5f);
                    ImGui.Text("[CraftingList] New Macro system!");
                    ImGui.SetWindowFontScale(1f);

                    ImGui.Text("CraftingList has a new Macro system! This plugin now features in-house macros, with");
                    ImGui.Text("the text saved in the plugin config, freeing up sapce on your macro page.");
                    ImGui.NewLine();
                    ImGui.Text("These new macros are unlimited in length. You can copy+paste them from teamcraft, but");
                    ImGui.Text("You don't have to worry about their durations anymore. They also support ignoring");
                    ImGui.Text("the <wait.X> modifiers, and moving on to the next step as soon as it's ready.");
                    ImGui.NewLine();
                    ImGui.Text("You won't be able to use the old macros, sorry. I know it's a breaking change, but");
                    ImGui.Text("it's a better macro system, and I'd rather get the migration over with quickly.");
                    ImGui.Text("I've tried to make it as painless as possible by adding an import button that will");
                    ImGui.Text("automatically re-create your old macros in the new format. Hopefully you should be");
                    ImGui.Text("able to just press the button and be done with the transition. It can be found under");
                    ImGui.Text("the Options tab.");
                    ImGui.Text("I'll leave the old macro data there to import for about a month, so import before then.");

                    ImGui.NewLine();
                    ImGui.Text("Press the button below to make this message go away. There's a button in the options tab");
                    ImGui.Text("to make this message re-appear.");

                    if (ImGui.Button("ACKNOWLEDGE"))
                    {
                        DalamudApi.Configuration.AcknowledgedMacroChange = true;
                    }
                }
            }
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(500f, 400f), new Vector2(float.PositiveInfinity, float.PositiveInfinity));
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
