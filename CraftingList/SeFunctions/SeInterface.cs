using CraftingList.Crafting;
using CraftingList.Utility;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public unsafe class SeInterface
    {
        public enum DoHJob : int
        {
            Carpenter = 0,
            Blacksmith = 1,
            Armorer = 2,
            Goldsmith = 3,
            Leatherworker = 4,
            Weaver = 5,
            Alchemist = 6,
            Culinarian = 7
        }
        private readonly IntPtr m_baseUiObject;
        private readonly IntPtr m_uiProperties;
        private readonly bool m_canGetUiObjects;
        private readonly GetUiObjectByNameDelegate? m_getUiObjectByNameDelegate;
        private readonly OpenRecipeByRecipeIdDelegate? m_openRecipeDelegate;
        private readonly UseActionDelegate? m_useActionDelegate;

        private AddonWaitlist m_waitlist = new();

        private Macro[] ChangeJobMacros;
        private Macro OpenRepairMacro;
        private Macro RemoveFoodMacro;
        public Macro CloseNoteMacro;
        public Macro AnnounceCompleteMacro;

        public Hook<AddonRecipeNoteReceiveEventDelegate>? recipeREHook;
        public void Dispose()
        {
            recipeREHook?.Disable();
            recipeREHook?.Dispose();
            m_waitlist?.Dispose();
        }

        public SeInterface()
        {
            m_baseUiObject = Singleton<GetBaseUiObject>.Get().Invoke() ?? IntPtr.Zero;
            m_uiProperties = (m_baseUiObject != IntPtr.Zero) ? Marshal.ReadIntPtr(m_baseUiObject, 0x20) : IntPtr.Zero;
            m_getUiObjectByNameDelegate = Singleton<GetUiObjectByName>.Get().Delegate();
            m_canGetUiObjects = (m_uiProperties != IntPtr.Zero) && m_getUiObjectByNameDelegate != null;
            m_openRecipeDelegate = Singleton<OpenRecipebyRecipeId>.Get().Delegate();
            m_useActionDelegate = Singleton<UseAction>.Get().Delegate();

            ChangeJobMacros = new Macro[] {
                 new Macro(0, 0, "Name", "/gearset change Carpenter"),
                 new Macro(0, 0, "Name", "/gearset change Blacksmith"),
                 new Macro(0, 0, "Name", "/gearset change Armorer"),
                 new Macro(0, 0, "Name", "/gearset change Goldsmith"),
                 new Macro(0, 0, "Name", "/gearset change Leatherworker"),
                 new Macro(0, 0, "Name", "/gearset change Weaver"),
                 new Macro(0, 0, "Name", "/gearset change Alchemist"),
                 new Macro(0, 0, "Name", "/gearset change Culinarian"),
            };

            OpenRepairMacro = new Macro(0, 0, "Open Repair", "/gaction \"Repair\"");
            RemoveFoodMacro = new Macro(0, 0, "Remove Food", "/statusoff \"Well Fed\"");
            CloseNoteMacro = new Macro(0, 0, "Close", "/craftinglist 0");

            //recipeREHook = Singleton<AddonRecipeNoteReceiveEvent>.Get().CreateHook(ReceiveEventLogDetour);
            //recipeREHook?.Enable();
        }

        public void ReceiveEventLogDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused)
        {
            PluginLog.Debug($"Receive Event: {atkUnit:X}, eventType: {eventType}, which: {which}, source: {source:X}, unused: {unused:X}");
            recipeREHook?.Original(atkUnit, eventType, which, source, unused);
        }

        public IntPtr GetUiObject(string name, int index = 1)
        {
            if (m_canGetUiObjects)
            {
                return m_getUiObjectByNameDelegate!(m_uiProperties, name, index);
            }

            PluginLog.Error($"Cannot obtain Ui Object: {name}. Can't obtain UI objects.");
            return IntPtr.Zero;
        }

        public IntPtr GetUiObjectSolo(string name) => GetUiObject(name);

        public void ExecuteMacroByNumber(int macroNum) => RaptureShellModule.Instance->ExecuteMacro(RaptureMacroModule.Instance->Individual[macroNum]);
        public void ExecuteMacro(Macro* m) => RaptureShellModule.Instance->ExecuteMacro((RaptureMacroModule.Macro*)m);
        public void ExecuteMacro(Macro m) => RaptureShellModule.Instance->ExecuteMacro((RaptureMacroModule.Macro*)&m);
        public void ExecuteMacro(CraftingMacro m) => ExecuteMacroByNumber(m.MacroNum);


        public PtrRecipeNote RecipeNote() => GetUiObject("RecipeNote");
        public PtrSynthesis Synthesis() => GetUiObject("Synthesis");
        public PtrRepair Repair() => GetUiObject("Repair");
        public PtrSelectYesNo SelectYesNo() => GetUiObject("SelectYesno");
        public RaptureMacroModule* MacroManager() => RaptureMacroModule.Instance;
        public InventoryManager* InventoryManager() => FFXIVClientStructs.FFXIV.Client.Game.InventoryManager.Instance();

        public Task<IntPtr> WaitForAddon(string addonName, bool requiresVisible, int timeoutMs)
        {
            return m_waitlist.Add(addonName, requiresVisible, timeoutMs, GetUiObjectSolo, IsAddonAvailable);
        }

        public Task<IntPtr> WaitForCloseAddon(string addonName, bool requiresVisible, int timeoutMs)
        {
            return m_waitlist.Add(addonName, requiresVisible, timeoutMs, GetUiObjectSolo, IsAddonUnavailable);
        }

        public bool IsAddonAvailable(IntPtr addon, bool needsTobeVisible)
        {
            return (addon != IntPtr.Zero && (!needsTobeVisible || ((AtkUnitBase*)addon.ToPointer())->IsVisible));
        }

        public bool IsAddonUnavailable(IntPtr addon, bool needsToBeVisible)
        {
            return (addon == IntPtr.Zero || (needsToBeVisible && !((AtkUnitBase*)addon.ToPointer())->IsVisible));
        }
        public void SwapToDOHJob(DoHJob job)
        {
            ExecuteMacro(ChangeJobMacros[(int)job]);
        }

        public void ToggleRepairWindow() => ExecuteMacro(OpenRepairMacro);

        public void UseAction(IntPtr AM, uint actionType, uint actionID, long targetID, uint a4, uint a5, int a6, IntPtr a7)
        {
            if (m_useActionDelegate == null)
                return;
            m_useActionDelegate(AM, actionType, actionID, targetID, a4, a5, a6, a7);
        }

        public void UseItem(uint itemId)
        {
            UseAction((IntPtr)ActionManager.Instance(), 2, itemId, 0xE0000000, 65535, 0, 0, (IntPtr)null);
        }

        public void RemoveFood() => ExecuteMacro(RemoveFoodMacro);

    }
}
