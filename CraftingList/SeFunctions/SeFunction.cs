using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using System;
using System.Runtime.InteropServices;

namespace CraftingList.SeFunctions
{
    public class SeFunction<T> where T : Delegate
    {
        public IntPtr Address;
        protected T? FunctionDelegate;

        public SeFunction(SigScanner sigScanner, string signature)
        {
            Address = sigScanner.ScanText(signature);

            FunctionDelegate = Marshal.GetDelegateForFunctionPointer<T>(Address);
        }

        public SeFunction(IntPtr address)
        {
            Address = address;
            FunctionDelegate = Marshal.GetDelegateForFunctionPointer<T>(Address);
        }

        public T? Delegate()
        {
            if (FunctionDelegate == null)
            {
                PluginLog.Error($"Trying to generate delegate for {GetType().Name} failed.");
                return null;
            }

            return FunctionDelegate;
        }

        public unsafe dynamic? Invoke(params dynamic[] parameters)
        {
            if (FunctionDelegate == null)
            {
                PluginLog.Error($"Trying to generate delegate for {GetType().Name} failed.");
                return null;
            }
            try
            {
                return FunctionDelegate.DynamicInvoke(parameters);
            }
            catch(Exception e)
            {
                PluginLog.Error(e.ToString());
                return null;
            }
        }

        public Hook<T>? CreateHook(T detour)
        {
            if (Address != IntPtr.Zero)
            {
                var hook = Hook<T>.FromAddress(Address, detour);
                PluginLog.Debug($"Hooking {GetType().Name} at 0x{Address.ToInt64():X16}.");
                return hook;
            }

            PluginLog.Error($"Unable to create hook for {GetType().Name}, no pointer available.");
            return null;
        }
    }
}
