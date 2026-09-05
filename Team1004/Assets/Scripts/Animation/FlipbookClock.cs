namespace Game.Animation
{
    public sealed class FlipbookClock
    {
        public const float MinimumLength = 0.0001f;

        private int frameCount;
        private float length = MinimumLength;
        private bool loop;
        private float speed = 1f;
        private float elapsed;
        private int cycleCount;
        private int lastEntered = -1;
        private bool finished;

        public int FrameCount => frameCount;
        public float Length => length;
        public bool Loop => loop;
        public float Elapsed => elapsed;
        public int CycleCount => cycleCount;
        public bool IsFinished => finished;
        public int LastEnteredGlobal => lastEntered;

        public float Speed
        {
            get => speed;
            set => speed = value < 0f ? 0f : value;
        }

        public float FrameDuration => frameCount > 0 ? length / frameCount : 0f;

        public int Global => cycleCount * frameCount + LocalGlobal;

        public int Frame => ToFrame(Global);

        public float NormalizedTime
        {
            get
            {
                if (length <= 0f)
                    return 0f;

                if (!loop && elapsed >= length)
                    return 1f;

                return elapsed / length;
            }
        }

        public void Configure(int frames, float cycleLength, bool looping)
        {
            frameCount = frames < 0 ? 0 : frames;
            length = cycleLength < MinimumLength ? MinimumLength : cycleLength;
            loop = looping;
            Reset();
        }

        public void Reset()
        {
            elapsed = 0f;
            cycleCount = 0;
            lastEntered = -1;
            finished = frameCount <= 0;
        }

        public void SeekToFrame(int index)
        {
            if (frameCount <= 0)
            {
                Reset();
                return;
            }

            var clamped = index < 0 ? 0 : index >= frameCount ? frameCount - 1 : index;
            elapsed = clamped * FrameDuration;
            cycleCount = 0;
            lastEntered = clamped;
            finished = false;
        }

        public int ToFrame(int globalIndex)
        {
            if (frameCount <= 0)
                return 0;

            if (!loop)
                return globalIndex < 0 ? 0 : globalIndex >= frameCount ? frameCount - 1 : globalIndex;

            var wrapped = globalIndex % frameCount;
            return wrapped < 0 ? wrapped + frameCount : wrapped;
        }

        public FlipbookStep Advance(float deltaTime)
        {
            if (frameCount <= 0 || finished)
                return FlipbookStep.Idle;

            var previousFrame = Frame;
            var previousEntered = lastEntered;
            var cycles = 0;
            var completed = false;

            if (deltaTime > 0f && speed > 0f)
                elapsed += deltaTime * speed;

            if (loop)
            {
                if (elapsed >= length)
                {
                    cycles = (int)(elapsed / length);
                    elapsed -= cycles * length;

                    if (elapsed < 0f)
                        elapsed = 0f;

                    cycleCount += cycles;
                }
            }
            else if (elapsed >= length)
            {
                elapsed = length;
                finished = true;
                completed = true;
            }

            var global = Global;
            var first = previousEntered + 1;
            var last = global;

            if (last - first + 1 > frameCount)
                first = last - frameCount + 1;

            if (last >= first)
                lastEntered = last;
            else
                first = last + 1;

            var frame = ToFrame(global);
            return new FlipbookStep(frame, frame != previousFrame || previousEntered < 0, first, last, cycles, completed);
        }

        private int LocalGlobal
        {
            get
            {
                var duration = FrameDuration;

                if (duration <= 0f || frameCount <= 0)
                    return 0;

                var index = (int)(elapsed / duration);

                if (index < 0)
                    return 0;

                return index >= frameCount ? frameCount - 1 : index;
            }
        }
    }
}
