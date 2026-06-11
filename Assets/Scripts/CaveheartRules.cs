namespace MyLittleCaveheart
{
    public static class CaveheartRules
    {
        public const int SitUpAwake = 6;
        public const int SitUpTrust = 4;
        public const int SitUpMaxStress = 4;
        public const int ResistStress = 7;
        public const int StartledStress = 4;
        public const int SettledTrust = 3;
        public const int SettledMaxStress = 3;
        public const int TouchStressGate = 6;
        public const int HelpfulBlanketUses = 2;
        public const int HelpfulGentleTouchUses = 2;
        public const int HelpfulWaterUses = 2;
        public const int HelpfulCurtainUses = 1;
        public const int HelpfulScratchUses = 1;
        private const string WaterCue = "He looks toward the cup.";
        private const string PlayCue = "One foot shifts toward your hand, as if he might play.";
        private const string WaterAndPlayCue = "His eyes move between the cup and your hand. One foot rests outside the blanket.";

        public static int GetTimeCost(CaveheartInteractionType interaction)
        {
            switch (interaction)
            {
                case CaveheartInteractionType.Alarm:
                case CaveheartInteractionType.ShakeBed:
                case CaveheartInteractionType.OpenCurtain:
                case CaveheartInteractionType.Scratch:
                    return 1;
                case CaveheartInteractionType.GentleTouch:
                case CaveheartInteractionType.OfferWater:
                case CaveheartInteractionType.TuckBlanket:
                    return 2;
                case CaveheartInteractionType.Wait:
                    return 1;
                default:
                    return 1;
            }
        }

        public static CaveheartInteractionResult Apply(CaveheartStats current, CaveheartState currentState, CaveheartInteractionType interaction)
        {
            return Apply(current, currentState, interaction, new CaveheartInteractionContext(0, 0, 0, 0));
        }

        public static CaveheartInteractionResult Apply(CaveheartStats current, CaveheartState currentState, CaveheartInteractionType interaction, int previousBlanketUses)
        {
            return Apply(current, currentState, interaction, new CaveheartInteractionContext(previousBlanketUses, 0, 0, 0));
        }

        public static CaveheartInteractionResult Apply(CaveheartStats current, CaveheartState currentState, CaveheartInteractionType interaction, CaveheartInteractionContext context)
        {
            var before = current.Clamped();
            var next = before;
            var accepted = true;
            var message = string.Empty;
            var reaction = string.Empty;

            switch (interaction)
            {
                case CaveheartInteractionType.Wait:
                    if (before.stress >= StartledStress)
                    {
                        next.stress -= 2;
                        if (context.lastInteractionWasForceful && context.waitStreak == 0)
                        {
                            next.trust += 1;
                        }

                        message = "You give him space.";
                        reaction = "His body stops bracing quite so hard.";
                    }
                    else if (context.lastInteractionWasRejected && context.waitStreak == 0)
                    {
                        next.stress -= 1;
                        message = "You accept his no.";
                        reaction = "The room settles.";
                    }
                    else
                    {
                        next.stress -= 1;
                        message = "You observe him.";
                        reaction = "You watch his breathing and posture.";
                    }
                    break;
                case CaveheartInteractionType.Alarm:
                    next.awake += 2;
                    next.stress += 3;
                    next.trust -= 1;
                    message = "The alarm rings.";
                    reaction = "He covers his ears.";
                    break;
                case CaveheartInteractionType.ShakeBed:
                    next.awake += 3;
                    next.stress += 4;
                    next.trust -= 2;
                    message = "The bed is shaken.";
                    reaction = "He pushes back.";
                    break;
                case CaveheartInteractionType.GentleTouch:
                    if (context.lastInteraction == CaveheartInteractionType.GentleTouch)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "The touch repeats too soon.";
                        reaction = "He moves his head away.";
                    }
                    else if (context.gentleTouchUses >= HelpfulGentleTouchUses)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "He has had enough touch for now.";
                        reaction = "He tucks his head away.";
                    }
                    else if (before.stress >= TouchStressGate)
                    {
                        accepted = false;
                        next.stress += 1;
                        next.trust -= 1;
                        message = "The touch is too soon.";
                        reaction = "He dodges your hand.";
                    }
                    else
                    {
                        next.trust += 1;
                        next.stress -= 1;
                        message = "You stroke his hair slowly.";
                        reaction = context.lastInteraction == CaveheartInteractionType.Wait
                            ? "He lets your hand rest there."
                            : "His breathing becomes steadier.";
                    }
                    break;
                case CaveheartInteractionType.OfferWater:
                    if (context.waterUses >= HelpfulWaterUses)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "He has had enough water.";
                        reaction = "He turns away.";
                    }
                    else if (context.lastInteraction == CaveheartInteractionType.OfferWater)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "Water is offered again too soon.";
                        reaction = "He keeps the cup away.";
                    }
                    else if (before.stress > StartledStress)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "He is too tense for water.";
                        reaction = "He turns away from the cup.";
                    }
                    else if (HasThirstCue(before, context.lastInteraction) && before.trust >= 2)
                    {
                        next.awake += 2;
                        next.trust += 1;
                        next.stress -= 1;
                        message = "You offer water.";
                        reaction = "He drinks a little.";
                    }
                    else if (HasThirstCue(before, context.lastInteraction))
                    {
                        accepted = false;
                        message = "You offer water.";
                        reaction = "He looks at the cup, but does not take it from you.";
                    }
                    else
                    {
                        accepted = false;
                        message = "You offer water.";
                        reaction = "His eyes do not follow the cup. He is not asking for water.";
                    }
                    break;
                case CaveheartInteractionType.OpenCurtain:
                    if (context.curtainUses >= HelpfulCurtainUses)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "The curtain moves again.";
                        reaction = "He squints.";
                    }
                    else if (context.lastInteraction == CaveheartInteractionType.Wait && before.stress <= SettledMaxStress)
                    {
                        next.awake += 3;
                        message = "You open the curtain slowly.";
                        reaction = "The light reaches him without making him pull away.";
                    }
                    else if (before.awake == 0)
                    {
                        next.awake += 2;
                        next.stress += 1;
                        message = "You open the curtain a little.";
                        reaction = "A thin line of light enters. He curls up, but does not panic.";
                    }
                    else
                    {
                        next.awake += 2;
                        next.stress += 1;
                        message = "You open the curtain.";
                        reaction = "He wakes, but the light catches him before he is ready.";
                    }
                    break;
                case CaveheartInteractionType.TuckBlanket:
                    if (context.blanketUses >= HelpfulBlanketUses)
                    {
                        if (before.stress >= ResistStress && context.lastInteraction == CaveheartInteractionType.Wait)
                        {
                            accepted = false;
                            next.stress -= 1;
                            message = "You leave the blanket nearby.";
                            reaction = "He calms a little.";
                        }
                        else
                        {
                            accepted = false;
                            next.stress += 1;
                            next.trust -= 1;
                            message = "That is too much blanket.";
                            reaction = "He kicks it away.";
                        }
                    }
                    else
                    {
                        next.stress -= context.blanketUses == 0 || currentState == CaveheartState.Resisting ? 2 : 1;
                        if (context.blanketUses == 0)
                        {
                            next.trust += 1;
                        }

                        message = context.blanketUses == 0
                            ? "You tuck the blanket."
                            : "The blanket helps a little.";
                        reaction = context.blanketUses == 0
                            ? "His shoulders loosen."
                            : "He settles, then shifts.";
                    }
                    break;
                case CaveheartInteractionType.Scratch:
                    if (context.scratchUses >= HelpfulScratchUses
                        || context.lastInteraction == CaveheartInteractionType.Scratch)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "The playful scratch is repeated.";
                        reaction = "He pulls his feet under the blanket.";
                    }
                    else if (before.awake == 0 || before.stress >= StartledStress)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "He is not ready to play.";
                        reaction = before.awake == 0
                            ? "His feet pull away without waking."
                            : "His body is too tight for a joke.";
                    }
                    else if (HasPlaySignal(before))
                    {
                        next.awake += 2;
                        next.trust += 1;
                        next.stress += 1;
                        message = "You lightly scratch the sole of his foot.";
                        reaction = "He laughs and nudges your hand before hiding his foot again.";
                    }
                    else
                    {
                        next.awake += 1;
                        next.stress += 1;
                        message = "You try a small playful scratch.";
                        reaction = "He wakes a little, but does not join the game.";
                    }
                    break;
            }

            next = next.Clamped();
            var state = EvaluateState(next, currentState);
            if (interaction == CaveheartInteractionType.Wait)
            {
                reaction = BuildObserveReport(before, next, currentState, context);
            }

            reaction = AddNextChoiceCue(reaction, next, interaction, context);
            return new CaveheartInteractionResult(interaction, before, next, state, accepted, message, reaction);
        }

        private static string AddNextChoiceCue(
            string reaction,
            CaveheartStats statsAfterAction,
            CaveheartInteractionType interaction,
            CaveheartInteractionContext context)
        {
            var canUseWater = CanUseStrongWaterNext(statsAfterAction, interaction, context);
            var canUseScratch = CanUseStrongScratchNext(statsAfterAction, interaction, context);
            if (!canUseWater && !canUseScratch)
            {
                return reaction;
            }

            var cue = canUseWater && canUseScratch
                ? WaterAndPlayCue
                : canUseWater
                    ? WaterCue
                    : PlayCue;
            var alreadyShowsWater = reaction.Contains("cup");
            var alreadyShowsScratch = reaction.Contains("foot")
                || reaction.Contains("feet")
                || reaction.Contains("play");
            if ((canUseWater && canUseScratch && alreadyShowsWater && alreadyShowsScratch)
                || (canUseWater && !canUseScratch && alreadyShowsWater)
                || (!canUseWater && canUseScratch && alreadyShowsScratch))
            {
                return reaction;
            }

            return string.IsNullOrEmpty(reaction) ? cue : reaction + "\n" + cue;
        }

        private static bool CanUseStrongWaterNext(
            CaveheartStats statsAfterAction,
            CaveheartInteractionType lastInteraction,
            CaveheartInteractionContext context)
        {
            return lastInteraction != CaveheartInteractionType.OfferWater
                && context.waterUses < HelpfulWaterUses
                && statsAfterAction.trust >= 2
                && HasThirstCue(statsAfterAction, lastInteraction);
        }

        private static bool CanUseStrongScratchNext(
            CaveheartStats statsAfterAction,
            CaveheartInteractionType lastInteraction,
            CaveheartInteractionContext context)
        {
            return lastInteraction != CaveheartInteractionType.Scratch
                && context.scratchUses < HelpfulScratchUses
                && HasPlaySignal(statsAfterAction);
        }

        private static string BuildObserveReport(
            CaveheartStats beforeObserve,
            CaveheartStats afterObserve,
            CaveheartState stateBeforeObserve,
            CaveheartInteractionContext context)
        {
            var report = GetBodyStageText(afterObserve) + "\n" + GetRelationshipText(afterObserve);
            if (IsFreshOpeningObserve(beforeObserve, stateBeforeObserve, context))
            {
                return report;
            }

            string cue;
            var canUseWater = CanUseStrongWaterNext(afterObserve, CaveheartInteractionType.Wait, context);
            var canUseScratch = CanUseStrongScratchNext(afterObserve, CaveheartInteractionType.Wait, context);
            if (canUseWater && canUseScratch)
            {
                cue = WaterAndPlayCue;
            }
            else if (canUseWater)
            {
                cue = "His gaze keeps returning to the cup.";
            }
            else if (context.lastInteractionWasForceful)
            {
                cue = "His feet stay tucked away, but he leaves his head within reach of a slow touch.";
            }
            else if (afterObserve.stress >= StartledStress)
            {
                cue = "His shoulders stay tight. More stimulation would crowd him.";
            }
            else if (canUseScratch)
            {
                cue = "His feet shift toward your hand, as if he might play.";
            }
            else if (context.curtainUses == 0 && afterObserve.awake < 3 && afterObserve.stress <= SettledMaxStress)
            {
                cue = "His body is calm, but his eyes are still heavy. His gaze does not settle on the cup.";
            }
            else if (afterObserve.trust < SettledTrust && afterObserve.stress <= SettledMaxStress)
            {
                cue = "He leaves his head within reach, but waits to see what you do.";
            }
            else
            {
                cue = string.Empty;
            }

            return string.IsNullOrEmpty(cue) ? report : report + "\n" + cue;
        }

        private static string GetBodyStageText(CaveheartStats stats)
        {
            if (stats.awake == 0)
            {
                return "Body: He is still in deep sleep.";
            }

            if (stats.awake <= 2)
            {
                return "Body: He is starting to wake, but his eyes are still heavy.";
            }

            if (stats.awake <= 5)
            {
                return "Body: He is half-awake and following the room.";
            }

            return "Body: He is awake enough to sit up.";
        }

        private static string GetRelationshipText(CaveheartStats stats)
        {
            if (stats.stress >= TouchStressGate)
            {
                return "Relationship: He is defensive and protecting himself.";
            }

            if (stats.stress >= StartledStress)
            {
                return "Relationship: He is guarded and watching for pressure.";
            }

            if (stats.trust >= 5)
            {
                return "Relationship: He is actively showing you what he needs.";
            }

            if (stats.trust >= SettledTrust)
            {
                return "Relationship: He is accepting your help.";
            }

            return "Relationship: He is hesitant, but still watching you.";
        }

        private static bool IsFreshOpeningObserve(CaveheartStats beforeObserve, CaveheartState stateBeforeObserve, CaveheartInteractionContext context)
        {
            return beforeObserve.awake == CaveheartStats.Starting.awake
                && beforeObserve.trust == CaveheartStats.Starting.trust
                && beforeObserve.stress == CaveheartStats.Starting.stress
                && stateBeforeObserve == CaveheartState.Sleeping
                && context.blanketUses == 0
                && context.gentleTouchUses == 0
                && context.waterUses == 0
                && context.curtainUses == 0
                && context.scratchUses == 0
                && context.waitStreak == 0
                && !context.lastInteractionWasForceful
                && !context.lastInteractionWasRejected;
        }

        private static bool HasThirstCue(CaveheartStats stats, CaveheartInteractionType lastInteraction)
        {
            return stats.awake >= 3
                && stats.stress <= SettledMaxStress
                && lastInteraction != CaveheartInteractionType.OpenCurtain;
        }

        private static bool HasPlaySignal(CaveheartStats stats)
        {
            return stats.awake >= 2
                && stats.awake <= 5
                && stats.stress <= 1;
        }

        public static CaveheartState EvaluateState(CaveheartStats stats, CaveheartState currentState = CaveheartState.Sleeping)
        {
            stats = stats.Clamped();

            if (stats.awake >= SitUpAwake && stats.trust >= SitUpTrust && stats.stress <= SitUpMaxStress)
            {
                return CaveheartState.SittingUp;
            }

            if (stats.stress >= ResistStress)
            {
                return CaveheartState.Resisting;
            }

            if (stats.trust >= SettledTrust && stats.stress <= SettledMaxStress)
            {
                return CaveheartState.Settled;
            }

            if (stats.stress >= StartledStress || stats.awake > 0)
            {
                return CaveheartState.Startled;
            }

            return currentState == CaveheartState.SittingUp ? CaveheartState.SittingUp : CaveheartState.Sleeping;
        }

    }
}
