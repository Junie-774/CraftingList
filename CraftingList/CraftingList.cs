using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace CraftingList
{


    public unsafe sealed class CraftingList : IDalamudPlugin
    {
        public string Name => "Crafting List";

        internal Configuration Configuration { get; init; }
        internal PluginUI PluginUi { get; init; }

        internal Crafter Crafter { get; init; }

        public static void InitializeSingletons()
        {
            Singleton<OpenRecipebyRecipeId>.Set(DalamudApi.SigScanner);
            Singleton<GetBaseUiObject>.Set(DalamudApi.SigScanner);
            Singleton<GetUiObjectByName>.Set(DalamudApi.SigScanner);
            Singleton<OpenRecipeByItemId>.Set(DalamudApi.SigScanner);
            Singleton<UseAction>.Set(DalamudApi.SigScanner);
            Singleton<OpenContextMenuForAddon>.Set(DalamudApi.SigScanner);
            Singleton<AgentRepairReceiveEvent>.Set(DalamudApi.SigScanner);
            Singleton<AgentRecipeNoteReceiveEvent>.Set(DalamudApi.SigScanner);
            Singleton<AddonRepairReceiveEvent>.Set(DalamudApi.SigScanner);
            Singleton<AddonRecipeNoteReceiveEvent>.Set(DalamudApi.SigScanner);
            Singleton<AgentRecipeNoteHide>.Set(DalamudApi.SigScanner);
            Singleton<AgentRecipeMaterialListReceiveEvent>.Set(DalamudApi.SigScanner);
        }

        public CraftingList(DalamudPluginInterface pluginInterface)
        {
            Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(pluginInterface);

            DalamudApi.Initialize(pluginInterface, Configuration);
            Module.Initialize();
            SignatureHelper.Initialise(this);

            InitializeSingletons();
            MacroManager.InitializeMacros();


            Crafter = new Crafter(SeInterface.Instance, Configuration);


            this.PluginUi = new PluginUI(this);


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
            DalamudApi.CommandManager.AddHandler("/testcommand", new CommandInfo(OnCommand)
            {
                ShowInHelp = false
            });
            DalamudApi.CommandManager.AddHandler("/closerecipenote", new CommandInfo(OnCloseRecipeNote)
            {
                ShowInHelp = false
            });
        
            DalamudApi.PluginInterface.UiBuilder.Draw += DrawUI;
            //DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            SeInterface.Dispose();
            PluginUi.Dispose();
            DalamudApi.CommandManager.RemoveHandler("/craftinglist");
            DalamudApi.CommandManager.RemoveHandler("/craftallitems");
            DalamudApi.CommandManager.RemoveHandler("/clist");
            DalamudApi.CommandManager.RemoveHandler("/clcancel");
            DalamudApi.CommandManager.RemoveHandler("/testcommand");
            DalamudApi.CommandManager.RemoveHandler("/closerecipenote");

            //DalamudApi.CommandManager.RemoveHandler("/clconfig");
        }

        private void OnCommand(string command, string args)
        {
            DalamudApi.ChatGui.Print(args);
            //int button = int.Parse(args);
            //((PtrSynthesisSimpleDialog)SeInterface.GetUiObject("SynthesisSimpleDialog")).ClickButton(button);
        }

        private void OnCraftingList(string command, string args)
        {

            this.PluginUi.Visible = true;
        }

        private void OnCraftAllItems(string command, string args)
        {
            Crafter.CraftAllItems();
        }

        private void OnCancel(string command, string args)
        {
            Crafter.Cancel("Cancelling craft...", false);
        }

        private void OnOpenConfig(string command, string args)
        {
            DrawConfigUI();
        }

        //I could not ever explain why, but closing the recipe note directly
        //crashes the game in some circumstances, but directing a call through 
        //the macro system doesn't.
        //I dunno.
        private void OnCloseRecipeNote(string command, string args)
        {
            SeInterface.RecipeNote().Close();
        }
        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
