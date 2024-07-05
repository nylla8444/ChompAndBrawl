using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

// JSON for Game Data
[Serializable]
public class GameData
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
        public Vector2 coordinate;
        public Vector2 direction;
        public float speed_multiplier;
        public float vision_multiplier;
        public bool has_power_pellet;
        public bool has_won_at_fight;
        public string current_effect_item;
    }

    [Serializable]
    public class GhostData
    {
        public string current_controlling;
        public string current_fighting;
        public List<string> list_alive;
        public List<GhostPositions> ghost_positions;
        public List<GhostSpeedMultipliers> ghost_speed_multipliers;
        public bool is_control_inverted;

        [Serializable]
        public class GhostPositions
        {
            public string name;
            public Vector2 coordinate;
        }

        [Serializable]
        public class GhostSpeedMultipliers
        {
            public string name;
            public float speed_multiplier;
        }

        public GhostData()
        {
            ghost_positions = new List<GhostPositions>();
            ghost_speed_multipliers = new List<GhostSpeedMultipliers>();
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

public static class GameDataManager 
{
    // File path for saved data
    private static string filePath;
    private static DefaultGameData defaultGameData;

    static GameDataManager()
    {
        filePath = Path.Combine(Application.persistentDataPath, "game_data.dat");
    }

    public static void Initialize(DefaultGameData _defaultGameData)
    {
        defaultGameData = _defaultGameData;
    }

    // Function for saving gameData to the file
    public static void SaveData(GameData gameData)
    {
        try
        {
            // Convert object to JSON
            string jsonData = JsonUtility.ToJson(gameData);

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
    public static GameData LoadData()
    {
        if (defaultGameData == null)
        {
            Debug.LogError("DefaultGameData is not set. Call Initialize() first.");
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
                GameData loadedGameData = JsonUtility.FromJson<GameData>(jsonData);

                // Debug.Log("Data loaded successfully.");
                return loadedGameData;
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
        Debug.Log("GameData file deleted successfully.");

        LoadData();
    }

    private static GameData InitializeDefaultData()
    {
        GameData gameData = defaultGameData.gameData;
        SaveData(gameData);
        return gameData;
    }
}
