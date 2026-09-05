using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Config;
using UnityEditor;
using UnityEngine;

namespace Game.Spawner.Editor
{
    public static class SpawnerBalanceReport
    {
        private const int SeedsPerSection = 20;
        private const float StepSeconds = 0.1f;

        [MenuItem("Team1004/Spawner Balance Report")]
        public static void Generate()
        {
            var settings = FindAsset<ObstacleSpawnerSettings>();
            var configAsset = FindAsset<GameConfigAsset>();
            var values = configAsset != null ? configAsset.Values : new GameConfigValues();

            SimulationConfig config;
            List<PatternSpec> specs;
            DifficultyProfile profile;
            string source;

            if (settings != null && settings.Library != null && settings.Difficulty != null)
            {
                config = settings.CreateSimulationConfig(values);
                specs = new List<PatternSpec>();
                settings.Library.BuildSpecs(specs);
                profile = settings.Difficulty.ToProfile();
                source = AssetDatabase.GetAssetPath(settings);
            }
            else
            {
                config = SpawnerDefaults.CreateConfig();
                specs = SpawnerDefaults.CreatePatterns(SpawnerDefaults.CreateObstacles());
                profile = SpawnerDefaults.CreateProfile();
                source = "SpawnerDefaults (Design/Spawner 에셋 없음)";
            }

            if (!config.Validate(out var error))
            {
                Debug.LogError("[SpawnerBalanceReport] " + error);
                return;
            }

            var report = Build(config, specs, profile, source, configAsset != null ? AssetDatabase.GetAssetPath(configAsset) : "기본 GameConfigValues");
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Docs", "SpawnerBalance.md"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report, new UTF8Encoding(false));
            Debug.Log("[SpawnerBalanceReport] Written: " + path);
        }

        private static string Build(SimulationConfig config, List<PatternSpec> specs, DifficultyProfile profile, string source, string configSource)
        {
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            var baseAnalyses = PatternAnalyzer.AnalyzeAll(specs, config, 1f);

            sb.AppendLine("# 스포너 밸런스 리포트");
            sb.AppendLine();
            sb.AppendLine("`Team1004 > Spawner Balance Report`가 생성한다. 손으로 고치지 않는다.");
            sb.AppendLine();
            sb.AppendLine("- 생성 시각: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", culture));
            sb.AppendLine("- 스포너 설정: `" + source + "`");
            sb.AppendLine("- GameConfig: `" + configSource + "`");
            sb.AppendLine("- 스크롤 " + F(config.ScrollSpeed) + " u/s, 레인 " + config.LaneCount + ", 이동 " + F(config.LaneMoveDuration) + "s, 점프 " + F(config.JumpDuration) + "s, 쿨타임 " + F(config.JumpCooldown) + "s, 입력 허용 오차(padding) " + F(config.SafetyPadding) + "s, 격자 " + F(config.TickSeconds) + "s");
            sb.AppendLine("- 구간당 시드 " + SeedsPerSection + "개, 구간 시간 25/30/35/15초(기획서 3판 9절), 시뮬레이션 스텝 " + F(StepSeconds) + "s");
            sb.AppendLine();

            sb.AppendLine("## 패턴 (속도 배율 1.0)");
            sb.AppendLine();
            sb.AppendLine("| 패턴 | 종류 | 티어 | 최악 입력 수 | 반응 여유(s) | 도달(s) | 막는 시간(s) | 비고 |");
            sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");

            for (var i = 0; i < baseAnalyses.Count; i++)
            {
                var a = baseAnalyses[i];
                var note = !a.Solvable ? "해결 불가: " + a.Error : a.ManualMismatch ? "수동 표기와 판정 불일치" : "";
                sb.AppendLine("| " + a.Pattern.Id + " | " + Kind(a) + " | " + a.Tier + " | " + a.WorstCaseInputs + " | " + F(a.ReactionMargin) + " | " + F(a.EarliestArrival) + " | " + F(a.BlockDuration) + " | " + note + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 구간별 코스");
            sb.AppendLine();
            sb.AppendLine("| 구간 | 시간(s) | 최대 속도 배율 | 최소 반응 여유@최대속도(s) | 평균 패턴 수 | 점프 필수 비율 | 평균 도착 간격(s) | 검증 거절/코스 | 빈 결정/코스 | 해결 가능 코스 |");
            sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");

            var runner = new CourseRunner(config, specs, baseAnalyses, profile);
            var usage = new List<Dictionary<string, int>>();

            for (var section = 0; section < profile.SectionCount; section++)
            {
                var maxSpeed = profile.GetSection(section).MaxSpeedMultiplier;
                var fastAnalyses = PatternAnalyzer.AnalyzeAll(specs, config, maxSpeed);
                var minMargin = float.MaxValue;

                for (var i = 0; i < fastAnalyses.Count; i++)
                    if (fastAnalyses[i].Solvable)
                        minMargin = Math.Min(minMargin, fastAnalyses[i].ReactionMargin);

                var totalPatterns = 0;
                var totalJump = 0;
                var totalRejected = 0;
                var totalEmpty = 0;
                var survivable = 0;
                var spacingSum = 0f;
                var spacingCount = 0;
                var sectionUsage = new Dictionary<string, int>(StringComparer.Ordinal);

                for (var seed = 1; seed <= SeedsPerSection; seed++)
                {
                    var result = runner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), seed, StepSeconds);
                    totalPatterns += result.Placements.Count;
                    totalJump += result.JumpRequiredCount;
                    totalRejected += result.Stats.RejectedChecks;
                    totalEmpty += result.Stats.EmptyDecisions;
                    if (result.Survivable)
                        survivable++;

                    for (var i = 1; i < result.Placements.Count; i++)
                    {
                        spacingSum += result.Placements[i].FirstBlockStart - result.Placements[i - 1].FirstBlockStart;
                        spacingCount++;
                    }

                    foreach (var pair in result.Stats.AcceptedByPattern)
                    {
                        sectionUsage.TryGetValue(pair.Key, out var count);
                        sectionUsage[pair.Key] = count + pair.Value;
                    }
                }

                usage.Add(sectionUsage);

                var jumpRatio = totalPatterns > 0 ? totalJump / (float)totalPatterns : 0f;
                sb.AppendLine("| " + (section + 1) + " | " + F(SpawnerDefaults.GetSectionDuration(section)) + " | " + F(maxSpeed) + " | " + (minMargin == float.MaxValue ? "-" : F(minMargin)) + " | " + F(totalPatterns / (float)SeedsPerSection) + " | " + F(jumpRatio) + " | " + (spacingCount > 0 ? F(spacingSum / spacingCount) : "-") + " | " + F(totalRejected / (float)SeedsPerSection) + " | " + F(totalEmpty / (float)SeedsPerSection) + " | " + survivable + "/" + SeedsPerSection + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 구간별 패턴 사용 횟수 (시드 합계)");
            sb.AppendLine();
            sb.Append("| 패턴 |");
            for (var section = 0; section < usage.Count; section++)
                sb.Append(" 구간 " + (section + 1) + " |");
            sb.AppendLine();
            sb.Append("| --- |");
            for (var section = 0; section < usage.Count; section++)
                sb.Append(" --- |");
            sb.AppendLine();

            for (var i = 0; i < specs.Count; i++)
            {
                sb.Append("| " + specs[i].Id + " |");
                for (var section = 0; section < usage.Count; section++)
                {
                    usage[section].TryGetValue(specs[i].Id, out var count);
                    sb.Append(" " + count + " |");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("반응 여유는 패턴이 완전히 보인 뒤 아무 입력 없이 버틸 수 있는 최대 시간이다. 검증 거절은 후보가 시뮬레이터 생존 검사에 떨어진 횟수, 빈 결정은 모든 후보가 떨어져 스폰을 미룬 횟수다.");
            return sb.ToString();
        }

        private static string Kind(PatternAnalysis analysis)
        {
            if (!analysis.Solvable)
                return "불가";

            return analysis.JumpRequired ? "점프 필수" : "일반";
        }

        private static string F(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static T FindAsset<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids == null || guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
