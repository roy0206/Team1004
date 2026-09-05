using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Spawner.Tests
{
    public sealed class ObstacleVisualTests
    {
        private const float RockAlphaWidth = 560f;
        private const float RockAlphaHeight = 252f;
        private const float LogAlphaWidth = 531f;
        private const float LogAlphaHeight = 288f;
        private const float FishAlphaWidth = 699f;
        private const float FishAlphaHeight = 318f;
        private const float FishBodyLength = 0.9f;
        private const float FishCollisionLength = 0.78f;
        private const float LongRockAlphaWidth = 692f;
        private const float LongRockAlphaHeight = 779f;

        private const float PlayerAlphaWidth = 531f;
        private const float PlayerAlphaHeight = 280f;
        private const float PlayerScale = 0.29f;
        private const float PlayerHitboxWidthScale = 0.6f;
        private const float PlayerHitboxHeightScale = 0.87f;

        [Test]
        public void UniformScale_MakesTheAlphaBoxAsLongAsTheBody()
        {
            var scale = ObstacleVisual.UniformScale(RockAlphaWidth, 1.0f);
            Assert.AreEqual(0.178571f, scale, 0.00001f);
            Assert.AreEqual(1.0f, RockAlphaWidth / ObstacleVisual.PixelsPerUnit * scale, 0.0001f);
        }

        [Test]
        public void UniformScale_HandlesTheLogBody()
        {
            var scale = ObstacleVisual.UniformScale(LogAlphaWidth, 1.8f);
            Assert.AreEqual(0.338983f, scale, 0.00001f);
            Assert.AreEqual(1.8f, LogAlphaWidth / ObstacleVisual.PixelsPerUnit * scale, 0.0001f);
        }

        [Test]
        public void UniformScale_HandlesTheFishBody()
        {
            var scale = ObstacleVisual.UniformScale(FishAlphaWidth, FishBodyLength);

            Assert.AreEqual(0.1287554f, scale, 0.00001f);
            Assert.AreEqual(FishBodyLength, FishAlphaWidth / ObstacleVisual.PixelsPerUnit * scale, 0.0001f);
        }

        [Test]
        public void VisualHeight_KeepsTheFishAspect()
        {
            Assert.AreEqual(0.409442f, ObstacleVisual.VisualHeight(FishAlphaWidth, FishAlphaHeight, FishBodyLength), 0.0001f);
            Assert.LessOrEqual(ObstacleVisual.VisualHeight(FishAlphaWidth, FishAlphaHeight, FishBodyLength), 1.0f);
        }

        [Test]
        public void VisualHeight_KeepsTheArtAspect()
        {
            Assert.AreEqual(0.45f, ObstacleVisual.VisualHeight(RockAlphaWidth, RockAlphaHeight, 1.0f), 0.0001f);
            Assert.AreEqual(0.97627f, ObstacleVisual.VisualHeight(LogAlphaWidth, LogAlphaHeight, 1.8f), 0.0001f);
        }

        [Test]
        public void VisualHeight_FitsInsideTheLaneVisualHeight()
        {
            const float laneVisualHeight = 1.0f;
            Assert.LessOrEqual(ObstacleVisual.VisualHeight(FishAlphaWidth, FishAlphaHeight, FishBodyLength), laneVisualHeight);
            Assert.LessOrEqual(ObstacleVisual.VisualHeight(LogAlphaWidth, LogAlphaHeight, 1.8f), laneVisualHeight);
        }

        [Test]
        public void EveryObstacle_FitsInsideItsLaneBand()
        {
            foreach (var obstacle in SpawnerDefaults.CreateObstacles())
            {
                var box = ArtBox(obstacle.Id);
                var size = ObstacleVisual.VisualWorldSize(obstacle, box.x, box.y);
                var band = obstacle.LaneSpan * SpawnerDefaults.LaneSpacing;

                if (obstacle.Id == SpawnerDefaults.LongRockId)
                {
                    Assert.Greater(size.y, band, "긴 돌은 강바닥에서 수면 위까지 솟아 레인 대역보다 높다(v8 05절).");
                    continue;
                }

                Assert.LessOrEqual(size.y, band + 0.001f, obstacle.Id + " 그림 높이는 레인 높이를 넘지 않는다.");
            }
        }

        [Test]
        public void Rock_FitsTheLaneHeightAndKeepsTheArtAspect()
        {
            var rock = Find(SpawnerDefaults.RockId);

            Assert.AreEqual(ObstacleFitAxis.Height, rock.FitAxis, "돌은 높이 기준으로 맞춘다(강바닥 바위).");
            Assert.AreEqual(SpawnerDefaults.LaneSpacing, rock.VisualHeight, 0.0001f);

            var scale = ObstacleVisual.UniformScale(rock, RockAlphaWidth, RockAlphaHeight);
            var size = ObstacleVisual.VisualWorldSize(rock, RockAlphaWidth, RockAlphaHeight);

            Assert.AreEqual(0.4365079f, scale, 0.00001f, "Rock.prefab의 m_LocalScale");
            Assert.AreEqual(SpawnerDefaults.LaneSpacing, size.y, 0.0001f, "돌은 레인 높이 1.1을 꽉 채운다.");
            Assert.AreEqual(2.444444f, size.x, 0.0001f);
            Assert.AreEqual(
                size.x,
                ObstacleVisual.BodyLengthForHeight(RockAlphaWidth, RockAlphaHeight, rock.VisualHeight),
                0.0001f);
            Assert.AreEqual(rock.BodyLength, size.x, 0.005f, "몸길이는 높이 맞춤에서 나온 가로다(2.444 → 2.44).");
        }

        [Test]
        public void Rock_SitsOnTheBottomLaneFloor()
        {
            var rock = Find(SpawnerDefaults.RockId);
            var size = ObstacleVisual.VisualWorldSize(rock, RockAlphaWidth, RockAlphaHeight);
            const float bottomLaneCenter = -SpawnerDefaults.LaneSpacing;

            Assert.AreEqual(-1.65f, bottomLaneCenter - size.y * 0.5f, 0.0001f, "바위 바닥은 맨 아랫줄 아래 경계에 앉는다.");
            Assert.AreEqual(-0.55f, bottomLaneCenter + size.y * 0.5f, 0.0001f, "바위 윗면은 맨 아랫줄 위 경계다.");
        }

        [Test]
        public void Rock_ColliderIsTheArtTimesTheHitboxScale()
        {
            var rock = Find(SpawnerDefaults.RockId);
            var size = ObstacleVisual.VisualWorldSize(rock, RockAlphaWidth, RockAlphaHeight);

            Assert.AreEqual(size.x * ObstacleVisual.HitboxScale, rock.CollisionLength, 0.005f);
            Assert.AreEqual(size.y * ObstacleVisual.HitboxScale, rock.CollisionHeight, 0.005f);
            Assert.AreEqual(2.13f, rock.CollisionLength, 0.0001f);
            Assert.AreEqual(0.96f, rock.CollisionHeight, 0.0001f);

            var scale = ObstacleVisual.UniformScale(rock, RockAlphaWidth, RockAlphaHeight);
            var local = ObstacleVisual.LocalSize(scale, rock.CollisionLength, rock.CollisionHeight);

            Assert.AreEqual(4.87964f, local.x, 0.001f, "Rock.prefab의 BoxCollider2D m_Size.x");
            Assert.AreEqual(2.19927f, local.y, 0.001f, "Rock.prefab의 BoxCollider2D m_Size.y");
        }

        [Test]
        public void Rock_DoesNotReachTheMiddleLane()
        {
            var rock = Find(SpawnerDefaults.RockId);

            Assert.IsFalse(
                rock.ReachesNeighbourLane(SpawnerDefaults.LaneSpacing, SpawnerDefaults.PlayerHitboxHeight),
                "레인을 꽉 채워도 히트박스가 중간줄을 물면 안 된다.");
        }

        [Test]
        public void FishAndLog_StillFitByLength()
        {
            foreach (var id in new[] { SpawnerDefaults.FishId, SpawnerDefaults.LogId })
            {
                var obstacle = Find(id);
                var box = ArtBox(id);

                Assert.AreEqual(ObstacleFitAxis.Length, obstacle.FitAxis, id + "은 길이 기준이다.");
                Assert.AreEqual(
                    ObstacleVisual.UniformScale(box.x, obstacle.BodyLength),
                    ObstacleVisual.UniformScale(obstacle, box.x, box.y),
                    0.00001f,
                    id + " 길이 맞춤 스케일이 바뀌면 안 된다.");
            }
        }

        [Test]
        public void CollisionHeight_IsTheArtHeightTimesTheHitboxScale()
        {
            Assert.AreEqual(0.87f, ObstacleVisual.HitboxScale, 0.0001f);
            Assert.AreEqual(0.3915f, ObstacleVisual.CollisionHeight(RockAlphaWidth, RockAlphaHeight, 1.0f, ObstacleVisual.HitboxScale), 0.0001f);
            Assert.AreEqual(0.356215f, ObstacleVisual.CollisionHeight(FishAlphaWidth, FishAlphaHeight, FishBodyLength, ObstacleVisual.HitboxScale), 0.0001f);
            Assert.AreEqual(0.849356f, ObstacleVisual.CollisionHeight(LogAlphaWidth, LogAlphaHeight, 1.8f, ObstacleVisual.HitboxScale), 0.0001f);
        }

        [Test]
        public void CollisionHeight_IsAlwaysShorterThanTheArt()
        {
            var art = ObstacleVisual.VisualHeight(LogAlphaWidth, LogAlphaHeight, 1.8f);
            var collision = ObstacleVisual.CollisionHeight(LogAlphaWidth, LogAlphaHeight, 1.8f, ObstacleVisual.HitboxScale);

            Assert.Less(collision, art, "히트박스는 그림보다 작아야 한다(4번 기획서 17절 G).");
            Assert.AreEqual(0f, ObstacleVisual.CollisionHeight(0f, 0f, 1.0f, ObstacleVisual.HitboxScale), 0.0001f);
        }

        [Test]
        public void ObstacleColliders_MatchTheSpecInWorldUnits()
        {
            foreach (var obstacle in SpawnerDefaults.CreateObstacles())
            {
                var box = ArtBox(obstacle.Id);
                var scale = ObstacleVisual.UniformScale(obstacle, box.x, box.y);
                var local = ObstacleVisual.LocalSize(scale, obstacle.CollisionLength, obstacle.CollisionHeight);
                var visual = ObstacleVisual.VisualWorldSize(obstacle, box.x, box.y);

                Assert.AreEqual(obstacle.CollisionLength, local.x * scale, 0.0001f, obstacle.Id + " collider width");
                Assert.AreEqual(obstacle.CollisionHeight, local.y * scale, 0.0001f, obstacle.Id + " collider height");

                if (obstacle.Id == SpawnerDefaults.LongRockId)
                {
                    Assert.Less(
                        obstacle.CollisionHeight,
                        visual.y * ObstacleVisual.HitboxScale,
                        "긴 돌 충돌 높이는 아트가 아니라 점프 궤적에서 나오므로 아트 × 0.87보다 낮다.");
                    Assert.AreEqual(
                        visual.x * ObstacleVisual.HitboxScale,
                        obstacle.CollisionLength,
                        0.006f,
                        obstacle.Id + " 충돌 길이는 아트에서 나온다.");
                    continue;
                }

                Assert.AreEqual(
                    visual.y * ObstacleVisual.HitboxScale,
                    obstacle.CollisionHeight,
                    0.006f,
                    obstacle.Id + " 충돌 높이는 아트에서 나온다.");
                Assert.AreEqual(
                    visual.x * ObstacleVisual.HitboxScale,
                    obstacle.CollisionLength,
                    0.006f,
                    obstacle.Id + " 충돌 길이는 아트에서 나온다.");
            }
        }

        [Test]
        public void LocalSize_GivesTheFishSpecColliderInWorldUnits()
        {
            var scale = ObstacleVisual.UniformScale(FishAlphaWidth, FishBodyLength);
            var size = ObstacleVisual.LocalSize(scale, FishCollisionLength, SpawnerDefaults.FishCollisionHeight);

            Assert.AreEqual(6.058f, size.x, 0.001f);
            Assert.AreEqual(2.796f, size.y, 0.001f);
            Assert.AreEqual(FishCollisionLength, size.x * scale, 0.0001f);
            Assert.AreEqual(SpawnerDefaults.FishCollisionHeight, size.y * scale, 0.0001f);
        }

        [Test]
        public void FishSpec_MatchesTheArtScaleInputs()
        {
            var obstacles = SpawnerDefaults.CreateObstacles();
            ObstacleSpec fish = null;

            for (var i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].Id == SpawnerDefaults.FishId)
                    fish = obstacles[i];
            }

            Assert.IsNotNull(fish, "Fish 장애물 명세가 없다.");
            Assert.AreEqual(FishBodyLength, fish.BodyLength, 0.0001f);
            Assert.AreEqual(FishCollisionLength, fish.CollisionLength, 0.0001f);
            Assert.AreEqual(SpawnerDefaults.FishCollisionHeight, fish.CollisionHeight, 0.0001f);
        }

        [Test]
        public void LongRock_ReachesFromTheRiverbedToTheWaterSurface()
        {
            var longRock = Find(SpawnerDefaults.LongRockId);
            var scale = ObstacleVisual.UniformScale(longRock, LongRockAlphaWidth, LongRockAlphaHeight);
            var size = ObstacleVisual.VisualWorldSize(longRock, LongRockAlphaWidth, LongRockAlphaHeight);

            Assert.AreEqual(ObstacleFitAxis.Height, longRock.FitAxis);
            Assert.AreEqual(0.4839538f, scale, 0.00001f, "LongRock.prefab의 Visual m_LocalScale");
            Assert.AreEqual(3.77f, size.y, 0.0005f);
            Assert.AreEqual(3.34896f, size.x, 0.0005f);
            Assert.AreEqual(size.x, longRock.BodyLength, 0.005f, "몸길이는 반올림한 아트 가로다.");
            Assert.AreEqual(
                SpawnerDefaults.LongRockVisualBottomY,
                SpawnerDefaults.LongRockVisualCenterY - size.y * 0.5f,
                0.0005f,
                "아랫변 -1.72는 강바닥 -1.65보다 조금 아래다.");
            Assert.AreEqual(
                SpawnerDefaults.LongRockVisualTopY,
                SpawnerDefaults.LongRockVisualCenterY + size.y * 0.5f,
                0.0005f,
                "윗변 2.05는 수면 2.0 위다.");
        }

        [Test]
        public void LongRockCollider_CoversEveryLaneBandAndStaysUnderTheJumpArc()
        {
            var longRock = Find(SpawnerDefaults.LongRockId);
            var half = longRock.CollisionHeight * 0.5f;
            var playerHalf = SpawnerDefaults.PlayerHitboxHeight * 0.5f;
            var topLaneY = 1.1f;
            var bottomLaneY = -1.1f;
            var apexBottom = topLaneY + 2.2f - playerHalf;

            Assert.Greater(half, topLaneY - playerHalf, "상단 레인의 연어에 닿는다.");
            Assert.Greater(half, -bottomLaneY - playerHalf, "하단 레인의 연어에 닿는다.");
            Assert.Less(half, apexBottom, "점프 정점의 연어 아랫변은 콜라이더 윗변보다 높다.");
            Assert.IsFalse(longRock.ReachesNeighbourLane(SpawnerDefaults.LaneSpacing, SpawnerDefaults.PlayerHitboxHeight));
        }

        [Test]
        public void UniformScale_FallsBackToOneWhenTheAlphaBoxIsEmpty()
        {
            Assert.AreEqual(1f, ObstacleVisual.UniformScale(0f, 1.0f), 0.0001f);
            Assert.AreEqual(1f, ObstacleVisual.UniformScale(RockAlphaWidth, 0f), 0.0001f);
        }

        [Test]
        public void LocalSize_ScalesBackToTheSpecSizeInWorldUnits()
        {
            var scale = ObstacleVisual.UniformScaleForHeight(RockAlphaHeight, SpawnerDefaults.RockVisualHeight);
            var size = ObstacleVisual.LocalSize(scale, SpawnerDefaults.RockCollisionLength, SpawnerDefaults.RockCollisionHeight);

            Assert.AreEqual(SpawnerDefaults.RockCollisionLength, size.x * scale, 0.0001f);
            Assert.AreEqual(SpawnerDefaults.RockCollisionHeight, size.y * scale, 0.0001f);
        }

        [Test]
        public void UniformScaleForHeight_FallsBackToOneWhenTheInputIsEmpty()
        {
            Assert.AreEqual(1f, ObstacleVisual.UniformScaleForHeight(0f, 1.1f), 0.0001f);
            Assert.AreEqual(1f, ObstacleVisual.UniformScaleForHeight(RockAlphaHeight, 0f), 0.0001f);
            Assert.AreEqual(1f, ObstacleVisual.UniformScale(null, RockAlphaWidth, RockAlphaHeight), 0.0001f);
        }

        private static Vector2 ArtBox(string id)
        {
            if (id == SpawnerDefaults.RockId)
                return new Vector2(RockAlphaWidth, RockAlphaHeight);

            if (id == SpawnerDefaults.FishId)
                return new Vector2(FishAlphaWidth, FishAlphaHeight);

            if (id == SpawnerDefaults.LongRockId)
                return new Vector2(LongRockAlphaWidth, LongRockAlphaHeight);

            return new Vector2(LogAlphaWidth, LogAlphaHeight);
        }

        private static ObstacleSpec Find(string id)
        {
            var obstacles = SpawnerDefaults.CreateObstacles();

            for (var i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].Id == id)
                    return obstacles[i];
            }

            Assert.Fail("장애물 명세가 없다: " + id);
            return null;
        }

        [Test]
        public void PlayerHitbox_IsSixtyPercentWideAndEightySevenPercentTall()
        {
            var world = ObstacleVisual.HitboxWorldSize(
                PlayerAlphaWidth, PlayerAlphaHeight, PlayerScale, PlayerHitboxWidthScale, PlayerHitboxHeightScale);

            Assert.AreEqual(0.92394f, world.x, 0.001f, "연어 히트박스 가로는 몸통의 60%다(기획자 답변 1).");
            Assert.AreEqual(0.70644f, world.y, 0.001f, "연어 히트박스 세로는 몸통의 87%다.");
            Assert.AreEqual(SpawnerDefaults.PlayerHitboxWidth, world.x, 0.01f);
            Assert.AreEqual(SpawnerDefaults.PlayerHitboxHeight, world.y, 0.01f);
        }

        [Test]
        public void PlayerHitbox_LocalSizeMatchesThePlayScene()
        {
            var world = ObstacleVisual.HitboxWorldSize(
                PlayerAlphaWidth, PlayerAlphaHeight, PlayerScale, PlayerHitboxWidthScale, PlayerHitboxHeightScale);

            Assert.AreEqual(3.186f, world.x / PlayerScale, 0.001f, "Play.unity의 BoxCollider2D m_Size.x");
            Assert.AreEqual(2.436f, world.y / PlayerScale, 0.001f, "Play.unity의 BoxCollider2D m_Size.y");
        }

        [Test]
        public void PlayerHalfWidth_MatchesTheSimulationAssumption()
        {
            var world = ObstacleVisual.HitboxWorldSize(
                PlayerAlphaWidth, PlayerAlphaHeight, PlayerScale, PlayerHitboxWidthScale, PlayerHitboxHeightScale);

            Assert.AreEqual(world.x * 0.5f, SpawnerDefaults.PlayerHalfWidth, 0.005f,
                "시뮬레이터의 playerHalfWidth가 실제 히트박스 반폭과 달라지면 안 된다.");
            Assert.AreEqual(SpawnerDefaults.PlayerHalfWidth, SpawnerDefaults.CreateConfig().PlayerHalfWidth, 0.0001f);
        }

        [Test]
        public void PickVariant_IsInsideTheArray()
        {
            for (var seed = 0u; seed < 64u; seed++)
            {
                var index = ObstacleVisual.PickVariant(seed, 2);
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 2);
            }
        }

        [Test]
        public void PickVariant_HandlesEmptyAndSingleVariantArrays()
        {
            Assert.AreEqual(0, ObstacleVisual.PickVariant(12345u, 0));
            Assert.AreEqual(0, ObstacleVisual.PickVariant(12345u, 1));
        }

        [Test]
        public void VariantStream_IsDeterministicForTheSameSeed()
        {
            var first = Draw(4242, 32);
            var second = Draw(4242, 32);

            CollectionAssert.AreEqual(first, second, "The same seed must pick the same rock variants.");
        }

        [Test]
        public void VariantStream_DiffersBetweenSeeds()
        {
            var first = Draw(1, 32);
            var second = Draw(2, 32);

            CollectionAssert.AreNotEqual(first, second, "Different seeds must pick different rock variants.");
        }

        [Test]
        public void VariantStream_UsesEveryVariant()
        {
            var picked = Draw(SpawnerDefaults.DefaultSeed, 64);
            Assert.Contains(0, picked, "The first rock variant never appears.");
            Assert.Contains(1, picked, "The second rock variant never appears.");
        }

        private static List<int> Draw(int seed, int count)
        {
            var random = new DeterministicRandom(seed);
            var picks = new List<int>(count);

            for (var i = 0; i < count; i++)
                picks.Add(ObstacleVisual.PickVariant(random.NextUInt(), 2));

            return picks;
        }
    }
}
