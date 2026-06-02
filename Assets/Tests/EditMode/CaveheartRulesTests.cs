using MyLittleCaveheart;
using NUnit.Framework;
using UnityEngine;

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
        public void GentleSequenceCanSitUpWithoutAForcedWindowWaterCombo()
        {
            var stats = CaveheartStats.Starting;
            var state = CaveheartState.Sleeping;

            var touch = CaveheartRules.Apply(stats, state, CaveheartInteractionType.GentleTouch, new CaveheartInteractionContext(0, 0, 0, 0));
            var observe = CaveheartRules.Apply(touch.after, touch.state, CaveheartInteractionType.Wait, new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));
            var secondTouch = CaveheartRules.Apply(observe.after, observe.state, CaveheartInteractionType.GentleTouch, new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.Wait, 1, false, false));
            var window = CaveheartRules.Apply(secondTouch.after, secondTouch.state, CaveheartInteractionType.OpenCurtain, new CaveheartInteractionContext(0, 2, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));
            var earlyScratch = CaveheartRules.Apply(window.after, window.state, CaveheartInteractionType.Scratch, new CaveheartInteractionContext(0, 2, 0, 1, CaveheartInteractionType.OpenCurtain, 0, false, false));
            var repair = CaveheartRules.Apply(earlyScratch.after, earlyScratch.state, CaveheartInteractionType.Wait, new CaveheartInteractionContext(0, 2, 0, 1, CaveheartInteractionType.Scratch, 0, false, true, 1));
            var scratch = CaveheartRules.Apply(repair.after, repair.state, CaveheartInteractionType.Scratch, new CaveheartInteractionContext(0, 2, 0, 1, CaveheartInteractionType.Wait, 1, false, false, 1));
            var water = CaveheartRules.Apply(scratch.after, scratch.state, CaveheartInteractionType.OfferWater, new CaveheartInteractionContext(0, 2, 0, 1, CaveheartInteractionType.Scratch, 0, false, false, 2));

            Assert.GreaterOrEqual(water.after.awake, CaveheartRules.SitUpAwake);
            Assert.GreaterOrEqual(water.after.trust, CaveheartRules.SitUpTrust);
            Assert.LessOrEqual(water.after.stress, CaveheartRules.SitUpMaxStress);
            Assert.AreEqual(CaveheartState.SittingUp, water.state);
        }

        [Test]
        public void WaterIsRejectedWhenTrustIsTooLow()
        {
            var result = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.OfferWater);

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(0, result.after.awake);
            Assert.AreEqual(1, result.after.trust);
            Assert.AreEqual(1, result.after.stress);
        }

        [Test]
        public void FirstObserveStaysAmbiguousAndDoesNotPointToWater()
        {
            var observed = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.Wait);

            StringAssert.DoesNotContain("dry", observed.observedReaction);
            StringAssert.DoesNotContain("cup", observed.observedReaction);
            StringAssert.Contains("clear signal", observed.observedReaction);
        }

        [Test]
        public void ObserveAvoidHintNeverPointsToUrgeOrNeed()
        {
            var firstTouch = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.GentleTouch);
            var observed = CaveheartRules.Apply(
                firstTouch.after,
                firstTouch.state,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));

            StringAssert.Contains("light", observed.observedReaction);
            StringAssert.DoesNotContain("clear signal", observed.observedReaction);
            StringAssert.DoesNotContain("Urge", observed.observedReaction);
            StringAssert.DoesNotContain("Need", observed.observedReaction);
        }

        [Test]
        public void ObserveBestHintCanPointToAUsefulNextAction()
        {
            var observed = CaveheartRules.Apply(
                new CaveheartStats(4, 3, 1),
                CaveheartState.Settled,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(2, 2, 0, 1, CaveheartInteractionType.GentleTouch, 0, false, false));

            StringAssert.Contains("cup", observed.observedReaction);
            StringAssert.DoesNotContain("clear signal", observed.observedReaction);
            StringAssert.DoesNotContain("Need", observed.observedReaction);
        }

        [Test]
        public void ObserveShowsCupWhenBodyIsReadyToDrink()
        {
            var observed = CaveheartRules.Apply(
                new CaveheartStats(4, 3, 1),
                CaveheartState.Settled,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.Scratch, 0, false, false));

            Assert.AreEqual("He looks toward the cup.", observed.observedReaction);
        }

        [Test]
        public void AnyActionShowsCupWhenStrongWaterIsAvailableNext()
        {
            var scratch = CaveheartRules.Apply(
                new CaveheartStats(3, 3, 1),
                CaveheartState.Settled,
                CaveheartInteractionType.Scratch,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.Wait, 1, false, false, 1));

            StringAssert.Contains("He looks toward the cup.", scratch.observedReaction);
        }

        [Test]
        public void WindowWakingBodyDoesNotShowCupCueImmediately()
        {
            var window = CaveheartRules.Apply(
                new CaveheartStats(3, 3, 1),
                CaveheartState.Settled,
                CaveheartInteractionType.OpenCurtain,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.Wait, 1, false, false));

            StringAssert.DoesNotContain("cup", window.observedReaction);
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
        public void ConsecutiveTouchNeedsSpaceInsteadOfBuildingTrustAgain()
        {
            var firstTouch = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.GentleTouch);
            var secondTouch = CaveheartRules.Apply(
                firstTouch.after,
                firstTouch.state,
                CaveheartInteractionType.GentleTouch,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));

            Assert.IsTrue(firstTouch.accepted);
            Assert.IsFalse(secondTouch.accepted);
            Assert.AreEqual(firstTouch.after.trust, secondTouch.after.trust);
            Assert.Greater(secondTouch.after.stress, firstTouch.after.stress);
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
        public void RespectfulWaterOfferDoesNotBuildTrustWithoutClearBodyCue()
        {
            var settledButUnsure = new CaveheartStats(1, 3, 1);

            var unobserved = CaveheartRules.Apply(
                settledButUnsure,
                CaveheartState.Settled,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(2, 2, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));
            var observed = CaveheartRules.Apply(
                settledButUnsure,
                CaveheartState.Settled,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(2, 2, 0, 0, CaveheartInteractionType.Wait, 1, false, false));

            Assert.IsFalse(unobserved.accepted);
            Assert.AreEqual(3, unobserved.after.trust);
            Assert.AreEqual(1, unobserved.after.stress);
            Assert.IsFalse(observed.accepted);
            Assert.AreEqual(3, observed.after.trust);
            Assert.AreEqual(1, observed.after.stress);
        }

        [Test]
        public void GenericObserveDoesNotMakeWaterAThirstCue()
        {
            var observedButNotThirsty = CaveheartRules.Apply(
                new CaveheartStats(3, 3, 1),
                CaveheartState.Settled,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(0, 0, 0, 0, CaveheartInteractionType.Wait, 1, false, false));

            Assert.IsFalse(observedButNotThirsty.accepted);
            Assert.AreEqual(3, observedButNotThirsty.after.awake);
            Assert.AreEqual(3, observedButNotThirsty.after.trust);
            Assert.AreEqual(1, observedButNotThirsty.after.stress);
        }

        [Test]
        public void RejectedWaterThenObserveDoesNotBuildTrust()
        {
            var water = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.OfferWater);
            var observe = CaveheartRules.Apply(
                water.after,
                water.state,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(0, 0, 1, 0, CaveheartInteractionType.OfferWater, 0, false, true));

            Assert.IsFalse(water.accepted);
            Assert.AreEqual(water.after.trust, observe.after.trust);
            Assert.Less(observe.after.stress, water.after.stress);
        }

        [Test]
        public void SettledWaterCanWakeALittleWithoutBuildingTrust()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(0, 3, 0),
                CaveheartState.Settled,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(0, 2, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(1, result.after.awake);
            Assert.AreEqual(3, result.after.trust);
            Assert.AreEqual(0, result.after.stress);
        }

        [Test]
        public void ConsecutiveWaterDoesNotKeepBuildingTrust()
        {
            var firstWater = CaveheartRules.Apply(
                new CaveheartStats(0, 2, 0),
                CaveheartState.Sleeping,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));
            var secondWater = CaveheartRules.Apply(
                firstWater.after,
                firstWater.state,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(0, 1, 1, 0, CaveheartInteractionType.OfferWater, 0, false, true));

            Assert.IsFalse(firstWater.accepted);
            Assert.IsFalse(secondWater.accepted);
            Assert.AreEqual(firstWater.after.trust, secondWater.after.trust);
            Assert.Greater(secondWater.after.stress, firstWater.after.stress);
        }

        [Test]
        public void SlowCurtainWakesWithoutBecomingATrustCombo()
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
            Assert.AreEqual(3, firstCurtain.after.trust);
            Assert.AreEqual(4, firstCurtain.after.awake);
            Assert.IsFalse(repeatedCurtain.accepted);
            Assert.Greater(repeatedCurtain.after.stress, firstCurtain.after.stress);
        }

        [Test]
        public void WindowDoesNotAutomaticallyMakeWaterTheBestNextMove()
        {
            var settled = new CaveheartStats(2, 3, 2);
            var curtain = CaveheartRules.Apply(
                settled,
                CaveheartState.Settled,
                CaveheartInteractionType.OpenCurtain,
                new CaveheartInteractionContext(0, 0, 0, 0));
            var water = CaveheartRules.Apply(
                curtain.after,
                curtain.state,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(0, 0, 0, 1, CaveheartInteractionType.OpenCurtain, 0, false, false));

            Assert.IsFalse(water.accepted);
            Assert.AreEqual(curtain.after.trust, water.after.trust);
            Assert.AreEqual(curtain.after.awake, water.after.awake);
        }

        [Test]
        public void OpeningWindowWakesLightlyWithoutTrustLoss()
        {
            var result = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.OpenCurtain);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(1, result.after.awake);
            Assert.AreEqual(1, result.after.trust);
            Assert.AreEqual(2, result.after.stress);
            StringAssert.Contains("thin line of light", result.observedReaction);
        }

        [Test]
        public void OpeningScratchWakesStronglyButCostsTrust()
        {
            var result = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.Scratch);

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(2, result.after.awake);
            Assert.AreEqual(0, result.after.trust);
            Assert.AreEqual(2, result.after.stress);
            Assert.AreEqual(CaveheartState.Startled, result.state);
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
        public void ObservedEarlyScratchCanBuildTrustAtHigherStressCost()
        {
            var window = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.OpenCurtain);
            var observe = CaveheartRules.Apply(
                window.after,
                window.state,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(0, 0, 0, 1, CaveheartInteractionType.OpenCurtain, 0, false, false));
            var scratch = CaveheartRules.Apply(
                observe.after,
                observe.state,
                CaveheartInteractionType.Scratch,
                new CaveheartInteractionContext(0, 0, 0, 1, CaveheartInteractionType.Wait, 1, false, false));

            Assert.IsTrue(scratch.accepted);
            Assert.AreEqual(observe.after.awake + 1, scratch.after.awake);
            Assert.AreEqual(observe.after.trust + 2, scratch.after.trust);
            Assert.AreEqual(observe.after.stress + 2, scratch.after.stress);
        }

        [Test]
        public void EarlyScratchWithoutObservationStillCostsTrust()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(1, 1, 1),
                CaveheartState.Startled,
                CaveheartInteractionType.Scratch,
                new CaveheartInteractionContext(0, 0, 0, 0, CaveheartInteractionType.OpenCurtain, 0, false, false));

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(2, result.after.awake);
            Assert.AreEqual(0, result.after.trust);
            Assert.AreEqual(2, result.after.stress);
        }

        [Test]
        public void SettledScratchBeforeAwakeIsAcceptedButDoesNotBuildTrust()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(0, 3, 0),
                CaveheartState.Settled,
                CaveheartInteractionType.Scratch);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(1, result.after.awake);
            Assert.AreEqual(3, result.after.trust);
            Assert.AreEqual(1, result.after.stress);
        }

        [Test]
        public void ScratchAfterAwakeFeelsPlayfulAndBuildsTrust()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(4, 3, 2),
                CaveheartState.Startled,
                CaveheartInteractionType.Scratch,
                new CaveheartInteractionContext(0, 0, 0, 0, CaveheartInteractionType.Wait, 1, false, false));

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(6, result.after.awake);
            Assert.AreEqual(4, result.after.trust);
            Assert.AreEqual(3, result.after.stress);
        }

        [Test]
        public void ScratchAtHighTrustStillAddsStress()
        {
            var result = CaveheartRules.Apply(new CaveheartStats(4, 5, 2), CaveheartState.Settled, CaveheartInteractionType.Scratch);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(6, result.after.awake);
            Assert.AreEqual(5, result.after.trust);
            Assert.AreEqual(3, result.after.stress);
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

        [Test]
        public void PreparedWindowUsesStateOrObservation()
        {
            var settled = new CaveheartStats(2, 3, 2);
            var fromSettled = CaveheartRules.Apply(settled, CaveheartState.Settled, CaveheartInteractionType.OpenCurtain);
            var afterObserve = CaveheartRules.Apply(
                settled,
                CaveheartState.Startled,
                CaveheartInteractionType.OpenCurtain,
                new CaveheartInteractionContext(0, 0, 0, 0, CaveheartInteractionType.Wait, 1, false, false));

            Assert.IsTrue(fromSettled.accepted);
            Assert.IsTrue(afterObserve.accepted);
            Assert.AreEqual(4, fromSettled.after.awake);
            Assert.AreEqual(4, afterObserve.after.awake);
        }

        [Test]
        public void ImmediateDoubleTouchCanBeRecoveredByObservation()
        {
            var firstTouch = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.GentleTouch);
            var secondTouch = CaveheartRules.Apply(
                firstTouch.after,
                firstTouch.state,
                CaveheartInteractionType.GentleTouch,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));
            var repair = CaveheartRules.Apply(
                secondTouch.after,
                secondTouch.state,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(0, 2, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, true));

            Assert.IsFalse(secondTouch.accepted);
            Assert.Greater(repair.after.trust, secondTouch.after.trust);
            Assert.Less(repair.after.stress, secondTouch.after.stress);
        }

        [Test]
        public void ObserveSeparatedTouchCanRebuildTrustAfterUrge()
        {
            var urge = CaveheartRules.Apply(CaveheartStats.Starting, CaveheartState.Sleeping, CaveheartInteractionType.Alarm);
            var firstTouch = CaveheartRules.Apply(
                urge.after,
                urge.state,
                CaveheartInteractionType.GentleTouch,
                new CaveheartInteractionContext(0, 0, 0, 0, CaveheartInteractionType.Alarm, 0, true, false));
            var observe = CaveheartRules.Apply(
                firstTouch.after,
                firstTouch.state,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));
            var secondTouch = CaveheartRules.Apply(
                observe.after,
                observe.state,
                CaveheartInteractionType.GentleTouch,
                new CaveheartInteractionContext(0, 1, 0, 0, CaveheartInteractionType.Wait, 1, false, false));
            var secondObserve = CaveheartRules.Apply(
                secondTouch.after,
                secondTouch.state,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(0, 2, 0, 0, CaveheartInteractionType.GentleTouch, 0, false, false));
            var repairedTouch = CaveheartRules.Apply(
                secondObserve.after,
                secondObserve.state,
                CaveheartInteractionType.GentleTouch,
                new CaveheartInteractionContext(0, 2, 0, 0, CaveheartInteractionType.Wait, 1, false, false));

            Assert.IsTrue(repairedTouch.accepted);
            Assert.Greater(repairedTouch.after.trust, secondObserve.after.trust);
            Assert.LessOrEqual(repairedTouch.after.trust, CaveheartRules.SitUpTrust);
        }

        [Test]
        public void ObserveSeparatedTouchDoesNotBuildTrustPastSitUpThreshold()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(4, CaveheartRules.SitUpTrust, 1),
                CaveheartState.Settled,
                CaveheartInteractionType.GentleTouch,
                new CaveheartInteractionContext(0, 3, 0, 0, CaveheartInteractionType.Wait, 1, false, false));

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(CaveheartRules.SitUpTrust - 1, result.after.trust);
            Assert.Greater(result.after.stress, 1);
        }

        [Test]
        public void TimeWindowClosesAsTodayStopsHereText()
        {
            var gameObject = new GameObject("Caveheart test controller");
            try
            {
                var controller = gameObject.AddComponent<CaveheartGameController>();
                for (var i = 0; i < 13; i++)
                {
                    controller.Interact(CaveheartInteractionType.Wait);
                }

                var result = controller.Interact(CaveheartInteractionType.OfferWater);

                Assert.IsTrue(controller.HasMorningClosed);
                Assert.IsFalse(result.accepted);
                StringAssert.Contains("Today can stop here", result.observedReaction);
                StringAssert.DoesNotContain("fail", result.observedReaction.ToLowerInvariant());
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                var whiteboxUi = GameObject.Find("Whitebox UI");
                if (whiteboxUi != null)
                {
                    Object.DestroyImmediate(whiteboxUi);
                }

                var eventSystem = GameObject.Find("EventSystem");
                if (eventSystem != null)
                {
                    Object.DestroyImmediate(eventSystem);
                }
            }
        }
    }
}
