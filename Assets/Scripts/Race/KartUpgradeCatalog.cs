using System.Collections.Generic;

namespace RoguelikeRacing.Race
{
    /// <summary>
    /// The full pool of level-up upgrades, one per distinct KartController knob, offered
    /// a few at a time (see LevelUpController). Each is applied multiplicatively (or
    /// additively for slowResistance, which is clamped at use time in
    /// KartController.ApplySlow), so picking the same upgrade again on a later lap
    /// keeps compounding rather than re-applying a fixed flat bonus.
    /// </summary>
    public static class KartUpgradeCatalog
    {
        const float TopSpeedMultiplier = 1.12f;
        const float AccelerationMultiplier = 1.15f;
        const float HandlingMultiplier = 1.12f;
        const float TurboMultiplier = 1.25f;
        const float SlowResistanceBonus = 0.2f;
        const float TractionMultiplier = 0.85f;
        const float DriftBoostThresholdMultiplier = 0.8f;
        const float BrakeMultiplier = 1.2f;
        const float EngineBrakeMultiplier = 0.8f;
        const float ReverseSpeedMultiplier = 1.2f;
        const float DriftBoostDurationMultiplier = 1.25f;

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

            new KartUpgrade(
                "Blindagem",
                "Reduz o efeito de mancha de oleo e pulso de choque",
                kart => kart.slowResistance += SlowResistanceBonus),

            new KartUpgrade(
                "Tracao",
                "Atinge a curva maxima com menos velocidade",
                kart => kart.minSpeedFactorForFullTurn *= TractionMultiplier),

            new KartUpgrade(
                "Reflexo de piloto",
                "Precisa de menos tempo de drift para ganhar o mini-turbo",
                kart => kart.minDriftSecondsForBoost *= DriftBoostThresholdMultiplier),

            new KartUpgrade(
                "Freio competitivo",
                "+20% na forca de frenagem",
                kart => kart.brakeDeceleration *= BrakeMultiplier),

            new KartUpgrade(
                "Rolamento leve",
                "Perde menos velocidade ao soltar o acelerador",
                kart => kart.engineBrakeDeceleration *= EngineBrakeMultiplier),

            new KartUpgrade(
                "Marcha a re reforcada",
                "+20% na velocidade de re",
                kart => kart.maxReverseSpeed *= ReverseSpeedMultiplier),

            new KartUpgrade(
                "Turbo prolongado",
                "+25% na duracao do boost do mini-turbo",
                kart => kart.driftBoostDuration *= DriftBoostDurationMultiplier),
        };
    }
}
