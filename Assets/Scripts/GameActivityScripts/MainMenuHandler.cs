using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuHandler : MonoBehaviour
{
    [Header("===Main Menu Buttons===")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private SceneDictionary mainSceneIds;
    [SerializeField] private MazeMapList mazeMapList;

    [Header("===Map Selection Misc===")]
    [SerializeField] private Button prevMapButton;
    [SerializeField] private Button nextMapButton;
    [SerializeField] private Button backMapButton;
    [SerializeField] private List<GameObject> mapDisplayObjects;
    [SerializeField] private Text mapTitleText;
    private int currentStartIndex = 0;

    [Header("===Game Preparation Misc===")]
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

    [Header("===Loading Screen Misc===")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject loadingFloaterObject;

    
    // [Header("===Leaderboard Misc===")]
    
    // [Header("===Settings Misc===")]
    
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
        string curentScene = SceneManager.GetActiveScene().name;
        if (curentScene == mainSceneIds.GetSceneName("main_menu_scene"))
        {
            MainMenu_Start();
        }
        else if (curentScene == mainSceneIds.GetSceneName("map_selection_scene"))
        {
            MapSelection_Start();
        }
        else if (curentScene == mainSceneIds.GetSceneName("game_preparation_scene"))
        {
            GamePreparation_Start();
        }
        else if (curentScene == mainSceneIds.GetSceneName("leaderboard_scene"))
        {
            return;
        }
        else if (curentScene == mainSceneIds.GetSceneName("settings_scene"))
        {
            return;
        }
    }

    private void OnDestroy()
    {
        KeybindDataManager.ResetKeyActions();
    }

    /*********************************************************************/
    //
    //                           Main Menu
    //
    /*********************************************************************/

    private void OnMainMenuSelected()
    {
        SceneManager.LoadScene(mainSceneIds.GetSceneName("main_menu_scene"));
    }

    private void MainMenu_Start()
    {
        MainMenu_PrepareObjectListeners();
    }
    
    private void MainMenu_PrepareObjectListeners()
    {
        try { startButton.onClick.AddListener(() => OnMapSelectionSelected()); } catch (Exception) { }
        try { leaderboardButton.onClick.AddListener(() => OnLeaderboardSelected()); } catch (Exception) { }
        try { settingsButton.onClick.AddListener(() => OnSettingsSelected()); } catch (Exception) { }
    }

    /*********************************************************************/
    //
    //                         Map Selection
    //
    /*********************************************************************/

    private void OnMapSelectionSelected()
    {
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
            currentStartIndex = (currentStartIndex + 1) % mazeMapList.mazeMaps.Count;
        }
        else
        {
            currentStartIndex = (currentStartIndex - 1 + mazeMapList.mazeMaps.Count) % mazeMapList.mazeMaps.Count;
        }

        for (int i = 0; i < mapDisplayObjects.Count; i++)
        {
            int mapIndex = (currentStartIndex + i) % mazeMapList.mazeMaps.Count;
            MazeMap map = mazeMapList.mazeMaps[mapIndex];

            mapDisplayObjects[i].transform.GetChild(0).GetComponent<Image>().sprite = map.mapImage;
        }

        if (mapTitleText != null)
        {
            int firstMapIndex = currentStartIndex % mazeMapList.mazeMaps.Count;
            mapTitleText.text = mazeMapList.mazeMaps[firstMapIndex].translatedName;
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
        int firstMapIndex = currentStartIndex % mazeMapList.mazeMaps.Count;
        string mapId = mazeMapList.mazeMaps[firstMapIndex].mapId;
        
        SaveSelectedMap(mapId);
        OnGamePreparationSelected();
    }

    private void SaveSelectedMap(string selectedMap)
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        playerData.selected_map = selectedMap;
        PlayerDataManager.SaveData(playerData);
    }

    private void OntoBackFromMapSelection()
    {
        OnMainMenuSelected();
    }

    /*********************************************************************/
    //
    //                       Game Preparation
    //
    /*********************************************************************/

    private void OnGamePreparationSelected()
    {
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
        KeybindDataManager.RegisterKeyAction("general.move_next_selection", () => OnPacmanIsReady());       // to be changed
        KeybindDataManager.RegisterKeyAction("general.move_previous_selection", () => OnGhostIsReady());    // to be changed
        KeybindDataManager.RegisterKeyAction("general.go_back", () => OntoBackFromGamePreparation());
    }

    private void UpdateMapSelectedDisplay()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        MazeMap map = mazeMapList.mazeMaps.Find(map => map.mapId == playerData.selected_map);
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
    }

    private bool ArePlayersReady()
    {
        return isPacmanPrepared && isGhostPrepared;
    }

    private void OntoBackFromGamePreparation()
    {
        OnMapSelectionSelected();
    }

    /*********************************************************************/
    //
    //                        Loading Screen
    //
    /*********************************************************************/



    /*********************************************************************/
    //
    //                          Leaderboard
    //
    /*********************************************************************/

    private void OnLeaderboardSelected()
    {

    }

    /*********************************************************************/
    //
    //                            Settings
    //
    /*********************************************************************/

    private void OnSettingsSelected()
    {

    }
}
