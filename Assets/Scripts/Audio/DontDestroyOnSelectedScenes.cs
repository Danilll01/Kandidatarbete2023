using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DontDestroyOnSelectedScenes : MonoBehaviour
{

    [SerializeField] private List<string> sceneNames;


    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckIfSceneInList();
    }

    // This will make sure that we don't have duplicate objects in the start scene
    // if the player goes back to it
    private void CheckIfSceneInList()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (sceneNames.Contains(currentScene))
        {
            // keep the object alive 
        }
        else
        {
            // unsubscribe to the scene load callback
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DestroyImmediate(this.gameObject);
        }
    }
}