using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public unsafe class ClickSynthesisSimpleDialog : ClickLib.Bases.ClickBase<ClickSynthesisSimpleDialog, AtkUnitBase>
    {
        public ClickSynthesisSimpleDialog() : base("SynthesisSimpleDialog", default)
        {
        }

        public void ClickUseHQMats()
            => this.ClickAddonCheckBox((AtkComponentCheckBox*) ((AtkUnitBase*) SeInterface.GetUiObject("SynthesisSimpleDialog"))->GetNodeById(5)->GetAsAtkComponentNode()->Component, 2);
    }
}
