using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralManager : MonoBehaviour
{
    [Header("===Default Data===")]
    [SerializeField] private DefaultIngameData defaultIngameData;
    [SerializeField] private DefaultPlayerData defaultPlayerData;
    [SerializeField] private DefaultInputKeybind defaultInputKeybind;
    [SerializeField] private DefaultLeaderboardData defaultLeaderboardData;

    [Header("===General GameObjects===")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject dialoguePanel;

    [Header("===Direct Buttons===")]
    [SerializeField] private Button directResetButton;
    [SerializeField] private Button directTrueResetButton;
    [SerializeField] private Button directQuitButton;
    
    private void Awake()
    {
        PrepareApplicationSettings();
        PrepareAllData();
        PrepareObjectListeners();
        InitializeUi();
    }

    private void Update()
    {
        KeybindDataManager.Update();
    }

    private void PrepareApplicationSettings()
    {
        // Set the application as running in background, and the time scale should be in normal state
        Application.runInBackground = true;
        Time.timeScale = 1.0f;
    }

    private void PrepareAllData()
    {
        // Initialize data managers with the default data
        try { IngameDataManager.Initialize(defaultIngameData); } catch (Exception) { }
        try { PlayerDataManager.Initialize(defaultPlayerData); } catch (Exception) { }
        try { KeybindDataManager.Initialize(defaultInputKeybind); } catch (Exception) { }
        try { LeaderboardDataManager.Initialize(defaultLeaderboardData); } catch (Exception) { }
    }

    private void PrepareObjectListeners()
    {
        try { directResetButton.onClick.AddListener(PrepareResetData); } catch (Exception) {  }
        try { directTrueResetButton.onClick.AddListener(PrepareTrueResetData); } catch (Exception) { }
        try { directQuitButton.onClick.AddListener(ExecuteQuit); } catch (Exception) { }
    }

    private void InitializeUi()
    {
        try { menuPanel.SetActive(true); } catch (Exception) { }
        try { dialoguePanel.SetActive(true); } catch (Exception) { }
    }

    public void PrepareResetData()
    {
        IngameDataManager.DeleteData();
        KeybindDataManager.DeleteKeyBindings();
    }

    public void PrepareTrueResetData()
    {
        IngameDataManager.DeleteData();
        PlayerDataManager.DeleteData();
        KeybindDataManager.DeleteKeyBindings();
        LeaderboardDataManager.DeleteData();
    }

    public void ExecuteQuit()
    {
        // Quit the application if the quit button is pressed
        Application.Quit();
        Debug.Log("Successfully executed quit.");
    }

    private void PauseOnDeviceOnly()
    {
        // Pause the device if the application loses focus
        if (Application.isEditor) return;
        Time.timeScale = 0.0f;
        Debug.Log("Device currently on pause. Unfocusing the device.");

        PlayerData playerData = PlayerDataManager.LoadData();
        if (playerData == null)
        {
            Debug.LogError("Failed to retrieve data.");
            return;
        }

        PlayerDataManager.SaveData(playerData);
    }
 
    // private void OnApplicationFocus(bool hasFocus)
    // {
    //     if (!hasFocus) PauseOnDeviceOnly();
    // }

    // private void OnApplicationPause(bool hasPause)
    // {
    //     if (!hasPause) PauseOnDeviceOnly();
    // }

    private void OnApplicationQuit()
    {
        PrepareResetData();
    }
}
