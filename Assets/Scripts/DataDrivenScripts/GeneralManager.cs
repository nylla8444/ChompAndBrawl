using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralManager : MonoBehaviour
{
    [SerializeField] private DefaultPlayerData defaultPlayerData;
    [SerializeField] private DefaultInputKeybind defaultInputKeybind;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject transitionScreen;
    [SerializeField] private Button directResetButton;
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
        PlayerDataManager.Initialize(defaultPlayerData);
        KeybindDataManager.Initialize(defaultInputKeybind);
    }

    private void PrepareObjectListeners()
    {
        try { directResetButton.onClick.AddListener(PrepareResetData); } catch (Exception ex) { Debug.LogWarning("Reset button does not exist. " + ex.Message); }
        try { directQuitButton.onClick.AddListener(ExecuteQuit); } catch (Exception ex) { Debug.LogWarning("Quit button does not exist. " + ex.Message); }
    }

    private void InitializeUi()
    {
        try { menuPanel.SetActive(true); } catch (Exception ex) { Debug.LogWarning("Menu panel does not exist. " + ex.Message); }
        try { transitionScreen.SetActive(true); } catch (Exception ex) { Debug.LogWarning("Transition screen does not exist. " + ex.Message); }
        try { dialoguePanel.SetActive(true); } catch (Exception ex) { Debug.LogWarning("Dialogue overlay does not exist. " + ex.Message); }
    }

    public void PrepareResetData()
    {
        KeybindDataManager.DeleteKeyBindings();
        PlayerDataManager.DeleteData();
        PlayerDataManager.Initialize(defaultPlayerData);
        PlayerDataManager.LoadData();
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
 
    private void OnApplicationFocus(bool hasFocus)
    {
        PauseOnDeviceOnly();
    }

    private void OnApplicationPause(bool hasPause)
    {
        PauseOnDeviceOnly();
    }

    private void OnApplicationQuit()
    {
        PauseOnDeviceOnly();
    }
}
