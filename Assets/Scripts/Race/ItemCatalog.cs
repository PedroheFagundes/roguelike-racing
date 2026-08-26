using System.Collections.Generic;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// The full pool of items offered from an item box. Grown from the original 4 to 8 --
    /// ItemBox now samples a fixed number at random (see ItemBox.OptionsPerBox) instead
    /// of always showing every entry, so the player still sees the same-sized choice as
    /// before even as the roster grows.
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

        const float OverdriveExtraSpeed = 16f;
        const float OverdriveDuration = 3.5f;

        const float ForwardTrapSlowMultiplier = 0.4f;
        const float ForwardTrapSlowDuration = 2f;
        const float ForwardTrapDropOffset = 3f;

        const float MissileSpeed = 26f;
        const float MissileTurnRateDegPerSec = 140f;
        const float MissileLifetimeSeconds = 6f;
        const float MissileSlowMultiplier = 0.35f;
        const float MissileSlowDuration = 2.5f;
        const float MissileHitRadius = 1.2f;

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

            new ItemDefinition(
                "Overdrive",
                "Impulso de velocidade maior e mais longo que o Nitro",
                kart => kart.ApplyItemBoost(OverdriveExtraSpeed, OverdriveDuration)),

            new ItemDefinition(
                "Investida",
                "Larga uma armadilha na frente, util pra bloquear quem vem atras numa curva",
                kart => ItemHazards.DropOilSlick(
                    kart.transform.position + kart.transform.forward * ForwardTrapDropOffset,
                    ForwardTrapSlowMultiplier, ForwardTrapSlowDuration)),

            new ItemDefinition(
                "Missil teleguiado",
                "Persegue o kart a sua frente na corrida e desacelera ao acertar",
                kart => ItemHazards.FireHomingMissile(
                    kart, MissileSpeed, MissileTurnRateDegPerSec, MissileLifetimeSeconds,
                    MissileSlowMultiplier, MissileSlowDuration, MissileHitRadius)),

            new ItemDefinition(
                "Reviravolta",
                "Troca de lugar com um kart aleatorio da corrida",
                kart => ItemHazards.SwapPositions(kart)),
        };
    }
}
