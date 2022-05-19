﻿using CraftingList.Crafting;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CraftingList
{
    [Serializable]
    public struct WaitDurationHelper
    {
        public int AfterChangeJobs = 2500;
        public int AfterEatFood = 3000;
        public int AfterCompleteMacroHQ = 3000;
        public int AfterCompleteMacroCollectible = 3000;
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

        public List<CraftingMacro> Macros { get; set; } = new();
        public List<CListEntry> EntryList { get; set; } = new();


        public int RepairThresholdPercent = 99;
        public bool OnlyRepairIfBelow99 = true;

        public WaitDurationHelper WaitDurations = new();

        public int ListCompleteSoundEffect = 6;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface, Crafter crafter)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
