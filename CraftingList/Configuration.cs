using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CraftingList
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public Crafting.Crafter Crafter { get; set; } = null!;

        public List<Crafting.CraftingMacro> Macros = new();

        public int RepairThresholdPercent = 99;
        public bool OnlyRepairIfBelow99 = true;

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
        }
    }
}
