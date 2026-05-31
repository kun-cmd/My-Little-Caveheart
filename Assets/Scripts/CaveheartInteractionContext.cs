namespace MyLittleCaveheart
{
    public readonly struct CaveheartInteractionContext
    {
        public readonly int blanketUses;
        public readonly int gentleTouchUses;
        public readonly int waterUses;
        public readonly int curtainUses;
        public readonly int scratchUses;
        public readonly CaveheartInteractionType lastInteraction;
        public readonly int waitStreak;
        public readonly bool lastInteractionWasForceful;
        public readonly bool lastInteractionWasRejected;

        public CaveheartInteractionContext(int blanketUses, int gentleTouchUses, int waterUses, int curtainUses)
            : this(blanketUses, gentleTouchUses, waterUses, curtainUses, CaveheartInteractionType.Wait, 0, false, false, 0)
        {
        }

        public CaveheartInteractionContext(
            int blanketUses,
            int gentleTouchUses,
            int waterUses,
            int curtainUses,
            CaveheartInteractionType lastInteraction,
            int waitStreak,
            bool lastInteractionWasForceful,
            bool lastInteractionWasRejected,
            int scratchUses = 0)
        {
            this.blanketUses = blanketUses;
            this.gentleTouchUses = gentleTouchUses;
            this.waterUses = waterUses;
            this.curtainUses = curtainUses;
            this.scratchUses = scratchUses;
            this.lastInteraction = lastInteraction;
            this.waitStreak = waitStreak;
            this.lastInteractionWasForceful = lastInteractionWasForceful;
            this.lastInteractionWasRejected = lastInteractionWasRejected;
        }
    }
}
