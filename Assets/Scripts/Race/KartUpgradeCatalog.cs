using System.Collections.Generic;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// The full pool of level-up upgrades. Kept intentionally small (4) for the
    /// prototype, one per KartController stat group. Each is applied multiplicatively,
    /// so picking the same upgrade again on a later lap keeps compounding rather than
    /// re-applying a fixed flat bonus.
    /// </summary>
    public static class KartUpgradeCatalog
    {
        const float TopSpeedMultiplier = 1.12f;
        const float AccelerationMultiplier = 1.15f;
        const float HandlingMultiplier = 1.12f;
        const float TurboMultiplier = 1.25f;

        public static readonly List<KartUpgrade> All = new List<KartUpgrade>
        {
            new KartUpgrade(
                "Motor turbinado",
                "+12% de velocidade maxima",
                kart => kart.maxForwardSpeed *= TopSpeedMultiplier),

            new KartUpgrade(
                "Aceleracao",
                "+15% de aceleracao",
                kart => kart.acceleration *= AccelerationMultiplier),

            new KartUpgrade(
                "Suspensao leve",
                "+12% na taxa de curva",
                kart => kart.baseTurnRateDegPerSec *= HandlingMultiplier),

            new KartUpgrade(
                "Mini-turbo",
                "+25% no boost do drift",
                kart =>
                {
                    kart.driftBoostSpeedBonus *= TurboMultiplier;
                    kart.maxDriftBoostSpeedBonus *= TurboMultiplier;
                }),
        };
    }
}
