using CraftingList.Crafting;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.ContextMenus;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using Lumina.Excel.GeneratedSheets;
using System.Text;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using System.Threading;

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

        private Hook<AddonContextReceiveEventDelegate>? ReceiveHook;
        public CraftingList(DalamudPluginInterface pluginInterface)
        {
            DalamudApi.Initialize(pluginInterface);
            InitializeSingletons();

            SeInterface = new SeInterface();

            Crafter = new Crafter(SeInterface);

            this.Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(DalamudApi.PluginInterface, Crafter);

            this.PluginUi = new PluginUI(this.Configuration);

            DalamudApi.CommandManager.AddHandler("/craftinglist", new CommandInfo(OnCraftingList)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            DalamudApi.CommandManager.AddHandler("/craftallitems", new CommandInfo(OnCraftAllItems)
            {
                HelpMessage = "Craft all items in your list"
            });
            DalamudApi.CommandManager.AddHandler("/command", new CommandInfo(OnCommand)
            {
                HelpMessage = "Command"
            });

            DalamudApi.PluginInterface.UiBuilder.Draw += DrawUI;
            DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Module.Initialize();
            SignatureHelper.Initialise(this);

            ReceiveHook = Singleton<AddonContextMenuReceiveEvent>.Get().CreateHook(AddonReceiveEventDetour);
        }

        void AddonReceiveEventDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused) {
            PluginLog.Information($"YesNo Addon Event: unit:{atkUnit:x}, type:{eventType}, which:{which}, source:{source:X16}, {unused:X16}");
            }

        public void Dispose()
        {

            this.PluginUi.Dispose();
            DalamudApi.CommandManager.RemoveHandler("/craftinglist");
            DalamudApi.CommandManager.RemoveHandler("/craftallitems");
            DalamudApi.CommandManager.RemoveHandler("/command");
            ReceiveHook?.Disable();
            ReceiveHook?.Dispose();

        }
        private void OnCommand(string command, string args)
        {
            string[] argArray = args.Split(' ');
            PluginLog.Information($"{Crafter.NeedToChangeFood(uint.Parse(argArray[0]), uint.Parse(argArray[1])).Result}");
        }
        private void OnCraftingList(string command, string args)
        {

            this.PluginUi.Visible = true;
        }

        private void OnCraftAllItems(string command, string args)
        {
            Crafter.CraftAllItems();
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
