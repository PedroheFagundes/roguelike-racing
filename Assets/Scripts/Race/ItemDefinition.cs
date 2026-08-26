using System;
using RoguelikeRacing.Kart;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// An item, applied immediately to the kart that picks it once chosen (there is no
    /// held inventory slot / separate "use" button in this prototype — see
    /// docs/DESIGN_DECISIONS.md for why). Use mirrors KartUpgrade's shape on purpose so
    /// both fit through the same ChoicePrompt/PauseChoiceUI pipeline as level-up.
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
