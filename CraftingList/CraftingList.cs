using CraftingList.Crafting;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using System;

namespace CraftingList
{


    public unsafe sealed class CraftingList : IDalamudPlugin
    {
        public string Name => "Crafting List";

        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private SeInterface SeInterface { get; init; }

        private Crafter Crafter { get; init; }

        public void InitializeSingletons()
        {
            Singleton<OpenRecipebyRecipeId>.Set(DalamudApi.SigScanner);
            Singleton<GetBaseUiObject>.Set(DalamudApi.SigScanner);
            Singleton<GetUiObjectByName>.Set(DalamudApi.SigScanner);
            Singleton<OpenRecipeByItemId>.Set(DalamudApi.SigScanner);
            Singleton<UseAction>.Set(DalamudApi.SigScanner);
            Singleton<OpenContextMenuForAddon>.Set(DalamudApi.SigScanner);
            Singleton<AgentRepairReceiveEvent>.Set(DalamudApi.SigScanner);
            Singleton<AddonRepairReceiveEvent>.Set(DalamudApi.SigScanner);
            Singleton<AddonContextMenuReceiveEvent>.Set(DalamudApi.SigScanner);
            Singleton<AgentRecipeNoteHide>.Set(DalamudApi.SigScanner);
        }

        public CraftingList(DalamudPluginInterface pluginInterface)
        {
            DalamudApi.Initialize(pluginInterface);
            Module.Initialize();
            SignatureHelper.Initialise(this);
            InitializeSingletons();

            SeInterface = new SeInterface();

            Crafter = new Crafter(SeInterface);

            this.Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(DalamudApi.PluginInterface, Crafter);

            this.PluginUi = new PluginUI(this.Configuration);


            DalamudApi.CommandManager.AddHandler("/craftinglist", new CommandInfo(OnCraftingList)
            {
                HelpMessage = "Create a list of items to craft"
            });
            DalamudApi.CommandManager.AddHandler("/clist", new CommandInfo(OnCraftingList)
            {
                HelpMessage = "Abbreviation for /craftinglist"
            });

            DalamudApi.CommandManager.AddHandler("/craftallitems", new CommandInfo(OnCraftAllItems)
            {
                HelpMessage = "Craft all items in your list"
            });

            DalamudApi.CommandManager.AddHandler("/clcancel", new CommandInfo(OnCancel)
            {
                HelpMessage = "Cancel current craft."
            });



            DalamudApi.PluginInterface.UiBuilder.Draw += DrawUI;
            DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;



        }

        public void Dispose()
        {

            this.PluginUi.Dispose();
            DalamudApi.CommandManager.RemoveHandler("/craftinglist");
            DalamudApi.CommandManager.RemoveHandler("/craftallitems");
            DalamudApi.CommandManager.RemoveHandler("/clist");
            DalamudApi.CommandManager.RemoveHandler("/clcancel");
        }


        private void OnCraftingList(string command, string args)
        {
            if (args != "" && int.Parse(args) == 0)
            {
                SeInterface.RecipeNote().Close();
            }
            else
            {
                this.PluginUi.Visible = true;
            }
        }

        private void OnCraftAllItems(string command, string args)
        {

            Crafter.CraftAllItems();
        }

        private void OnCancel(string command, string args)
        {
            Crafter.Cancel();
        }
        private void DrawUI()
        {
            try
            {
                this.PluginUi.Draw();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex.ToString());
            }
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
