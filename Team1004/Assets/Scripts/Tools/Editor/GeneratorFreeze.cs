using UnityEditor;
using UnityEngine;

namespace Game.Tools.Editor
{
    public static class GeneratorFreeze
    {
        private const string UnfreezeKey = "Team1004.GeneratorsUnfrozen";
        private const string Message =
            "2026-09-06부터 씬·프리팹 생성기는 동결되었다. 씬과 프리팹은 YAML 직접 편집이 정본이며, 생성기를 돌리면 수작업 변경(개발자 옵션 폼, 단차 3개, 물 폴리곤, 곰 보스 등)이 사라진다. 정말 필요하면 EditorPrefs 'Team1004.GeneratorsUnfrozen'을 true로 두고 다시 실행한다.";

        public static bool IsFrozen => !EditorPrefs.GetBool(UnfreezeKey, false);

        public static bool Block(string generator)
        {
            if (!IsFrozen)
                return false;

            var text = $"[{generator}] {Message}";

            if (Application.isBatchMode)
                Debug.LogError(text);
            else
                EditorUtility.DisplayDialog("생성기 동결", text, "확인");

            return true;
        }
    }
}
