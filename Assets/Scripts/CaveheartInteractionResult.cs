namespace MyLittleCaveheart
{
    public readonly struct CaveheartInteractionResult
    {
        public readonly CaveheartInteractionType interactionType;
        public readonly CaveheartStats before;
        public readonly CaveheartStats after;
        public readonly CaveheartState state;
        public readonly bool accepted;
        public readonly string message;
        public readonly string observedReaction;

        public CaveheartInteractionResult(
            CaveheartInteractionType interactionType,
            CaveheartStats before,
            CaveheartStats after,
            CaveheartState state,
            bool accepted,
            string message,
            string observedReaction)
        {
            this.interactionType = interactionType;
            this.before = before;
            this.after = after;
            this.state = state;
            this.accepted = accepted;
            this.message = message;
            this.observedReaction = observedReaction;
        }
    }
}
