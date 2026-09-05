using System;
using UnityEngine;

namespace Game.Qte
{
    public sealed class QteModule : Module
    {
        public const string DefaultUpAction = "Player/LaneUp";
        public const string DefaultDownAction = "Player/LaneDown";

        private readonly QteSequence sequence = new();

        private string upAction = DefaultUpAction;
        private string downAction = DefaultDownAction;
        private bool listening;

        public QteSequence Sequence => sequence;
        public QteState State => sequence.State;
        public bool IsRunning => sequence.IsRunning;
        public QteKey Expected => sequence.Expected;
        public int Progress => sequence.Progress;
        public int RequiredPresses => sequence.RequiredPresses;
        public bool AutoTick { get; set; } = true;
        public string UpAction => upAction;
        public string DownAction => downAction;

        public event Action<int> Accepted;
        public event Action<QteKey> Rejected;
        public event Action<QteKey> ExpectedChanged;
        public event Action Completed;
        public event Action Failed;

        protected override ModuleTick Ticks => ModuleTick.Update;

        public QteModule()
        {
            sequence.Accepted += OnAccepted;
            sequence.Rejected += OnRejected;
            sequence.ExpectedChanged += OnExpectedChanged;
            sequence.Completed += OnCompleted;
            sequence.Failed += OnFailed;
        }

        public void SetActions(string up, string down)
        {
            var wasListening = listening;
            Unsubscribe();

            upAction = string.IsNullOrEmpty(up) ? DefaultUpAction : up;
            downAction = string.IsNullOrEmpty(down) ? DefaultDownAction : down;

            if (wasListening)
                Subscribe();
        }

        public bool Begin(QteData data, float budgetSeconds)
        {
            if (data == null)
                return false;

            SetActions(data.UpAction, data.DownAction);
            return Begin(data.RequiredPresses, data.FirstKeyUp, budgetSeconds);
        }

        public bool Begin(int presses, bool firstKeyUp, float budgetSeconds)
        {
            sequence.Begin(presses, firstKeyUp, budgetSeconds);

            if (!sequence.IsRunning)
                return false;

            Subscribe();
            return true;
        }

        public void Stop()
        {
            Unsubscribe();
            sequence.Cancel();
        }

        public void SetRemaining(float seconds)
        {
            sequence.SetRemaining(seconds);
        }

        protected override void OnUpdate()
        {
            if (AutoTick && sequence.IsRunning)
                sequence.Tick(Time.deltaTime);
        }

        protected override void OnDetached()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (listening)
                return;

            if (!InputManager.TryGetInstance(out var input) || !input.IsInitialized)
            {
                Debug.LogWarning("[QteModule] InputManager is not initialized. The quick time event has no input.");
                return;
            }

            input.AddListener(upAction, InputPhase.Performed, OnUpPressed);
            input.AddListener(downAction, InputPhase.Performed, OnDownPressed);
            listening = true;
        }

        private void Unsubscribe()
        {
            if (!listening)
                return;

            listening = false;

            if (!InputManager.TryGetInstance(out var input))
                return;

            input.RemoveListener(upAction, InputPhase.Performed, OnUpPressed);
            input.RemoveListener(downAction, InputPhase.Performed, OnDownPressed);
        }

        private void OnUpPressed()
        {
            sequence.Press(QteKey.Up);
        }

        private void OnDownPressed()
        {
            sequence.Press(QteKey.Down);
        }

        private void OnAccepted(int progress)
        {
            Accepted?.Invoke(progress);
        }

        private void OnRejected(QteKey key)
        {
            Rejected?.Invoke(key);
        }

        private void OnExpectedChanged(QteKey key)
        {
            ExpectedChanged?.Invoke(key);
        }

        private void OnCompleted()
        {
            Unsubscribe();
            Completed?.Invoke();
        }

        private void OnFailed()
        {
            Unsubscribe();
            Failed?.Invoke();
        }
    }
}
