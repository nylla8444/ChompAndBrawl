using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TransitionDictionary", menuName = "Objects/DictionaryList/TransitionDictionary")]
public class TransitionDictionary : ScriptableObject
{
    [System.Serializable]
    public class TransitionInfo
    {
        public string startTransitionName;
        public string endTransitionName;
        public int transitionDuration;
    }

    public Dictionary<string, TransitionInfo> transitionDictionary;
    [SerializeField] private List<string> transitionIds;
    [SerializeField] private List<TransitionInfo> transitionInfos;

    private void OnEnable()
    {
        transitionDictionary = new Dictionary<string, TransitionInfo>();

        for (int i = 0; i < transitionInfos.Count; i++)
        {
            if (i < transitionIds.Count)
            {
                transitionDictionary[transitionIds[i]] = transitionInfos[i];
            }
        }
    }
}