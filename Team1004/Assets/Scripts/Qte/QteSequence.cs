using System;
using UnityEngine;

namespace Game.Qte
{
    public sealed class QteSequence
    {
        private int requiredPresses;
        private float timeBudget;
        private float remaining;

        public QteState State { get; private set; } = QteState.Idle;
        public int RequiredPresses => requiredPresses;
        public int Progress { get; private set; }
        public QteKey Expected { get; private set; } = QteKey.None;
        public QteKey LastAccepted { get; private set; } = QteKey.None;
        public float TimeBudget => timeBudget;
        public float Remaining => remaining;
        public bool HasTimeBudget => timeBudget > 0f;
        public bool IsRunning => State == QteState.Running;
        public bool IsCompleted => State == QteState.Completed;
        public bool IsFailed => State == QteState.Failed;

        public float Progress01 =>
            requiredPresses <= 0 ? 1f : Mathf.Clamp01((float)Progress / requiredPresses);

        public float Remaining01 =>
            timeBudget <= 0f ? 1f : Mathf.Clamp01(remaining / timeBudget);

        public event Action<int> Accepted;
        public event Action<QteKey> Rejected;
        public event Action<QteKey> ExpectedChanged;
        public event Action Completed;
        public event Action Failed;

        public void Begin(int presses, bool firstKeyUp, float budgetSeconds)
        {
            requiredPresses = Mathf.Max(1, presses);
            timeBudget = Mathf.Max(0f, budgetSeconds);
            remaining = timeBudget;
            Progress = 0;
            LastAccepted = QteKey.None;
            Expected = firstKeyUp ? QteKey.Up : QteKey.Down;
            State = QteState.Running;
            ExpectedChanged?.Invoke(Expected);
        }

        public void Cancel()
        {
            requiredPresses = 0;
            timeBudget = 0f;
            remaining = 0f;
            Progress = 0;
            LastAccepted = QteKey.None;
            Expected = QteKey.None;
            State = QteState.Idle;
        }

        public bool Press(QteKey key)
        {
            if (State != QteState.Running || key == QteKey.None)
                return false;

            if (key != Expected)
            {
                Rejected?.Invoke(key);
                return false;
            }

            Progress++;
            LastAccepted = key;

            if (Progress >= requiredPresses)
            {
                Expected = QteKey.None;
                State = QteState.Completed;
                Accepted?.Invoke(Progress);
                ExpectedChanged?.Invoke(Expected);
                Completed?.Invoke();
                return true;
            }

            Expected = key == QteKey.Up ? QteKey.Down : QteKey.Up;
            Accepted?.Invoke(Progress);
            ExpectedChanged?.Invoke(Expected);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (State != QteState.Running || timeBudget <= 0f)
                return;

            SetRemaining(remaining - Mathf.Max(0f, deltaTime));
        }

        public void SetRemaining(float seconds)
        {
            if (State != QteState.Running)
                return;

            remaining = seconds;

            if (timeBudget > 0f && remaining <= 0f)
                Fail();
        }

        public void Fail()
        {
            if (State != QteState.Running)
                return;

            remaining = 0f;
            Expected = QteKey.None;
            State = QteState.Failed;
            Failed?.Invoke();
        }
    }
}
