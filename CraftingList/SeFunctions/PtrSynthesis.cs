using CraftingList.Crafting;
using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

public unsafe struct PtrSynthesis
{
    // offset to Synthesis pointer.
    private const int OffsetCraftingAgent = 0x160;

    // Offset to result for CraftingAgent.
    private const int OffsetStep = 0xf8;
    private const int OffsetStatus = 0xc8;
    private const int OffsetDurability = 0x78;
    private const int OffsetHqChance = 0xa8;
    private const int OffsetQuality = 0x98;
    private const int OffsetProgress = 0x58;

    public AddonSynthesis* Pointer;
    private byte* _agent;


    public static implicit operator PtrSynthesis(IntPtr ptr)
    {
        var ret = new PtrSynthesis { Pointer = Module.Cast<AddonSynthesis>(ptr) };
        if (ret)
            ret._agent = *(byte**)((byte*)ret.Pointer + OffsetCraftingAgent);

        return ret;
    }

    public static implicit operator bool(PtrSynthesis ptr)
        => ptr.Pointer != null;


    public int Step
        => _agent == null ? 0 : *(int*)(_agent + OffsetStep);

    public Status Status
        => _agent == null ? 0 : *(Status*)(_agent + OffsetStatus);

    public int Durability
        => _agent == null ? 0 : *(int*)(_agent + OffsetDurability);

    public int HqChance
        => _agent == null ? 0 : *(int*)(_agent + OffsetHqChance);

    public int Quality
        => _agent == null ? 0 : *(int*)(_agent + OffsetQuality);

    public int Progress
        => _agent == null ? 0 : *(int*)(_agent + OffsetProgress);
}
