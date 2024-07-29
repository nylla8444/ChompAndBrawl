using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

// JSON for Player Data
[Serializable]
public class PlayerData
{
    public string selected_map;
    public Audio audio;

    [Serializable]
    public class Audio
    {
        public float music_volume;
        public float sfx_volume;
    }
}

public static class PlayerDataManager
{
    // File path for saved data
    private static string filePath;
    private static DefaultPlayerData defaultPlayerData;

    static PlayerDataManager()
    {
        filePath = Path.Combine(Application.persistentDataPath, "player_data.dat");
    }

    public static void Initialize(DefaultPlayerData _defaultPlayerData)
    {
        defaultPlayerData = _defaultPlayerData;
    }

    // Function for saving playerData to the file
    public static void SaveData(PlayerData playerData)
    {
        try
        {
            // Convert object to JSON
            string jsonData = JsonUtility.ToJson(playerData);

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

    // Function for loading playerData from the file
    public static PlayerData LoadData()
    {
        if (defaultPlayerData == null)
        {
            Debug.LogWarning("DefaultPlayerData is not set. Call Initialize() first.");
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
                PlayerData loadedPlayerData = JsonUtility.FromJson<PlayerData>(jsonData);
                Debug.Log("Data loaded successfully.");
                return loadedPlayerData;
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
        Debug.Log("PlayerData file deleted successfully.");

        LoadData();
    }

    // Function for initializing default data if no data is detected
    private static PlayerData InitializeDefaultData()
    {
        PlayerData playerData = defaultPlayerData.playerData;
        SaveData(playerData);
        return playerData;
    }
}
