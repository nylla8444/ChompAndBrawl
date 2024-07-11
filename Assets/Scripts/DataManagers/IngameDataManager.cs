using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

// JSON for InGameData
[Serializable]
public class IngameData
{
    public PacmanData pacman_data;
    public GhostData ghost_data;
    public ItemData item_data;

    [Serializable]
    public class PacmanData
    {
        public int points;
        public int playtime;
        public int lives;
        public string current_effect_item;
        public float speed_multiplier;
        public float vision_multiplier;
        public float wind_burst_speed_affect;
        public Vector2 coordinate;
        public Vector2 direction;
        public bool has_power_pellet;
        public bool has_won_at_fight;
        public bool is_immune_to_ghost;
    }

    [Serializable]
    public class GhostData
    {
        public string current_controlling;
        public string current_fighting;
        public float vision_multiplier;
        public bool is_control_inverted;
        public List<string> list_alive;
        public List<GhostSingleInfo> ghost_single_info;

        [Serializable]
        public class GhostSingleInfo
        {
            public string name;
            public string effect_item;
            public float speed_multiplier;
            public float wind_burst_speed_affect;
            public Vector2 coordinate;
            public Vector2 direction;
        }

        public GhostData()
        {
            ghost_single_info = new List<GhostSingleInfo>();
        }
    }

    [Serializable]
    public class ItemData
    {
        public List<Vector2> pacdot_positions;

        public ItemData()
        {
            pacdot_positions = new List<Vector2>();
        }
    }
}

public static class IngameDataManager 
{
    // File path for saved data
    private static string filePath;
    private static DefaultIngameData defaultIngameData;

    static IngameDataManager()
    {
        filePath = Path.Combine(Application.persistentDataPath, "game_data.dat");
    }

    public static void Initialize(DefaultIngameData _defaultIngameData)
    {
        defaultIngameData = _defaultIngameData;
    }

    // Function for saving ingameData to the file
    public static void SaveData(IngameData ingameData)
    {
        try
        {
            // Convert object to JSON
            string jsonData = JsonUtility.ToJson(ingameData);

            // Convert JSON to formatted binary
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                binaryFormatter.Serialize(fileStream, jsonData);
            }

            // Debug.Log("Data saved successfully. " + jsonData);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save data: " + ex.Message);
        }
    }

    // Function for loading gameData from the file
    public static IngameData LoadData()
    {
        if (defaultIngameData == null)
        {
            Debug.LogError("DefaultIngameData is not set. Call Initialize() first.");
            return null;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No data file found. Initializing with default data.");
            return InitializeDefaultData();
        }

        try
        {
            // Read binary data
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                string jsonData = (string)binaryFormatter.Deserialize(fileStream);

                // Convert JSON to object
                IngameData loadedIngameData = JsonUtility.FromJson<IngameData>(jsonData);

                // Debug.Log("Data loaded successfully.");
                return loadedIngameData;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load data: " + ex.Message);
            return null;
        }
    }

    public static void DeleteData()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No data file found to delete.");
        }

        File.Delete(filePath);
        Debug.Log("IngameData file deleted successfully.");

        LoadData();
    }

    private static IngameData InitializeDefaultData()
    {
        IngameData ingameData = defaultIngameData.ingameData;
        SaveData(ingameData);
        return ingameData;
    }

    public static T LoadSpecificData<T>(string path)
    {
        IngameData ingameData = LoadData() ?? InitializeDefaultData();
        object data = ingameData;

        foreach (var part in path.Split('.'))
        {
            var type = data.GetType();
            var field = type.GetField(part);
            if (field == null) throw new Exception($"Field '{part}' not found in type '{type}'");

            data = field.GetValue(data);
        }

        return (T)data;
    }

    public static void SaveSpecificData<T>(string path, T value)
    {
        IngameData ingameData = LoadData() ?? InitializeDefaultData();
        object data = ingameData;

        var parts = path.Split('.');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var type = data.GetType();
            var field = type.GetField(parts[i]);
            if (field == null) throw new Exception($"Field '{parts[i]}' not found in type '{type}'");

            data = field.GetValue(data);
        }

        var finalType = data.GetType();
        var finalField = finalType.GetField(parts[^1]);
        if (finalField == null) throw new Exception($"Field '{parts[^1]}' not found in type '{finalType}'");

        finalField.SetValue(data, value);
        SaveData(ingameData);
    }

    public static T LoadSpecificListData<T>(string listPath, string elementName, string fieldName)
    {
        IngameData ingameData = LoadData() ?? InitializeDefaultData();
        object data = ingameData;

        foreach (var part in listPath.Split('.'))
        {
            var type = data.GetType();
            var p_field = type.GetField(part);
            if (p_field == null) throw new Exception($"Field '{part}' not found in type '{type}'");

            data = p_field.GetValue(data);
        }

        if (!(data is IList list)) throw new Exception("The specified path does not point to a list");

        object element = null;
        foreach (var item in list)
        {
            var nameField = item.GetType().GetField("name");
            if (nameField != null && (string)nameField.GetValue(item) == elementName)
            {
                element = item;
                break;
            }
        }
        if (element == null) throw new Exception($"Element with name '{elementName}' not found in the list");

        var n_field = element.GetType().GetField(fieldName);
        if (n_field == null) throw new Exception($"Field '{fieldName}' not found in type '{element.GetType()}'");

        return (T)n_field.GetValue(element);
    }

    public static void SaveSpecificListData<T>(string listPath, string elementName, string fieldName, T value)
    {
        IngameData ingameData = LoadData() ?? InitializeDefaultData();
        object data = ingameData;

        foreach (var part in listPath.Split('.'))
        {
            var type = data.GetType();
            var p_field = type.GetField(part);
            if (p_field == null) throw new Exception($"Field '{part}' not found in type '{type}'");

            data = p_field.GetValue(data);
        }

        if (!(data is IList list)) throw new Exception("The specified path does not point to a list");

        object element = null;
        foreach (var item in list)
        {
            var nameField = item.GetType().GetField("name");
            if (nameField != null && (string)nameField.GetValue(item) == elementName)
            {
                element = item;
                break;
            }
        }
        if (element == null) throw new Exception($"Element with name '{elementName}' not found in the list");

        var n_field = element.GetType().GetField(fieldName);
        if (n_field == null) throw new Exception($"Field '{fieldName}' not found in type '{element.GetType()}'");

        n_field.SetValue(element, value);
        SaveData(ingameData);
    }
}
