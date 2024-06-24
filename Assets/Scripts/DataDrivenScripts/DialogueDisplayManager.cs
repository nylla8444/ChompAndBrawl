using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueDisplayManager : MonoBehaviour
{
    [ReadOnly] public DialogueList dialogueIds;
    [ReadOnly] public string characterName;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text characterNameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private GameObject dialogueBackground;

    [SerializeField] private List<Transform> characterAnchorList; // 0: characterLeftScreen, 1: characterOnScreen
    [SerializeField] private List<Transform> dialogueBoxAnchorList; // 0: dialogueBoxBelowScreen, 1: dialogueBoxOnScreen
    [SerializeField] private List<Color> colorList; // 0: invisible, 1: 50%-black

    [SerializeField] private float typingSpeed;
    [SerializeField] private float characterTransitionSpeed;
    [SerializeField] private float dialogueBoxTransitionSpeed;
    [SerializeField] private float colorTransitionSpeed;

    private int dialogueIndex = 0;
    private bool isTyping = false;
    private bool isTransitioning = false;
    private Vector3 characterVelocity = Vector3.zero;
    private Vector3 dialogueBoxVelocity = Vector3.zero;
    [HideInInspector] public bool isDialogueDisplaying = false;

    private void Awake()
    {
        InitializeUi();
    }

    private void InitializeUi()
    {
        dialogueBackground.SetActive(false);
        characterImage.transform.position = characterAnchorList[0].position;
        dialogueBox.transform.position = dialogueBoxAnchorList[0].position;
        dialogueBox.GetComponent<Button>().onClick.AddListener(SkipDialogue);
    }

    public IEnumerator StartDisplayDialogue()
    {
        characterNameText.text = characterName;
        characterImage.sprite = dialogueIds.dialogueList[dialogueIds.dialogues[0]];

        if (dialogueIds.dialogueList.Count == 0)
        {
            Debug.LogWarning("No dialogues found inside dialogue list.");
            yield break;
        }

        isDialogueDisplaying = true;
        dialogueBackground.SetActive(true);
        StartCoroutine(UpdateObjectPosition(characterImage.gameObject, characterAnchorList[1], characterVelocity, characterTransitionSpeed));
        StartCoroutine(UpdateObjectPosition(dialogueBox, dialogueBoxAnchorList[1], dialogueBoxVelocity, dialogueBoxTransitionSpeed));
        StartCoroutine(UpdateObjectColor(dialogueBackground, colorList[1], colorTransitionSpeed));
        
        while (isTransitioning)
        {
            yield return null;
        }

        ShowNextDialogue();
    }

    private void SkipDialogue()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = dialogueIds.dialogues[dialogueIndex];
            isTyping = false;

            dialogueIndex++;
        }
        else
        {
            ShowNextDialogue();
        }
    }

    public void ShowNextDialogue()
    {
        if (dialogueIndex < dialogueIds.dialogues.Count)
        {
            string currentDialogue = dialogueIds.dialogues[dialogueIndex];
            StartCoroutine(TypeDialogue(currentDialogue));

            // Update character expression if available
            if (dialogueIds.dialogueList.ContainsKey(currentDialogue))
            {
                characterImage.sprite = dialogueIds.dialogueList[currentDialogue];
            }
        }
        else
        {
            StartCoroutine(FinishDisplayDialogue());
        }
    }

    private IEnumerator TypeDialogue(string dialogue)
    {
        dialogueText.text = "";
        isTyping = true;
        foreach (char letter in dialogue.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;

        dialogueIndex++;
    }

    private IEnumerator UpdateObjectPosition(GameObject _object, Transform _anchor, Vector3 _velocity, float _transitionSpeed)
    {
        isTransitioning = true;
        while (Vector3.Distance(_object.transform.position, _anchor.position) > 0.05f)
        {
            _object.transform.position = Vector3.SmoothDamp(_object.transform.position, _anchor.position, ref _velocity, (100.0f / _transitionSpeed) * Time.deltaTime);
            yield return null;
        }
        
        _object.transform.position = _anchor.position;
        isTransitioning = false;
    }

    private IEnumerator UpdateObjectColor(GameObject _object, Color _color, float _transitionSpeed)
    {
        Image image = _object.GetComponent<Image>();
        Color currentColor = image.color;

        while (!AreColorsAdjacent(currentColor, _color))
        {
            currentColor = Color.Lerp(currentColor, _color, _transitionSpeed * Time.deltaTime);
            image.color = currentColor;
            yield return null;
        }

        image.color = _color;
    }

    private bool AreColorsAdjacent(Color a, Color b, float tolerance = 0.01f)
    {
        return (Mathf.Abs(a.r - b.r) < tolerance) && (Mathf.Abs(a.g - b.g) < tolerance) && (Mathf.Abs(a.b - b.b) < tolerance) && (Mathf.Abs(a.a - b.a) < tolerance);
    }

    private IEnumerator FinishDisplayDialogue()
    {
        // Transition character and dialogue group back to initial position
        StartCoroutine(UpdateObjectPosition(characterImage.gameObject, characterAnchorList[0], characterVelocity, characterTransitionSpeed));
        StartCoroutine(UpdateObjectPosition(dialogueBox, dialogueBoxAnchorList[0], dialogueBoxVelocity, dialogueBoxTransitionSpeed));
        StartCoroutine(UpdateObjectColor(dialogueBackground, colorList[0], colorTransitionSpeed));

        while (isTransitioning)
        {
            yield return null;
        }

        dialogueIndex = 0;
        dialogueText.text = "";
        dialogueBackground.SetActive(false);
        isDialogueDisplaying = false;
    }
}
