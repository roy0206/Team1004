namespace Game.Settings
{
    public static class UiSound
    {
        private const string ClickId = "ui_click";

        public static void PlayClick()
        {
            if (AudioManager.TryGetInstance(out var audio) && audio.IsInitialized)
                audio.PlayGlobal(ClickId, sceneBound: false);
        }
    }
}
