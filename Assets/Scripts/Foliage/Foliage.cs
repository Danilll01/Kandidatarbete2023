using UnityEngine;
using Random = System.Random;

public class Foliage : MonoBehaviour
{

    [SerializeField] private bool debug = true;
    [SerializeField] private GameObject tree;
    [SerializeField] private GameObject bush;
    [SerializeField] private GameObject waterBoi;
    [SerializeField] private GameObject stone;
    
    private Vector3[] spots = null;

    private FoliageHandler foliageHandler;
    private Random random;

    [SerializeField] private float frequency = 0.1f;

    public void Initialize(Vector3[] meshVertices, Vector3 pos)
    {
        // Seedar en random för denna chunken
        random = new Random(Universe.seed + (int)pos.x + (int)pos.y + (int)pos.z);
        foliageHandler = transform.parent.parent.parent.GetComponent<Planet>().foliageHandler;

        if (foliageHandler == null) return;

        // Checks if the chunk is on the surface
        Vector3 rayOrigin = pos.normalized * foliageHandler.PlanetRadius + foliageHandler.PlanetPosition;
        Ray ray = new (rayOrigin, foliageHandler.PlanetPosition - rayOrigin);
        Physics.Raycast(ray, out RaycastHit hit);

        if(hit.transform == transform.parent && foliageHandler.IsPlanet)
        {
            InitFoliage(meshVertices);
        }
    }

    private void InitFoliage(Vector3[] meshVertices)
    {
        int max = meshVertices.Length;

        spots = new Vector3[Mathf.Min(foliageHandler.Density, meshVertices.Length)];

        float radius = foliageHandler.PlanetRadius;

        for (int i = 0; i < spots.Length; i++)
        {
            spots[i] = (meshVertices[random.Next(0, max)] + new Vector3(random.Next(-5, 5), random.Next(-5, 5), random.Next(5, 5))).normalized * radius;
        }
    }

    public void SpawnFoliageOnChunk()
    {
        if (spots == null) return;

        Ray ray;
        RaycastHit hit;
        Vector3 planetPos = foliageHandler.PlanetPosition;
        float radius = foliageHandler.PlanetRadius;
        float waterRadius = foliageHandler.WaterRadius;

        foreach(Vector3 spot in spots)
        {
            Vector3 rayOrigin = spot + planetPos;
            ray = new Ray(rayOrigin, planetPos - rayOrigin);
            Physics.Raycast(ray, out hit);
            if (debug) Debug.DrawLine(rayOrigin, hit.point, Color.green, 10f);

            if (hit.transform == transform.parent)
            {
                if (hit.distance < radius - waterRadius) // On land
                {
                    SpawnOnLand(hit, rayOrigin, planetPos);
                }
                else // In water
                {
                    SpawnInWater(hit, rayOrigin, hit.distance - radius - waterRadius);
                }
            }
        }
        spots = null;
    }

    private void SpawnOnLand(RaycastHit hit, Vector3 rayOrigin, Vector3 planetPosition)
    {
        if(Mathf.Abs(Vector3.Angle(rayOrigin - planetPosition, hit.normal)) > 30)
        {
            AboveAngle(hit, rayOrigin);
        }
        else
        {
            BelowAngle(hit, rayOrigin);
        }
    }

    private void AboveAngle(RaycastHit hit, Vector3 rayOrigin)
    {
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        Instantiate(stone, hit.point, rotation, transform);
    }

    private void BelowAngle(RaycastHit hit, Vector3 rayOrigin)
    {
        if (Perlin.Noise(hit.point * frequency) < 0)
        {
            Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
            rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
            Instantiate(tree, hit.point, rotation, transform);
        }
        else
        {
            Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
            rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
            Instantiate(bush, hit.point, rotation, transform);
        }
    }


    private void SpawnInWater(RaycastHit hit, Vector3 rayOrigin, float depth)
    {
        if(depth < 6)
        {
            Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
            rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
            GameObject waterObject = Instantiate(waterBoi, hit.point, rotation, transform);
            waterObject.transform.localScale = new Vector3(2, depth + 2, 2);
        }
    }
}
