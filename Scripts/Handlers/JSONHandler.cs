using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using System;
using static ClassListData;
using System.Runtime.Serialization.Json;
using System.Text;

public static class JSONHandler
{
    //SAVING PATH FOR FULL GAME RELEASE
    //string playerSaveFilePath = Path.Combine(Application.persistentDataPath, "savename.json");

    public static List<int> CreatePlayerSaveList(UserClasses playerClass, int playerLevel, int playerCurrentXP)
    {
        return new List<int> { ClassEnumToInt(playerClass), playerLevel, playerCurrentXP };
    }

    public static void SavePlayerSave(List<int> saveList, string filePath)
    {
        int[]saveArray = {saveList[0], saveList[1], saveList[2]};
        string json = JsonUtility.ToJson(saveList[0], true);
        Debug.LogError(json);
        File.WriteAllText(filePath, json);
        Debug.Log("PlayerSave saved to " + filePath);
    }

    public static List<int> LoadPlayerSave(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("PlayerSave file not found.");
            return new List<int>();
        }

        string json = File.ReadAllText(filePath);
        List<int> playerSaveList = JsonUtility.FromJson<List<int>>(json);
        return playerSaveList;
    }
}
