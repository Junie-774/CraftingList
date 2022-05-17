using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CraftingList
{
    [Serializable]
    public struct WaitDurationHelper
    {
        public int AfterChangeJobs = 1500;
        public int AfterEatFood = 3000;
        public int AfterCompleteMacroHQ = 4000;
        public int AfterCompleteMacroCollectible = 4000;
        public int AfterOpenRecipeNote = 1000;
        public int AfterClickSynthesize = 2000;
        public int AfterExitCrafting = 2500;
        public int AfterOpenCloseMenu = 1000;
        public int AfterRepairConfirm = 3000;
        public int AfterClickOffFood = 1000;
        public int QuickSynthPerItem = 3000;

        public WaitDurationHelper() { }
    }

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public Crafting.Crafter Crafter { get; set; } = null!;

        public List<Crafting.CraftingMacro> Macros = new();

        public int RepairThresholdPercent = 99;
        public bool OnlyRepairIfBelow99 = true;

        public WaitDurationHelper WaitDurations = new();

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface, Crafting.Crafter crafter)
        {
            this.pluginInterface = pluginInterface;
            this.Crafter = crafter;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
            Crafter.OnlyRepairIfBelow99 = OnlyRepairIfBelow99;
            Crafter.RepairThresholdPercent = RepairThresholdPercent;
            Crafter.WaitDurationHelper = WaitDurations;
        }
    }
}
