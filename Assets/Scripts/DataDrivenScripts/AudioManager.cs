using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioList audioList;
    private Dictionary<string, AudioSource> activeAudioSources = new Dictionary<string, AudioSource>();

    private float musicVolume = 0.5f;
    private float sfxVolume = 0.5f;

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            UpdateVolume("music");

            PlayerData playerData = PlayerDataManager.LoadData();
            playerData.audio.music_volume = musicVolume;
            PlayerDataManager.SaveData(playerData);
        }
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            UpdateVolume("sfx");

            PlayerData playerData = PlayerDataManager.LoadData();
            playerData.audio.sfx_volume = sfxVolume;
            PlayerDataManager.SaveData(playerData);
        }
    }

    public void Start()
    {
        PlayerData playerData = PlayerDataManager.LoadData();

        MusicVolume = playerData.audio.music_volume;
        SfxVolume = playerData.audio.sfx_volume;
    }

    public void PlayAudio(string audioIdentifier, bool shouldLoop)
    {
        string[] parts = audioIdentifier.Split('.');
        if (parts.Length != 3) return;

        string type = parts[0];
        string category = parts[1];
        string id = parts[2];

        AudioClip clip = GetAudioClip(type, category, id);
        if (clip == null) return;

        if (activeAudioSources.ContainsKey(audioIdentifier))
        {
            if (shouldLoop) return;
            StopAudio(audioIdentifier);
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = shouldLoop;
        source.volume = (type == "music") ? musicVolume : sfxVolume;
        source.Play();

        activeAudioSources[audioIdentifier] = source;
    }

    public void StopAudio(string audioIdentifier)
    {
        if (activeAudioSources.TryGetValue(audioIdentifier, out AudioSource source))
        {
            source.Stop();
            Destroy(source);
            activeAudioSources.Remove(audioIdentifier);
        }
    }

    private AudioClip GetAudioClip(string type, string category, string id)
    {
        List<AudioCategory> categories = null;

        switch (type)
        {
            case "music":
                categories = audioList.musicCategories;
                break;
            
            case "sfx":
                categories = audioList.sfxCategories;
                break;

            default:
                return null;
        }

        foreach (var cat in categories)
        {
            if (cat.name == category)
            {
                foreach (var clipWithId in cat.audioClips)
                {
                    if (clipWithId.id == id)
                    {
                        return clipWithId.audioClip;
                    }
                }
            }
        }

        return null;
    }

    private void UpdateVolume(string type)
    {
        foreach (var kvp in activeAudioSources)
        {
            string[] parts = kvp.Key.Split('.');
            if (parts[0] == type)
            {
                kvp.Value.volume = (type == "music") ? musicVolume : sfxVolume;
            }
        }
    }
}
