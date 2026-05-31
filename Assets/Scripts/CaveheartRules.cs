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
        public const int ScratchAwakeTrustGate = 4;

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
                    if (before.stress >= ResistStress)
                    {
                        next.stress -= 2;
                        if ((context.lastInteractionWasForceful || context.lastInteractionWasRejected) && context.waitStreak == 0)
                        {
                            next.trust += 1;
                        }

                        message = "You stop and give him space.";
                        reaction = "He stays curled up, but the shaking in his body begins to slow.";
                    }
                    else if (before.stress >= StartledStress)
                    {
                        next.stress -= 2;
                        if (context.waitStreak == 0)
                        {
                            next.trust += 1;
                        }

                        message = "You wait nearby without asking anything.";
                        reaction = "He peeks out for a second, checking whether it is safe.";
                    }
                    else if (context.lastInteractionWasRejected && context.waitStreak == 0)
                    {
                        next.stress -= 1;
                        next.trust += 1;
                        message = "You accept the no and stay quiet.";
                        reaction = "He notices that refusing did not make things worse.";
                    }
                    else
                    {
                        next.stress -= 1;
                        message = "You watch him before choosing anything.";
                        reaction = context.waitStreak == 0
                            ? "He gives small signals: ears covered, dry lips, a glance toward the window."
                            : "Watching longer shows the same signals. Something else will need to change.";
                    }
                    break;
                case CaveheartInteractionType.Alarm:
                    next.awake += 2;
                    next.stress += 3;
                    next.trust -= 1;
                    message = "Alarm rings sharply.";
                    reaction = "He covers his ears and curls deeper into the blanket.";
                    break;
                case CaveheartInteractionType.ShakeBed:
                    next.awake += 3;
                    next.stress += 4;
                    next.trust -= 2;
                    message = "The bed is shaken.";
                    reaction = "He pushes back, frightened and tearful.";
                    break;
                case CaveheartInteractionType.GentleTouch:
                    if (before.stress >= TouchStressGate)
                    {
                        if (context.lastInteraction == CaveheartInteractionType.Wait && context.waitStreak > 0)
                        {
                            next.stress -= 2;
                            next.trust += 1;
                            message = "After waiting, the touch is offered slowly.";
                            reaction = "He flinches at first, then lets the hand rest for a moment.";
                        }
                        else
                        {
                            accepted = false;
                            next.stress += 1;
                            next.trust -= 1;
                            message = "The touch comes too soon.";
                            reaction = "He dodges the hand and hides his face.";
                        }
                    }
                    else
                    {
                        if (context.gentleTouchUses >= HelpfulGentleTouchUses)
                        {
                            accepted = false;
                            next.stress += 1;
                            next.trust -= 1;
                            message = "The touch repeats after he has already been soothed.";
                            reaction = "He pulls away. Too much touching starts to feel like pressure.";
                        }
                        else
                        {
                            next.trust += 1;
                            next.stress -= 1;
                            message = "A gentle touch lands softly.";
                            reaction = context.gentleTouchUses == 0
                                ? "His breathing slows a little."
                                : "He accepts it, but his body seems to ask for space next.";
                        }
                    }
                    break;
                case CaveheartInteractionType.OfferWater:
                    if (context.waterUses >= HelpfulWaterUses)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "More water is offered, but he has had enough.";
                        reaction = "He turns away from the cup. Repeating help without listening makes him tense.";
                    }
                    else if (before.trust >= WaterTrustGate || (before.trust >= SettledTrust && before.stress <= StartledStress && context.lastInteraction == CaveheartInteractionType.Wait))
                    {
                        next.awake += 2;
                        next.trust += 1;
                        next.stress -= 1;
                        message = "Water is offered quietly.";
                        reaction = "He accepts it and takes a tiny sip.";
                    }
                    else if (before.stress <= SettledMaxStress)
                    {
                        accepted = false;
                        next.trust += 1;
                        message = "Water is offered without pushing.";
                        reaction = "He does not take the cup yet, but he notices that no one forces him.";
                    }
                    else
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "Water is offered, but trust is still thin.";
                        reaction = "He turns away and does not take the cup.";
                    }
                    break;
                case CaveheartInteractionType.OpenCurtain:
                    if (context.curtainUses >= HelpfulCurtainUses)
                    {
                        accepted = false;
                        next.stress += 1;
                        message = "The curtain is fussed with again.";
                        reaction = "He squints and gets annoyed. The light is already there.";
                    }
                    else
                    {
                        next.awake += 2;
                        if (before.trust >= SettledTrust && before.stress <= StartledStress && (currentState == CaveheartState.Settled || context.lastInteraction == CaveheartInteractionType.Wait))
                        {
                            next.trust += 1;
                            next.stress -= 1;
                            message = "The curtain opens slowly.";
                            reaction = "He blinks, then looks toward the light. The slow warning helps him trust you.";
                        }
                        else
                        {
                            next.awake -= 1;
                            next.stress += 2;
                            next.trust -= 1;
                            message = "The curtain opens too brightly.";
                            reaction = "He squints and pulls the blanket up.";
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
                            message = "You offer the blanket without forcing it.";
                            reaction = "He does not want more blanket, but he is less panicked because you leave it nearby.";
                        }
                        else
                        {
                            accepted = false;
                            next.stress += 1;
                            next.trust -= 1;
                            message = "The blanket is tucked again, but it is too much now.";
                            reaction = "He kicks the blanket away. He is getting hot and less willing to trust this.";
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
                            ? "The blanket is tucked around him."
                            : "The blanket helps one more time, but he is warm enough now.";
                        reaction = context.blanketUses == 0
                            ? "His shoulders loosen under the warmth."
                            : "He settles briefly, then shifts like he may not want more blanket.";
                    }
                    break;
                case CaveheartInteractionType.Scratch:
                    next.awake += 1;
                    if (before.stress >= ResistStress)
                    {
                        accepted = false;
                        next.stress += 1;
                        next.trust -= 1;
                        message = "You try to scratch him playfully while he is overwhelmed.";
                        reaction = "He curls away. It feels like too much when his body is already bracing.";
                    }
                    else if (context.scratchUses >= HelpfulScratchUses)
                    {
                        accepted = false;
                        next.stress += 1;
                        next.trust -= 1;
                        message = "The playful scratching keeps going after the joke has landed.";
                        reaction = "He twists away. Even play starts to feel like pressure when it repeats.";
                    }
                    else if (before.awake >= ScratchAwakeTrustGate || currentState == CaveheartState.Settled)
                    {
                        next.trust += 1;
                        next.stress -= 1;
                        message = "A tiny scratch is offered like a joke.";
                        reaction = "He gives a small sleepy laugh and stays close.";
                    }
                    else
                    {
                        accepted = false;
                        next.trust -= 1;
                        next.stress += 1;
                        message = "The scratch comes before he is ready for play.";
                        reaction = "He twitches and turns away. It is waking him, but he does not trust it yet.";
                    }
                    break;
            }

            next = next.Clamped();
            var state = EvaluateState(next, currentState);
            return new CaveheartInteractionResult(interaction, before, next, state, accepted, message, reaction);
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
