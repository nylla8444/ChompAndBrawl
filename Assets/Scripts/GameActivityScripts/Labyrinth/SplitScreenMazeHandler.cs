using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SplitScreenMazeHandler : MonoBehaviour
{
    [Header("Player Cameras")]
    [SerializeField] private Camera player1Camera;
    [SerializeField] private Camera player2Camera;

    [Header("Transform Objects")]
    [SerializeField] private Transform player1;
    [SerializeField] private List<Transform> allGhosts;

    [Header("Camera Properties")]
    [SerializeField] private float cameraSmoothSpeed = 0.125f;
    [SerializeField] private float offsetDistance = 1.0f;

    [Header("Effect Objects")]
    [SerializeField] private GameObject pacman_effectDisplayPrefab;
    [SerializeField] private GameObject ghost_effectDisplayPrefab;
    [SerializeField] private Transform pacman_effectDisplayAnchor;
    [SerializeField] private Transform ghost_effectDisplayAnchor;
    [SerializeField] private EffectItemList pacman_effectItemList;
    [SerializeField] private EffectItemList ghost_effectItemList;

    private Transform currentGhost;
    private Vector3 player1Velocity = Vector3.zero;
    private Vector3 player2Velocity = Vector3.zero;
    private float DEFAULT_ORTHOGRAPHIC_SIZE = 1.25f;

    private void Start()
    {
        UpdateCurrentControllingGhost();
        StartCoroutine(UpdatePacmanEffectDisplay());
        StartCoroutine(UpdateGhostEffectDisplay());
    }

    private void Update()
    {
        CameraFollow();
    }

    private void CameraFollow()
    {
        float _pacman_visionMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.vision_multiplier");
        float _ghost_visionMultiplier = IngameDataManager.LoadSpecificData<float>("ghost_data.vision_multiplier");

        // Player 1 camera follows Pacman with offset
        Vector3 player1TargetPosition = GetTargetPosition(player1, player1Camera);
        player1Camera.transform.position = Vector3.SmoothDamp(player1Camera.transform.position, player1TargetPosition, 
                                                              ref player1Velocity, cameraSmoothSpeed);
        player1Camera.transform.position = new Vector3(player1Camera.transform.position.x, player1Camera.transform.position.y, -10f);
        player1Camera.orthographicSize = DEFAULT_ORTHOGRAPHIC_SIZE * _pacman_visionMultiplier;

        // Player 2 camera follows the current ghost
        Transform currentCharacter = GetCurrentControllingGhost();
        if (currentCharacter != null)
        {
            Vector3 player2TargetPosition = GetTargetPosition(currentCharacter, player2Camera);
            player2Camera.transform.position = Vector3.SmoothDamp(player2Camera.transform.position, player2TargetPosition, 
                                                                  ref player2Velocity, cameraSmoothSpeed);
            player2Camera.transform.position = new Vector3(player2Camera.transform.position.x, player2Camera.transform.position.y, -10f);
            player2Camera.orthographicSize = DEFAULT_ORTHOGRAPHIC_SIZE * _ghost_visionMultiplier;
        }
    }

    private Vector3 GetTargetPosition(Transform target, Camera camera)
    {
        Vector3 offset = Vector3.zero;
        if (target.hasChanged)
        {
            Vector3 direction = (target.position - camera.transform.position).normalized;
            offset = direction * offsetDistance;
        }
        return new Vector3(target.position.x, target.position.y, camera.transform.position.z) + offset;
    }

    private void UpdateCurrentControllingGhost()
    {
        string _ghost_currentControlling = IngameDataManager.LoadSpecificData<string>("ghost_data.current_controlling");
        foreach (Transform ghost in allGhosts)
        {
            if (ghost.name == _ghost_currentControlling)
            {
                currentGhost = ghost;
                StartCoroutine(SmoothTransition(player2Camera.transform, new Vector3(ghost.position.x, ghost.position.y, player2Camera.transform.position.z)));
                break;
            }
        }
    }

    private Transform GetCurrentControllingGhost()
    {
        string _ghost_currentControlling = IngameDataManager.LoadSpecificData<string>("ghost_data.current_controlling");
        foreach (Transform ghost in allGhosts)
        {
            if (ghost.name == _ghost_currentControlling)
            {
                if (currentGhost != ghost)
                {
                    currentGhost = ghost;
                    StartCoroutine(SmoothTransition(player2Camera.transform, new Vector3(ghost.position.x, ghost.position.y, player2Camera.transform.position.z)));
                }
                return ghost;
            }
        }
        return null;
    }

    private IEnumerator SmoothTransition(Transform cameraTransform, Vector3 targetPosition)
    {
        float elapsedTime = 0.0f;
        Vector3 startingPosition = cameraTransform.position;
        while (elapsedTime < cameraSmoothSpeed)
        {
            cameraTransform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / cameraSmoothSpeed);
            cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y, -10f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cameraTransform.position = new Vector3(targetPosition.x, targetPosition.y, -10f);
    }

    private IEnumerator UpdatePacmanEffectDisplay()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);

            List<string> _pacman_affectedItems = IngameDataManager.LoadSpecificData<List<string>>("pacman_data.affected_items");
            DisplayEffects(_pacman_affectedItems, pacman_effectDisplayAnchor, pacman_effectDisplayPrefab, true);
        }
    }

    private IEnumerator UpdateGhostEffectDisplay()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);

            string _ghost_currentControlling = IngameDataManager.LoadSpecificData<string>("ghost_data.current_controlling");
            List<string> _ghost_affectedItems = IngameDataManager.LoadSpecificListData<List<string>>("ghost_data.ghost_single_info", _ghost_currentControlling, "affected_items");
            DisplayEffects(_ghost_affectedItems, ghost_effectDisplayAnchor, ghost_effectDisplayPrefab, false);
        }
    }

    private void DisplayEffects(List<string> affectedItems, Transform anchor, GameObject effectDisplayPrefab, bool isPacman)
    {
        foreach (Transform effect in anchor)
        {
            Destroy(effect.gameObject);
        }

        Dictionary<string, int> itemCounts = new Dictionary<string, int>();
        foreach (string item in affectedItems)
        {
            if (itemCounts.ContainsKey(item)) itemCounts[item]++;
            else itemCounts[item] = 1;
        }
        
        List<EffectItem> filteredEffects = new List<EffectItem>();
        foreach (var kvp in itemCounts)
        {
            EffectItem effectItem = pacman_effectItemList.effectItems.Find(i => i.name == kvp.Key);
            if (effectItem == null)
            {
                effectItem = ghost_effectItemList.effectItems.Find(i => i.name == kvp.Key);
            }
            if (effectItem != null)
            {
                filteredEffects.Add(effectItem);
            }
        }

        if (filteredEffects.Count <= 0) return;

        for (int i = 0; i < filteredEffects.Count; i++)
        {
            GameObject effectInstance = Instantiate(effectDisplayPrefab, anchor);
            effectInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80 * i);
            effectInstance.name = filteredEffects[i].inEffect.id;

            effectInstance.transform.GetChild(0).GetComponent<Image>().sprite = filteredEffects[i].inEffect.iconSprite;
            
            string itemName = filteredEffects[i].inEffect.id.Replace("_", " ");
            if (itemCounts[filteredEffects[i].name] > 1)
            {
                itemName += " " + itemCounts[filteredEffects[i].name];
            }
            
            effectInstance.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = itemName;
            effectInstance.transform.GetChild(1).GetChild(1).GetComponent<Text>().text = filteredEffects[i].inEffect.description;
        }
    }
}
