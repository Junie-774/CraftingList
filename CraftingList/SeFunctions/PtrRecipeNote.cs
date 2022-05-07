using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;

namespace CraftingList.SeFunctions
{
    public unsafe struct PtrRecipeNote
    {
        private const int SynthesizeButtonId = 13;

        public AddonRecipeNote* Pointer;

        public static implicit operator PtrRecipeNote(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonRecipeNote>(ptr) };

        public static implicit operator bool(PtrRecipeNote ptr)
            => ptr.Pointer != null;

        public void Synthesize()
        {
            //PluginLog.Information($"Pointer: {(IntPtr) Pointer:X16}");
            Module.ClickAddon(Pointer, Pointer->SynthesizeButton->AtkComponentBase.OwnerNode, EventType.Change, SynthesizeButtonId);
        }

        public void ClickJob(int which)
        {
            var radioButtonNode = Pointer->AtkUnitBase.UldManager.NodeList[97 - which];
            Module.ClickAddon(Pointer, radioButtonNode, EventType.Change, 1);
        }

        public void OpenRecipeByRecipeId(int id)
        {
            Singleton<OpenRecipebyRecipeId>.Get().Invoke((IntPtr)AgentRecipeNote.Instance(), id);
        }

        public void OpenRecipeByItemId(int id)
        {
            Singleton<OpenRecipeByItemId>.Get().Invoke((IntPtr)AgentRecipeNote.Instance(), id);
        }

        public void Close()
        {
            Singleton<AgentRecipeNoteHide>.Get().Invoke((IntPtr)AgentRecipeNote.Instance());
        }
    }
}
