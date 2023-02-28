using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunksHandler : MonoBehaviour
{
    private Planet planet;
    private Transform player;
    private Vector3 playerLastPosition;
    private bool initialized = false;
    private List<Vector3> chunkPositions;

    /// <summary>
    /// Initialize the values
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="player"></param>
    public void Initialize(Planet planet, Transform player)
    {
        this.planet = planet;
        this.player = player;
        playerLastPosition = Vector3.zero;
        chunkPositions = new List<Vector3>();
        InitializeChunkPositions();
        UpdateChunksVisibility();
        initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Only update the chunks if the player is close to the planet
        if (initialized && (player.position - planet.transform.position).magnitude < 3000)
        {
            UpdateChunksVisibility();
        }
    }

    private void InitializeChunkPositions()
    {
        for (int i = 0; i < planet.chunks.Count; i++)
        {
            chunkPositions.Add(planet.chunks[i].transform.GetComponent<MeshRenderer>().bounds.center);
        }
    }

    private void UpdateChunksVisibility()
    {
        Vector3 playerPos = player.position;

        // Only update chunks if player has moved a certain distance
        if (Mathf.Abs(Vector3.Distance(playerPos, playerLastPosition)) < 50 || !initialized)
        {
            return;
        }

        playerLastPosition = playerPos;
        Vector3 planetCenter = Vector3.zero;
        Vector3 playerToPlanetCenter = playerPos - planetCenter;
        Vector3 cutoffPoint = new Vector3(playerToPlanetCenter.x / 1.5f, playerToPlanetCenter.y / 1.5f, playerToPlanetCenter.z / 1.5f);

        for (int i = 0; i < chunkPositions.Count; i++)
        {
            bool isBelowHalfWayPoint = CheckIfPointBIsBelowPointA(cutoffPoint, chunkPositions[i], cutoffPoint.normalized);
            if (isBelowHalfWayPoint)
            {
                planet.chunks[i].gameObject.SetActive(false);
            }
            else
            {
                planet.chunks[i].gameObject.SetActive(true);
            }
        }
    }

    private bool CheckIfPointBIsBelowPointA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0) ? true : false;
    }
}
