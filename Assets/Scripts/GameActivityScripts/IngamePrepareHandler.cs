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
    
    [Header("===Tutorial Misc===")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TutorialList tutorialList;
    [SerializeField] private GameObject pacmanTutorialSide;
    [SerializeField] private GameObject ghostTutorialSide;
    [SerializeField] private GameObject pacmanWaitingSide;
    [SerializeField] private GameObject ghostWaitingSide;
    private int currentPacmanTutorialIndex = 0;
    private int currentGhostTutorialIndex = 0;

    [Header("===Countdown Misc===")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private Text countdownText;
    [SerializeField] private List<GameObject> lightsLeft;
    [SerializeField] private List<GameObject> lightsRight;

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
            Maze_PrepareUi();
        }

        MazeToBrawlSetUi();
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

    private void Maze_PrepareUi()
    {
        if (!IsAllReadTutorial())
        {
            tutorialPanel.SetActive(true);
            countdownPanel.SetActive(false);
            pacmanWaitingSide.SetActive(false);
            ghostWaitingSide.SetActive(false);
            DisplayPacmanTutorial();
            DisplayGhostTutorial();
            Maze_RegisterKeyActions();
        }
        else
        {
            StartCoroutine(DisplayCountdown());
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

            StartCoroutine(DisplayCountdown());
        }
    }

    private bool IsAllReadTutorial()
    {
        return IngameDataManager.LoadSpecificData<bool>("pacman_data.has_read_tutorial") &&
               IngameDataManager.LoadSpecificData<bool>("ghost_data.has_read_tutorial");
    }

    private IEnumerator DisplayCountdown()
    {
        Color WHITE = new Color(1.0000f, 1.0000f, 1.0000f);
        Color RED = new Color(1.0000f, 0.0000f, 0.1981f);
        Color YELLOW = new Color(1.0000f, 0.8651f, 0.0000f);
        Color GREEN = new Color(0.2247f, 1.0000f, 0.07075f);
        
        countdownPanel.SetActive(true);
        countdownText.text = "Starting in...";
        StartCoroutine(ChangeLightColor(WHITE));

        yield return new WaitForSeconds(1.0f);
        countdownText.text = "3";
        StartCoroutine(ChangeLightColor(RED));

        yield return new WaitForSeconds(1.0f);
        countdownText.text = "2";
        StartCoroutine(ChangeLightColor(YELLOW));

        yield return new WaitForSeconds(1.0f);
        countdownText.text = "1";
        StartCoroutine(ChangeLightColor(GREEN));

        yield return new WaitForSeconds(1.0f);
        
        countdownPanel.SetActive(false);
        StartControls();
    }

    private IEnumerator ChangeLightColor(Color color)
    {
        int lightCount = lightsLeft.Count;

        for (int i = 0; i < lightCount; i++)
        {
            lightsLeft[i].SetActive(false);
            lightsRight[i].SetActive(false);
        }

        for (int j = 0; j < lightCount; j++)
        {
            yield return new WaitForSeconds(0.25f);
            lightsLeft[j].GetComponent<Image>().color = color;
            lightsLeft[j].SetActive(true);
            lightsRight[j].GetComponent<Image>().color = color;
            lightsRight[j].SetActive(true);
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
    //                         Collide Detected
    //
    /*********************************************************************/

    private IEnumerator<float> CollideDetection()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(0.02f);
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
}
