using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartManager : MonoBehaviour
{
    public TextMeshProUGUI seedInput;
    public TextMeshProUGUI nrOfPlanetsText;

    public void UpdatePlanetInputValue(Slider slider)
    {
        nrOfPlanetsText.text = slider.value.ToString();
    }

    public void StartGame()
    {

        if (seedInput.text.Trim() == "")
        {
            seedInput.text = Random.Range(0,100000).ToString();
        }

        //Universe.seed = int.Parse(seedInput.text);
        Universe.nrOfPlanets = int.Parse(nrOfPlanetsText.text);
        SceneManager.LoadScene("Main");
    }
}