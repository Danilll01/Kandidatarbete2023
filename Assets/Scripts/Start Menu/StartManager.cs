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
        int tryParseSeed = 0;
        int tryParsePlanets = 0;

        int.TryParse(seedInput.text, out tryParseSeed);
        int.TryParse(nrOfPlanetsText.text, out tryParsePlanets);

        if (string.IsNullOrEmpty(seedInput.text) || tryParseSeed == 0)
        {
            seedInput.text = Random.Range(0, 1000000).ToString();
        }

        if (tryParsePlanets == 0)
        {
            int[] nrOfPlanetsArray = new int[] { 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5 };
            int randomValue = Random.Range(0, nrOfPlanetsArray.Length);
            int nrOfPlanets = nrOfPlanetsArray[randomValue];
            Universe.nrOfPlanets = nrOfPlanets;
        }
        else
        {
            Universe.nrOfPlanets = int.Parse(nrOfPlanetsText.text);
        }

        Universe.seed = int.Parse(seedInput.text);
        Universe.InitializeSeed();
        SceneManager.LoadScene("Main");
    }
}
