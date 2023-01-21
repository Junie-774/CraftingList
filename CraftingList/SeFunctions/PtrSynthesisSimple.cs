using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    internal unsafe struct PtrSynthesisSimple
    {
        IntPtr Pointer;

        public static implicit operator PtrSynthesisSimple(IntPtr ptr)
            => new() { Pointer = ptr };


        public string GetMaxCraftsText()
        {
            return ((AtkUnitBase*)Pointer)->GetNodeById(4)->ChildNode->GetAsAtkTextNode()->NodeText.ToString();
        }


        public string GetCurrCraftsText()
        {
            return ((AtkUnitBase*)Pointer)->GetNodeById(4)->ChildNode->PrevSiblingNode->PrevSiblingNode->GetAsAtkTextNode()->NodeText.ToString();
        }

        public int GetMaxCrafts()
            => int.TryParse(GetMaxCraftsText(), out int ret) ? ret: -1;


        public int GetCurrCrafts()
            => int.TryParse(GetCurrCraftsText(), out int ret) ? ret : -1;


        public void ClickQuit()
            => Utility.Module.ClickAddon((void*) Pointer, null, EventType.Change, 1);

    }
}
