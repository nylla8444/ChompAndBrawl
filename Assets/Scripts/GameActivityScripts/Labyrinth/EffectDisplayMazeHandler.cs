using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectDisplayMazeHandler : MonoBehaviour
{
    [Header("Effect Objects")]
    [SerializeField] private GameObject pacman_effectDisplayPrefab;
    [SerializeField] private GameObject ghost_effectDisplayPrefab;
    [SerializeField] private Transform pacman_effectDisplayAnchor;
    [SerializeField] private Transform ghost_effectDisplayAnchor;
    [SerializeField] private EffectItemList pacman_effectItemList;
    [SerializeField] private EffectItemList ghost_effectItemList;
    
    private void Start()
    {
        StartCoroutine(UpdatePacmanEffectDisplay());
        StartCoroutine(UpdateGhostEffectDisplay());
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
