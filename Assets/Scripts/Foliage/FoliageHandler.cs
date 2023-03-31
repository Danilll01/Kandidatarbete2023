using UnityEngine;
using Random = System.Random;
using Noise;

public class FoliageHandler : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField]
    public bool debug = true;
    [SerializeField]
    public GameObject debugObject;

    [Header("GameObjects")]
    [SerializeField] public GameObject fallenTree;
    [SerializeField] private GameObject[] trees;
    private int treeArrSize;
    [SerializeField] private GameObject[] bushes;
    private int bushArrSize;
    [SerializeField] private GameObject[] waterBois;
    private int waterArrSize;
    [SerializeField] private GameObject[] stones;
    private int stoneArrSize;
    [SerializeField] private GameObject[] foragables;
    private int foragablesArrSize;
    

    // Noise for forests, change these if you know what you are doing
    // Check the Noise Tester shader graph for more insight (Forests are the black areas)
    private const float NOISE_ONE = 2f;
    private const float NOISE_TWO = 4f;

    // How many extra hits every chunks has
    public readonly int MISS_COMPLIMENT = 200;

    public bool isInstantiated = false;
    private Planet planet;
    private float waterRadius;
    private float density;
    // This random needs to be fixed
    private Random random = new (Universe.seed);
    private Vector3[] forestTypes;

    // Stats for this planet
    [HideInInspector]
    public int treeNr = 0;
    [HideInInspector]
    public int bushNr = 0;
    [HideInInspector]
    public int waterPlantNr = 0;
    [HideInInspector]
    public int stoneNr = 0;
    [HideInInspector]
    public int foragableNr = 0;

    /// <summary>
    /// Initializes the foliageHandler
    /// </summary>
    /// <param name="planet"></param>
    public void Initialize(Planet planet)
    {
        this.planet = planet;
        waterRadius = Mathf.Abs(planet.waterDiameter / 2);
        density = planet.radius * 0.000002f; // Magic numbers * "random"
        InitArrays();
        InitForestTypes();

        isInstantiated = true;
    }

    // Updates the debug menu
    public void UpdateDebug()
    {
        DisplayDebug.AddOrSetDebugVariable("Trees", treeNr);
        DisplayDebug.AddOrSetDebugVariable("Bushes", bushNr);
        DisplayDebug.AddOrSetDebugVariable("Water plants", waterPlantNr);
        DisplayDebug.AddOrSetDebugVariable("Stones", stoneNr);
        DisplayDebug.AddOrSetDebugVariable("Foragables", foragableNr);
    }

    // Inits arrays
    private void InitArrays()
    {
        // Init array lengths
        treeArrSize = trees.Length;
        bushArrSize = bushes.Length;
        waterArrSize = waterBois.Length;
        stoneArrSize = stones.Length;
        foragablesArrSize = foragables.Length;
    }

    // Sets up the all forests on a planet
    private void InitForestTypes()
    {
        // Number of types of forests
        forestTypes = new Vector3[2];

        // X value decides which kind of forest that should be planted
        for (int i = 0; i < forestTypes.Length; i++)
        {
            // Random.Next(How many tree types), Random.Next(Noise ofset), Random.Next(Noise ofset)
            // X value determines the type of tree that should be there
            forestTypes[i] = new Vector3(random.Next(treeArrSize), random.Next(200), random.Next(200));
        }
    }

    // Returns a tree from a given index
    public GameObject GetForstetTree(int treeType)
    {
        return trees[treeType];
    }

    // Returns a random tree GameObject
    public GameObject GetTreeType()
    {
        return trees[random.Next(treeArrSize)];
    }

    // Returns a random tree GameObject
    public GameObject GetBushType()
    {
        return bushes[random.Next(bushArrSize)];
    }

    // Returns a random stone GameObject
    public GameObject GetStoneType()
    {
        return stones[random.Next(stoneArrSize)];
    }
    
    // Returns a random foragable GameObject
    public GameObject GetForagableType()
    {
        return foragables[random.Next(foragablesArrSize)];
    }
    
    // Returns a random water plant GameObject
    public GameObject GetWaterPlantType()
    {
        return waterBois[random.Next(waterArrSize)];
    }

    // Returns a what kind of tree that should be in that forest spot
    public int CheckForestSpawn(Vector3 pos)
    {
        int len = forestTypes.Length;
        for (int i = 0; i < len; i++)
            if (ForestNoiseFunction(pos, i) == 0)
                return (int)forestTypes[i].x;
        return 0;
    }

    // Noise function for forests
    private int ForestNoiseFunction(Vector3 pos, int index)
    {
        // Layering noise :)
        return (int)Mathf.Round(
            (Simplex.Evaluate((pos + forestTypes[index]) * NOISE_ONE) + 1) * 
            (Simplex.Evaluate((pos + forestTypes[index] )* NOISE_TWO) + 1));
    }

    public Vector3 PlanetPosition
    {
        get { return planet.transform.position; }
    }

    public float Density
    {
        get { return density; }
    }

    public float WaterRadius
    {
        get { return waterRadius; }
    }
    
    public float PlanetRadius
    {
        get { return planet.radius; }
    }

    public bool IsPlanet
    {
        get { return !planet.bodyName.Contains("Moon"); }
    }

}
