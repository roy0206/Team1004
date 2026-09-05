using System;
using UnityEngine;

public sealed class AudioVolumeMixer
{
    private float masterVolume = 1f;
    private float sfxVolume = 1f;
    private float bgmVolume = 1f;
    private bool masterMuted;
    private bool sfxMuted;
    private bool bgmMuted;

    public event Action Changed;

    public float MasterVolume
    {
        get => masterVolume;
        set => SetVolume(ref masterVolume, value);
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set => SetVolume(ref sfxVolume, value);
    }

    public float BgmVolume
    {
        get => bgmVolume;
        set => SetVolume(ref bgmVolume, value);
    }

    public bool MasterMuted
    {
        get => masterMuted;
        set => SetMuted(ref masterMuted, value);
    }

    public bool SfxMuted
    {
        get => sfxMuted;
        set => SetMuted(ref sfxMuted, value);
    }

    public bool BgmMuted
    {
        get => bgmMuted;
        set => SetMuted(ref bgmMuted, value);
    }

    public float Calculate(AudioBus bus, float baseVolume)
    {
        if (masterMuted)
            return 0f;

        var busMuted = bus == AudioBus.Sfx ? sfxMuted : bgmMuted;
        if (busMuted)
            return 0f;

        var busVolume = bus == AudioBus.Sfx ? sfxVolume : bgmVolume;
        return Mathf.Clamp01(baseVolume) * masterVolume * busVolume;
    }

    public void Apply(AudioRuntimeSettings settings)
    {
        if (settings == null)
            return;

        masterVolume = settings.MasterVolume;
        sfxVolume = settings.SfxVolume;
        bgmVolume = settings.BgmVolume;
        masterMuted = settings.MasterMuted;
        sfxMuted = settings.SfxMuted;
        bgmMuted = settings.BgmMuted;
        Changed?.Invoke();
    }

    private void SetVolume(ref float target, float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(target, value))
            return;

        target = value;
        Changed?.Invoke();
    }

    private void SetMuted(ref bool target, bool value)
    {
        if (target == value)
            return;

        target = value;
        Changed?.Invoke();
    }
}
