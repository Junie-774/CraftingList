using CraftingList.Crafting;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Game.Command;
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
        internal SeInterface SeInterface { get; init; }

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
        }

        public CraftingList(DalamudPluginInterface pluginInterface)
        {
            DalamudApi.Initialize(pluginInterface);
            Module.Initialize();
            SignatureHelper.Initialise(this);
            InitializeSingletons();

            SeInterface = new SeInterface();


            Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(DalamudApi.PluginInterface);
            Crafter = new Crafter(SeInterface, Configuration);


            this.PluginUi = new PluginUI(this, Configuration);


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
            DalamudApi.CommandManager.AddHandler("/addentry", new CommandInfo(OnCommand)
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
            DalamudApi.CommandManager.RemoveHandler("/addentry");
            //DalamudApi.CommandManager.RemoveHandler("/clconfig");
        }

        private void OnCommand(string command, string args)
        {
            var argsArray = args.Split(' ');
            uint itemId = 0;
            if (!uint.TryParse(argsArray[0]!, out itemId)) {
                DalamudApi.ChatGui.PrintError("[CraftingList] Invalid arg.");
            }
            string numCrafts = argsArray[1]!;
            int macroIdx = 0;
            if (!int.TryParse(argsArray[2]!, out macroIdx))
            {
                DalamudApi.ChatGui.PrintError("[CraftingList] Invalid arg.");
            }
            CListEntry newEntry = new CListEntry("Added Entry", itemId, numCrafts, macroIdx);
            for (int i = 0; i < 6 && i + 3 < argsArray.Length; i++)
            {
                int nHQ;
                if (!int.TryParse(argsArray[i + 3]!, out nHQ))
                {
                    DalamudApi.ChatGui.PrintError("[CraftingList] Invalid arg.");
                }
                newEntry.HQSelection[i] = nHQ;
                PluginLog.Debug(nHQ.ToString());
                foreach (var hq in newEntry.HQSelection)
                {
                    PluginLog.Debug($"- {hq}");
                }

            }
            Configuration.EntryList.Add(newEntry);
            foreach (var hq in Configuration.EntryList[0].HQSelection)
            {
                PluginLog.Debug($"- {hq}");
            }
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
            Crafter.Cancel("Cancelling craft...", false);
        }

        private void OnOpenConfig(string command, string args)
        {
            DrawConfigUI();
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
