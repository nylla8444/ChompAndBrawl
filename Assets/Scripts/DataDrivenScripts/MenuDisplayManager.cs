using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MenuMultimap
{
    public string menuName;
    public List<Button> buttons;
}

public class MenuDisplayManager : MonoBehaviour
{
    private Dictionary<string, GameObject> menus;
    [SerializeField] private List<GameObject> menuList;
    [SerializeField] private List<MenuMultimap> menuMultimap;

    [SerializeField] private GameObject menuBackground;
    [SerializeField] private Button backButton;
    [SerializeField] private List<Button> hideMenuButtons;

    [SerializeField] private List<Transform> menuAnchorList; // 0: menuAboveScreen, 1: menuOnScreen
    [SerializeField] private List<Color> colorList; // 0: invisible, 1: 50%-black
    [SerializeField] private float menuTransitionSpeed;
    [SerializeField] private float colorTransitionSpeed;
    
    private GameObject selectedMenu;
    private GameObject previousMenu;
    private Color targetColor;
    private Transform targetMenuAnchor;
    private Vector3 menuVelocity = Vector3.zero;


    /************************************ INITIAL *************************************/

    private void Awake()
    {
        ConvertListToDict();
        PrepareObjectListeners();
        InitializeUi();
    }

    private void ConvertListToDict()
    {
        menus = new Dictionary<string, GameObject>();

        for (int i = 0; i < menuMultimap.Count; i++)
        {
            var multimap = menuMultimap[i];
            if (i < menuList.Count)
            {
                menus[multimap.menuName] = menuList[i];
            }
            else
            {
                Debug.LogWarning($"Menu list does not contain enough items for {multimap.menuName}");
            }
        }
    }

    private void PrepareObjectListeners()
    {
        foreach (var multimap in menuMultimap)
        {
            foreach (var button in multimap.buttons)
            {
                string menuName = multimap.menuName;
                button.onClick.AddListener(() => OnMenuButtonPressed(menuName));
            }
        }

        foreach (Button hideMenuButton in hideMenuButtons)
        {
            hideMenuButton.onClick.AddListener(() => OnMenuHide());
        }
    }

    private void InitializeUi()
    {
        backButton.gameObject.SetActive(false);
        menuBackground.SetActive(false);
        menuBackground.GetComponent<Image>().color = colorList[0];

        foreach (GameObject menu in menuList)
        {
            menu.transform.position = menuAnchorList[0].position;
        }
    }


    /************************************* GENERAL *************************************/

    private IEnumerator MenuObjectTransition()
    {
        backButton.gameObject.SetActive(false);
        menuBackground.SetActive(true);
    
        // Check if there still exist a menu after pressing a menu button
        if (previousMenu != null)
        {
            while (Vector3.Distance(previousMenu.transform.position, menuAnchorList[0].position) > 0.05f)
            {
                UpdateObjectPosition(previousMenu, menuAnchorList[0], ref menuVelocity, menuTransitionSpeed);
                yield return null;
            }

            previousMenu.transform.position = menuAnchorList[0].position;
            previousMenu = null;
        }

        if (selectedMenu != null && previousMenu == null)
        {
            while (Vector3.Distance(selectedMenu.transform.position, targetMenuAnchor.position) > 0.05f)
            {
                UpdateObjectPosition(selectedMenu, targetMenuAnchor, ref menuVelocity, menuTransitionSpeed);
                UpdateObjectColor(menuBackground, targetColor, colorTransitionSpeed);
                yield return null;
            }

            backButton.gameObject.SetActive((targetMenuAnchor == menuAnchorList[0]) ? false : true);
            menuBackground.SetActive((targetMenuAnchor == menuAnchorList[0]) ? false : true);
                
            selectedMenu.transform.position = targetMenuAnchor.position;
            targetMenuAnchor = null;
        }
    }

    private void UpdateObjectPosition(GameObject _object, Transform _anchor, ref Vector3 _velocity, float _transitionSpeed)
    {
        _object.transform.position = Vector3.SmoothDamp(_object.transform.position, _anchor.position, ref _velocity, (100.0f / _transitionSpeed) * Time.deltaTime);
    }

    private void UpdateObjectColor(GameObject _object, Color _color, float _transitionSpeed)
    {
        _object.GetComponent<Image>().color = Color.Lerp(_object.GetComponent<Image>().color, _color, _transitionSpeed * Time.deltaTime);
    }
    
    public void OnMenuButtonPressed(string menuName)
    {
        // Check if the menuName is valid
        if (menus.ContainsKey(menuName))
        {
            targetColor = colorList[1];
            targetMenuAnchor = menuAnchorList[1];

            // Check if there still exist a menu after pressing a menu button
            if (selectedMenu != null && selectedMenu.transform.position == menuAnchorList[1].position)
            {
                previousMenu = selectedMenu;
                StartCoroutine(MenuObjectTransition());
            }

            selectedMenu = menus[menuName];
            if (previousMenu == null)
            {
                StartCoroutine(MenuObjectTransition());
            }
        }
    }

    public void OnMenuHide()
    {
        if (selectedMenu != null && selectedMenu.transform.position == menuAnchorList[1].position)
        {
            targetColor = colorList[0];
            targetMenuAnchor = menuAnchorList[0];
            StartCoroutine(MenuObjectTransition());
        }
    }
}
