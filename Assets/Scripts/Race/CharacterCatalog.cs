using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// The full pool of selectable characters: a classic speed/acceleration/handling
    /// triangle so no pick strictly dominates another. Exactly 3 entries on purpose —
    /// the player picks one on the setup screen, and GameBootstrap hands the other two
    /// to the two AI karts, so every race features all three regardless of pick.
    /// </summary>
    public static class CharacterCatalog
    {
        public static readonly List<CharacterDefinition> All = new List<CharacterDefinition>
        {
            new CharacterDefinition(
                "Equilibrado",
                "Stats parelhos, sem ponto fraco nem destaque",
                new Color(0.15f, 0.55f, 0.95f),
                topSpeedMultiplier: 1f, accelerationMultiplier: 1f, handlingMultiplier: 1f),

            new CharacterDefinition(
                "Veloz",
                "+velocidade maxima, -aceleracao e curva",
                new Color(0.9f, 0.25f, 0.15f),
                topSpeedMultiplier: 1.18f, accelerationMultiplier: 0.85f, handlingMultiplier: 0.85f),

            new CharacterDefinition(
                "Agil",
                "+aceleracao e curva, -velocidade maxima",
                new Color(0.95f, 0.8f, 0.1f),
                topSpeedMultiplier: 0.85f, accelerationMultiplier: 1.2f, handlingMultiplier: 1.2f),
        };
    }
}
