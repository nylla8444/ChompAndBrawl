using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DescriptionDictionary", menuName = "Objects/DictionaryList/DescriptionDictionary")]
public class DescriptionDictionary : ScriptableObject
{
    public Dictionary<string, string> descriptionDictionary;
    [SerializeField] private List<string> nameIds;
    [SerializeField] private List<string> descriptions;

    private void OnEnable()
    {
        descriptionDictionary = new Dictionary<string, string>();

        for (int i = 0; i < descriptions.Count; i++)
        {
            if (i < nameIds.Count)
            {
                descriptionDictionary[nameIds[i]] = descriptions[i];
            }
        }
    }
}
