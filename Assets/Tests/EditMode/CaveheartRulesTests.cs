using MyLittleCaveheart;
using NUnit.Framework;
using UnityEngine;

namespace MyLittleCaveheart.Tests
{
    public sealed class CaveheartRulesTests
    {
        [Test]
        public void UrgeWakesQuicklyButDamagesSafety()
        {
            var result = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.Alarm);

            Assert.AreEqual(2, result.after.awake);
            Assert.AreEqual(0, result.after.trust);
            Assert.AreEqual(4, result.after.stress);
            Assert.AreEqual(CaveheartState.Startled, result.state);
        }

        [Test]
        public void TouchBuildsSafetyWithoutWaking()
        {
            var result = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.GentleTouch);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(0, result.after.awake);
            Assert.AreEqual(2, result.after.trust);
            Assert.AreEqual(0, result.after.stress);
        }

        [Test]
        public void ConsecutiveTouchIsRejected()
        {
            var route = Run(
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.GentleTouch);

            Assert.IsFalse(route.lastResult.accepted);
            Assert.AreEqual(2, route.stats.trust);
            Assert.AreEqual(1, route.stats.stress);
            Assert.AreEqual(1, route.rejections);
        }

        [Test]
        public void ObserveSeparatedTouchCanBuildSafetyAgain()
        {
            var route = Run(
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch);

            Assert.IsTrue(route.lastResult.accepted);
            Assert.AreEqual(3, route.stats.trust);
            Assert.AreEqual(0, route.stats.stress);
        }

        [Test]
        public void AThirdAcceptedTouchIsNotAvailable()
        {
            var route = Run(
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch);

            Assert.IsFalse(route.lastResult.accepted);
            Assert.AreEqual(3, route.stats.trust);
            Assert.AreEqual(1, route.rejections);
        }

        [Test]
        public void OpeningWindowWakesWithoutChangingTrust()
        {
            var result = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.OpenCurtain);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(2, result.after.awake);
            Assert.AreEqual(1, result.after.trust);
            Assert.AreEqual(2, result.after.stress);
        }

        [Test]
        public void ObservedWindowUsesItsOneTimeOpportunityEfficiently()
        {
            var route = Run(
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.OpenCurtain);

            Assert.IsTrue(route.lastResult.accepted);
            Assert.AreEqual(3, route.stats.awake);
            Assert.AreEqual(2, route.stats.trust);
            Assert.AreEqual(0, route.stats.stress);
        }

        [Test]
        public void WindowCanOnlyBeUsedOnce()
        {
            var first = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.OpenCurtain);
            var second = CaveheartRules.Apply(
                first.after,
                first.state,
                CaveheartInteractionType.OpenCurtain,
                new CaveheartInteractionContext(
                    0,
                    0,
                    0,
                    1,
                    CaveheartInteractionType.OpenCurtain,
                    0,
                    false,
                    false));

            Assert.IsFalse(second.accepted);
            Assert.Greater(second.after.stress, first.after.stress);
        }

        [Test]
        public void WaterNeedsAVisibleBodyCue()
        {
            var early = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.OfferWater);
            var ready = CaveheartRules.Apply(
                new CaveheartStats(3, 2, 1),
                CaveheartState.Startled,
                CaveheartInteractionType.OfferWater,
                new CaveheartInteractionContext(
                    0,
                    1,
                    0,
                    1,
                    CaveheartInteractionType.GentleTouch,
                    0,
                    false,
                    false));

            Assert.IsFalse(early.accepted);
            Assert.AreEqual(CaveheartStats.Starting.awake, early.after.awake);
            Assert.AreEqual(CaveheartStats.Starting.trust, early.after.trust);
            Assert.AreEqual(CaveheartStats.Starting.stress, early.after.stress);

            Assert.IsTrue(ready.accepted);
            Assert.AreEqual(5, ready.after.awake);
            Assert.AreEqual(3, ready.after.trust);
            Assert.AreEqual(0, ready.after.stress);
        }

        [Test]
        public void WindowDoesNotImmediatelyCreateAWaterCue()
        {
            var route = Run(
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.OpenCurtain,
                CaveheartInteractionType.OfferWater);

            Assert.IsFalse(route.lastResult.accepted);
            StringAssert.Contains("not asking for water", route.lastResult.observedReaction);
        }

        [Test]
        public void DeepSleepScratchIsRejectedWithoutTrustLoss()
        {
            var result = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.Scratch);

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(0, result.after.awake);
            Assert.AreEqual(1, result.after.trust);
            Assert.AreEqual(2, result.after.stress);
        }

        [Test]
        public void PlaySignalScratchWakesAndBuildsTrustAtAStressCost()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(2, 1, 1),
                CaveheartState.Startled,
                CaveheartInteractionType.Scratch);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(4, result.after.awake);
            Assert.AreEqual(2, result.after.trust);
            Assert.AreEqual(2, result.after.stress);
        }

        [Test]
        public void HalfAwakeScratchWithoutPlaySignalOnlyWakes()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(2, 2, 2),
                CaveheartState.Startled,
                CaveheartInteractionType.Scratch);

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(3, result.after.awake);
            Assert.AreEqual(2, result.after.trust);
            Assert.AreEqual(3, result.after.stress);
        }

        [Test]
        public void RepeatedAcceptedScratchIsRejected()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(4, 2, 2),
                CaveheartState.Startled,
                CaveheartInteractionType.Scratch,
                new CaveheartInteractionContext(
                    0,
                    0,
                    0,
                    1,
                    CaveheartInteractionType.GentleTouch,
                    0,
                    false,
                    false,
                    1));

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(4, result.after.awake);
            Assert.AreEqual(2, result.after.trust);
            Assert.AreEqual(3, result.after.stress);
        }

        [Test]
        public void FirstObserveReportsStableBodyAndRelationshipStages()
        {
            var result = CaveheartRules.Apply(
                CaveheartStats.Starting,
                CaveheartState.Sleeping,
                CaveheartInteractionType.Wait);

            StringAssert.Contains("Body:", result.observedReaction);
            StringAssert.Contains("deep sleep", result.observedReaction);
            StringAssert.Contains("Relationship:", result.observedReaction);
            StringAssert.Contains("hesitant", result.observedReaction);
            StringAssert.DoesNotContain("clear signal", result.observedReaction);
        }

        [Test]
        public void ObserveAfterTouchDistinguishesSleepFromThirst()
        {
            var route = Run(
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait);

            StringAssert.Contains("eyes are still heavy", route.lastResult.observedReaction);
            StringAssert.Contains("does not settle on the cup", route.lastResult.observedReaction);
        }

        [Test]
        public void ObserveAfterWindowCanRevealPlaySignal()
        {
            var route = Run(
                CaveheartInteractionType.OpenCurtain,
                CaveheartInteractionType.Wait);

            StringAssert.Contains("feet shift toward your hand", route.lastResult.observedReaction);
        }

        [Test]
        public void ObserveShowsCupWhenWaterIsActuallyReady()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(3, 2, 1),
                CaveheartState.Startled,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(
                    0,
                    1,
                    0,
                    1,
                    CaveheartInteractionType.GentleTouch,
                    0,
                    false,
                    false,
                    1));

            StringAssert.Contains("gaze keeps returning to the cup", result.observedReaction);
            Assert.AreEqual(1, CountOccurrences(result.observedReaction, "cup"));
        }

        [Test]
        public void ObserveShowsBothSignalsWhenWaterAndPlayAreReasonable()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(3, 2, 1),
                CaveheartState.Startled,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(
                    0,
                    1,
                    0,
                    1,
                    CaveheartInteractionType.GentleTouch,
                    0,
                    false,
                    false));

            StringAssert.Contains("cup", result.observedReaction);
            StringAssert.Contains("foot", result.observedReaction);
            StringAssert.DoesNotContain("gaze keeps returning", result.observedReaction);
        }

        [Test]
        public void ObserveAtHighStressWarnsAboutMoreStimulation()
        {
            var result = CaveheartRules.Apply(
                new CaveheartStats(3, 1, 6),
                CaveheartState.Resisting,
                CaveheartInteractionType.Wait,
                new CaveheartInteractionContext(
                    0,
                    0,
                    0,
                    1,
                    CaveheartInteractionType.GentleTouch,
                    0,
                    false,
                    false));

            StringAssert.Contains("More stimulation would crowd him", result.observedReaction);
        }

        [Test]
        public void SafeRouteCanReachVeryGoodWithoutScratch()
        {
            var route = Run(
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.OpenCurtain,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.OfferWater,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.OfferWater);

            Assert.AreEqual(CaveheartState.SittingUp, route.state);
            Assert.AreEqual(0, route.rejections);
            Assert.AreEqual(0, route.forcefulUses);
            Assert.GreaterOrEqual(route.stats.trust, 5);
            Assert.LessOrEqual(route.stats.stress, 3);
            Assert.LessOrEqual(route.minutes, 12);
            Assert.AreEqual(0, route.acceptedScratchUses);
        }

        [Test]
        public void PlayRouteCanReachVeryGood()
        {
            var route = Run(
                CaveheartInteractionType.OpenCurtain,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.Scratch,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.OfferWater);

            Assert.AreEqual(CaveheartState.SittingUp, route.state);
            Assert.AreEqual(0, route.rejections);
            Assert.AreEqual(1, route.acceptedScratchUses);
            Assert.GreaterOrEqual(route.stats.trust, 5);
            Assert.LessOrEqual(route.stats.stress, 3);
            Assert.LessOrEqual(route.minutes, 12);
        }

        [Test]
        public void ObserveOpeningCanReachVeryGood()
        {
            var route = Run(
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.OpenCurtain,
                CaveheartInteractionType.Scratch,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.OfferWater);

            Assert.AreEqual(CaveheartState.SittingUp, route.state);
            Assert.AreEqual(0, route.rejections);
            Assert.AreEqual(0, route.forcefulUses);
            Assert.GreaterOrEqual(route.stats.trust, 5);
            Assert.LessOrEqual(route.stats.stress, 3);
            Assert.LessOrEqual(route.minutes, 12);
        }

        [Test]
        public void UrgeRouteCanRepairIntoAGoodMorning()
        {
            var route = Run(
                CaveheartInteractionType.Alarm,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.OpenCurtain,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.OfferWater);

            Assert.AreEqual(CaveheartState.SittingUp, route.state);
            Assert.AreEqual(1, route.forcefulUses);
            Assert.AreEqual(0, route.rejections);
            Assert.GreaterOrEqual(route.stats.trust, CaveheartRules.SitUpTrust);
            Assert.LessOrEqual(route.stats.stress, CaveheartRules.SitUpMaxStress);
            Assert.LessOrEqual(route.minutes, 14);
        }

        [Test]
        public void UrgeOpeningCanBeFullyRepaired()
        {
            var route = Run(
                CaveheartInteractionType.Alarm,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Scratch,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.OfferWater);

            Assert.AreEqual(CaveheartState.SittingUp, route.state);
            Assert.AreEqual(1, route.forcefulUses);
            Assert.AreEqual(0, route.rejections);
            Assert.GreaterOrEqual(route.stats.trust, 5);
            Assert.LessOrEqual(route.stats.stress, 2);
            Assert.LessOrEqual(route.minutes, 12);
        }

        [Test]
        public void RejectedOpeningScratchCanBeRepairedButNotCountAsVeryGood()
        {
            var route = Run(
                CaveheartInteractionType.Scratch,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.OpenCurtain,
                CaveheartInteractionType.Wait,
                CaveheartInteractionType.GentleTouch,
                CaveheartInteractionType.Scratch,
                CaveheartInteractionType.OfferWater);

            Assert.AreEqual(CaveheartState.SittingUp, route.state);
            Assert.AreEqual(1, route.rejections);
            Assert.AreEqual(1, route.acceptedScratchUses);
            Assert.GreaterOrEqual(route.stats.trust, CaveheartRules.SitUpTrust);
            Assert.LessOrEqual(route.stats.stress, CaveheartRules.SitUpMaxStress);
        }

        [Test]
        public void FastAndSlowActionsHaveDifferentTimeCosts()
        {
            Assert.AreEqual(1, CaveheartRules.GetTimeCost(CaveheartInteractionType.Alarm));
            Assert.AreEqual(1, CaveheartRules.GetTimeCost(CaveheartInteractionType.OpenCurtain));
            Assert.AreEqual(1, CaveheartRules.GetTimeCost(CaveheartInteractionType.Scratch));
            Assert.AreEqual(1, CaveheartRules.GetTimeCost(CaveheartInteractionType.Wait));
            Assert.AreEqual(2, CaveheartRules.GetTimeCost(CaveheartInteractionType.GentleTouch));
            Assert.AreEqual(2, CaveheartRules.GetTimeCost(CaveheartInteractionType.OfferWater));
        }

        [Test]
        public void TimeWindowClosesWithoutFailureLanguage()
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
                DestroyIfPresent("Whitebox UI");
                DestroyIfPresent("EventSystem");
            }
        }

        [Test]
        public void AcceptedUseLimitsDisableFurtherWorldInteraction()
        {
            var gameObject = new GameObject("Caveheart availability test controller");
            try
            {
                var controller = gameObject.AddComponent<CaveheartGameController>();
                controller.Interact(CaveheartInteractionType.GentleTouch);
                controller.Interact(CaveheartInteractionType.Wait);
                controller.Interact(CaveheartInteractionType.GentleTouch);

                Assert.IsFalse(controller.IsInteractionAvailable(CaveheartInteractionType.GentleTouch));
                var minutesBeforeUnavailableClick = controller.UsedMorningMinutes;
                var unavailable = controller.Interact(CaveheartInteractionType.GentleTouch);

                Assert.IsFalse(unavailable.accepted);
                Assert.AreEqual(minutesBeforeUnavailableClick, controller.UsedMorningMinutes);
                StringAssert.Contains("does not want more touch", unavailable.message);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                DestroyIfPresent("Whitebox UI");
                DestroyIfPresent("EventSystem");
            }
        }

        private static RouteState Run(params CaveheartInteractionType[] actions)
        {
            var route = new RouteState();
            for (var i = 0; i < actions.Length; i++)
            {
                route.Apply(actions[i]);
                if (route.state == CaveheartState.SittingUp)
                {
                    break;
                }
            }

            return route;
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static void DestroyIfPresent(string objectName)
        {
            var obj = GameObject.Find(objectName);
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        private sealed class RouteState
        {
            public CaveheartStats stats = CaveheartStats.Starting;
            public CaveheartState state = CaveheartState.Sleeping;
            public CaveheartInteractionResult lastResult;
            public int minutes;
            public int rejections;
            public int forcefulUses;
            public int acceptedScratchUses;

            private int blanketUses;
            private int touchUses;
            private int waterUses;
            private int curtainUses;
            private CaveheartInteractionType lastInteraction = CaveheartInteractionType.Wait;
            private int waitStreak;
            private bool lastWasForceful;
            private bool lastWasRejected;

            public void Apply(CaveheartInteractionType interaction)
            {
                var context = new CaveheartInteractionContext(
                    blanketUses,
                    touchUses,
                    waterUses,
                    curtainUses,
                    lastInteraction,
                    waitStreak,
                    lastWasForceful,
                    lastWasRejected,
                    acceptedScratchUses);
                lastResult = CaveheartRules.Apply(stats, state, interaction, context);
                stats = lastResult.after;
                state = lastResult.state;
                minutes += CaveheartRules.GetTimeCost(interaction);

                if (!lastResult.accepted)
                {
                    rejections++;
                }

                if (interaction == CaveheartInteractionType.Alarm
                    || interaction == CaveheartInteractionType.ShakeBed)
                {
                    forcefulUses++;
                }

                if (lastResult.accepted)
                {
                    switch (interaction)
                    {
                        case CaveheartInteractionType.TuckBlanket:
                            blanketUses++;
                            break;
                        case CaveheartInteractionType.GentleTouch:
                            touchUses++;
                            break;
                        case CaveheartInteractionType.OfferWater:
                            waterUses++;
                            break;
                        case CaveheartInteractionType.OpenCurtain:
                            curtainUses++;
                            break;
                        case CaveheartInteractionType.Scratch:
                            acceptedScratchUses++;
                            break;
                    }
                }

                lastWasForceful = interaction == CaveheartInteractionType.Alarm
                    || interaction == CaveheartInteractionType.ShakeBed;
                lastWasRejected = !lastResult.accepted;
                waitStreak = interaction == CaveheartInteractionType.Wait ? waitStreak + 1 : 0;
                lastInteraction = interaction;
            }
        }
    }
}
