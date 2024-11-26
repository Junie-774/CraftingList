using CraftingList.Crafting.Macro;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CraftingList.Utility
{
    internal class Service
    {
        public static void Initialize(IDalamudPluginInterface pluginInterface, Configuration config)
        {
            pluginInterface.Create<Service>();
            Configuration = config;

            CraftingConsumables = DataManager.GetExcelSheet<Item>()!
                .Where(item => item.ItemAction.Value!.DataHQ[1] != 0 && DataManager.GetExcelSheet<ItemFood>()!.Select(m => m.RowId).Contains(item.ItemAction.Value.DataHQ[1]))
                .Where(meal =>
                {
                    int param = (int)Service.DataManager.GetExcelSheet<ItemFood>()!
                        .GetRow(meal.ItemAction.Value!.DataHQ[1])!.Params.First().BaseParam.Value.RowId;
                    return param == 11 || param == 70 || param == 71;
                });

            GameEventManager = new();
            ChatManager = new(ChatGui);

            Recipes = DataManager.GetExcelSheet<Recipe>()!.Where(r => r.RowId != 0 && r.ItemResult.RowId > 0).ToList();
            Jobs = DataManager.GetExcelSheet<ClassJob>()!.ToList();
            Items = DataManager.GetExcelSheet<Item>()!.ToList();
        }

        public static void init2(Configuration config)
        {
            
        }

        public static Configuration Configuration { get; set; } = null!;

        [PluginService]
        public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

        [PluginService]
        public static ISigScanner SigScanner { get; private set; } = null!;

        [PluginService]
        private static IChatGui ChatGui { get; set; } = null!;

        [PluginService]
        public static ICommandManager CommandManager { get; private set; } = null!;

        [PluginService]
        public static IDataManager DataManager { get; private set; } = null!;

        [PluginService]
        public static IClientState ClientState { get; private set; } = null!;

        [PluginService]
        public static IFramework Framework { get; private set; } = null!;

        [PluginService]
        public static ICondition Condition { get; private set; } = null!;

        [PluginService]
        public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService]
        public static IPluginLog PluginLog { get; private set; } = null!;

        [PluginService]
        public static ITextureProvider TextureProvider { get; private set; } = null!;
        public static List<Recipe> Recipes { get; private set; } = null!;

        public static List<ClassJob> Jobs { get; private set; } = null!;

        public static List<Item> Items { get; private set; } = null!;

        public static IEnumerable<Item> CraftingConsumables { get; private set; } = null!;


        public static GameEventManager GameEventManager { get; private set; } = null!;
        public static ChatManager ChatManager { get; private set; } = null!;

        public static async Task<bool> WaitForCondition(ConditionFlag condition, bool value, int timeoutMS)
        {
            var startTime = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var timeout = (ulong)timeoutMS;
            var endTime = startTime + timeout;

            while (Condition[condition] != value)
            {
                var currTime = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                if (currTime > endTime)
                {
                    return false;
                }

                await Task.Delay(Configuration.WaitDurations.WaitForConditionLoop);
            }

            await Task.Delay(Configuration.WaitDurations.AfterWaitForCondition);

            return true;
        }


    }
}
