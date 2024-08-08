using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class LeaderboardData
{
    public List<RowData> rowData;

    [Serializable]
    public class RowData
    {
        public string code_name;
        public string winner_name;
        public int pac_points;
        public int playtime;
    }

    public void AddRowData(string codeName, string winnerName, int pacPoints, int playtime)
    {
        RowData newRow = new RowData
        {
            code_name = codeName,
            winner_name = winnerName,
            pac_points = pacPoints,
            playtime = playtime
        };

        rowData.Add(newRow);
    }
}

public static class LeaderboardDataManager
{
    // File path for saved data
    private static string filePath;
    private static DefaultLeaderboardData defaultLeaderboardData;

    static LeaderboardDataManager()
    {
        filePath = Path.Combine(Application.persistentDataPath, "leaderboard_data.dat");
    }

    public static void Initialize(DefaultLeaderboardData _defaultLeaderboardData)
    {
        defaultLeaderboardData = _defaultLeaderboardData;
    }

    // Function for saving leaderboardData to the file
    public static void SaveData(LeaderboardData leaderboardData)
    {
        try
        {
            // Convert object to JSON
            string jsonData = JsonUtility.ToJson(leaderboardData);

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

    // Function for loading leaderboardData from the file
    public static LeaderboardData LoadData()
    {
        if (defaultLeaderboardData == null)
        {
            Debug.LogWarning("DefaultLeaderboardData is not set. Call Initialize() first.");
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
                LeaderboardData loadedLeaderboardData = JsonUtility.FromJson<LeaderboardData>(jsonData);
                Debug.Log("Data loaded successfully.");
                return loadedLeaderboardData;
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
        Debug.Log("LeaderboardData file deleted successfully.");

        LoadData();
    }

    // Function for initializing default data if no data is detected
    private static LeaderboardData InitializeDefaultData()
    {
        LeaderboardData leaderboardData = defaultLeaderboardData.leaderboardData;
        SaveData(leaderboardData);
        return leaderboardData;
    }
}
