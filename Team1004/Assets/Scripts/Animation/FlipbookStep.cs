namespace Game.Animation
{
    public readonly struct FlipbookStep
    {
        public static readonly FlipbookStep Idle = new FlipbookStep(0, false, 0, -1, 0, false);

        private readonly int frame;
        private readonly bool frameChanged;
        private readonly int firstEntered;
        private readonly int lastEntered;
        private readonly int cyclesCompleted;
        private readonly bool completed;

        public FlipbookStep(int frame, bool frameChanged, int firstEntered, int lastEntered, int cyclesCompleted, bool completed)
        {
            this.frame = frame;
            this.frameChanged = frameChanged;
            this.firstEntered = firstEntered;
            this.lastEntered = lastEntered;
            this.cyclesCompleted = cyclesCompleted;
            this.completed = completed;
        }

        public int Frame => frame;
        public bool FrameChanged => frameChanged;
        public int FirstEntered => firstEntered;
        public int LastEntered => lastEntered;
        public int CyclesCompleted => cyclesCompleted;
        public bool Completed => completed;
        public bool HasEnteredFrames => lastEntered >= firstEntered;
        public int EnteredCount => lastEntered >= firstEntered ? lastEntered - firstEntered + 1 : 0;
    }
}
