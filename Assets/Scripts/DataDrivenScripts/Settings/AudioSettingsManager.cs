using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [SerializeField] private Button musicButton;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private List<Sprite> musicIcons; // 0: mute, 1: unmute

    [SerializeField] private Button sfxButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private List<Sprite> sfxIcons; // 0: mute, 1: unmute

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
        PlayerData playerData = PlayerDataManager.LoadData();
        
        float musicVolume = playerData.audio.music_volume;
        musicSlider.value = musicVolume;
        UpdateMusicIcon(musicVolume);

        float sfxVolume = playerData.audio.sfx_volume;
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
        PlayerData playerData = PlayerDataManager.LoadData();
        
        float newVolume = (playerData.audio.music_volume > 0f) ? 0f : 0.5f;
        playerData.audio.music_volume = newVolume;
        musicSlider.value = newVolume;
        UpdateMusicIcon(newVolume);
        
        PlayerDataManager.SaveData(playerData);
    }

    public void ToggleSfxMute()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        
        float newVolume = (playerData.audio.sfx_volume > 0f) ? 0f : 0.5f;
        playerData.audio.sfx_volume = newVolume;
        sfxSlider.value = newVolume;
        UpdateSfxIcon(newVolume);
        
        PlayerDataManager.SaveData(playerData);
    }

    public void OnMusicSliderChanged()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        
        float volume = musicSlider.value;
        playerData.audio.music_volume = volume;
        UpdateMusicIcon(volume);
        
        PlayerDataManager.SaveData(playerData);
    }

    public void OnSfxSliderChanged()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        
        float volume = sfxSlider.value;
        playerData.audio.sfx_volume = volume;
        UpdateSfxIcon(volume);
        
        PlayerDataManager.SaveData(playerData);
    }
}
