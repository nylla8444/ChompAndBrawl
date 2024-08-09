using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MEC;

public class IngamePrepareHandler : MonoBehaviour
{
    [Header("===General===")]
    [SerializeField] private SceneDictionary mainSceneIds;
    [SerializeField] private SceneDictionary ingameMapSceneIds;
    [SerializeField] private IngameMapList ingameMapList;
    [SerializeField] private BrawlManager brawlManager;
    
    [Header("===Tutorial Misc===")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TutorialList tutorialList;
    [SerializeField] private GameObject pacmanTutorialSide;
    [SerializeField] private GameObject ghostTutorialSide;
    [SerializeField] private GameObject pacmanWaitingSide;
    [SerializeField] private GameObject ghostWaitingSide;
    private int currentPacmanTutorialIndex = 0;
    private int currentGhostTutorialIndex = 0;

    [Header("===Maze Countdown Misc===")]
    [SerializeField] private GameObject countdownMazePanel;
    [SerializeField] private Text countdownMazeText;
    [SerializeField] private List<GameObject> lightsMazeLeft;
    [SerializeField] private List<GameObject> lightsMazeRight;
    
    [Header("===Brawl Countdown Misc===")]
    [SerializeField] private GameObject countdownBrawlPanel;
    [SerializeField] private Image countdownBrawlImageText; 
    [SerializeField] private List<Sprite> countdownBrawlTextSprites; // 0: ready, 1: fight
    [SerializeField] private List<GameObject> lightsBrawlLeft;
    [SerializeField] private List<GameObject> lightsBrawlRight;

    [Header("===Labyrinth Part Misc===")]
    [SerializeField] private PacmanMazeController pacmanMazeController;
    [SerializeField] private GhostMazeController ghostMazeController;
    [SerializeField] private ItemMazeHandler itemMazeHandler;

    [Header("===Transition Misc===")]
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private Image mazeToBrawlPacmanImage;
    [SerializeField] private Image mazeToBrawlGhostImage;
    [SerializeField] private List<Sprite> pacmanBrawlSprites;   // 0: pacman_normal, 1: pacman_monster
    [SerializeField] private List<Sprite> ghostBrawlSprites;    // 0: blinky, 1: clyde, 2: inky, 3: pinky
    [SerializeField] private Image brawlToMazeMapImage;
    [SerializeField] private Text brawlToMazeMapText;
    
    /*********************************************************************/
    //
    //                            General
    //
    /*********************************************************************/

    private void Start()
    {
        SetForCurrentScene();
    }

    private void SetForCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != ingameMapSceneIds.GetSceneName("brawl_arena"))
        {
            StartCoroutine(Maze_PrepareUi());
        }
        else
        {
            StartCoroutine(Brawl_PrepareUi());
        }

        MazeToBrawlSetUi();
        BrawlToMazeSetUi();
    }

    private void ResetKeys()
    {
        KeybindDataManager.ResetKeyActions();
    }

    /*********************************************************************/
    //
    //                        Maze Preparation
    //
    /*********************************************************************/

    private IEnumerator Maze_PrepareUi()
    {
        if (!IsAllReadTutorial())
        {
            tutorialPanel.SetActive(true);
            countdownMazePanel.SetActive(false);
            pacmanWaitingSide.SetActive(false);
            ghostWaitingSide.SetActive(false);
            DisplayPacmanTutorial();
            DisplayGhostTutorial();
            Maze_RegisterKeyActions();
            yield return null;
        }
        else
        {
            yield return new WaitForSeconds(2.5f);
            StartCoroutine(DisplayMazeCountdown());
        }
    }

    private void Maze_RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.pacman_next_tutorial", () => UpdatePacmanTutorial("next"));
        KeybindDataManager.RegisterKeyAction("general.pacman_skip_tutorial", () => UpdatePacmanTutorial("skip"));
        KeybindDataManager.RegisterKeyAction("general.ghost_next_tutorial", () => UpdateGhostTutorial("next"));
        KeybindDataManager.RegisterKeyAction("general.ghost_skip_tutorial", () => UpdateGhostTutorial("skip"));
    }

    private void UpdatePacmanTutorial(string mode)
    {
        if (IngameDataManager.LoadSpecificData<bool>("pacman_data.has_read_tutorial")) return;

        if (mode == "next")
        {
            currentPacmanTutorialIndex++;
            var pacmanTutorial = tutorialList.tutorials.Find(t => t.tutorialId == "pacman_tutorials");
            if (currentPacmanTutorialIndex >= pacmanTutorial.tutorialSprites.Count)
            {
                DisplayWaiting("pacman");
            }
            else
            {
                DisplayPacmanTutorial();
            }
        }
        else if (mode == "skip")
        {
            DisplayWaiting("pacman");
        }
    }

    private void UpdateGhostTutorial(string mode)
    {
        if (IngameDataManager.LoadSpecificData<bool>("ghost_data.has_read_tutorial")) return;

        if (mode == "next")
        {
            currentGhostTutorialIndex++;
            var ghostTutorial = tutorialList.tutorials.Find(t => t.tutorialId == "ghost_tutorials");
            if (currentGhostTutorialIndex >= ghostTutorial.tutorialSprites.Count)
            {
                DisplayWaiting("ghost");
            }
            else
            {
                DisplayGhostTutorial();
            }
        }
        else if (mode == "skip")
        {
            DisplayWaiting("ghost");
        }
    }

    private void DisplayPacmanTutorial()
    {
        var pacmanTutorial = tutorialList.tutorials.Find(t => t.tutorialId == "pacman_tutorials");
        int pacmanTutorialCount = pacmanTutorial.tutorialSprites.Count;

        if (pacmanTutorial != null && pacmanTutorialCount > 0 && currentPacmanTutorialIndex < pacmanTutorialCount)
        {
            pacmanTutorialSide.transform.GetChild(0).GetComponent<Image>().sprite = pacmanTutorial.tutorialSprites[currentPacmanTutorialIndex];
            pacmanTutorialSide.transform.GetChild(2).GetComponent<Text>().text = $"PAGE {currentPacmanTutorialIndex + 1} / {pacmanTutorialCount}";
            pacmanTutorialSide.SetActive(true);
        }
    }

    private void DisplayGhostTutorial()
    {
        var ghostTutorial = tutorialList.tutorials.Find(t => t.tutorialId == "ghost_tutorials");
        int ghostTutorialCount = ghostTutorial.tutorialSprites.Count;

        if (ghostTutorial != null && ghostTutorialCount > 0 && currentGhostTutorialIndex < ghostTutorialCount)
        {
            ghostTutorialSide.transform.GetChild(0).GetComponent<Image>().sprite = ghostTutorial.tutorialSprites[currentGhostTutorialIndex];
            ghostTutorialSide.transform.GetChild(2).GetComponent<Text>().text = $"PAGE {currentGhostTutorialIndex + 1} / {ghostTutorialCount}";
            ghostTutorialSide.SetActive(true);
        }
    }

    private void DisplayWaiting(string character)
    {
        if (character == "pacman")
        {
            pacmanTutorialSide.SetActive(false);
            pacmanWaitingSide.SetActive(true);
            IngameDataManager.SaveSpecificData("pacman_data.has_read_tutorial", true);
        }
        else if (character == "ghost")
        {
            ghostTutorialSide.SetActive(false);
            ghostWaitingSide.SetActive(true);
            IngameDataManager.SaveSpecificData("ghost_data.has_read_tutorial", true);
        }

        if (IsAllReadTutorial())
        {
            tutorialPanel.SetActive(false);
            ResetKeys();

            StartCoroutine(DisplayMazeCountdown());
        }
    }

    private bool IsAllReadTutorial()
    {
        return IngameDataManager.LoadSpecificData<bool>("pacman_data.has_read_tutorial") &&
               IngameDataManager.LoadSpecificData<bool>("ghost_data.has_read_tutorial");
    }

    private IEnumerator DisplayMazeCountdown()
    {
        Color WHITE = new Color(1.0000f, 1.0000f, 1.0000f);
        Color RED = new Color(1.0000f, 0.0000f, 0.1981f);
        Color YELLOW = new Color(1.0000f, 0.8651f, 0.0000f);
        Color GREEN = new Color(0.2247f, 1.0000f, 0.07075f);
        
        countdownMazePanel.SetActive(true);
        countdownMazeText.text = "Starting in...";
        StartCoroutine(ChangeLightMazeColor(WHITE));

        yield return new WaitForSeconds(1.0f);
        countdownMazeText.text = "3";
        StartCoroutine(ChangeLightMazeColor(RED));

        yield return new WaitForSeconds(1.0f);
        countdownMazeText.text = "2";
        StartCoroutine(ChangeLightMazeColor(YELLOW));

        yield return new WaitForSeconds(1.0f);
        countdownMazeText.text = "1";
        StartCoroutine(ChangeLightMazeColor(GREEN));

        yield return new WaitForSeconds(1.0f);
        
        countdownMazePanel.SetActive(false);
        StartControls();
    }

    private IEnumerator ChangeLightMazeColor(Color color)
    {
        int lightCount = lightsMazeLeft.Count;

        for (int i = 0; i < lightCount; i++)
        {
            lightsMazeLeft[i].SetActive(false);
            lightsMazeRight[i].SetActive(false);
        }

        for (int j = 0; j < lightCount; j++)
        {
            yield return new WaitForSeconds(0.25f);
            lightsMazeLeft[j].GetComponent<Image>().color = color;
            lightsMazeLeft[j].SetActive(true);
            lightsMazeRight[j].GetComponent<Image>().color = color;
            lightsMazeRight[j].SetActive(true);
        }
    }

    private void StartControls()
    {
        pacmanMazeController.StartPacmanController(true);
        ghostMazeController.StartGhostController(true);
        itemMazeHandler.StartItemController(true);
        Timing.RunCoroutine(CollideDetection());
    }

    /*********************************************************************/
    //
    //                         Brawl Preparation
    //
    /*********************************************************************/

    private IEnumerator Brawl_PrepareUi()
    {
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(DisplayBrawlCountdown());
    }
    
    private IEnumerator DisplayBrawlCountdown()
    {
        Color WHITE = new Color(1.0000f, 1.0000f, 1.0000f);
        Color RED = new Color(1.0000f, 0.0000f, 0.1981f);
        Color GREEN = new Color(0.2247f, 1.0000f, 0.07075f);

        countdownBrawlPanel.SetActive(true);
        countdownBrawlImageText.enabled = false;
        StartCoroutine(ChangeLightBrawlColor(WHITE));

        yield return new WaitForSeconds(1.0f);
        countdownBrawlImageText.enabled = true;
        countdownBrawlImageText.sprite = countdownBrawlTextSprites[0];
        StartCoroutine(ChangeLightBrawlColor(RED));

        yield return new WaitForSeconds(1.0f);
        countdownBrawlImageText.sprite = countdownBrawlTextSprites[1];
        StartCoroutine(ChangeLightBrawlColor(GREEN));

        yield return new WaitForSeconds(1.0f);
        countdownBrawlPanel.SetActive(false);

        brawlManager.ToStart();
    }

    private IEnumerator ChangeLightBrawlColor(Color color)
    {
        int lightCount = lightsBrawlLeft.Count;

        for (int i = 0; i < lightCount; i++)
        {
            lightsBrawlLeft[i].SetActive(false);
            lightsBrawlRight[i].SetActive(false);
        }

        for (int j = 0; j < lightCount; j++)
        {
            yield return new WaitForSeconds(0.25f);
            lightsBrawlLeft[j].GetComponent<Image>().color = color;
            lightsBrawlLeft[j].SetActive(true);
            lightsBrawlRight[j].GetComponent<Image>().color = color;
            lightsBrawlRight[j].SetActive(true);
        }
    }

    /*********************************************************************/
    //
    //                         Collide Detected
    //
    /*********************************************************************/

    private IEnumerator<float> CollideDetection()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(0.01f);
            if (IngameDataManager.LoadSpecificData<bool>("pacman_data.is_immune_to_ghost")) continue;
            
            Vector2 _pacman_coordinate = IngameDataManager.LoadSpecificData<Vector2>("pacman_data.coordinate");
            List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");

            foreach (string ghostName in _ghost_listAlive)
            {
                Vector2 _coordinate = IngameDataManager.LoadSpecificListData<Vector2>("ghost_data.ghost_single_info", ghostName, "coordinate");
                float distance = Vector2.Distance(_coordinate, _pacman_coordinate);
                if (distance < 0.04f)
                {
                    Timing.RunCoroutine(CollideDetected(ghostName));
                    yield break;
                }
            }
        }
    }

    private IEnumerator<float> CollideDetected(string ghostName)
    {
        ResetKeys();
        IngameDataManager.SaveSpecificData("ghost_data.current_fighting", ghostName);
        ghostMazeController.DirectChangeGhost(ghostName);
        
        pacmanMazeController.SetSlowSpeed();
        ghostMazeController.SetSlowSpeed();
        Timing.RunCoroutine(pacmanMazeController.InEffect_ZoomOutPacman("remove", -0.4f, 3.0f));
        Timing.RunCoroutine(pacmanMazeController.InEffect_ZoomOutGhost(-0.4f, 3.0f));
        
        yield return Timing.WaitForSeconds(2.0f);
        MazeToBrawlTransition();

        yield return Timing.WaitForSeconds(0.5f);
        Timing.KillCoroutines();
    }

    public void SetDataForWinning(string winner, string loser)
    {
        int points = IngameDataManager.LoadSpecificData<int>("pacman_data.points");
        if (winner == "pacman")
        {
            List<string> listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
            listAlive.Remove(loser);
            IngameDataManager.SaveSpecificData("ghost_data.list_alive", listAlive);

            points += 500 * (int)Mathf.Pow(4 - listAlive.Count, 2);
        }
        else if (winner == "ghost")
        {
            int lives = IngameDataManager.LoadSpecificData<int>("pacman_data.lives");
            lives = lives - 1;
            IngameDataManager.SaveSpecificData("pacman_data.lives", lives);

            points -= 500 * (int)Mathf.Pow(3 - lives, 2);
        }
        IngameDataManager.SaveSpecificData("pacman_data.points", points);

        StartCoroutine(SetTransitionFromWinning());
    }

    private IEnumerator SetTransitionFromWinning()
    {
        yield return new WaitForSeconds(3.0f);
        if (IngameDataManager.LoadSpecificData<int>("pacman_data.lives") <= 0 ||
            IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive").Count <= 0)
        {
            AftergameTransition();
        }
        else
        {
            BrawlToMazeTransition();
        }
    }

    /*********************************************************************/
    //
    //                          Transitioning
    //
    /*********************************************************************/

    private void MazeToBrawlTransition()
    {
        string sceneName = ingameMapSceneIds.GetSceneName("brawl_arena");
        sceneTransitionManager.LoadScene(sceneName, "maze_to_brawl");
        MazeToBrawlSetUi();
    }

    private void MazeToBrawlSetUi()
    {
        string ghostFightingName = IngameDataManager.LoadSpecificData<string>("ghost_data.current_fighting");
        bool hasPowerPellet = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");

        if (System.String.IsNullOrEmpty(ghostFightingName)) return;

        int ghostIndex = ghostFightingName switch
        {
            "blinky" => 0,
            "clyde" => 1,
            "inky" => 2,
            "pinky" => 3,
            _ => -1
        };

        mazeToBrawlPacmanImage.sprite = (hasPowerPellet) ? pacmanBrawlSprites[1] : pacmanBrawlSprites[0];
        mazeToBrawlGhostImage.sprite = ghostBrawlSprites[ghostIndex];
    }

    private void BrawlToMazeTransition()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        string sceneName = ingameMapSceneIds.GetSceneName(playerData.selected_map);
        sceneTransitionManager.LoadScene(sceneName, "brawl_to_maze");
        BrawlToMazeSetUi();
    }

    private void BrawlToMazeSetUi()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        IngameMap map = ingameMapList.ingameMaps.Find(map => map.mapId == playerData.selected_map);
        brawlToMazeMapImage.sprite = map.mapImage;
        brawlToMazeMapText.text = map.translatedName;
    }

    private void AftergameTransition()
    {
        string sceneName = mainSceneIds.GetSceneName("aftergame_scene");
        sceneTransitionManager.LoadScene(sceneName, "checkered_wipe");
    }
}
