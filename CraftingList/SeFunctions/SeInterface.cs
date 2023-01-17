using CraftingList.Crafting;
using CraftingList.Utility;
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

namespace CraftingList.SeFunctions
{
    
    public unsafe class SeInterface
    {
        //Using the Singleton<FunctionObject> pattern doesn't work for this one for reasons I don't understand.
        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9")]
        private static readonly ProcessChatBoxDelegate processChatBox = null!;
        private unsafe delegate void ProcessChatBoxDelegate(UIModule* uiModule, IntPtr message, IntPtr unused, byte a4);

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

        private readonly IntPtr m_baseUiObject;
        private readonly IntPtr m_uiProperties;
        private readonly bool m_canGetUiObjects;
        private readonly GetUiObjectByNameDelegate? m_getUiObjectByNameDelegate;
        private readonly OpenRecipeByRecipeIdDelegate? m_openRecipeDelegate;
        private readonly UseActionDelegate? m_useActionDelegate;

        private AddonWaitlist m_waitlist = new();

        //I could not ever explain why, but closing the recipe note directly
        //crashes the game in some circumstances, but directing a call through 
        //the macro system doesn't.
        //I dunno.
        public Macro CloseNoteMacro;        

        public Hook<AddonRecipeNoteReceiveEventDelegate>? recipeREHook;
        public Hook<AgentRecipeNoteReceiveEventDelegate>? recipeAgentREHook;
        public Hook<AgentRecipeMaterialListReceiveEventDelegate>? recipeMaterialREHook;

        public static void Dispose()
        {
            Instance.recipeREHook?.Disable();
            Instance.recipeREHook?.Dispose();
            Instance.recipeAgentREHook?.Disable();
            Instance.recipeAgentREHook?.Dispose();
            Instance.m_waitlist?.Dispose();
        }
        public SeInterface()
        {
            SignatureHelper.Initialise(this);
            m_baseUiObject = Singleton<GetBaseUiObject>.Get().Invoke() ?? IntPtr.Zero;
            m_uiProperties = (m_baseUiObject != IntPtr.Zero) ? Marshal.ReadIntPtr(m_baseUiObject, 0x20) : IntPtr.Zero;
            m_getUiObjectByNameDelegate = Singleton<GetUiObjectByName>.Get().Delegate();
            m_canGetUiObjects = (m_uiProperties != IntPtr.Zero) && m_getUiObjectByNameDelegate != null;
            m_openRecipeDelegate = Singleton<OpenRecipebyRecipeId>.Get().Delegate();
            m_useActionDelegate = Singleton<UseAction>.Get().Delegate();

            CloseNoteMacro = new Macro(0, 0, "Close", "/closerecipenote");

            recipeREHook = Singleton<AddonRecipeNoteReceiveEvent>.Get().CreateHook(ReceiveEventLogDetour);
            //recipeREHook?.Enable();
            recipeAgentREHook = Singleton<AgentRecipeNoteReceiveEvent>.Get().CreateHook(AgentReceiveEventDetour);
            //recipeAgentREHook?.Enable();
            recipeMaterialREHook = Singleton<AgentRecipeMaterialListReceiveEvent>.Get().CreateHook(AgentMaterialListReceiveEventDetour);
            //recipeMaterialREHook?.Enable();

        }

        public static void ReceiveEventLogDetour(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused)
        {
            PluginLog.Debug($"Receive Event: {atkUnit:X}, eventType: {eventType}, which: {which}, source: {source:X}, unused: {unused:X}");
            Instance.recipeREHook?.Original(atkUnit, eventType, which, source, unused);
        }

        public static long AgentReceiveEventDetour(IntPtr agent, long ptr1, long atkvalue, long dum1, long dum2)
        {
            PluginLog.Debug($"AgentReceiveEvent: agent: {agent}, ptr1: {ptr1} Atk 1 value: {((AtkValue*)atkvalue)->Int}, Atk 2 value: {((AtkValue*)atkvalue)[1].Int}.");
            if (Instance.recipeAgentREHook != null)
            {
                return Instance.recipeAgentREHook.Original(agent, ptr1, atkvalue, dum1, dum2);
            }
            return 0;
        }

        public static long AgentMaterialListReceiveEventDetour(IntPtr agent, long ptr1, long atkvalue, long dum1, long dum2)
        {

            PluginLog.Debug($"AgentMaterialListReceiveEvent: agent: {agent}, ptr1: {ptr1} Atk 1 value: {((AtkValue*)atkvalue)->Int}, Atk 2 value: {((AtkValue*)atkvalue)[1].Int}.");
            if (Instance.recipeMaterialREHook != null)
            {
                return Instance.recipeMaterialREHook.Original(agent, ptr1, atkvalue, dum1, dum2);
            }
            return 0;
        }


        public static IntPtr GetUiObject(string name, int index = 1)
        {
            if (Instance.m_canGetUiObjects)
            {
                return Instance.m_getUiObjectByNameDelegate!(Instance.m_uiProperties, name, index);
            }

            PluginLog.Error($"Cannot obtain Ui Object: {name}. Can't obtain UI objects.");
            return IntPtr.Zero;
        }

        public static IntPtr GetUiObjectSolo(string name) => GetUiObject(name);

        public static void ExecuteMacroByNumber(int macroNum) => RaptureShellModule.Instance->ExecuteMacro(RaptureMacroModule.Instance->Individual[macroNum]);
        public static void ExecuteMacro(Macro* m) => RaptureShellModule.Instance->ExecuteMacro((RaptureMacroModule.Macro*)m);
        public static void ExecuteMacro(Macro m) => RaptureShellModule.Instance->ExecuteMacro((RaptureMacroModule.Macro*)&m);

        public static unsafe void SendChatMessage(string message)
        {
            var uiModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule();

            using var payload = new ChatPayload(message);
            var payloadPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ChatPayload)));
            Marshal.StructureToPtr(payload, payloadPtr, false);

            processChatBox(uiModule, payloadPtr, IntPtr.Zero, 0);

            Marshal.FreeHGlobal(payloadPtr);
        }

        public static PtrRecipeNote RecipeNote() => GetUiObject("RecipeNote");
        public static PtrSynthesis Synthesis() => GetUiObject("Synthesis");
        public static PtrRepair Repair() => GetUiObject("Repair");
        public static PtrSelectYesNo SelectYesNo() => GetUiObject("SelectYesno");
        public static RaptureMacroModule* MacroManager() => RaptureMacroModule.Instance;
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
            return (addon != IntPtr.Zero && (!needsTobeVisible || ((AtkUnitBase*)addon.ToPointer())->IsVisible));
        }

        public static bool IsAddonUnavailable(IntPtr addon, bool needsToBeVisible)
        {
            return (addon == IntPtr.Zero || (needsToBeVisible && !((AtkUnitBase*)addon.ToPointer())->IsVisible));
        }
        public static void SwapToDOHJob(int job)
        {
            SendChatMessage("/gearset change " + DoHJobs[job]);
        }

        public static void ToggleRepairWindow() => SendChatMessage("/gaction repair");

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

        public static void RemoveFood() => SendChatMessage("Well Fed");

        public static void RemoveMedicated() => Statusoff("Medicated");

        public static void Statusoff(string status) => SendChatMessage("/statusoff \"" + status + "\"");

        

        public static unsafe bool NeedsRepair()
        {
            bool existsItemBelowThreshold = false;
            bool existsItemAbove100 = false;
            bool existsBrokenItem = false;
            for (int i = 0; i < 13; i++)
            {
                var condition = InventoryManager()->GetInventoryContainer(InventoryType.EquippedItems)->GetInventorySlot(i)->Condition;
                if (condition <= (ushort)30000 * DalamudApi.Configuration.RepairThresholdPercent / 100 || condition == 0)
                {
                    existsItemBelowThreshold = true;
                }
                if (condition > 30000)
                {
                    existsItemAbove100 = true;
                }
                if (condition == 0)
                {
                    existsBrokenItem = true;
                }
            }

            if (existsBrokenItem) return true;
            if (existsItemAbove100 && DalamudApi.Configuration.OnlyRepairIfBelow99) return false;
            return existsItemBelowThreshold;
        }

        public static bool HasStatusID(uint statusID)
        {
            bool hasStatus = false;

            foreach (var status in DalamudApi.ClientState.LocalPlayer!.StatusList)
            {
                if (status == null) continue;

                if (status.StatusId == statusID)
                {
                    hasStatus = true;
                }

            }
            return hasStatus;
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly struct ChatPayload : IDisposable
        {
            [FieldOffset(0)]
            private readonly IntPtr textPtr;

            [FieldOffset(16)]
            private readonly ulong textLen;

            [FieldOffset(8)]
            private readonly ulong unk1;

            [FieldOffset(24)]
            private readonly ulong unk2;

            internal ChatPayload(string text)
            {
                var stringBytes = Encoding.UTF8.GetBytes(text);
                this.textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);

                Marshal.Copy(stringBytes, 0, this.textPtr, stringBytes.Length);
                Marshal.WriteByte(this.textPtr + stringBytes.Length, 0);

                this.textLen = (ulong)(stringBytes.Length + 1);

                this.unk1 = 64;
                this.unk2 = 0;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(this.textPtr);
            }
        }
    }
}
