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
        public const int WaterTrustGate = 4;
        public const int TouchStressGate = 6;
        public const int HelpfulBlanketUses = 2;
        public const int HelpfulGentleTouchUses = 2;
        public const int HelpfulWaterUses = 3;
        public const int HelpfulCurtainUses = 1;
        public const int HelpfulScratchUses = 2;
        public const int ScratchAwakeTrustGate = 3;
        private const int StrongBestHintPercent = 30;

        private static readonly CaveheartInteractionType[] ObserveHintCandidates =
        {
            CaveheartInteractionType.GentleTouch,
            CaveheartInteractionType.OfferWater,
            CaveheartInteractionType.OpenCurtain,
            CaveheartInteractionType.Scratch
        };

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
            return Apply(current, currentState, interaction, context, true);
        }

        private static CaveheartInteractionResult Apply(
            CaveheartStats current,
            CaveheartState currentState,
            CaveheartInteractionType interaction,
            CaveheartInteractionContext context,
            bool includeObserveHint)
        {
            var before = current.Clamped();
            var next = before;
            var accepted = true;
            var message = string.Empty;
            var reaction = string.Empty;

            switch (interaction)
            {
                case CaveheartInteractionType.Wait:
                    if (before.stress >= ResistStress)
                    {
                        next.stress -= 2;
                        if ((context.lastInteractionWasForceful || context.lastInteractionWasRejected) && context.waitStreak == 0)
                        {
                            next.trust += 1;
                        }

                        message = "You give him space.";
                        reaction = "His shaking slows.";
                    }
                    else if (before.stress >= StartledStress)
                    {
                        next.stress -= 2;
                        if (context.waitStreak == 0)
                        {
                            next.trust += 1;
                        }

                        message = "You wait quietly.";
                        reaction = "He peeks out for a moment.";
                    }
                    else if (context.lastInteractionWasRejected && context.waitStreak == 0)
                    {
                        next.stress -= 1;
                        if (CanBuildTrustAfterLowStressRejection(context.lastInteraction))
                        {
                            next.trust += 1;
                            message = "You accept his no.";
                            reaction = "He relaxes a little.";
                        }
                        else
                        {
                            message = "You give him a moment.";
                            reaction = "The room settles.";
                        }
                    }
                    else
                    {
                        next.stress -= 1;
                        message = "You observe him.";
                        reaction = context.waitStreak == 0
                            ? "He moves a little, but gives no clear signal."
                            : "The signal is still unclear.";
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
                    if (before.stress >= TouchStressGate)
                    {
                        if (context.lastInteraction == CaveheartInteractionType.Wait && context.waitStreak > 0)
                        {
                            next.stress -= 2;
                            next.trust += 1;
                            message = "You touch him slowly.";
                            reaction = "He flinches, then stays.";
                        }
                        else
                        {
                            accepted = false;
                            next.stress += 1;
                            next.trust -= 1;
                            message = "The touch is too soon.";
                            reaction = "He dodges your hand.";
                        }
                    }
                    else
                    {
                        if (context.gentleTouchUses >= HelpfulGentleTouchUses
                            && context.lastInteraction == CaveheartInteractionType.Wait
                            && context.waitStreak > 0
                            && before.trust < SitUpTrust)
                        {
                            next.trust += 1;
                            next.stress -= 1;
                            message = "You touch him after waiting.";
                            reaction = "He lets you help a little more.";
                        }
                        else if (context.gentleTouchUses >= HelpfulGentleTouchUses)
                        {
                            accepted = false;
                            next.stress += 1;
                            next.trust -= 1;
                            message = "That is too much touching.";
                            reaction = "He pulls away.";
                        }
                        else if (context.lastInteraction == CaveheartInteractionType.GentleTouch)
                        {
                            accepted = false;
                            next.stress += 1;
                            message = "The touch repeats too soon.";
                            reaction = "He shifts away.";
                        }
                        else
                        {
                            next.trust += 1;
                            next.stress -= 1;
                            message = "You touch him gently.";
                            reaction = context.gentleTouchUses == 0
                                ? "His breathing slows."
                                : "He accepts it, then needs space.";
                        }
                    }
                    break;
                case CaveheartInteractionType.OfferWater:
                    var hasObservedBodySignal = context.lastInteraction == CaveheartInteractionType.Wait && context.waitStreak > 0;
                    var isCalmEnoughToDrink = before.stress <= StartledStress;
                    var isBodyReadyToDrink = IsBodyReadyToDrink(before, context.lastInteraction);
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
                    else if (!isCalmEnoughToDrink)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "He is too tense for water.";
                        reaction = "He turns away from the cup.";
                    }
                    else if (before.trust >= SettledTrust && isBodyReadyToDrink)
                    {
                        next.awake += 2;
                        next.trust += 1;
                        next.stress -= 1;
                        message = "You offer water.";
                        reaction = "He drinks a little.";
                    }
                    else if (currentState == CaveheartState.Settled
                        && before.trust >= SettledTrust
                        && before.stress <= 1
                        && context.lastInteraction != CaveheartInteractionType.OpenCurtain)
                    {
                        next.awake += 1;
                        next.stress -= 1;
                        message = "You offer water.";
                        reaction = "He takes a small sip.";
                    }
                    else if (before.trust >= WaterTrustGate && before.stress <= SettledMaxStress)
                    {
                        next.awake += 1;
                        next.stress -= 1;
                        message = "You offer water.";
                        reaction = "He takes a small sip.";
                    }
                    else if (before.stress <= SettledMaxStress && hasObservedBodySignal)
                    {
                        accepted = false;
                        message = "You offer water.";
                        reaction = "He does not drink.";
                    }
                    else if (before.stress <= SettledMaxStress)
                    {
                        accepted = false;
                        message = "You offer water.";
                        reaction = "He does not drink.";
                    }
                    else
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "He is not ready for water.";
                        reaction = "He turns away.";
                    }
                    break;
                case CaveheartInteractionType.OpenCurtain:
                    var hasLightWarning = context.lastInteraction == CaveheartInteractionType.Wait && context.waitStreak > 0;
                    if (context.curtainUses >= HelpfulCurtainUses)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "The curtain moves again.";
                        reaction = "He squints.";
                    }
                    else
                    {
                        next.awake += 2;
                        var lightHasConsent = currentState == CaveheartState.Settled || hasLightWarning;
                        if (before.awake == 0)
                        {
                            next.awake -= 1;
                            next.stress += 1;
                            message = "You open the curtain a little.";
                            reaction = "A thin line of light enters. He curls up, but does not panic.";
                        }
                        else if (before.trust >= SettledTrust && before.stress <= StartledStress && lightHasConsent)
                        {
                            next.stress -= 1;
                            message = "You open the curtain slowly.";
                            reaction = "He blinks at the light.";
                        }
                        else
                        {
                            next.awake -= 1;
                            next.stress += 2;
                            next.trust -= 1;
                            message = "The light is too sudden.";
                            reaction = "He pulls the blanket up.";
                        }
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
                    if (before.stress >= TouchStressGate)
                    {
                        accepted = false;
                        next.awake += 1;
                        next.stress += 1;
                        next.trust -= 1;
                        message = "He is too tense for play.";
                        reaction = "He curls away.";
                    }
                    else if (context.lastInteraction == CaveheartInteractionType.Scratch)
                    {
                        accepted = false;
                        next.awake += 1;
                        next.stress += 2;
                        next.trust -= 1;
                        message = "The scratch repeats too soon.";
                        reaction = "He twists away.";
                    }
                    else if (context.scratchUses >= HelpfulScratchUses)
                    {
                        accepted = false;
                        next.awake += 1;
                        next.stress += 1;
                        next.trust -= 1;
                        message = "That is too much scratching.";
                        reaction = "He twists away.";
                    }
                    else if (before.awake == 0)
                    {
                        accepted = false;
                        next.awake += 2;
                        next.stress += 1;
                        next.trust -= 1;
                        message = "You try a playful scratch.";
                        reaction = "He jolts awake a little, but turns away.";
                    }
                    else if (before.trust < SettledTrust
                        && before.awake >= 1
                        && before.stress <= 2
                        && context.lastInteraction == CaveheartInteractionType.Wait
                        && context.waitStreak > 0)
                    {
                        next.awake += 1;
                        next.trust += 2;
                        next.stress += 2;
                        message = "You try a playful scratch after watching him.";
                        reaction = "He laughs before he can stop himself, then hides again.";
                    }
                    else if (before.awake >= ScratchAwakeTrustGate && before.trust >= 2)
                    {
                        next.awake += 2;
                        next.stress += 1;
                        if (before.trust >= SettledTrust && context.lastInteraction == CaveheartInteractionType.Wait && context.waitStreak > 0)
                        {
                            next.trust += 1;
                        }

                        message = "You scratch him lightly.";
                        reaction = "He gives a sleepy laugh.";
                    }
                    else if (currentState == CaveheartState.Settled && before.trust >= SettledTrust && before.stress <= 1)
                    {
                        next.awake += 1;
                        next.stress += 1;
                        message = "You scratch him lightly.";
                        reaction = "He stirs, but is not ready to play.";
                    }
                    else
                    {
                        accepted = false;
                        next.awake += 1;
                        next.trust -= 1;
                        next.stress += 1;
                        message = "He is not ready for play.";
                        reaction = "He turns away.";
                    }
                    break;
            }

            next = next.Clamped();
            var state = EvaluateState(next, currentState);
            if (includeObserveHint && interaction == CaveheartInteractionType.Wait)
            {
                var observeHint = BuildObserveHint(before, next, currentState, state, context);
                if (!string.IsNullOrEmpty(observeHint))
                {
                    reaction = string.IsNullOrEmpty(reaction) || IsGenericObserveReaction(reaction)
                        ? observeHint
                        : reaction + "\n" + observeHint;
                }
            }

            reaction = AddWaterReadyCue(reaction, next, interaction, context);
            return new CaveheartInteractionResult(interaction, before, next, state, accepted, message, reaction);
        }

        private static string AddWaterReadyCue(
            string reaction,
            CaveheartStats statsAfterAction,
            CaveheartInteractionType interaction,
            CaveheartInteractionContext context)
        {
            const string cue = "He looks toward the cup.";
            if (!CanUseStrongWaterNext(statsAfterAction, interaction, context) || reaction.Contains(cue))
            {
                return reaction;
            }

            return string.IsNullOrEmpty(reaction) || IsGenericObserveReaction(reaction)
                ? cue
                : reaction + "\n" + cue;
        }

        private static bool CanUseStrongWaterNext(
            CaveheartStats statsAfterAction,
            CaveheartInteractionType lastInteraction,
            CaveheartInteractionContext context)
        {
            return lastInteraction != CaveheartInteractionType.OfferWater
                && context.waterUses < HelpfulWaterUses
                && statsAfterAction.trust >= SettledTrust
                && statsAfterAction.stress <= StartledStress
                && IsBodyReadyToDrink(statsAfterAction, lastInteraction);
        }

        private static bool IsGenericObserveReaction(string reaction)
        {
            return reaction == "He moves a little, but gives no clear signal."
                || reaction == "The signal is still unclear.";
        }

        private static string BuildObserveHint(
            CaveheartStats beforeObserve,
            CaveheartStats afterObserve,
            CaveheartState stateBeforeObserve,
            CaveheartState stateAfterObserve,
            CaveheartInteractionContext context)
        {
            if (IsFreshOpeningObserve(beforeObserve, stateBeforeObserve, context))
            {
                return string.Empty;
            }

            var nextContext = new CaveheartInteractionContext(
                context.blanketUses,
                context.gentleTouchUses,
                context.waterUses,
                context.curtainUses,
                CaveheartInteractionType.Wait,
                context.waitStreak + 1,
                false,
                false,
                context.scratchUses);

            var bestAction = ObserveHintCandidates[0];
            var worstAction = ObserveHintCandidates[0];
            var bestScore = int.MinValue;
            var secondBestScore = int.MinValue;
            var worstScore = int.MaxValue;
            var secondWorstScore = int.MaxValue;

            for (var i = 0; i < ObserveHintCandidates.Length; i++)
            {
                var candidate = ObserveHintCandidates[i];
                var result = Apply(afterObserve, stateAfterObserve, candidate, nextContext, false);
                var score = ScoreCandidate(result);

                if (score > bestScore)
                {
                    secondBestScore = bestScore;
                    bestScore = score;
                    bestAction = candidate;
                }
                else if (score > secondBestScore)
                {
                    secondBestScore = score;
                }

                if (score < worstScore)
                {
                    secondWorstScore = worstScore;
                    worstScore = score;
                    worstAction = candidate;
                }
                else if (score < secondWorstScore)
                {
                    secondWorstScore = score;
                }
            }

            if (ShouldShowBestObserveHint(afterObserve, context))
            {
                return bestScore >= 4 && bestScore - secondBestScore >= 2
                    ? GetBestObserveHint(bestAction)
                    : string.Empty;
            }

            return worstScore <= -5 && secondWorstScore - worstScore >= 2
                ? GetAvoidObserveHint(worstAction)
                : string.Empty;
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
                && context.lastInteraction == CaveheartInteractionType.Wait
                && context.waitStreak == 0
                && !context.lastInteractionWasForceful
                && !context.lastInteractionWasRejected;
        }

        private static bool ShouldShowBestObserveHint(CaveheartStats afterObserve, CaveheartInteractionContext context)
        {
            var hash = afterObserve.awake * 31
                + afterObserve.trust * 17
                + afterObserve.stress * 13
                + context.blanketUses * 11
                + context.gentleTouchUses * 7
                + context.waterUses * 5
                + context.curtainUses * 3
                + context.scratchUses * 19
                + context.waitStreak;

            if (hash < 0)
            {
                hash = -hash;
            }

            return hash % 100 < StrongBestHintPercent;
        }

        private static bool IsBodyReadyToDrink(CaveheartStats stats, CaveheartInteractionType lastInteraction)
        {
            return stats.awake >= 4 && lastInteraction != CaveheartInteractionType.OpenCurtain;
        }

        private static int ScoreCandidate(CaveheartInteractionResult result)
        {
            var awakeDelta = result.after.awake - result.before.awake;
            var trustDelta = result.after.trust - result.before.trust;
            var stressDelta = result.after.stress - result.before.stress;

            var score = awakeDelta;
            score += trustDelta * 4;
            score -= stressDelta * 2;

            if (!result.accepted)
            {
                score -= 4;
            }

            if (trustDelta < 0)
            {
                score += trustDelta * 2;
            }

            if (result.after.stress >= ResistStress)
            {
                score -= 6;
            }

            if (result.state == CaveheartState.SittingUp)
            {
                score += 8;
            }

            return score;
        }

        private static string GetBestObserveHint(CaveheartInteractionType action)
        {
            switch (action)
            {
                case CaveheartInteractionType.GentleTouch:
                    return "He stays close enough that a slow touch could help.";
                case CaveheartInteractionType.OfferWater:
                    return "His body looks ready for a small sip.";
                case CaveheartInteractionType.OpenCurtain:
                    return "A little morning light may help him stir.";
                case CaveheartInteractionType.Scratch:
                    return "There is a tiny playful twitch under the blanket.";
                default:
                    return string.Empty;
            }
        }

        private static string GetAvoidObserveHint(CaveheartInteractionType action)
        {
            switch (action)
            {
                case CaveheartInteractionType.GentleTouch:
                    return "He is still tucked inward. Getting close now would crowd him.";
                case CaveheartInteractionType.OfferWater:
                    return "He is not looking for the cup yet. Offering water now would interrupt him.";
                case CaveheartInteractionType.OpenCurtain:
                    return "His eyes are still guarded. More light now would be too sudden.";
                case CaveheartInteractionType.Scratch:
                    return "His body is too braced for play. A joke now would feel like pressure.";
                default:
                    return string.Empty;
            }
        }

        private static bool CanBuildTrustAfterLowStressRejection(CaveheartInteractionType rejectedInteraction)
        {
            return rejectedInteraction == CaveheartInteractionType.GentleTouch;
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
