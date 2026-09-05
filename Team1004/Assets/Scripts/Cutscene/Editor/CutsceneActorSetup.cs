using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Cutscene.Editor
{
    public static class CutsceneActorSetup
    {
        public const string ChildObjectName = "CutsceneChild";

        private const string PlaceholderSpritePath = "Assets/GameAssets/Placeholder/Square.png";
        private const float SpriteUnits = 0.64f;
        private const int ActorSortingOrder = 6;

        private static readonly Vector3 ChildPosition = new(3.4f, -0.4f, 0f);
        private static readonly Vector2 ChildSize = new(0.7f, 1.5f);
        private static readonly Color ChildColor = new(0.95f, 0.82f, 0.62f, 1f);

        public static int EnsureActors(Scene scene)
        {
            var player = FindPlayer(scene);
            if (player == null)
            {
                Debug.LogWarning(
                    "[CutsceneActorSetup] No CutscenePlayer was found in the scene. Cutscene actors were not created.");
                return 0;
            }

            return EnsureActors(player);
        }

        public static int EnsureActors(CutscenePlayer player)
        {
            if (player == null)
            {
                Debug.LogWarning("[CutsceneActorSetup] CutscenePlayer is null. Cutscene actors were not created.");
                return 0;
            }

            var added = 0;

            if (!HasActor(player, CutsceneActorIds.Child))
            {
                var child = CreateChild(player.gameObject.scene);
                Append(player, child);
                added++;
            }

            return added;
        }

        private static CutscenePlayer FindPlayer(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentInChildren<CutscenePlayer>(true);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static bool HasActor(CutscenePlayer player, string id)
        {
            var actors = player.Actors;
            if (actors == null)
                return false;

            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor != null && actor.Id == id)
                    return true;
            }

            return false;
        }

        private static CutsceneActor CreateChild(Scene scene)
        {
            var childObject = new GameObject(ChildObjectName);
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(childObject, scene);

            childObject.transform.position = ChildPosition;
            childObject.transform.localScale =
                new Vector3(ChildSize.x / SpriteUnits, ChildSize.y / SpriteUnits, 1f);

            var renderer = childObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            renderer.color = ChildColor;
            renderer.sortingOrder = ActorSortingOrder;
            renderer.enabled = false;

            if (renderer.sprite == null)
                Debug.LogWarning(
                    $"[CutsceneActorSetup] '{PlaceholderSpritePath}' is missing. " +
                    $"'{ChildObjectName}' has no sprite.", childObject);

            var actor = childObject.AddComponent<CutsceneActor>();
            var serialized = new SerializedObject(actor);
            serialized.FindProperty("id").stringValue = CutsceneActorIds.Child;
            serialized.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return actor;
        }

        private static void Append(CutscenePlayer player, CutsceneActor actor)
        {
            var serialized = new SerializedObject(player);
            var actors = serialized.FindProperty("actors");
            var index = actors.arraySize;
            actors.InsertArrayElementAtIndex(index);
            actors.GetArrayElementAtIndex(index).objectReferenceValue = actor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
