using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocaleSelector : MonoBehaviour
{
    [SerializeField] private List<string> localeNames;
    [SerializeField] private List<Button> localeButtons;
    [HideInInspector] public string currentLocale;

    private void Awake()
    {
        PrepareObjectListeners();
    }

    public void PrepareObjectListeners()
    {
        try
        {
            for (int i = 0; i < localeButtons.Count; i++)
            {
                int index = i;
                localeButtons[i].onClick.AddListener(() => OnLocaleButtonPressed(localeNames[index]));
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Skipping adding listener. " + ex.Message);
        }
    }

    public void OnLocaleButtonPressed(string localeName)
    {
        // Check if the locationName is valid
        if (!localeNames.Contains(localeName))
        {
            Debug.LogError($"{localeName} does not exist.");
            return;
        }
        currentLocale = localeName;
    }
}
