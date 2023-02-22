using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class DisplayDebug
{
    private static TextMeshProUGUI debugTextContainer;
    private static List<KeyValuePair<string, string>> debugListInt;

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

        foreach (var debug in debugListInt)
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
        if (debugListInt == null)
        {
            InitializeDictionary();
        }

        int existingIndex = -1;
        for (int i = 0; i < debugListInt.Count; i++)
        {
            KeyValuePair<string, string> pair = debugListInt[i];
            if (pair.Key == text)
            {
                existingIndex = i;
            }
        }

        if (existingIndex >= 0)
        {
            debugListInt[existingIndex] = debugVariable;
        }
        else
        {
            if (debugListInt.Count > indexInList && indexInList != -1)
            {
                debugListInt.Insert(indexInList, debugVariable);
            }
            else
            {
                debugListInt.Add(debugVariable);
            }
        }
    }

    private static void InitializeDictionary()
    {
        if (debugListInt != null)
        {
            return;
        }

        debugListInt = new List<KeyValuePair<string, string>>();
    }

}
