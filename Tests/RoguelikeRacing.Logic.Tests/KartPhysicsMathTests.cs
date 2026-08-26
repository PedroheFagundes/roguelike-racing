using NUnit.Framework;
using RoguelikeRacing.Kart;

namespace RoguelikeRacing.Logic.Tests
{
    public class KartPhysicsMathTests
    {
        [Test]
        public void IntegrateSpeed_FullThrottleFromRest_AcceleratesTowardsMax()
        {
            float speed = 0f;
            for (int i = 0; i < 60; i++)
            {
                speed = KartPhysicsMath.IntegrateSpeed(
                    speed, throttle: 1f, maxForwardSpeed: 24f, maxReverseSpeed: 10f,
                    acceleration: 18f, brakeDeceleration: 30f, engineBrakeDeceleration: 10f,
                    deltaTime: 0.02f);
            }

            Assert.That(speed, Is.EqualTo(21.6f).Within(0.01f));
        }

        [Test]
        public void IntegrateSpeed_NeverExceedsMaxForwardSpeed()
        {
            float speed = 0f;
            for (int i = 0; i < 500; i++)
            {
                speed = KartPhysicsMath.IntegrateSpeed(
                    speed, throttle: 1f, maxForwardSpeed: 24f, maxReverseSpeed: 10f,
                    acceleration: 18f, brakeDeceleration: 30f, engineBrakeDeceleration: 10f,
                    deltaTime: 0.02f);
            }

            Assert.That(speed, Is.EqualTo(24f).Within(0.001f));
        }

        [Test]
        public void IntegrateSpeed_NoThrottle_DeceleratesTowardsZero()
        {
            float speed = KartPhysicsMath.IntegrateSpeed(
                currentSpeed: 20f, throttle: 0f, maxForwardSpeed: 24f, maxReverseSpeed: 10f,
                acceleration: 18f, brakeDeceleration: 30f, engineBrakeDeceleration: 10f,
                deltaTime: 0.5f);

            Assert.That(speed, Is.EqualTo(15f).Within(0.01f));
        }

        [Test]
        public void IntegrateSpeed_BrakeFromRest_ReversesInsteadOfGoingNegativePastZero()
        {
            float speed = 0f;
            speed = KartPhysicsMath.IntegrateSpeed(
                speed, throttle: -1f, maxForwardSpeed: 24f, maxReverseSpeed: 10f,
                acceleration: 18f, brakeDeceleration: 30f, engineBrakeDeceleration: 10f,
                deltaTime: 0.1f);

            Assert.That(speed, Is.LessThan(0f));
            Assert.That(speed, Is.GreaterThanOrEqualTo(-10f));
        }

        [Test]
        public void IntegrateSpeed_BrakeWhileMovingForward_DecceleratesButDoesNotReverseImmediately()
        {
            float speed = KartPhysicsMath.IntegrateSpeed(
                currentSpeed: 5f, throttle: -1f, maxForwardSpeed: 24f, maxReverseSpeed: 10f,
                acceleration: 18f, brakeDeceleration: 30f, engineBrakeDeceleration: 10f,
                deltaTime: 0.02f);

            Assert.That(speed, Is.LessThan(5f));
            Assert.That(speed, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ComputeTurnRateDegPerSec_AtStandstill_IsScaledDownByLowSpeedFactor()
        {
            float turnRate = KartPhysicsMath.ComputeTurnRateDegPerSec(
                currentSpeed: 0f, maxForwardSpeed: 24f, baseTurnRateDegPerSec: 140f,
                minSpeedFactorForFullTurn: 0.25f, lowSpeedTurnFactor: 0.4f);

            Assert.That(turnRate, Is.EqualTo(140f * 0.4f).Within(0.01f));
        }

        [Test]
        public void ComputeTurnRateDegPerSec_AtOrAboveThreshold_ReachesFullBaseRate()
        {
            float turnRate = KartPhysicsMath.ComputeTurnRateDegPerSec(
                currentSpeed: 6f, maxForwardSpeed: 24f, baseTurnRateDegPerSec: 140f,
                minSpeedFactorForFullTurn: 0.25f, lowSpeedTurnFactor: 0.4f);

            Assert.That(turnRate, Is.EqualTo(140f).Within(0.01f));
        }

        [Test]
        public void ComputeDriftBoostSpeed_BelowMinimumHoldTime_GrantsNoBoost()
        {
            float boost = KartPhysicsMath.ComputeDriftBoostSpeed(
                driftHeldSeconds: 0.3f, minDriftSecondsForBoost: 0.6f,
                boostSpeedBonus: 6f, maxBoostSpeedBonus: 14f);

            Assert.That(boost, Is.EqualTo(0f));
        }

        [Test]
        public void ComputeDriftBoostSpeed_AtMinimumHoldTime_GrantsBaseBonus()
        {
            float boost = KartPhysicsMath.ComputeDriftBoostSpeed(
                driftHeldSeconds: 0.6f, minDriftSecondsForBoost: 0.6f,
                boostSpeedBonus: 6f, maxBoostSpeedBonus: 14f);

            Assert.That(boost, Is.EqualTo(6f).Within(0.01f));
        }

        [Test]
        public void ComputeDriftBoostSpeed_LongHold_IsClampedToMax()
        {
            float boost = KartPhysicsMath.ComputeDriftBoostSpeed(
                driftHeldSeconds: 10f, minDriftSecondsForBoost: 0.6f,
                boostSpeedBonus: 6f, maxBoostSpeedBonus: 14f);

            Assert.That(boost, Is.EqualTo(14f));
        }
    }
}
