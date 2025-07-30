using System;

namespace Nanover.Visualisation.Node.Protein
{
    /// <summary>
    /// Patterns that occur in protein secondary structure.
    /// </summary>
    [Flags]
    public enum SecondaryStructurePattern
    {
        None = 0x0,
        ThreeTurn = 0x1,
        FourTurn = 0x2,
        FiveTurn = 0x4,
        ParallelBridge = 0x8,
        AntiparallelBridge = 0x10,
        Bridge = ParallelBridge | AntiparallelBridge,
        Turn = ThreeTurn | FourTurn | FiveTurn
    }
}