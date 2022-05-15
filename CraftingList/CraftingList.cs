using CraftingList.Crafting;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;

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
            DalamudApi.CommandManager.AddHandler("/command", new CommandInfo(OnCommand)
            {
                ShowInHelp = false
            });



            DalamudApi.PluginInterface.UiBuilder.Draw += DrawUI;
            DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.SeInterface.Dispose();
            this.PluginUi.Dispose();
            DalamudApi.CommandManager.RemoveHandler("/craftinglist");
            DalamudApi.CommandManager.RemoveHandler("/craftallitems");
            DalamudApi.CommandManager.RemoveHandler("/clist");
            DalamudApi.CommandManager.RemoveHandler("/clcancel");
            DalamudApi.CommandManager.RemoveHandler("/command");
        }

        private void OnCommand(string command, string args)
        {
            if (args.Split(' ').Length == 2)
            {
                PluginLog.Information($"Food: {Crafter.NeedToChangeFood(uint.Parse(args.Split(' ')[0]), uint.Parse(args.Split(' ')[1])).Result}");
            }
            /*
            PluginLog.Debug($"Recipe note: {(IntPtr) SeInterface.RecipeNote().Pointer:X}");
            PluginLog.Debug($"Base: {(IntPtr) SeInterface.RecipeNote().Pointer->AtkUnitBase.Name}");
            PluginLog.Debug($"Pointer {*(long*)SeInterface.RecipeNote().Pointer + 0x2}");*/
            /*
            for (long offset = 0x220; offset < 0x790; offset += 0x8)
            {
                PluginLog.Debug($"Offset {offset:X}: {*((ulong*) SeInterface.RecipeNote().Pointer + offset):X}");
            }
            PluginLog.Debug($"Unk260: {(IntPtr) SeInterface.RecipeNote().Pointer->Unk390:X}");
            SeInterface.RecipeNote().Pointer->Unk258->SetScale(1.2f, 1.2f);
            PluginLog.Debug($"Children: {SeInterface.RecipeNote().Pointer->Unk258->ChildCount}");*/
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
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
