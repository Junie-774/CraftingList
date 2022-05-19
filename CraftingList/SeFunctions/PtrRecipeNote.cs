﻿using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;

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
            Module.ClickAddon(Pointer, null/*Pointer->SynthesizeButton->AtkComponentBase.OwnerNode*/, EventType.Change, SynthesizeButtonId);
        }

        public void QuickSynthesize()
        {
            Module.ClickAddon(Pointer, null, EventType.Click, QuickSynthButtonId);
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

        public void ClickButton(int which)
        {
            Module.ClickAddon(Pointer, Pointer->Unk330->AtkComponentBase.OwnerNode, EventType.Change, which);
        }

        public void Close()
        {
            if (AgentRecipeNote.Instance() != null && Pointer->AtkUnitBase.IsVisible)
            {
                AgentRecipeNote.Instance()->AgentInterface.Show();
            }
        }

        public bool IsVisible()
        {
            return Pointer == null ? false : Pointer->AtkUnitBase.IsVisible;
        }
    }
}
