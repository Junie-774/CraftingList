using CraftingList.Crafting.Macro;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

namespace CraftingList.Utility
{
    internal class Service
    {
        public static void Initialize(DalamudPluginInterface pluginInterface, Configuration config)
        {
            pluginInterface.Create<Service>();
            Configuration = config;

            CraftingConsumables = DataManager.GetExcelSheet<Item>()!
                .Where(item => item.ItemAction.Value!.DataHQ[1] != 0 && DataManager.GetExcelSheet<ItemFood>()!.Select(m => m.RowId).Contains(item.ItemAction.Value.DataHQ[1]))
                .Where(meal =>
                {
                    int param = Service.DataManager.GetExcelSheet<ItemFood>()!
                        .GetRow(meal.ItemAction.Value!.DataHQ[1])!.UnkData1[0].BaseParam;
                    return param == 11 || param == 70 || param == 71;
                });

            GameEventManager = new();
            ChatManager = new(ChatGui);
        }


        public static Configuration Configuration { get; set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static SigScanner SigScanner { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        private static ChatGui ChatGui { get; set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static CommandManager CommandManager { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static DataManager DataManager { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static ClientState ClientState { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static Framework Framework { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static Dalamud.Game.ClientState.Conditions.Condition Condition { get; private set; } = null!;

        public static Item? GetRowFromId(uint id)
        {
            return DataManager.GetExcelSheet<Item>()!.GetRow(id);
        }

        public static System.Collections.Generic.IEnumerable<Item> CraftingConsumables { get; private set; } = null!;

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
