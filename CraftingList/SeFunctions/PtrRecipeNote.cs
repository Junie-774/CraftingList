﻿using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Runtime.InteropServices;

namespace CraftingList.SeFunctions
{
    public unsafe struct PtrRecipeNote
    {
        private const int SynthesizeButtonId = 13;
        private const int QuickSynthButtonId = 14;

        public AddonRecipeNote* Pointer;

        public static implicit operator PtrRecipeNote(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonRecipeNote>(ptr) };

        public static implicit operator bool(PtrRecipeNote ptr)
            => ptr.Pointer != null;


        public void Synthesize()
        {
            if (Pointer == null) return;
            Module.ClickAddon(Pointer, null/*Pointer->SynthesizeButton->AtkComponentBase.OwnerNode*/, EventType.Change, SynthesizeButtonId);
        }

        public void QuickSynthesize()
        {
            if (Pointer == null) return;
            Module.ClickAddon(Pointer, null, EventType.Change, QuickSynthButtonId);
        }

        public void ClickJob(int which)
        {
            if (Pointer == null) return;
            var radioButtonNode = Pointer->AtkUnitBase.UldManager.NodeList[97 - which];
            Module.ClickAddon(Pointer, radioButtonNode, EventType.Change, 1);
        }

        public void OpenRecipeByRecipeId(int id)
        {
            if (AgentRecipeNote.Instance() == null) return;
            AgentRecipeNote.Instance()->OpenRecipeByRecipeId((uint) id);
        }

        public void OpenRecipeByItemId(int id)
        {
            if (AgentRecipeNote.Instance() == null) return;
            AgentRecipeNote.Instance()->OpenRecipeByItemId((uint)id);
        }

        public void ClickButton(int which)
        {
            if (Pointer == null) return;
            Module.ClickAddon(Pointer, Pointer->SynthesizeButton->AtkComponentBase.OwnerNode, EventType.Change, which);
        }

        public void ClickHQ(int which)
        {
            if (which < 0 || which > 5) return;
            AtkValue* atkValue = stackalloc AtkValue[2];
            atkValue[0].ChangeType(FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int);
            atkValue[1].ChangeType(FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int);
            atkValue[0].Int = 6;
            atkValue[1].Int = 65536 + which;
            byte* numPtr1 = stackalloc byte[16];
            Singleton<AgentRecipeNoteReceiveEvent>.Get().Invoke((IntPtr) AgentRecipeNote.Instance(),
                (long)numPtr1, (long)atkValue, 0, 0);
        }

        private void FillHQ(int arg)
        {
            AtkValue* atkValuePtr = stackalloc AtkValue[2];
            atkValuePtr[0].ChangeType(FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int);
            atkValuePtr[1].ChangeType(FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int);
            atkValuePtr[0].Int = 6;
            atkValuePtr[1].Int = arg;
            byte* numPtr1 = stackalloc byte[16];
            Singleton<AgentRecipeNoteReceiveEvent>.Get().Invoke((IntPtr)AgentRecipeNote.Instance(),
                (long)numPtr1, (long)atkValuePtr, 0, 0);
        }

        public void FillHQ(bool hq) =>
            FillHQ(hq ? 131071 : ushort.MaxValue);

        public void Close()
        {
            try
            {
                if (Pointer != null
                    && Pointer->AtkUnitBase.IsVisible)
                {
                    Module.Fire(&Pointer->AtkUnitBase, true, -1);
                }
            }
            catch (Exception ex) {
                Service.PluginLog.Debug(ex.ToString());
            }
        }

        public bool IsVisible()
        {
            return Pointer != null && Pointer->AtkUnitBase.IsVisible;
        }
    }
}
