using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class StateSet
    {
        private readonly int[] cost;

        public StateSet(StateLayout layout)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            cost = new int[layout.StateCount];
            Clear();
        }

        public StateLayout Layout { get; }

        internal int[] Costs => cost;

        public bool IsEmpty
        {
            get
            {
                for (var i = 0; i < cost.Length; i++)
                    if (cost[i] >= 0)
                        return false;

                return true;
            }
        }

        public int Count
        {
            get
            {
                var count = 0;
                for (var i = 0; i < cost.Length; i++)
                    if (cost[i] >= 0)
                        count++;

                return count;
            }
        }

        public int MinCost
        {
            get
            {
                var min = -1;
                for (var i = 0; i < cost.Length; i++)
                    if (cost[i] >= 0 && (min < 0 || cost[i] < min))
                        min = cost[i];

                return min;
            }
        }

        public bool AllInCooldown
        {
            get
            {
                var any = false;

                for (var i = 0; i < cost.Length; i++)
                {
                    if (cost[i] < 0)
                        continue;

                    any = true;
                    var state = Layout.Decode(i);
                    if (!Layout.IsAir(state.Lane) && state.CooldownTicks == 0)
                        return false;
                }

                return any;
            }
        }

        public void Clear()
        {
            Array.Fill(cost, -1);
        }

        public void Add(int lane, int lockTicks, int cooldownTicks, int inputCost = 0)
        {
            lane = Math.Clamp(lane, 0, Layout.AirLane);
            lockTicks = Math.Clamp(lockTicks, 0, Layout.MaxLockTicks);
            cooldownTicks = Math.Clamp(cooldownTicks, 0, Layout.CooldownTicks);
            inputCost = Math.Max(0, inputCost);

            var index = Layout.Index(lane, lockTicks, cooldownTicks);
            if (cost[index] < 0 || cost[index] > inputCost)
                cost[index] = inputCost;
        }

        public void Add(in PlayerSimState state, int inputCost = 0)
        {
            Add(state.Lane, state.LockTicks, state.CooldownTicks, inputCost);
        }

        public bool Contains(int lane, int lockTicks, int cooldownTicks)
        {
            if (lane < 0 || lane > Layout.AirLane || lockTicks < 0 || lockTicks > Layout.MaxLockTicks ||
                cooldownTicks < 0 || cooldownTicks > Layout.CooldownTicks)
                return false;

            return cost[Layout.Index(lane, lockTicks, cooldownTicks)] >= 0;
        }

        public int GetCost(int index)
        {
            return cost[index];
        }

        public void CopyFrom(StateSet other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (other.Layout.StateCount != Layout.StateCount)
                throw new ArgumentException("State layouts differ.", nameof(other));

            Array.Copy(other.cost, cost, cost.Length);
        }

        public StateSet Clone()
        {
            var clone = new StateSet(Layout);
            clone.CopyFrom(this);
            return clone;
        }

        public int CollectStates(List<PlayerSimState> buffer)
        {
            buffer.Clear();

            for (var i = 0; i < cost.Length; i++)
                if (cost[i] >= 0)
                    buffer.Add(Layout.Decode(i));

            return buffer.Count;
        }
    }
}
