using MyLittleCaveheart;
using NUnit.Framework;

namespace MyLittleCaveheart.Tests
{
    public sealed class CaveheartRulesTests
    {
        [Test]
        public void AlarmCreatesShortTermAwakeButHurtsSafety()
        {
            var result = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.Alarm);

            Assert.AreEqual(2, result.after.awake);
            Assert.AreEqual(0, result.after.trust);
            Assert.AreEqual(4, result.after.stress);
            Assert.AreEqual(CaveheartState.Startled, result.state);
        }

        [Test]
        public void RepeatedForcePushesIntoResistance()
        {
            var stats = CaveheartStats.Starting;
            var state = CaveheartState.Sleeping;

            var alarm = CaveheartRules.Apply(stats, state, CaveheartInteractionType.Alarm);
            var shake = CaveheartRules.Apply(alarm.after, alarm.state, CaveheartInteractionType.ShakeBed);
            var secondShake = CaveheartRules.Apply(shake.after, shake.state, CaveheartInteractionType.ShakeBed);

            Assert.GreaterOrEqual(secondShake.after.awake, 6);
            Assert.LessOrEqual(secondShake.after.trust, 0);
            Assert.GreaterOrEqual(secondShake.after.stress, CaveheartRules.ResistStress);
            Assert.AreEqual(CaveheartState.Resisting, secondShake.state);
        }

        [Test]
        public void GentleSequenceBuildsTrustBeforeSittingUp()
        {
            var stats = CaveheartStats.Starting;
            var state = CaveheartState.Sleeping;

            var tuck = CaveheartRules.Apply(stats, state, CaveheartInteractionType.TuckBlanket, new CaveheartInteractionContext(0, 0, 0, 0));
            var secondTuck = CaveheartRules.Apply(tuck.after, tuck.state, CaveheartInteractionType.TuckBlanket, new CaveheartInteractionContext(1, 0, 0, 0));
            var touch = CaveheartRules.Apply(secondTuck.after, secondTuck.state, CaveheartInteractionType.GentleTouch, new CaveheartInteractionContext(2, 0, 0, 0));
            var secondTouch = CaveheartRules.Apply(touch.after, touch.state, CaveheartInteractionType.GentleTouch, new CaveheartInteractionContext(2, 1, 0, 0));
            var water = CaveheartRules.Apply(secondTouch.after, secondTouch.state, CaveheartInteractionType.OfferWater, new CaveheartInteractionContext(2, 2, 0, 0));
            var light = CaveheartRules.Apply(water.after, water.state, CaveheartInteractionType.OpenCurtain, new CaveheartInteractionContext(2, 2, 1, 0));
            var finalAlarm = CaveheartRules.Apply(light.after, light.state, CaveheartInteractionType.Alarm, new CaveheartInteractionContext(2, 2, 1, 1));

            Assert.GreaterOrEqual(finalAlarm.after.awake, CaveheartRules.SitUpAwake);
            Assert.GreaterOrEqual(finalAlarm.after.trust, CaveheartRules.SitUpTrust);
            Assert.LessOrEqual(finalAlarm.after.stress, CaveheartRules.SitUpMaxStress);
            Assert.AreEqual(CaveheartState.SittingUp, finalAlarm.state);
        }

        [Test]
        public void WaterIsRejectedWhenTrustIsTooLow()
        {
            var result = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.OfferWater);

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(0, result.after.awake);
            Assert.AreEqual(2, result.after.trust);
            Assert.AreEqual(1, result.after.stress);
        }

        [Test]
        public void TouchIsRejectedWhenStressIsTooHigh()
        {
            var result = CaveheartRules.Apply(new CaveheartStats(4, 2, 7), CaveheartState.Resisting, CaveheartInteractionType.GentleTouch);

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(8, result.after.stress);
            Assert.AreEqual(CaveheartState.Resisting, result.state);
        }

        [Test]
        public void BlanketStopsHelpingAndHurtsTrustAfterTwoUses()
        {
            var first = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.TuckBlanket, 0);
            var second = CaveheartRules.Apply(first.after, first.state, CaveheartInteractionType.TuckBlanket, 1);
            var overheated = CaveheartRules.Apply(second.after, second.state, CaveheartInteractionType.TuckBlanket, 2);

            Assert.IsTrue(first.accepted);
            Assert.IsTrue(second.accepted);
            Assert.IsFalse(overheated.accepted);
            Assert.Less(overheated.after.trust, second.after.trust);
            Assert.Greater(overheated.after.stress, second.after.stress);
        }

        [Test]
        public void RepeatingGentleActionsDoesNotBecomeAFreeWin()
        {
            var stats = CaveheartStats.Starting;
            var state = CaveheartState.Sleeping;

            var firstTouch = CaveheartRules.Apply(stats, state, CaveheartInteractionType.GentleTouch, new CaveheartInteractionContext(0, 0, 0, 0));
            var secondTouch = CaveheartRules.Apply(firstTouch.after, firstTouch.state, CaveheartInteractionType.GentleTouch, new CaveheartInteractionContext(0, 1, 0, 0));
            var thirdTouch = CaveheartRules.Apply(secondTouch.after, secondTouch.state, CaveheartInteractionType.GentleTouch, new CaveheartInteractionContext(0, 2, 0, 0));
            var firstBlanket = CaveheartRules.Apply(thirdTouch.after, thirdTouch.state, CaveheartInteractionType.TuckBlanket, new CaveheartInteractionContext(0, 3, 0, 0));
            var secondBlanket = CaveheartRules.Apply(firstBlanket.after, firstBlanket.state, CaveheartInteractionType.TuckBlanket, new CaveheartInteractionContext(1, 3, 0, 0));
            var thirdBlanket = CaveheartRules.Apply(secondBlanket.after, secondBlanket.state, CaveheartInteractionType.TuckBlanket, new CaveheartInteractionContext(2, 3, 0, 0));

            Assert.AreNotEqual(CaveheartState.SittingUp, thirdBlanket.state);
            Assert.Less(thirdBlanket.after.trust, secondBlanket.after.trust);
            Assert.Greater(thirdBlanket.after.stress, secondBlanket.after.stress);
        }

        [Test]
        public void RespectfulLowPressureWaterOfferCanBuildTrustWithoutForcingAcceptance()
        {
            var settledButUnsure = new CaveheartStats(1, 3, 1);

            var result = CaveheartRules.Apply(
                settledButUnsure,
                CaveheartState.Settled,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(2, 2, 0, 0));

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(4, result.after.trust);
            Assert.AreEqual(1, result.after.stress);
        }

        [Test]
        public void SlowCurtainAfterSettlingBuildsTrustButDoesNotRepeat()
        {
            var settled = new CaveheartStats(2, 3, 2);

            var firstCurtain = CaveheartRules.Apply(
                settled,
                CaveheartState.Settled,
                CaveheartInteractionType.OpenCurtain,
                new CaveheartInteractionContext(2, 2, 0, 0));
            var repeatedCurtain = CaveheartRules.Apply(
                firstCurtain.after,
                firstCurtain.state,
                CaveheartInteractionType.OpenCurtain,
                new CaveheartInteractionContext(2, 2, 0, 1));

            Assert.IsTrue(firstCurtain.accepted);
            Assert.AreEqual(4, firstCurtain.after.trust);
            Assert.IsFalse(repeatedCurtain.accepted);
            Assert.Greater(repeatedCurtain.after.stress, firstCurtain.after.stress);
        }

        [Test]
        public void ScratchTooEarlyWakesButCostsTrust()
        {
            var result = CaveheartRules.Apply(new CaveheartStats(1, 3, 2), CaveheartState.Startled, CaveheartInteractionType.Scratch);

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(2, result.after.awake);
            Assert.AreEqual(2, result.after.trust);
            Assert.AreEqual(3, result.after.stress);
        }

        [Test]
        public void ScratchAfterAwakeFeelsPlayfulAndBuildsTrust()
        {
            var result = CaveheartRules.Apply(new CaveheartStats(4, 3, 2), CaveheartState.Startled, CaveheartInteractionType.Scratch);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(5, result.after.awake);
            Assert.AreEqual(4, result.after.trust);
            Assert.AreEqual(1, result.after.stress);
        }

        [Test]
        public void RepeatingScratchEventuallyFeelsLikePressure()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(5, 4, 1),
                CaveheartState.Settled,
                CaveheartInteractionType.Scratch,
                new CaveheartInteractionContext(1, 1, 1, 1, CaveheartInteractionType.Scratch, 0, false, false, 2));

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(6, result.after.awake);
            Assert.AreEqual(3, result.after.trust);
            Assert.AreEqual(2, result.after.stress);
        }

        [Test]
        public void FastAndSlowActionsHaveDifferentTimeCosts()
        {
            Assert.AreEqual(1, CaveheartRules.GetTimeCost(CaveheartInteractionType.Alarm));
            Assert.AreEqual(1, CaveheartRules.GetTimeCost(CaveheartInteractionType.Scratch));
            Assert.AreEqual(1, CaveheartRules.GetTimeCost(CaveheartInteractionType.Wait));
            Assert.AreEqual(2, CaveheartRules.GetTimeCost(CaveheartInteractionType.OfferWater));
        }
    }
}
