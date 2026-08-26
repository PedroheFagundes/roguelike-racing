using System;

namespace RoguelikeRacing.Kart
{
    /// <summary>
    /// Pure math used by KartController. No UnityEngine dependency on purpose,
    /// so it can be unit tested with a plain dotnet test project (see /Tests)
    /// without opening the Unity Editor.
    /// </summary>
    public static class KartPhysicsMath
    {
        /// <summary>
        /// Moves the current forward speed towards a target speed derived from throttle input.
        /// throttle in [-1, 1]: positive accelerates forward, negative brakes/reverses,
        /// zero applies engine braking towards 0.
        /// </summary>
        public static float IntegrateSpeed(
            float currentSpeed,
            float throttle,
            float maxForwardSpeed,
            float maxReverseSpeed,
            float acceleration,
            float brakeDeceleration,
            float engineBrakeDeceleration,
            float deltaTime)
        {
            throttle = Clamp(throttle, -1f, 1f);

            float target;
            float rate;

            if (throttle > 0f)
            {
                target = maxForwardSpeed;
                rate = acceleration * throttle;
            }
            else if (throttle < 0f)
            {
                if (currentSpeed > 0f)
                {
                    // braking while still moving forward
                    target = 0f;
                    rate = brakeDeceleration * -throttle;
                }
                else
                {
                    // already stopped or reversing: accelerate backwards
                    target = -maxReverseSpeed;
                    rate = acceleration * -throttle;
                }
            }
            else
            {
                target = 0f;
                rate = engineBrakeDeceleration;
            }

            float maxDelta = Math.Max(0f, rate) * deltaTime;
            return MoveTowards(currentSpeed, target, maxDelta);
        }

        /// <summary>
        /// Effective turn rate (degrees/second) at the given speed. Turning is scaled down
        /// at low speed (so the kart doesn't pirouette while nearly stationary) and reaches
        /// full turn rate once speed passes minSpeedFactorForFullTurn * maxForwardSpeed.
        /// </summary>
        public static float ComputeTurnRateDegPerSec(
            float currentSpeed,
            float maxForwardSpeed,
            float baseTurnRateDegPerSec,
            float minSpeedFactorForFullTurn,
            float lowSpeedTurnFactor)
        {
            if (maxForwardSpeed <= 0f) return 0f;

            float speedFraction = Clamp01(Math.Abs(currentSpeed) / maxForwardSpeed);
            float denom = Math.Max(0.0001f, minSpeedFactorForFullTurn);
            float t = Clamp01(speedFraction / denom);
            float turnFactor = Lerp(lowSpeedTurnFactor, 1f, t);

            return baseTurnRateDegPerSec * turnFactor;
        }

        /// <summary>
        /// Extra top-speed bonus granted after releasing a drift, based on how long the
        /// drift was held. Below minDriftSecondsForBoost the drift grants no bonus at all
        /// (mirrors "mini-turbo" mechanics from Mario Kart-likes).
        /// </summary>
        public static float ComputeDriftBoostSpeed(
            float driftHeldSeconds,
            float minDriftSecondsForBoost,
            float boostSpeedBonus,
            float maxBoostSpeedBonus)
        {
            if (driftHeldSeconds < minDriftSecondsForBoost) return 0f;

            float extra = driftHeldSeconds - minDriftSecondsForBoost;
            float bonus = boostSpeedBonus + extra * boostSpeedBonus * 0.5f;
            return Clamp(bonus, 0f, maxBoostSpeedBonus);
        }

        static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }

        static float Clamp01(float v) => Clamp(v, 0f, 1f);

        static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }
}
