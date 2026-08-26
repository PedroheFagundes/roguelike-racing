using System.Collections.Generic;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// The full pool of items offered from an item box: one instant self-buff (Nitro),
    /// one timed self-buff (Shield), one placed/delayed offensive item (Oil Slick), and
    /// one instant-AoE offensive item (Shockwave) — enough variety to test the pick
    /// without growing the roster past what the prototype needs.
    /// </summary>
    public static class ItemCatalog
    {
        const float NitroExtraSpeed = 10f;
        const float NitroDuration = 2.5f;

        const float ShieldDuration = 5f;

        const float OilSlowMultiplier = 0.45f;
        const float OilSlowDuration = 2f;
        const float OilDropOffset = 2.5f;

        const float ShockwaveRadius = 10f;
        const float ShockwaveSlowMultiplier = 0.5f;
        const float ShockwaveSlowDuration = 1.5f;

        public static readonly List<ItemDefinition> All = new List<ItemDefinition>
        {
            new ItemDefinition(
                "Nitro",
                "Impulso de velocidade por alguns segundos",
                kart => kart.ApplyItemBoost(NitroExtraSpeed, NitroDuration)),

            new ItemDefinition(
                "Escudo",
                "Bloqueia o proximo ataque ou obstaculo por um tempo",
                kart => kart.ApplyShield(ShieldDuration)),

            new ItemDefinition(
                "Mancha de oleo",
                "Derruba uma mancha atras de voce que desacelera quem passar por cima",
                kart => ItemHazards.DropOilSlick(
                    kart.transform.position - kart.transform.forward * OilDropOffset,
                    OilSlowMultiplier, OilSlowDuration)),

            new ItemDefinition(
                "Pulso de choque",
                "Desacelera todos os karts perto de voce na hora",
                kart => ItemHazards.Shockwave(kart, ShockwaveRadius, ShockwaveSlowMultiplier, ShockwaveSlowDuration)),
        };
    }
}
