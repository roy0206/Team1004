using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Cutscene.Tests
{
    public sealed class CutsceneSkipTests
    {
        private sealed class ProbeCutscene : CutsceneBase
        {
            private readonly CutsceneActor actor;
            private readonly Vector2 first;
            private readonly Vector2 second;

            public ProbeCutscene(CutsceneActor actor, Vector2 first, Vector2 second)
            {
                this.actor = actor;
                this.first = first;
                this.second = second;
            }

            public List<string> Log { get; } = new();
            public bool Completed { get; private set; }

            public override string Id => "probe";

            protected override async Awaitable Run()
            {
                Log.Add("begin");
                await Move(actor, first, 2f);

                Log.Add("moved");
                await Wait(3f);

                Log.Add("waited");
                await Together(Move(actor, second, 1.5f), Wait(1f));

                Log.Add("together");
                await Say("probe.01");

                Log.Add("said");
                Completed = true;
            }
        }

        private GameObject host;
        private CutsceneActor actor;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("CutsceneSkipTestsActor");
            actor = host.AddComponent<CutsceneActor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                Object.DestroyImmediate(host);

            host = null;
            actor = null;
        }

        [Test]
        public void SkipBeforeRunAppliesEveryFinalStateSynchronously()
        {
            var first = new Vector2(1f, 2f);
            var second = new Vector2(-3f, 0.5f);
            var cutscene = new ProbeCutscene(actor, first, second);

            cutscene.Begin(NewContext());
            cutscene.Skip();

            Assert.IsTrue(cutscene.IsSkipping);

            _ = cutscene.ExecuteAsync();

            Assert.IsTrue(cutscene.Completed, "A skipped cutscene must finish without suspending.");
            Assert.AreEqual(new[] { "begin", "moved", "waited", "together", "said" }, cutscene.Log.ToArray());
            Assert.AreEqual(second, (Vector2)host.transform.position);
            Assert.IsFalse(cutscene.IsRunning);
        }

        [Test]
        public void CancelLeavesTheSceneUntouched()
        {
            var start = new Vector2(4f, -1f);
            host.transform.position = start;

            var cutscene = new ProbeCutscene(actor, new Vector2(1f, 2f), new Vector2(-3f, 0.5f));

            cutscene.Begin(NewContext());
            cutscene.Cancel();

            Assert.IsTrue(cutscene.IsCancelled);

            _ = cutscene.ExecuteAsync();

            Assert.IsTrue(cutscene.Completed);
            Assert.AreEqual(start, (Vector2)host.transform.position);
        }

        [Test]
        public void BeginTwiceIsRejected()
        {
            var cutscene = new ProbeCutscene(actor, Vector2.zero, Vector2.zero);
            cutscene.Begin(NewContext());

            Assert.Throws<System.InvalidOperationException>(() => cutscene.Begin(NewContext()));

            cutscene.Skip();
            _ = cutscene.ExecuteAsync();
        }

        [Test]
        public void ExecuteWithoutBeginIsRejected()
        {
            var cutscene = new ProbeCutscene(actor, Vector2.zero, Vector2.zero);

            Assert.Throws<System.InvalidOperationException>(() => { _ = cutscene.ExecuteAsync(); });
        }

        [Test]
        public void SkippedCutsceneCanRunAgain()
        {
            var target = new Vector2(2.5f, 2.5f);
            var cutscene = new ProbeCutscene(actor, Vector2.zero, target);

            cutscene.Begin(NewContext());
            cutscene.Skip();
            _ = cutscene.ExecuteAsync();

            host.transform.position = Vector3.zero;

            cutscene.Begin(NewContext());
            cutscene.Skip();
            _ = cutscene.ExecuteAsync();

            Assert.AreEqual(target, (Vector2)host.transform.position);
        }

        private static CutsceneContext NewContext()
        {
            return new CutsceneContext(null, null, null, null);
        }
    }
}
