using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AudioManager : Singleton<AudioManager>
{
    private struct ChannelState
    {
        public AudioSource Source;
        public Transform FollowTarget;
        public bool StopWithTarget;
        public bool Loop;
        public bool SceneBound;
        public int Remaining;
        public float BaseVolume;
        public long Sequence;
    }

    private struct BgmState
    {
        public AudioSource Source;
        public float BaseVolume;
    }

    private readonly Dictionary<string, AudioClip> clips = new(StringComparer.Ordinal);
    private readonly Dictionary<int, ChannelState> activeChannels = new();
    private readonly Dictionary<string, BgmState> bgmLayers = new(StringComparer.Ordinal);
    private readonly Queue<AudioSource> availableChannels = new();
    private readonly List<int> removeBuffer = new();
    private readonly List<int> replayBuffer = new();
    private readonly HashSet<string> missingIdWarnings = new(StringComparer.Ordinal);

    private IAudioClipProvider provider;
    private Transform poolParent;
    private int channelCount;
    private int maxSfxChannels = 32;
    private int nextId = 1;
    private long nextSequence = 1;
    private float spatialBlend = 1f;
    private float minDistance = 1f;
    private float maxDistance = 15f;
    private bool initializationWarningReported;
    private bool voiceLimitWarningReported;

    public bool IsInitializing { get; private set; }
    public bool IsInitialized { get; private set; }
    public int ActiveSfxCount => activeChannels.Count;
    public string LastSfxId { get; private set; }
    public AudioVolumeMixer Mixer { get; } = new();

    public float MasterVolume
    {
        get => Mixer.MasterVolume;
        set => Mixer.MasterVolume = value;
    }

    public float SfxVolume
    {
        get => Mixer.SfxVolume;
        set => Mixer.SfxVolume = value;
    }

    public float BgmVolume
    {
        get => Mixer.BgmVolume;
        set => Mixer.BgmVolume = value;
    }

    public bool MasterMuted
    {
        get => Mixer.MasterMuted;
        set => Mixer.MasterMuted = value;
    }

    public bool SfxMuted
    {
        get => Mixer.SfxMuted;
        set => Mixer.SfxMuted = value;
    }

    public bool BgmMuted
    {
        get => Mixer.BgmMuted;
        set => Mixer.BgmMuted = value;
    }

    protected override void OnRegistered()
    {
        poolParent = transform;
        Mixer.Changed += RefreshVolumes;
    }

    protected override void OnUnregistering()
    {
        Mixer.Changed -= RefreshVolumes;
        StopAllSfx();
        StopAllBgm();
        provider?.Release();
        provider = null;
        clips.Clear();
        LastSfxId = null;
        IsInitialized = false;
        IsInitializing = false;
    }

    private void Update()
    {
        if (!IsInitialized || activeChannels.Count == 0)
            return;

        removeBuffer.Clear();
        replayBuffer.Clear();

        foreach (var pair in activeChannels)
        {
            var state = pair.Value;
            if (state.Source == null)
            {
                removeBuffer.Add(pair.Key);
                continue;
            }

            if (state.FollowTarget != null)
                state.Source.transform.position = state.FollowTarget.position;
            else if (state.StopWithTarget)
            {
                removeBuffer.Add(pair.Key);
                continue;
            }

            if (state.Source.isPlaying)
                continue;

            if (state.Remaining > 1)
                replayBuffer.Add(pair.Key);
            else
                removeBuffer.Add(pair.Key);
        }

        for (var i = 0; i < replayBuffer.Count; i++)
        {
            var id = replayBuffer[i];
            if (!activeChannels.TryGetValue(id, out var state) || state.Source == null)
                continue;

            state.Remaining--;
            activeChannels[id] = state;
            state.Source.Play();
        }

        for (var i = 0; i < removeBuffer.Count; i++)
            ReturnChannel(removeBuffer[i]);
    }

    public async Awaitable<bool> InitializeAsync(
        IAudioClipProvider clipProvider,
        IReadOnlyList<AudioEntryData> entries,
        AudioRuntimeSettings settings = null)
    {
        if (clipProvider == null)
            throw new ArgumentNullException(nameof(clipProvider));

        if (IsInitialized)
            throw new InvalidOperationException("AudioManager is already initialized.");

        if (IsInitializing)
            throw new InvalidOperationException("AudioManager initialization is already in progress.");

        var validEntries = entries ?? Array.Empty<AudioEntryData>();
        ValidateEntries(validEntries);

        var runtimeSettings = settings ?? new AudioRuntimeSettings();
        runtimeSettings.Validate();

        IsInitializing = true;
        provider = clipProvider;
        var owned = true;

        try
        {
            var loadedClips = await clipProvider.LoadAsync(validEntries);

            if (this == null || provider != clipProvider)
            {
                owned = false;
                clipProvider.Release();
                return false;
            }

            if (loadedClips == null)
                throw new InvalidOperationException("Audio clip provider returned null.");

            clips.Clear();
            for (var i = 0; i < validEntries.Count; i++)
            {
                var id = validEntries[i].Id;
                if (!loadedClips.TryGetValue(id, out var clip) || clip == null)
                    throw new InvalidOperationException($"Audio clip provider did not return a clip for '{id}'.");

                clips.Add(id, clip);
            }

            ApplySettings(runtimeSettings);
            IsInitialized = true;
            initializationWarningReported = false;
            missingIdWarnings.Clear();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            clipProvider.Release();

            if (provider == clipProvider)
                provider = null;

            clips.Clear();
            return false;
        }
        finally
        {
            if (owned)
                IsInitializing = false;
        }
    }

    public AudioHandle PlayGlobal(
        string id,
        float volume = 1f,
        int repeatCount = 1,
        bool sceneBound = true)
    {
        return PlaySfx(
            id,
            Vector3.zero,
            null,
            false,
            0f,
            volume,
            repeatCount,
            false,
            sceneBound);
    }

    public AudioHandle PlayAt(
        string id,
        Vector3 position,
        float volume = 1f,
        int repeatCount = 1,
        bool sceneBound = true)
    {
        return PlaySfx(
            id,
            position,
            null,
            false,
            spatialBlend,
            volume,
            repeatCount,
            false,
            sceneBound);
    }

    public AudioHandle PlayAttached(
        string id,
        Transform target,
        float volume = 1f,
        int repeatCount = 1,
        bool sceneBound = true)
    {
        if (target == null)
        {
            Debug.LogWarning($"[AudioManager] Attached target is null for '{id}'.");
            return AudioHandle.Invalid;
        }

        return PlaySfx(
            id,
            target.position,
            target,
            true,
            spatialBlend,
            volume,
            repeatCount,
            false,
            sceneBound);
    }

    public AudioHandle PlayLoopAt(
        string id,
        Vector3 position,
        float volume = 1f,
        bool sceneBound = true)
    {
        return PlaySfx(
            id,
            position,
            null,
            false,
            spatialBlend,
            volume,
            1,
            true,
            sceneBound);
    }

    public AudioHandle PlayLoopGlobal(
        string id,
        float volume = 1f,
        bool sceneBound = true)
    {
        return PlaySfx(
            id,
            Vector3.zero,
            null,
            false,
            0f,
            volume,
            1,
            true,
            sceneBound);
    }

    public AudioHandle PlayLoopAttached(
        string id,
        Transform target,
        float volume = 1f,
        bool sceneBound = true)
    {
        if (target == null)
        {
            Debug.LogWarning($"[AudioManager] Attached target is null for '{id}'.");
            return AudioHandle.Invalid;
        }

        return PlaySfx(
            id,
            target.position,
            target,
            true,
            spatialBlend,
            volume,
            1,
            true,
            sceneBound);
    }

    public void Stop(AudioHandle handle)
    {
        if (handle.IsValid)
            ReturnChannel(handle.Id);
    }

    public bool IsPlaying(AudioHandle handle)
    {
        return handle.IsValid &&
               activeChannels.TryGetValue(handle.Id, out var state) &&
               state.Source != null &&
               (state.Source.isPlaying || state.Remaining > 1);
    }

    public void SetVolume(AudioHandle handle, float volume)
    {
        if (!handle.IsValid ||
            !activeChannels.TryGetValue(handle.Id, out var state) ||
            state.Source == null)
            return;

        state.BaseVolume = Mathf.Clamp01(volume);
        state.Source.volume = Mixer.Calculate(AudioBus.Sfx, state.BaseVolume);
        activeChannels[handle.Id] = state;
    }

    public void PlayBgm(string id, float volume = 1f, bool loop = true)
    {
        if (!TryGetClip(id, out var clip))
            return;

        if (!bgmLayers.TryGetValue(id, out var state) || state.Source == null)
        {
            state = new BgmState
            {
                Source = CreateBgmSource(id),
                BaseVolume = Mathf.Clamp01(volume)
            };
        }

        state.BaseVolume = Mathf.Clamp01(volume);
        state.Source.volume = Mixer.Calculate(AudioBus.Bgm, state.BaseVolume);
        state.Source.loop = loop;

        if (state.Source.clip != clip || !state.Source.isPlaying)
        {
            state.Source.clip = clip;
            state.Source.Play();
        }

        bgmLayers[id] = state;
    }

    public bool IsBgmPlaying(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
               bgmLayers.TryGetValue(id, out var state) &&
               state.Source != null &&
               state.Source.isPlaying;
    }

    public void StopBgm(string id)
    {
        if (bgmLayers.TryGetValue(id, out var state) && state.Source != null)
            state.Source.Stop();
    }

    public void StopAllBgm()
    {
        foreach (var state in bgmLayers.Values)
            if (state.Source != null)
                state.Source.Stop();
    }

    public void StopSceneSounds()
    {
        removeBuffer.Clear();
        foreach (var pair in activeChannels)
            if (pair.Value.SceneBound)
                removeBuffer.Add(pair.Key);

        for (var i = 0; i < removeBuffer.Count; i++)
            ReturnChannel(removeBuffer[i]);
    }

    public void StopAllSfx()
    {
        removeBuffer.Clear();
        foreach (var id in activeChannels.Keys)
            removeBuffer.Add(id);

        for (var i = 0; i < removeBuffer.Count; i++)
            ReturnChannel(removeBuffer[i]);
    }

    private AudioHandle PlaySfx(
        string id,
        Vector3 position,
        Transform followTarget,
        bool stopWithTarget,
        float sourceSpatialBlend,
        float volume,
        int repeatCount,
        bool loop,
        bool sceneBound)
    {
        if (!TryGetClip(id, out var clip))
            return AudioHandle.Invalid;

        var source = GetChannel();
        if (source == null)
            return AudioHandle.Invalid;

        source.transform.SetParent(poolParent, false);
        source.transform.position = position;
        source.clip = clip;
        source.loop = loop;
        source.spatialBlend = Mathf.Clamp01(sourceSpatialBlend);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;

        var baseVolume = Mathf.Clamp01(volume);
        source.volume = Mixer.Calculate(AudioBus.Sfx, baseVolume);
        source.Play();

        LastSfxId = id;
        var handle = new AudioHandle(NextId());
        activeChannels[handle.Id] = new ChannelState
        {
            Source = source,
            FollowTarget = followTarget,
            StopWithTarget = stopWithTarget,
            Loop = loop,
            SceneBound = sceneBound,
            Remaining = loop ? int.MaxValue : Mathf.Max(1, repeatCount),
            BaseVolume = baseVolume,
            Sequence = nextSequence++
        };
        return handle;
    }

    private bool TryGetClip(string id, out AudioClip clip)
    {
        if (!IsInitialized)
        {
            if (!initializationWarningReported)
            {
                initializationWarningReported = true;
                Debug.LogWarning("[AudioManager] AudioManager is not initialized.");
            }

            clip = null;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(id) && clips.TryGetValue(id, out clip))
            return true;

        if (missingIdWarnings.Add(id ?? string.Empty))
            Debug.LogWarning($"[AudioManager] Unknown audio ID: '{id}'.");

        clip = null;
        return false;
    }

    private void ApplySettings(AudioRuntimeSettings settings)
    {
        maxSfxChannels = settings.MaxSfxChannels;
        spatialBlend = settings.SpatialBlend;
        minDistance = settings.MinDistance;
        maxDistance = settings.MaxDistance;
        Mixer.Apply(settings);

        while (channelCount < settings.InitialPoolSize)
            availableChannels.Enqueue(CreateChannel());
    }

    private static void ValidateEntries(IReadOnlyList<AudioEntryData> entries)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null)
                throw new InvalidOperationException($"Audio entry at index {i} is null.");

            if (string.IsNullOrWhiteSpace(entry.Id))
                throw new InvalidOperationException($"Audio ID at index {i} is empty.");

            if (string.IsNullOrWhiteSpace(entry.LoadKey))
                throw new InvalidOperationException($"Audio load key for '{entry.Id}' is empty.");

            if (!ids.Add(entry.Id))
                throw new InvalidOperationException($"Duplicate audio ID: '{entry.Id}'.");
        }
    }

    private AudioSource GetChannel()
    {
        while (availableChannels.Count > 0 && availableChannels.Peek() == null)
        {
            availableChannels.Dequeue();
            channelCount = Mathf.Max(0, channelCount - 1);
        }

        if (availableChannels.Count == 0 && channelCount < maxSfxChannels)
            availableChannels.Enqueue(CreateChannel());

        if (availableChannels.Count == 0)
        {
            var oldestId = 0;
            var oldestSequence = long.MaxValue;

            foreach (var pair in activeChannels)
            {
                var state = pair.Value;
                if (state.Loop || state.Source == null || state.Sequence >= oldestSequence)
                    continue;

                oldestId = pair.Key;
                oldestSequence = state.Sequence;
            }

            if (oldestId > 0)
                ReturnChannel(oldestId);
        }

        if (availableChannels.Count == 0)
        {
            if (!voiceLimitWarningReported)
            {
                voiceLimitWarningReported = true;
                Debug.LogWarning("[AudioManager] SFX channel limit reached.");
            }

            return null;
        }

        var source = availableChannels.Dequeue();
        source.gameObject.SetActive(true);
        return source;
    }

    private AudioSource CreateChannel()
    {
        var channel = new GameObject($"SFXChannel_{channelCount:00}");
        channel.transform.SetParent(poolParent, false);
        var source = channel.AddComponent<AudioSource>();
        source.playOnAwake = false;
        channel.SetActive(false);
        channelCount++;
        return source;
    }

    private AudioSource CreateBgmSource(string id)
    {
        var layer = new GameObject($"BGM_{id}");
        layer.transform.SetParent(poolParent, false);
        var source = layer.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        return source;
    }

    private void ReturnChannel(int id)
    {
        if (!activeChannels.TryGetValue(id, out var state))
            return;

        activeChannels.Remove(id);

        var source = state.Source;
        if (source == null)
        {
            channelCount = Mathf.Max(0, channelCount - 1);
            return;
        }

        source.Stop();
        source.clip = null;
        source.loop = false;
        source.spatialBlend = 0f;
        source.transform.SetParent(poolParent, false);
        source.gameObject.SetActive(false);
        availableChannels.Enqueue(source);
        voiceLimitWarningReported = false;
    }

    private int NextId()
    {
        if (nextId <= 0 || nextId == int.MaxValue)
            nextId = 1;

        while (activeChannels.ContainsKey(nextId))
        {
            nextId++;
            if (nextId == int.MaxValue)
                nextId = 1;
        }

        return nextId++;
    }

    private void RefreshVolumes()
    {
        foreach (var pair in activeChannels)
        {
            var state = pair.Value;
            if (state.Source != null)
                state.Source.volume = Mixer.Calculate(AudioBus.Sfx, state.BaseVolume);
        }

        foreach (var state in bgmLayers.Values)
            if (state.Source != null)
                state.Source.volume = Mixer.Calculate(AudioBus.Bgm, state.BaseVolume);
    }
}
