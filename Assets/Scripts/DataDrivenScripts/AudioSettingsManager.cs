using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Music Miscellaneous")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private List<Sprite> musicIcons; // 0: mute, 1: unmute

    [Header("Sfx Miscellaneous")]
    [SerializeField] private Button sfxButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private List<Sprite> sfxIcons; // 0: mute, 1: unmute

    [Header("Scripts")]
    [SerializeField] private AudioManager audioManager;

    private PlayerData playerData;

    private void Start()
    {
        PrepareObjectListeners();
        UpdateVolumeUi();
    }

    private void PrepareObjectListeners()
    {
        musicButton.onClick.AddListener(ToggleMusicMute);
        musicSlider.onValueChanged.AddListener(delegate { OnMusicSliderChanged(); });
        sfxButton.onClick.AddListener(ToggleSfxMute);
        sfxSlider.onValueChanged.AddListener(delegate { OnSfxSliderChanged(); });
    }

    private void UpdateVolumeUi()
    {
        float musicVolume = audioManager.MusicVolume;
        musicSlider.value = musicVolume;
        UpdateMusicIcon(musicVolume);

        float sfxVolume = audioManager.SfxVolume;
        sfxSlider.value = sfxVolume;
        UpdateSfxIcon(sfxVolume);
    }

    private void UpdateMusicIcon(float volume)
    {
        musicButton.GetComponent<Image>().sprite = (volume > 0f) ? musicIcons[1] : musicIcons[0];
    }

    private void UpdateSfxIcon(float volume)
    {
        sfxButton.GetComponent<Image>().sprite = (volume > 0f) ? sfxIcons[1] : sfxIcons[0];
    }

    public void ToggleMusicMute()
    {
        float newVolume = (audioManager.MusicVolume > 0f) ? 0f : 0.5f;
        audioManager.MusicVolume = newVolume;
        musicSlider.value = newVolume;
        UpdateMusicIcon(newVolume);
    }

    public void ToggleSfxMute()
    {
        float newVolume = (audioManager.SfxVolume > 0f) ? 0f : 0.5f;
        audioManager.SfxVolume = newVolume;
        sfxSlider.value = newVolume;
        UpdateSfxIcon(newVolume);
    }

    public void OnMusicSliderChanged()
    {
        float volume = musicSlider.value;
        audioManager.MusicVolume = volume;
        UpdateMusicIcon(volume);
    }

    public void OnSfxSliderChanged()
    {
        float volume = sfxSlider.value;
        audioManager.SfxVolume = volume;
        UpdateSfxIcon(volume);
    }
}
