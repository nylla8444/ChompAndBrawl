using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ButtonType
{
    SingleButton,
    LabelButton,
    NavigableButton
}

public enum HighlightType
{
    Outline,
    TextColor,
    BackgroundImage
}

[Serializable]
public class ButtonGroup
{
    [Header("Button Type")]
    public ButtonType buttonType;

    [Header("Single Button Item Properties")]
    public List<Button> buttons;

    [Header("Label Button Item Properties")]
    public Text label;
    public List<Button> labelButtons;

    [Header("Navigable Button Item Properties")]
    public Button navigableButton;
    public List<Button> targetButtons;

    [Header("Highlight Type")]
    public HighlightType highlightType;

    [Header("Highlight Properties")]
    public Color highlightColor;
    public Sprite highlightBackgroundSprite;
}

public class ButtonSelector : MonoBehaviour
{
    [SerializeField] private List<ButtonGroup> buttonGroupList;

    private List<List<ButtonItem>> allButtonGroups;
    private int currentGroupIndex = 0;
    private int currentButtonIndex = 0;

    private void Start()
    {
        SetButtonGroups();
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        UnregisterKeyActions();
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.move_next_selection", MoveToNextSelection);
        KeybindDataManager.RegisterKeyAction("general.move_previous_selection", MoveToPreviousSelection);
        KeybindDataManager.RegisterKeyAction("general.go_select", SelectButton);
    }

    private void UnregisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("general.move_next_selection", MoveToNextSelection);
        KeybindDataManager.RegisterKeyAction("general.move_previous_selection", MoveToPreviousSelection);
        KeybindDataManager.RegisterKeyAction("general.go_select", SelectButton);
    }

    private void SetButtonGroups()
    {
        allButtonGroups = new List<List<ButtonItem>>();

        foreach (var buttonGroup in buttonGroupList)
        {
            var items = CreateItems(buttonGroup);
            if (items.Count > 0)
            {
                allButtonGroups.Add(items);
            }
        }

        if (allButtonGroups.Count > 0 && allButtonGroups[0].Count > 0)
        {
            HighlightButton(currentGroupIndex, currentButtonIndex);
        }
    }

    private List<ButtonItem> CreateItems(ButtonGroup buttonGroup)
    {
        List<ButtonItem> items = new List<ButtonItem>();

        switch (buttonGroup.buttonType)
        {
            case ButtonType.SingleButton:
                foreach (var button in buttonGroup.buttons)
                {
                    items.Add(new SingleButtonItem(button, buttonGroup.highlightType, buttonGroup.highlightColor, buttonGroup.highlightBackgroundSprite));
                }
                break;
            
            case ButtonType.LabelButton:
                if (buttonGroup.label != null && buttonGroup.labelButtons.Count > 0)
                {
                    items.Add(new LabelButtonItem(buttonGroup.label, buttonGroup.labelButtons, buttonGroup.highlightType, buttonGroup.highlightColor, buttonGroup.highlightBackgroundSprite));
                }
                break;
            
            case ButtonType.NavigableButton:
                if (buttonGroup.navigableButton != null && buttonGroup.targetButtons.Count > 0)
                {
                    items.Add(new NavigableButtonItem(buttonGroup.navigableButton, buttonGroup.targetButtons, buttonGroup.highlightType, buttonGroup.highlightColor, buttonGroup.highlightBackgroundSprite));
                }
                break;
        }

        return items;
    }

    private void MoveToNextSelection()
    {
        if (allButtonGroups.Count == 0) return;
        
        currentButtonIndex++;
        if (currentButtonIndex >= allButtonGroups[currentGroupIndex].Count)
        {
            currentButtonIndex = 0;
            currentGroupIndex = (currentGroupIndex + 1) % allButtonGroups.Count;
        }
        HighlightButton(currentGroupIndex, currentButtonIndex);
    }

    private void MoveToPreviousSelection()
    {
        if (allButtonGroups.Count == 0) return;
        
        currentButtonIndex--;
        if (currentButtonIndex < 0)
        {
            currentGroupIndex = (currentGroupIndex - 1 + allButtonGroups.Count) % allButtonGroups.Count;
            currentButtonIndex = allButtonGroups[currentGroupIndex].Count - 1;
        }
        HighlightButton(currentGroupIndex, currentButtonIndex);
    }

    private void HighlightButton(int groupIndex, int buttonIndex)
    {
        if (groupIndex < 0 || groupIndex >= allButtonGroups.Count) return;

        for (int i = 0; i < allButtonGroups.Count; i++)
        {
            for (int j = 0; j < allButtonGroups[i].Count; j++)
            {
                bool isSelected = (i == groupIndex) && (j == buttonIndex);
                allButtonGroups[i][j].Highlight(isSelected);
            }
        }
    }

    private void SelectButton()
    {
        if (currentGroupIndex >= 0 && currentGroupIndex < allButtonGroups.Count &&
            currentButtonIndex >= 0 && currentButtonIndex < allButtonGroups[currentGroupIndex].Count)
        {
            allButtonGroups[currentGroupIndex][currentButtonIndex].Press();
        }
    }
}


public abstract class ButtonItem
{
    protected HighlightType highlightType;
    protected Color highlightColor;
    protected Sprite highlightBackgroundSprite;
    protected Color originalTextColor;
    private bool originalTextColorSet = false;

    public ButtonItem(HighlightType highlightType, Color highlightColor, Sprite highlightBackgroundSprite)
    {
        this.highlightType = highlightType;
        this.highlightColor = highlightColor;
        this.highlightBackgroundSprite = highlightBackgroundSprite;
    }

    public abstract void Highlight(bool isHighlighted);
    public abstract void Press();

    protected void SetHighlight(GameObject target, bool isHighlighted)
    {
        Text text = null;
        GameObject highlightImage = null;
        Image backgroundImage = null;

        try { text = target.transform.GetChild(0).GetComponent<Text>(); } catch (Exception) { }
        try 
        { 
            highlightImage = target.transform.GetChild(1).gameObject; 
            backgroundImage = highlightImage.GetComponent<Image>(); 
        } 
        catch (Exception) { }

        switch (highlightType)
        {
            case HighlightType.Outline:
                Outline outline = text.GetComponent<Outline>();
                if (isHighlighted)
                {
                    if (outline == null)
                    {
                        outline = text?.gameObject.AddComponent<Outline>();
                    }
                    if (outline != null)
                    {
                        outline.effectColor = highlightColor;
                        outline.effectDistance = new Vector2(4, -4);
                    }
                }
                else if (outline != null)
                {
                    UnityEngine.Object.Destroy(outline);
                }
                break;
            
            case HighlightType.BackgroundImage:
                highlightImage?.SetActive(isHighlighted);
                if (isHighlighted && backgroundImage != null)
                {
                    backgroundImage.sprite = highlightBackgroundSprite;
                }
                break;
            
            case HighlightType.TextColor:
                if (text != null)
                {
                    if (!originalTextColorSet)
                    {
                        originalTextColor = text.color;
                        originalTextColorSet = true;
                    }
                    text.color = isHighlighted ? highlightColor : originalTextColor;
                }
                break;
        }
    }
}

public class SingleButtonItem : ButtonItem
{
    private Button button;

    public SingleButtonItem(Button button, HighlightType highlightType, Color highlightColor, Sprite highlightBackgroundSprite)
        : base(highlightType, highlightColor, highlightBackgroundSprite)
        {
            this.button = button;
        }
    
    public override void Highlight(bool isHighlighted)
    {
        SetHighlight(button.gameObject, isHighlighted);
    }

    public override void Press()
    {
        button.onClick.Invoke();
    }
}

public class LabelButtonItem : ButtonItem
{
    private Text label;
    private List<Button> buttons;
    private int currentButtonIndex = 0;

    public LabelButtonItem(Text label, List<Button> buttons, HighlightType highlightType, Color highlightColor, Sprite highlightBackgroundSprite)
        :base(highlightType, highlightColor, highlightBackgroundSprite)
        {
            this.label = label;
            this.buttons = buttons;
        }

    public override void Highlight(bool isHighlighted)
    {
        SetHighlight(label.gameObject, isHighlighted);
        if (isHighlighted && buttons.Count > 0)
        {
            HighlightButton(currentButtonIndex, true);
        }
    }

    private void HighlightButton(int index, bool isHighlighted)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            SetHighlight(buttons[i].gameObject, i == index && isHighlighted);
        }
    }

    public override void Press()
    {
        if (buttons.Count > 0)
        {
            buttons[currentButtonIndex].onClick.Invoke();
        }
    }
}

public class NavigableButtonItem : ButtonItem
{
    private Button button;
    private List<Button> targetButtons;
    private int currentButtonIndex = 0;

    public NavigableButtonItem(Button button, List<Button> targetButtons, HighlightType highlightType, Color highlightColor, Sprite highlightBackgroundSprite)
        : base(highlightType, highlightColor, highlightBackgroundSprite)
        {
            this.button = button;
            this.targetButtons = targetButtons;
        }

    public override void Highlight(bool isHighlighted)
    {
        SetHighlight(button.gameObject, isHighlighted);
        if (isHighlighted && targetButtons.Count > 0)
        {
            HighlightButton(currentButtonIndex, true);
        }
    }

    private void HighlightButton(int index, bool isHighlighted)
    {
        for (int i = 0; i < targetButtons.Count; i++)
        {
            SetHighlight(targetButtons[i].gameObject, i == index && isHighlighted);
        }
    }

    public override void Press()
    {
        if (targetButtons.Count > 0)
        {
            targetButtons[currentButtonIndex].onClick.Invoke();
        }
    }
}