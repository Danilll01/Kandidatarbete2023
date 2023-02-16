using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartManager : MonoBehaviour
{
    public TMP_InputField seedInput;
    public TextMeshProUGUI nrOfPlanetsText;

    public void UpdatePlanetInputValue(Slider slider)
    {
        nrOfPlanetsText.text = slider.value.ToString();
    }

    public void StartGame()
    {
        int tryParse = 0;
        int.TryParse(seedInput.text, out tryParse);
        if (string.IsNullOrEmpty(seedInput.text) || tryParse == 0)
        {
            seedInput.text = Random.Range(0, 1000000).ToString();
        }

        Universe.seed = int.Parse(seedInput.text);
        Universe.nrOfPlanets = int.Parse(nrOfPlanetsText.text);
        Universe.InitializeSeed();
        SceneManager.LoadScene("Main");
    }
}
