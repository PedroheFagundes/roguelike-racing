using System;
using RoguelikeRacing.Kart;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// An item: Use is the effect applied when it's actually activated. Picking an item
    /// from a box only holds it (see KartInventory); Use runs later, either when the
    /// player presses the item button (KartInput) or immediately for AI, which has no
    /// use-timing strategy yet — see docs/DESIGN_DECISIONS.md. Mirrors KartUpgrade's
    /// shape on purpose so both fit through the same ChoicePrompt/PauseChoiceUI
    /// pipeline as level-up.
    /// </summary>
    public readonly struct ItemDefinition
    {
        public readonly string Name;
        public readonly string Description;
        public readonly Action<KartController> Use;

        public ItemDefinition(string name, string description, Action<KartController> use)
        {
            Name = name;
            Description = description;
            Use = use;
        }
    }
}
