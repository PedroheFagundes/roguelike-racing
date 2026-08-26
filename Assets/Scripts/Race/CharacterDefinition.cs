using RoguelikeRacing.Kart;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// A selectable "character": just a body color and a starting stat profile applied
    /// once at spawn (ApplyTo), on top of KartController's own defaults. Level-up
    /// upgrades (KartUpgrade) keep stacking multiplicatively from whatever baseline this
    /// leaves the kart at — characters set the starting point, upgrades build on it.
    /// </summary>
    public readonly struct CharacterDefinition
    {
        public readonly string Name;
        public readonly string Description;
        public readonly Color BodyColor;
        public readonly float TopSpeedMultiplier;
        public readonly float AccelerationMultiplier;
        public readonly float HandlingMultiplier;

        public CharacterDefinition(string name, string description, Color bodyColor, float topSpeedMultiplier, float accelerationMultiplier, float handlingMultiplier)
        {
            Name = name;
            Description = description;
            BodyColor = bodyColor;
            TopSpeedMultiplier = topSpeedMultiplier;
            AccelerationMultiplier = accelerationMultiplier;
            HandlingMultiplier = handlingMultiplier;
        }

        public void ApplyTo(KartController kart)
        {
            kart.maxForwardSpeed *= TopSpeedMultiplier;
            kart.acceleration *= AccelerationMultiplier;
            kart.baseTurnRateDegPerSec *= HandlingMultiplier;
        }
    }
}
