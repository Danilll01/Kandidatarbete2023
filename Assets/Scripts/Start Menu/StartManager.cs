using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField seedInput;
    [SerializeField] private TextMeshProUGUI nrOfPlanetsText;


    /// <summary>
    /// Update the text for the planet slider corresponding to value of slider
    /// </summary>
    /// <param name="slider"></param>
    public void UpdatePlanetInputValue(Slider slider)
    {
        nrOfPlanetsText.text = slider.value.ToString();
    }

    /// <summary>
    /// Function to start the game
    /// </summary>
    public void StartGame()
    {
        int tryParseSeed = 0;
        int tryParsePlanets = 0;

        // These will try and parse the text inputs to ints, will return 0 if it cant
        int.TryParse(seedInput.text, out tryParseSeed);
        int.TryParse(nrOfPlanetsText.text, out tryParsePlanets);

        // If the seedInput is empty or if it can't be cast to int, randomize it
        if (string.IsNullOrEmpty(seedInput.text) || tryParseSeed == 0)
        {
            seedInput.text = Random.Range(0, 1000000).ToString();
        }

        // If there was no planets input on the slider, randomize it
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

        // Set the seed and load the game
        Universe.InitializeRandomWithSeed();
        SceneManager.LoadScene("Main");
    }
}
