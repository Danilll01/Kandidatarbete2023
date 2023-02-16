using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{

    public void StartGame()
    {
        SceneManager.LoadScene("Main");
    }
}
