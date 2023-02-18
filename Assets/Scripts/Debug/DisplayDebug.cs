using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class DisplayDebug
{
    
    private static TextMeshProUGUI debugTextContainer;
    private static Dictionary<string, int> debugDictionary;
    private static List<KeyValuePair<string, int>> debugList;

    public static void InitalizeDebugManager(GameObject debugContainer)
    {
        debugTextContainer = debugContainer.GetComponentsInChildren<TextMeshProUGUI>()[0];
        debugTextContainer.text = "";
    }

    public static void UpdateDebugs()
    {
        LoopVariables();
    }

    private static void LoopVariables()
    {
        debugTextContainer.text = "";

        foreach (var debug in debugList)
        {
            debugTextContainer.text += debug.Key + ": " + debug.Value + "\n";
        }
    }

    public static void AddOrSetDebugVariable(string text, int variableValue, int indexInList)
    {
        KeyValuePair<string, int> debugVariable = new KeyValuePair<string, int>(text,variableValue);
        if (debugList == null)
        {
            InitializeDictionary();
        }

        int existingIndex = -1;
        for (int i = 0; i < debugList.Count; i++)
        {
            KeyValuePair<string, int> pair = debugList[i];
            if (pair.Key == text)
            {
                existingIndex = i;
            }   
        }

        if (existingIndex >= 0)
        {
            debugList[existingIndex] = debugVariable;
        }
        else
        {
            if (debugList.Count > indexInList)
            {
                debugList.Insert(indexInList,debugVariable);
            }
            else
            {
                debugList.Add(debugVariable);
            }
        }
    }

    private static void InitializeDictionary()
    {
        if (debugList != null)
        {
            return;
        }

        debugList = new List<KeyValuePair<string, int>>();
    }

}
