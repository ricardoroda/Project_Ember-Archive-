using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using UnityEditor;
using UnityEngine;
using static ClassListData;

public static class ClassListData
{
    public enum UserClasses
    {
        Mage,
        Warrior,
        Rogue
    }

    public static string[] UserClassesString =
    {
        "Mage",
        "Warrior",
        "Rogue"
    };

    // public static ClassSO GetClassSOByInt(int index)
    // {
    //     string classSONameString = UserClassesString[index] + "SO.asset";

    //         string[] guides = AssetDatabase.FindAssets($"t:" + "MageSO", new[] { "Assets/Scripts/Class" });
    //         Debug.LogError("Here 1");
    //         if ( guides.Length > 0 )
    //         {
    //             Debug.LogError("Not empty");
    //         }
    //         string classSOPath = AssetDatabase.GUIDToAssetPath(guides[0]);
    //         Debug.LogError("Here 2");

    //         ClassSO classSO = null;
    //         classSO = AssetDatabase.LoadAssetAtPath<ClassSO>(classSOPath);
    //         Debug.LogError("Here 3");

    //         return classSO;
    // }


    public static int ClassEnumToInt(UserClasses userClass)
    {
        try
        {
            int userClassInt = (int)userClass;
            return userClassInt;
        }
        catch
        {
            Debug.Log("Player Class Save Error!");
            return -1;
        }
    }

    public static UserClasses ClassIntToEnum(int userClassInt)
    {
        try
        {
            UserClasses userClass = (UserClasses)userClassInt;
            return userClass;
        }
        catch
        {
            Debug.Log("Player Class Load Error!");
            return UserClasses.Mage;
        }
    }
}
