using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class DisplayDebug
{
#if DEBUG || UNITY_EDITOR
    private static TextMeshProUGUI debugTextContainer;
    private static List<KeyValuePair<string, string>> debugList;

    /// <summary>
    /// Initialize the parameters
    /// </summary>
    /// <param name="debugContainer"></param>
    public static void Initalize(GameObject debugContainer)
    {
        debugTextContainer = debugContainer.GetComponentsInChildren<TextMeshProUGUI>()[0];
        debugTextContainer.text = "";
    }

    /// <summary>
    /// Update the debug texts
    /// </summary>
    public static void UpdateDebugs()
    {
        
        debugTextContainer.text = "";

        foreach (var debug in debugList)
        {
            debugTextContainer.text += debug.Key + ": " + debug.Value + "\n";
        }
        
    }


    /// <summary>
    /// Add or update a variable to display in the debug text
    /// </summary>
    /// <param name="text">The prompt text in front of value</param>
    /// <param name="variableValue">The value to be displayed</param>
    /// <param name="indexInList">What order in the debug text it should be displayed (optional)</param>
    public static void AddOrSetDebugVariable(string text, int variableValue, int indexInList = -1)
    {
        UpdateOrAddToList(text, variableValue.ToString(), indexInList);
    }

    /// <summary>
    /// Overload of other method, this one takes in a string instead of an int
    /// </summary>
    /// <param name="text"></param>
    /// <param name="variableValue"></param>
    /// <param name="indexInList"></param>
    public static void AddOrSetDebugVariable(string text, string variableValue, int indexInList = -1)
    {
        UpdateOrAddToList(text, variableValue, indexInList);
    }

    private static void UpdateOrAddToList(string text, string variableValue, int indexInList)
    {
        KeyValuePair<string, string> debugVariable = new KeyValuePair<string, string>(text, variableValue);
        if (debugList == null)
        {
            InitializeDictionary();
        }

        int existingIndex = -1;
        for (int i = 0; i < debugList.Count; i++)
        {
            KeyValuePair<string, string> pair = debugList[i];
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
            if (debugList.Count > indexInList && indexInList != -1)
            {
                debugList.Insert(indexInList, debugVariable);
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

        debugList = new List<KeyValuePair<string, string>>();
    }
#endif
}
