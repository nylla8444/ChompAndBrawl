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

    [Serializable]
    public class PacmanData
    {
        public int pacman_score;
        public int pacman_lives;
    }

    [Serializable]
    public class GhostData
    {
        public List<string> list_alive_ghost;
        public string current_controlling_ghost;
        public string current_fighting_ghost;
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

            Debug.Log("Data saved successfully. " + jsonData);
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
        Debug.Log("Data file deleted successfully.");

        LoadData();
    }

    private static GameData InitializeDefaultData()
    {
        GameData gameData = defaultGameData.gameData;
        SaveData(gameData);
        return gameData;
    }
}
