using CraftingList.Crafting;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CraftingList.Utility
{
    internal class GameEventManager
    {
        [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 54 24 ?? 56 57 41 56 48 83 EC 50", DetourName = nameof(EventFrameworkDetour))]
        private readonly Hook<EventFrameworkDelegate> eventFrameworkHook = null!;

        public GameEventManager()
        {
            SignatureHelper.Initialise(this);
            this.eventFrameworkHook.Enable();
        }

        private unsafe delegate IntPtr EventFrameworkDelegate(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, CraftingState* dataPtr, byte dataSize);

        public ManualResetEvent DataAvailableWaiter { get; } = new(false);

        public CraftingState CraftingData { get; private set; } = default;

        public void Dispose()
        {
            this.eventFrameworkHook.Dispose();
            this.DataAvailableWaiter.Dispose();
        }

        private unsafe IntPtr EventFrameworkDetour(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, CraftingState* data, byte dataSize)
        {
            try
            {
                if (dataSize >= 4)
                {
                    if (data->ActionType == ActionType.MainCommand || data->ActionType == ActionType.CraftAction)
                    {
                        this.CraftingData = *data;

                        this.DataAvailableWaiter.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Don't crash the game.");
            }

            return this.eventFrameworkHook.Original(a1, a2, a3, a4, a5, data, dataSize);
        }
    }
}
