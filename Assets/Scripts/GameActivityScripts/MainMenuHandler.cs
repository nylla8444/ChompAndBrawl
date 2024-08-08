using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MEC;

public class MainMenuHandler : MonoBehaviour
{
    [Header("===Main Menu Buttons==="), Space(12)]
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;

    [Header("===Main Misc==="), Space(12)]
    [SerializeField] private SceneDictionary mainSceneIds;
    [SerializeField] private SceneDictionary ingameMapSceneIds;
    [SerializeField] private IngameMapList ingameMapList;
    [SerializeField] private AudioManager audioManager;

    [Header("===Map Selection Misc==="), Space(12)]
    [SerializeField] private Button prevMapButton;
    [SerializeField] private Button nextMapButton;
    [SerializeField] private Button backMapButton;
    [SerializeField] private List<GameObject> mapDisplayObjects;
    [SerializeField] private Text mapTitleText;
    private int currentStartIndex = 0;

    [Header("===Game Preparation Misc==="), Space(12)]
    [SerializeField] private GameObject gamePreparationScreen;
    [SerializeField] private Button pacmanPrepButton;
    [SerializeField] private Button ghostPrepButton;
    [SerializeField] private Button backPrepButton;
    [SerializeField] private Image prepMapDisplay;
    [SerializeField] private Text prepMapTitleText;
    [SerializeField] private Text prepMapDescText;
    [SerializeField] private List<GameObject> pacmanPrepMessages;   // 0: prepare, 1: ready
    [SerializeField] private List<GameObject> ghostPrepMessages;    // 0: prepare, 1: ready
    [SerializeField] private GameObject preparingMessageObj;
    private bool isPacmanPrepared = false;
    private bool isGhostPrepared = false;

    [Header("===Loading Screen Misc==="), Space(12)]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject loadingFloaterObject;
    [SerializeField] private List<Transform> loadingAnchors;       // 0: start, 1: end
    [SerializeField] private Image loadMapDisplay;
    [SerializeField] private Text loadMapTitleText;

    [Header("===Leaderboard Misc==="), Space(12)]
    [SerializeField] private GameObject rowBoardPrefab;
    [SerializeField] private Transform rowAnchor;
    [SerializeField] private Button backLeadButton;
    
    [Header("===Settings Misc==="), Space(12)]
    [SerializeField] private List<Button> musicVolumeButtons;      // 0: low, 1: high
    [SerializeField] private List<Button> sfxVolumeButtons;        // 0: low, 1: high
    [SerializeField] private List<GameObject> musicVolumeBars;     // 10 bars
    [SerializeField] private List<GameObject> sfxVolumeBars;       // 10 bars
    [SerializeField] private Button backSettingsButton;
    
    /*********************************************************************/
    //
    //                           General
    //
    /*********************************************************************/

    private void Start()
    {
        SetForCurrentScene();
    }

    private void SetForCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == mainSceneIds.GetSceneName("main_menu_scene"))
        {
            MainMenu_Start();
        }
        else if (currentScene == mainSceneIds.GetSceneName("map_selection_scene"))
        {
            MapSelection_Start();
        }
        else if (currentScene == mainSceneIds.GetSceneName("game_preparation_scene"))
        {
            GamePreparation_Start();
        }
        else if (currentScene == mainSceneIds.GetSceneName("leaderboard_scene"))
        {
            Leaderboard_Start();
        }
        else if (currentScene == mainSceneIds.GetSceneName("settings_scene"))
        {
            Settings_Start();
        }
    }

    private void OnDestroy()
    {
        KeybindDataManager.ResetKeyActions();
        Timing.KillCoroutines();
    }

    /*********************************************************************/
    //
    //                           Main Menu
    //
    /*********************************************************************/

    private IEnumerator<float> OnMainMenuSelected()
    {
        audioManager.PlayAudio("sfx.general.button_pressed", false);
        yield return Timing.WaitForSeconds(0.5f);
        SceneManager.LoadScene(mainSceneIds.GetSceneName("main_menu_scene"));
    }

    private void MainMenu_Start()
    {
        MainMenu_PrepareObjectListeners();
    }
    
    private void MainMenu_PrepareObjectListeners()
    {
        try { startButton.onClick.AddListener(() => Timing.RunCoroutine(OnMapSelectionSelected())); } catch (Exception) { }
        try { leaderboardButton.onClick.AddListener(() => Timing.RunCoroutine(OnLeaderboardSelected())); } catch (Exception) { }
        try { settingsButton.onClick.AddListener(() => Timing.RunCoroutine(OnSettingsSelected())); } catch (Exception) { }
    }

    /*********************************************************************/
    //
    //                         Map Selection
    //
    /*********************************************************************/

    private IEnumerator<float> OnMapSelectionSelected()
    {
        audioManager.PlayAudio("sfx.general.button_pressed", false);
        yield return Timing.WaitForSeconds(0.5f);
        SceneManager.LoadScene(mainSceneIds.GetSceneName("map_selection_scene"));
    }

    private void MapSelection_Start()
    {
        MapSelection_PrepareObjectListeners();
        MapSelection_RegisterKeyActions();
    }

    private void MapSelection_PrepareObjectListeners()
    {
        prevMapButton.onClick.AddListener(() => UpdateMapRotationDisplay(true));
        nextMapButton.onClick.AddListener(() => UpdateMapRotationDisplay(false));
        backMapButton.onClick.AddListener(() => OntoBackFromMapSelection());
    }

    private void MapSelection_RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.move_next_selection", () => OntoNextMap());
        KeybindDataManager.RegisterKeyAction("general.move_previous_selection", () => OntoPreviousMap());
        KeybindDataManager.RegisterKeyAction("general.go_select", () => OnMapSelect());
        KeybindDataManager.RegisterKeyAction("general.go_back", () => OntoBackFromMapSelection());
    }

    private void UpdateMapRotationDisplay(bool isNext = true)
    {
        if (isNext)
        {
            currentStartIndex = (currentStartIndex + 1) % ingameMapList.ingameMaps.Count;
        }
        else
        {
            currentStartIndex = (currentStartIndex - 1 + ingameMapList.ingameMaps.Count) % ingameMapList.ingameMaps.Count;
        }

        for (int i = 0; i < mapDisplayObjects.Count; i++)
        {
            int mapIndex = (currentStartIndex + i) % ingameMapList.ingameMaps.Count;
            IngameMap map = ingameMapList.ingameMaps[mapIndex];

            mapDisplayObjects[i].transform.GetChild(0).GetComponent<Image>().sprite = map.mapImage;
        }

        if (mapTitleText != null)
        {
            int firstMapIndex = currentStartIndex % ingameMapList.ingameMaps.Count;
            mapTitleText.text = ingameMapList.ingameMaps[firstMapIndex].translatedName;
        }
    }

    private void OntoNextMap()
    {
        nextMapButton.onClick.Invoke();
    }

    private void OntoPreviousMap()
    {
        prevMapButton.onClick.Invoke();
    }

    private void OnMapSelect()
    {
        int firstMapIndex = currentStartIndex % ingameMapList.ingameMaps.Count;
        string mapId = ingameMapList.ingameMaps[firstMapIndex].mapId;
        
        SaveSelectedMap(mapId);
        Timing.RunCoroutine(OnGamePreparationSelected());
    }

    private void SaveSelectedMap(string selectedMap)
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        playerData.selected_map = selectedMap;
        PlayerDataManager.SaveData(playerData);
    }

    private void OntoBackFromMapSelection()
    {
        Timing.RunCoroutine(OnMainMenuSelected());
    }

    /*********************************************************************/
    //
    //                       Game Preparation
    //
    /*********************************************************************/

    private IEnumerator<float> OnGamePreparationSelected()
    {
        audioManager.PlayAudio("sfx.general.button_pressed", false);
        yield return Timing.WaitForSeconds(0.5f);
        SceneManager.LoadScene(mainSceneIds.GetSceneName("game_preparation_scene"));
    }

    private void GamePreparation_Start()
    {
        GamePreparation_PrepareUi();
        GamePreparation_PrepareObjectListeners();
        GamePreparation_RegisterKeyActions();
        UpdateMapSelectedDisplay();
    }

    private void GamePreparation_PrepareUi()
    {
        gamePreparationScreen.SetActive(true);
        loadingScreen.SetActive(false);
        
        pacmanPrepMessages[0].SetActive(true);
        pacmanPrepMessages[1].SetActive(false);
        ghostPrepMessages[0].SetActive(true);
        ghostPrepMessages[1].SetActive(false);
        preparingMessageObj.SetActive(false);
    }

    private void GamePreparation_PrepareObjectListeners()
    {
        pacmanPrepButton.onClick.AddListener(() => OnPacmanIsReady());
        ghostPrepButton.onClick.AddListener(() => OnGhostIsReady());
        backPrepButton.onClick.AddListener(() => OntoBackFromGamePreparation());
    }

    private void GamePreparation_RemoveObjectListeners()
    {
        pacmanPrepButton.onClick.RemoveAllListeners();
        ghostPrepButton.onClick.RemoveAllListeners();
        backPrepButton.onClick.RemoveAllListeners();
    }

    private void GamePreparation_RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.ready_player_one", () => OnPacmanIsReady());
        KeybindDataManager.RegisterKeyAction("general.ready_player_two", () => OnGhostIsReady());
        KeybindDataManager.RegisterKeyAction("general.go_back", () => OntoBackFromGamePreparation());
    }

    private void UpdateMapSelectedDisplay()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        IngameMap map = ingameMapList.ingameMaps.Find(map => map.mapId == playerData.selected_map);
        if (map == null) return;

        prepMapDisplay.sprite = map.mapImage;
        prepMapTitleText.text = map.translatedName;
        prepMapDescText.text = map.description;
    }

    private void OnPacmanIsReady()
    {
        isPacmanPrepared = !isPacmanPrepared;
        pacmanPrepMessages[0].SetActive(!isPacmanPrepared);
        pacmanPrepMessages[1].SetActive(isPacmanPrepared);
        StartCoroutine(PrepareLoadingIngame());
    }

    private void OnGhostIsReady()
    {
        isGhostPrepared = !isGhostPrepared;
        ghostPrepMessages[0].SetActive(!isGhostPrepared);
        ghostPrepMessages[1].SetActive(isGhostPrepared);
        StartCoroutine(PrepareLoadingIngame());
    }

    private IEnumerator PrepareLoadingIngame()
    {
        if (!ArePlayersReady()) yield break;
        KeybindDataManager.ResetKeyActions();
        GamePreparation_RemoveObjectListeners();

        yield return new WaitForSeconds(1.5f);

        preparingMessageObj.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        Loading_Start();
    }

    private bool ArePlayersReady()
    {
        return isPacmanPrepared && isGhostPrepared;
    }

    private void OntoBackFromGamePreparation()
    {
        Timing.RunCoroutine(OnMapSelectionSelected());
    }

    /*********************************************************************/
    //
    //                        Loading Screen
    //
    /*********************************************************************/

    private void Loading_Start()
    {
        Loading_PrepareUi();
        StartCoroutine(LoadSceneAsync(GetSelectedMapScene()));
    }
    
    private void Loading_PrepareUi()
    {
        gamePreparationScreen.SetActive(false);
        loadingScreen.SetActive(true);

        PlayerData playerData = PlayerDataManager.LoadData();
        IngameMap map = ingameMapList.ingameMaps.Find(map => map.mapId == playerData.selected_map);
        loadMapDisplay.sprite = map.mapImage;
        loadMapTitleText.text = map.translatedName;
    }
    
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Load asynchronously to the selected scene, with loading screen active
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        yield return new WaitForSeconds(1.0f);

        // Increase the loading progress if the async operation is not yet done
        float progress = 0;
        while (!operation.isDone)
        {
            progress = Mathf.MoveTowards(progress, Mathf.Clamp01(operation.progress / 0.9f), Time.deltaTime / 4.0f);

            loadingFloaterObject.transform.position = Vector3.Lerp(loadingAnchors[0].position, loadingAnchors[1].position, progress);
            
            // Go to the prompt scene if the progress reaches 100%
            if (progress >= 1f)
            {
                yield return new WaitForSeconds(1.0f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private string GetSelectedMapScene()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        IngameMap map = ingameMapList.ingameMaps.Find(map => map.mapId == playerData.selected_map);
        if (map == null) return null;

        return ingameMapSceneIds.GetSceneName(map.mapId);
    }

    /*********************************************************************/
    //
    //                          Leaderboard
    //
    /*********************************************************************/

    private IEnumerator<float> OnLeaderboardSelected()
    {
        audioManager.PlayAudio("sfx.general.button_pressed", false);
        yield return Timing.WaitForSeconds(0.5f);
        SceneManager.LoadScene(mainSceneIds.GetSceneName("leaderboard_scene"));
    }

    private void Leaderboard_Start()
    {
        Leaderboard_PrepareUi();
        Leaderboard_PrepareObjectListeners();
        Leaderboard_RegisterKeyActions();
    }

    private void Leaderboard_PrepareObjectListeners()
    {
        backLeadButton.onClick.AddListener(() => OntoBackFromLeaderboard());
    }

    private void Leaderboard_RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.go_back", () => OntoBackFromLeaderboard());
    }

    private void Leaderboard_PrepareUi()
    {
        LeaderboardData leaderboardData = LeaderboardDataManager.LoadData();
        if (leaderboardData == null)
        {
            Debug.LogWarning("LeaderboardData not found.");
            return;
        }

        List<LeaderboardData.RowData> rowDataList;
        rowDataList = leaderboardData.rowData;
        rowDataList = rowDataList.OrderByDescending(row => row.pac_points).ToList();

        for (int i = 0; i < Mathf.Min(10, rowDataList.Count); i++)
        {
            Leaderboard_InstantiateRow(i + 1, rowDataList[i]);
        }
    }

    private void Leaderboard_InstantiateRow(int rank, LeaderboardData.RowData rowData)
    {
        GameObject newRow = Instantiate(rowBoardPrefab, rowAnchor);

        newRow.transform.localPosition = new Vector3(0, -60 * (rank - 1), 0);

        Text[] texts = newRow.GetComponentsInChildren<Text>();

        foreach (Text text in texts)
        {
            switch (text.name)
            {
                case "rank":
                    text.text = rank.ToString("00");
                    break;
                case "codename":
                    text.text = rowData.code_name;
                    break;
                case "winner":
                    text.text = rowData.winner_name;
                    break;
                case "pacpoints":
                    text.text = rowData.pac_points.ToString("00,000,000");
                    break;
                case "time":
                    TimeSpan timeSpan = TimeSpan.FromSeconds(rowData.playtime);
                    text.text = timeSpan.ToString(@"hh\:mm\:ss");
                    break;
            }
        }
    }

    private void OntoBackFromLeaderboard()
    {
        Timing.RunCoroutine(OnMainMenuSelected());
    }

    /*********************************************************************/
    //
    //                            Settings
    //
    /*********************************************************************/

    private IEnumerator<float> OnSettingsSelected()
    {
        audioManager.PlayAudio("sfx.general.button_pressed", false);
        yield return Timing.WaitForSeconds(0.5f);
        SceneManager.LoadScene(mainSceneIds.GetSceneName("settings_scene"));
    }

    private void Settings_Start()
    {
        Settings_UpdateUi("music");
        Settings_UpdateUi("sfx");
        Settings_PrepareObjectListeners();
        Settings_RegisterKeyActions();
    }

    private void Settings_PrepareObjectListeners()
    {
        musicVolumeButtons[0].onClick.AddListener(() => UpdateMusicVolume("low"));
        musicVolumeButtons[1].onClick.AddListener(() => UpdateMusicVolume("high"));
        sfxVolumeButtons[0].onClick.AddListener(() => UpdateSfxVolume("low"));
        sfxVolumeButtons[1].onClick.AddListener(() => UpdateSfxVolume("high"));
        backSettingsButton.onClick.AddListener(() => OntoBackFromSettings());
    }

    private void Settings_RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.go_back", () => OntoBackFromSettings());
    }

    private void Settings_UpdateUi(string type)
    {
        if (type == "music")
        {
            float musicVolume = audioManager.MusicVolume;
            int musicBars = (int)Mathf.Round(musicVolume * 10);
            foreach (GameObject musicVolumeBar in musicVolumeBars)
            {
                musicVolumeBar.SetActive(false);
            }
            for (int i = 0; i < musicBars; i++)
            {
                musicVolumeBars[i].SetActive(true);
            }
        }
        else if (type == "sfx")
        {
            float sfxVolume = audioManager.SfxVolume;
            int sfxBars = (int)Mathf.Round(sfxVolume * 10);
            foreach (GameObject sfxVolumeBar in sfxVolumeBars)
            {
                sfxVolumeBar.SetActive(false);
            }
            for (int j = 0; j < sfxBars; j++)
            {
                sfxVolumeBars[j].SetActive(true);
            }
        }
    }

    private void UpdateMusicVolume(string mode)
    {
        if (mode == "low")
        {
            if (audioManager.MusicVolume <= 0) return;
            audioManager.MusicVolume = Mathf.Clamp01(audioManager.MusicVolume - 0.1f);
            Settings_UpdateUi("music");
        }
        else if (mode == "high")
        {
            if (audioManager.MusicVolume >= 1) return;
            audioManager.MusicVolume = Mathf.Clamp01(audioManager.MusicVolume + 0.1f);
            Settings_UpdateUi("music");
        }
        audioManager.PlayAudio("sfx.general.button_pressed", false);
    }

    private void UpdateSfxVolume(string mode)
    {
        if (mode == "low")
        {
            if (audioManager.SfxVolume <= 0) return;
            audioManager.SfxVolume = Mathf.Clamp01(audioManager.SfxVolume - 0.1f);
            Settings_UpdateUi("sfx");
        }
        else if (mode == "high")
        {
            if (audioManager.SfxVolume >= 1) return;
            audioManager.SfxVolume = Mathf.Clamp01(audioManager.SfxVolume + 0.1f);
            Settings_UpdateUi("sfx");
        }
        audioManager.PlayAudio("sfx.general.button_pressed", false);
    }

    private float GetVolume(string type)
    {
        if (type == "music")
        {
            return audioManager.MusicVolume;
        }
        else if (type == "sfx")
        {
            return audioManager.SfxVolume;
        }
        return -1;
    }

    private void SetVolume(string type, float volume)
    {
        if (type == "music")
        {
            audioManager.MusicVolume = volume;
        }
        else if (type == "sfx")
        {
            audioManager.SfxVolume = volume;
        }
    }

    private void OntoBackFromSettings()
    {
        Timing.RunCoroutine(OnMainMenuSelected());
    }
}
