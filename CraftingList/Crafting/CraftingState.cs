using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting
{
    public enum CraftingCondition : uint
    {
        /// <summary>
        /// Normal condition.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Good condition.
        /// </summary>
        Good = 2,

        /// <summary>
        /// Excellent condition.
        /// </summary>
        Excellent = 3,

        /// <summary>
        /// Poor condition.
        /// </summary>
        Poor = 4,

        /// <summary>
        /// Centered condition.
        /// </summary>
        Centered = 5,

        /// <summary>
        /// Sturdy condition.
        /// </summary>
        Sturdy = 6,

        /// <summary>
        /// Pliant condition.
        /// </summary>
        Pliant = 7,

        /// <summary>
        /// Malleable condition.
        /// </summary>
        Malleable = 8,

        /// <summary>
        /// Primed condition.
        /// </summary>
        Primed = 9,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x64)]
    public struct CraftingState
    {
        /// <summary>
        /// Gets the action type.
        /// </summary>
        [FieldOffset(0x0)]
        public ActionType ActionType;

        // [FieldOffset(0x04)] public uint Unk04;
        // [FieldOffset(0x08)] public uint Unk08;
        // [FieldOffset(0x0C)] public uint Unk0C;

        /// <summary>
        /// Gets the action ID.
        /// </summary>
        [FieldOffset(0x10)]
        public uint ActionID;

        // [FieldOffset(0x14)] public int Unk14;

        /// <summary>
        /// Gets the current step.
        /// </summary>
        [FieldOffset(0x18)]
        public uint CurrentStep;

        /// <summary>
        /// Gets the current progress.
        /// </summary>
        [FieldOffset(0x1C)]
        public uint CurrentProgress;

        /// <summary>
        /// Gets the increase in progress from the last step.
        /// </summary>
        [FieldOffset(0x20)]
        public uint ProgressIncrease;

        /// <summary>
        /// Gets the current quality.
        /// </summary>
        [FieldOffset(0x24)]
        public uint CurrentQuality;

        /// <summary>
        /// Gets the increase in quality from the last step.
        /// </summary>
        [FieldOffset(0x28)]
        public uint QualityIncrease;

        /// <summary>
        /// Gets the current percent chance for an HQ result.
        /// </summary>
        [FieldOffset(0x2C)]
        public ushort PercentHQ;

        // [FieldOffset(0x2E)] public ushort Unk2E;

        /// <summary>
        /// Gets the current durability.
        /// </summary>
        [FieldOffset(0x30)]
        public int CurrentDurability;

        /// <summary>
        /// Gets the delta in durability from the last step, not counting increases from other actions.
        /// </summary>
        [FieldOffset(0x34)]
        public int DurabilityDelta;

        /// <summary>
        /// Gets the current condition.
        /// </summary>
        [FieldOffset(0x38)]
        public CraftingCondition CurrentCondition;

        /// <summary>
        /// Gets the previous condition.
        /// </summary>
        [FieldOffset(0x3C)]
        public CraftingCondition PreviousCondition;

        /// <summary>
        /// Gets the result of the last step.
        /// </summary>
        [FieldOffset(0x40)]
        public uint Result;

        // [FieldOffset(0x40)] public ActionResult Result;
        // [FieldOffset(0x40)] private readonly ActionResultFlags resultFlags;

        // [FieldOffset(0x42)] public ushort Unk42;
        // [FieldOffset(0x44)] public uint Unk44;
        // [FieldOffset(0x48)] public uint Unk48;
        // [FieldOffset(0x4C)] public uint Unk4C;
        // [FieldOffset(0x50)] public uint Unk50;
        // [FieldOffset(0x54)] public uint Unk54;
        // [FieldOffset(0x58)] public uint Unk58;
        // [FieldOffset(0x5C)] public uint Unk5C;
        // [FieldOffset(0x60)] public uint Unk60;

        // public bool Flag0 => this.resultFlags.HasFlag(ActionResultFlags.Unk0);
        // public bool Step1 => !this.resultFlags.HasFlag(ActionResultFlags.NotStep1);
        // public bool CraftingSuccess => this.resultFlags.HasFlag(ActionResultFlags.CraftingSuccess);
        // public bool CraftingFailure => this.resultFlags.HasFlag(ActionResultFlags.CraftingFailure);
        // public bool ActionSuccess => this.resultFlags.HasFlag(ActionResultFlags.ActionSuccess);
    }
}
