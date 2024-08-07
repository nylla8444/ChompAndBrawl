using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MEC;

public class AftergamePrepareHandler : MonoBehaviour
{
    [SerializeField] private Image pacmanIsVictoryImage;
    [SerializeField] private Image ghostIsVictoryImage;
    [SerializeField] private List<Sprite> isVictorySprites; // 0: defeat, 1: victory
    [SerializeField] private Text codeNameText;
    [SerializeField] private Text collectedPointsText;
    [SerializeField] private Text playtimeText;
    [SerializeField] private Text pointsFromPlaytimeText;
    [SerializeField] private Text overallPointsText;
    [SerializeField] private GameObject arrowOnPlaytime;
    [SerializeField] private Button backButton;
    [SerializeField] private SceneDictionary mainSceneIds;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GeneralManager generalManager;

    private void Start()
    {
        PrepareUi();
        AddToLeaderboard();
        PrepareObjectListeners();
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        KeybindDataManager.ResetKeyActions();
    }

    private void PrepareObjectListeners()
    {
        backButton.onClick.AddListener(() => Timing.RunCoroutine(OntoBackFromAftergame()));
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.go_back", () => Timing.RunCoroutine(OntoBackFromAftergame()));
    }

    private void PrepareUi()
    {
        codeNameText.text = "";
        collectedPointsText.text = "";
        playtimeText.text = "";
        pointsFromPlaytimeText.text = "";
        overallPointsText.text = "";
        arrowOnPlaytime.SetActive(false);
    }

    private void AddToLeaderboard()
    {
        bool isPacmanWon = IsPacmanWon();
        string codeName = GenerateRandomCode();
        string winnerName = isPacmanWon ? "PAC-MAN" : "GHOST";
        int collectedPoints = GetPacmanPoints();
        int playtime = GetPacmanPlaytime();
        int pointsFromPlaytime = CalculatePointsFromPlaytime(playtime);
        int overallPoints = CalculateOverallPoints(collectedPoints, pointsFromPlaytime);

        LeaderboardData leaderboardData = LeaderboardDataManager.LoadData();
        leaderboardData.AddRowData(codeName, winnerName, overallPoints, playtime);
        LeaderboardDataManager.SaveData(leaderboardData);

        StartCoroutine(SettleUi(isPacmanWon, codeName, collectedPoints, playtime, pointsFromPlaytime, overallPoints));
    }

    private IEnumerator SettleUi(bool isPacmanWon, string codeName, int collectedPoints, int playtime, int pointsFromPlaytime, int overallPoints)
    {
        DisplayVictory(isPacmanWon);
        StartCoroutine(DisplayCodename(codeName));
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(DisplayPoints(collectedPoints, false, collectedPointsText));
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(DisplayPlaytime(playtime));
        yield return new WaitForSeconds(2.0f);
        arrowOnPlaytime.SetActive(true);
        StartCoroutine(DisplayPoints(pointsFromPlaytime, true, pointsFromPlaytimeText));
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(DisplayPoints(overallPoints, false, overallPointsText));
    }

    private bool IsPacmanWon()
    {
        return IngameDataManager.LoadSpecificData<int>("pacman_data.lives") > 0 &&
               IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive").Count > 0;
    }

    private string GenerateRandomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        System.Random random = new System.Random();
        string randomCodeName;
        
        do
        {
            randomCodeName = new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        } while (IsCodenameExist(randomCodeName));

        return randomCodeName;
    }

    private bool IsCodenameExist(string code)
    {
        LeaderboardData leaderboardData = LeaderboardDataManager.LoadData();
        foreach (var row in leaderboardData.rowData)
        {
            if (row.code_name == code)
            {
                return true;
            }
        }
        return false;
    }

    private int GetPacmanPoints()
    {
        return IngameDataManager.LoadSpecificData<int>("pacman_data.points");
    }

    private int GetPacmanPlaytime()
    {
        return IngameDataManager.LoadSpecificData<int>("pacman_data.playtime");
    }

    private int CalculatePointsFromPlaytime(int playtime)
    {
        return playtime * 2;
    }

    private int CalculateOverallPoints(int collectedPoints, int pointsFromPlaytime)
    {
        return collectedPoints - pointsFromPlaytime;
    }

    // =============== Display ================ //

    private void DisplayVictory(bool isPacmanWon)
    {
        pacmanIsVictoryImage.sprite = isVictorySprites[isPacmanWon ? 1 : 0];
        ghostIsVictoryImage.sprite = isVictorySprites[isPacmanWon ? 0 : 1];
    }

    private IEnumerator DisplayCodename(string finalCode)
    {
        Debug.Log("displaying code name");
        System.Random random = new System.Random();
        const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const float ANIMATION_DURATION = 2.0f;
        const float INITIAL_INTERVAL = 0.01f;
        const float MAX_INTERVAL = 0.1f;
        float elapsed_time = 0f;
        float interval = INITIAL_INTERVAL;

        while (elapsed_time < ANIMATION_DURATION)
        {
            elapsed_time += interval;
            codeNameText.text = new string(Enumerable.Repeat(CHARS, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            interval = Mathf.Lerp(INITIAL_INTERVAL, MAX_INTERVAL, elapsed_time / ANIMATION_DURATION);
            yield return new WaitForSeconds(interval);
        }

        codeNameText.text = finalCode;
    }

    private IEnumerator DisplayPoints(int points, bool isMinus, Text displayText)
    {
        const float ANIMATION_DURATION = 2.0f;
        const float INITIAL_INTERVAL = 0.01f;
        const float MAX_INTERVAL = 0.1f;
        float elapsed_time = 0f;
        float interval = INITIAL_INTERVAL;
        int currentPoints = 0;

        while (elapsed_time < ANIMATION_DURATION)
        {
            elapsed_time += interval;
            displayText.text = FormatPoints(currentPoints, isMinus);

            currentPoints = (int)Mathf.Lerp(0, points, elapsed_time / ANIMATION_DURATION);
            interval = Mathf.Lerp(INITIAL_INTERVAL, MAX_INTERVAL, elapsed_time / ANIMATION_DURATION);
            yield return new WaitForSeconds(interval);
        }

        displayText.text = FormatPoints(points, isMinus); 
    }

    private string FormatPoints(int points, bool isMinus)
    {
        return points.ToString(isMinus ? "- 00,000,000" : "00,000,000");
    }

    private IEnumerator DisplayPlaytime(int playtime)
    {
        const float ANIMATION_DURATION = 2.0f;
        const float INITIAL_INTERVAL = 0.01f;
        const float MAX_INTERVAL = 0.1f;
        float elapsed_time = 0f;
        float interval = INITIAL_INTERVAL;
        int currentPlaytime = 0;

        while (elapsed_time < ANIMATION_DURATION)
        {
            elapsed_time += interval;
            playtimeText.text = FormatTime(currentPlaytime);

            currentPlaytime = (int)Mathf.Lerp(0, playtime, elapsed_time / ANIMATION_DURATION);
            interval = Mathf.Lerp(INITIAL_INTERVAL, MAX_INTERVAL, elapsed_time / ANIMATION_DURATION);
            yield return new WaitForSeconds(interval);
        }

        playtimeText.text = FormatTime(playtime); 
    }

    private string FormatTime(int playtime)
    {
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(playtime);
        return timeSpan.ToString(@"hh\:mm\:ss");
    }

    // =============== Go Back ================ //

    private IEnumerator<float> OntoBackFromAftergame()
    {
        PlayerData playerData = PlayerDataManager.LoadData();
        playerData.selected_map = "";
        PlayerDataManager.SaveData(playerData);
        generalManager.PrepareResetData();

        yield return Timing.WaitForSeconds(0.5f);
        SceneManager.LoadScene(mainSceneIds.GetSceneName("main_menu_scene"));
    }
}
