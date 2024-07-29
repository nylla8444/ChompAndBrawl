using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class InputKeybind
{
    public string translated_action_name;
    public string action_name;
    public KeyCode key_code;
}

[Serializable]
public class InputKeybindCategory
{
    public string category_name;
    public List<InputKeybind> input_keybinds;

    public InputKeybindCategory(string categoryName)
    {
        category_name = categoryName;
        input_keybinds = new List<InputKeybind>();
    }
}

public static class KeybindDataManager
{
    private static string filePath;
    private static Dictionary<string, InputKeybindCategory> keyBindingCategories;
    private static DefaultInputKeybind defaultInputKeybind;

    public delegate void KeyActionDelegate();
    private static Dictionary<string, KeyActionDelegate> keyActionMap;

    static KeybindDataManager()
    {
        filePath = Path.Combine(Application.persistentDataPath, "key_input_data.dat");
        keyBindingCategories = new Dictionary<string, InputKeybindCategory>();
        keyActionMap = new Dictionary<string, KeyActionDelegate>();
    }

    public static void Initialize(DefaultInputKeybind _defaultInputKeybind)
    {
        defaultInputKeybind = _defaultInputKeybind;
        LoadKeyBindings();
    }

    public static void Update()
    {
        foreach (var category in keyBindingCategories.Values)
        {
            foreach (var keyAction in category.input_keybinds)
            {
                if (Input.GetKeyDown(keyAction.key_code))
                {
                    if (keyActionMap.ContainsKey(keyAction.action_name))
                    {
                        keyActionMap[keyAction.action_name]?.Invoke();
                    }
                }
            }
        }
    }

    public static void ChangeKeyBinding(string actionName, KeyCode newKey)
    {
        foreach (var category in keyBindingCategories.Values)
        {
            var keybind = category.input_keybinds.Find(key => key.action_name == actionName);
            if (keybind != null)
            {
                keybind.key_code = newKey;
                SaveKeyBindings();
                return;
            }
        }
    }

    public static void RegisterKeyAction(string actionName, KeyActionDelegate action)
    {
        if (!keyActionMap.ContainsKey(actionName))
        {
            keyActionMap.Add(actionName, action);
        }
        else
        {
            keyActionMap[actionName] += action;
        }
    }

    public static void UnregisterKeyAction(string actionName, KeyActionDelegate action)
    {
        if (keyActionMap.ContainsKey(actionName))
        {
            keyActionMap[actionName] -= action;
        }
    }

    private static void SaveKeyBindings()
    {
        try
        {
            SerializableDictionary serializedBindings = new SerializableDictionary(keyBindingCategories);
            string jsonData = JsonUtility.ToJson(serializedBindings);

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                binaryFormatter.Serialize(fileStream, jsonData);
            }

            Debug.Log("Key bindings saved successfully. " + jsonData);
        }
        catch (Exception ex)
        {
            Debug.Log("Failed to save key bindings: " + ex.Message);
        }
    }

    private static void LoadKeyBindings()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No data file found. Initializing with default data.");
            InitializeDefaultBindings();
            return;
        }

        try
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                string jsonData = (string)binaryFormatter.Deserialize(fileStream);
                SerializableDictionary loadedBindings = JsonUtility.FromJson<SerializableDictionary>(jsonData);
                keyBindingCategories = loadedBindings.ToDictionary();
            }

            Debug.Log("Key bindings loaded successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load key bindings: " + ex.Message);
            InitializeDefaultBindings();
        }
    }

    public static void DeleteKeyBindings()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No data file found to delete.");
        }

        File.Delete(filePath);
        Debug.Log("Data file deleted successfully.");
        
        LoadKeyBindings();
    }

    private static void InitializeDefaultBindings()
    {
        keyBindingCategories = new Dictionary<string, InputKeybindCategory>();
        if (defaultInputKeybind != null)
        {
            foreach (var category in defaultInputKeybind.inputKeybindCategories)
            {
                keyBindingCategories[category.category_name] = new InputKeybindCategory(category.category_name)
                {
                    input_keybinds = new List<InputKeybind>(category.input_keybinds)
                };
            }
        }

        SaveKeyBindings();
    }

    [Serializable]
    private class SerializableDictionary
    {
        public List<InputKeybindCategory> categories;

        public SerializableDictionary(Dictionary<string, InputKeybindCategory> dictionary)
        {
            categories = new List<InputKeybindCategory>();
            foreach (var category in dictionary.Values)
            {
                categories.Add(category);
            }
        }

        public Dictionary<string, InputKeybindCategory> ToDictionary()
        {
            var dictionary = new Dictionary<string, InputKeybindCategory>();

            foreach (var category in categories)
            {
                dictionary[category.category_name] = category;
            }

            return dictionary;
        }
    }
}
