using Game.Config;
using Game.Dialogue;
using Game.Settings;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string PlayerMap = "Player";
        private const string UiMap = "UI";

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private TextAsset audioManifest;
        [SerializeField] private GameConfigAsset configAsset;
        [SerializeField] private TextAsset dialogueCsv;
        [SerializeField] private SceneReference firstScene;

        public static bool IsComplete { get; private set; }

        private async void Start()
        {
            IsComplete = false;

            if (!ResourceManager.Instance.IsInitialized)
                ResourceManager.Instance.Initialize(new ResourcesResourceProvider());

            if (!PoolManager.Instance.IsInitialized)
                PoolManager.Instance.Initialize(new PoolSettings());

            if (!InputManager.Instance.IsInitialized)
                InputManager.Instance.Initialize(inputActions);

            if (!AudioManager.Instance.IsInitialized)
            {
                var manifest = JsonConvert.DeserializeObject<AudioManifest>(audioManifest.text);
                await AudioManager.Instance.InitializeAsync(
                    new ResourcesAudioClipProvider(), manifest.Sounds, manifest.Settings);
            }

            new AudioSceneBridge(AudioManager.Instance, SceneController.Instance).Attach();
            new PoolSceneBridge(PoolManager.Instance, SceneController.Instance).Attach();

            GameConfig.Load(configAsset);

            if (dialogueCsv != null)
                DialogueService.Load(dialogueCsv);
            else
                Debug.LogWarning("[GameBootstrap] Dialogue CSV is not assigned. Cutscene lines will be missing.", this);
            await SettingsService.LoadAsync();
            SettingsService.Apply();
            await RecordService.LoadAsync();

            InputManager.Instance.EnableMap(PlayerMap);
            InputManager.Instance.EnableMap(UiMap);
            IsComplete = true;

            await SceneController.Instance.LoadAsync(ResolveFirstScene());
        }

        private SceneReference ResolveFirstScene()
        {
            if (EditorPlayback.TryGetReturnScenePath(out var returnScenePath) &&
                SceneController.Instance.TryGetScene(returnScenePath, out var returnScene))
                return returnScene;

            return firstScene;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsComplete = false;
        }
    }
}
