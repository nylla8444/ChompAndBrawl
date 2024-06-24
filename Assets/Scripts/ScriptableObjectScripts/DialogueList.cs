using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueList", menuName = "Objects/DialogueList")]
public class DialogueList : ScriptableObject
{
    public Dictionary<string, Sprite> dialogueList;
    public List<string> dialogues;
    [SerializeField] private List<Sprite> expressions;

    private void OnEnable()
    {
        dialogueList = new Dictionary<string, Sprite>();

        for (int i = 0; i < dialogues.Count; i++)
        {
            if (i < expressions.Count)
            {
                dialogueList[dialogues[i]] = expressions[i];
            }
        }
    }
}
