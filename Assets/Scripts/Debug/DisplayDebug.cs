using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class DisplayDebug
{
    
    private static TextMeshProUGUI debugTextContainer;
    private static Dictionary<string, int> debugDictionary;

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

        foreach (var debug in debugDictionary)
        {
            debugTextContainer.text += debug.Key + ": " + debug.Value + "\n";
        }
    }

    public static void AddOrSetDebugVariable(string text, int variableValue)
    {
        if (debugDictionary == null)
        {
            InitializeDictionary();
        }

        if (debugDictionary.ContainsKey(text))
        {
            debugDictionary[text] = variableValue;
        }
        else
        {
            debugDictionary.Add(text, variableValue);
        }
    }

    private static void InitializeDictionary()
    {
        if (debugDictionary != null)
        {
            return;
        }

        debugDictionary = new Dictionary<string, int>();
    }

}
