using CraftingList.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public delegate void SynthesisSimpleDialogReceiveEventDelegate(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused);
    public unsafe class AddonSynthesisSimpleReceiveEvent : SeFunction<SynthesisSimpleDialogReceiveEventDelegate>
    {
        public AddonSynthesisSimpleReceiveEvent()
            :base(Module.ObtainReceiveEventDelegatePtr((void*) (IntPtr) SeInterface.SynthesisSimpleDialog()))
        {

        }
    }
}
