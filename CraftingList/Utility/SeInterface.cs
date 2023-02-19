using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.SeFunctions;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Utility
{

    public unsafe class SeInterface
    {
        public static readonly List<string> DoHJobs = new()
        {
            "Carpenter",
            "Blacksmith",
            "Armorer",
            "Goldsmith",
            "Leatherworker",
            "Weaver",
            "Alchemist",
            "Culinarian"
        };



        public static SeInterface Instance { get; } = new();

        private readonly UseActionDelegate? m_useActionDelegate;

        private AddonWaitlist m_waitlist = new();

        //I could not ever explain why, but closing the recipe note directly
        //crashes the game in some circumstances, but directing a call through 
        //the macro system doesn't.
        //I dunno.
        public FFXIVInternalMacro CloseNoteMacro;

        public Hook<AgentRecipeNoteReceiveEventDelegate>? recipeAgentREHook;
        public Hook<SynthesisSimpleDialogReceiveEventDelegate>? dialogREHook;

        public static void Dispose()
        {
            Instance.recipeAgentREHook?.Disable();
            Instance.recipeAgentREHook?.Dispose();
            Instance.dialogREHook?.Disable();
            Instance.dialogREHook?.Dispose();
            Instance.m_waitlist?.Dispose();
        }

        public void InitializeHooks()
        {
            //dialogREHook = Singleton<AddonSynthesisSimpleReceiveEvent>.Get().CreateHook(SynthSimpleREDetour);
            //dialogREHook?.Enable();
        }
        public SeInterface()
        {
            SignatureHelper.Initialise(this);

            m_useActionDelegate = Singleton<UseAction>.Get().Delegate();

            CloseNoteMacro = new FFXIVInternalMacro(0, 0, "Close", "/closerecipenote");
            
        }
        private static void SynthSimpleREDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused)
        {
            PluginLog.Debug($"atkunit: {atkUnit:X16} event type: {eventType}, which: {which}, source: {source:X16}, unused: {unused:X16}");
            Instance.dialogREHook?.Original(atkUnit, eventType, which, source, unused);
        }

        public static IntPtr GetUiObject(string name, int index = 1)
        {
            return (IntPtr) AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonByName(name);
        }

        public static IntPtr GetUiObjectSolo(string name) => GetUiObject(name);

        public static void ExecuteFFXivInternalMacroByNumber(int macroNum) => RaptureShellModule.Instance->ExecuteMacro(RaptureMacroModule.Instance->Individual[macroNum]);
        public static void ExecuteFFXIVInternalMacro(FFXIVInternalMacro m) => RaptureShellModule.Instance->ExecuteMacro((RaptureMacroModule.Macro*)&m);

        public static PtrRecipeNote RecipeNote() => GetUiObject("RecipeNote");
        public static PtrSynthesis Synthesis() => GetUiObject("Synthesis");
        public static PtrSynthesisSimpleDialog SynthesisSimpleDialog() => GetUiObject("SynthesisSimpleDialog");
        public static PtrRepair Repair() => GetUiObject("Repair");
        public static PtrSelectYesNo SelectYesNo() => GetUiObject("SelectYesno");
        public static RaptureMacroModule* RaptureMacroManager() => RaptureMacroModule.Instance;
        public static InventoryManager* InventoryManager() => FFXIVClientStructs.FFXIV.Client.Game.InventoryManager.Instance();

        public static Task<IntPtr> WaitForAddon(string addonName, bool requiresVisible, int timeoutMs)
        {
            return Instance.m_waitlist.Add(addonName, requiresVisible, timeoutMs, GetUiObjectSolo, IsAddonAvailable);
        }

        public static Task<IntPtr> WaitForCloseAddon(string addonName, bool requiresVisible, int timeoutMs)
        {
            return Instance.m_waitlist.Add(addonName, requiresVisible, timeoutMs, GetUiObjectSolo, IsAddonUnavailable);
        }

        public static bool IsAddonAvailable(IntPtr addon, bool needsTobeVisible)
        {
            return addon != IntPtr.Zero && (!needsTobeVisible || ((AtkUnitBase*)addon.ToPointer())->IsVisible);
        }

        public static bool IsAddonUnavailable(IntPtr addon, bool needsToBeVisible)
        {
            return addon == IntPtr.Zero || needsToBeVisible && !((AtkUnitBase*)addon.ToPointer())->IsVisible;
        }
        public static void SwapToDOHJob(int job)
        {
            Service.ChatManager.SendMessage("/gearset change " + DoHJobs[job]);
        }

        public static void ToggleRepairWindow() => Service.ChatManager.SendMessage("/gaction " + Localization.GetRepairString());

        public static void UseAction(IntPtr AM, uint actionType, uint actionID, long targetID, uint a4, uint a5, int a6, IntPtr a7)
        {
            if (Instance.m_useActionDelegate == null)
                return;
            Instance.m_useActionDelegate(AM, actionType, actionID, targetID, a4, a5, a6, a7);
        }

        public static void UseItem(uint itemId)
        {
            UseAction((IntPtr)ActionManager.Instance(), 2, itemId, 0xE0000000, 65535, 0, 0, (IntPtr)null);
        }

        public static void RemoveFood() => Statusoff(Localization.GetWellFedStatusString());

        public static void RemoveMedicated() => Statusoff(Localization.GetMedicatedString());

        public static void Statusoff(string status) => Service.ChatManager.SendMessage("/statusoff \"" + status + "\"");

        public static int GetItemCountInInventory(uint itemId, bool isHQ = false, bool checkEquipped = true, bool checkArmory = true, short minCollectability = 0)
            => InventoryManager()->GetInventoryItemCount(itemId, isHQ, checkEquipped, checkArmory, minCollectability);

        public static bool HasStatusID(uint statusID)
        {
            bool hasStatus = false;

            foreach (var status in Service.ClientState.LocalPlayer!.StatusList)
            {
                if (status == null) continue;

                if (status.StatusId == statusID)
                {
                    hasStatus = true;
                }

            }
            return hasStatus;
        }


    }
}
