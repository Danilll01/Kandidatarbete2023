using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;




public class SpawnStufOnSphere : MonoBehaviour
{ 
    [SerializeField] private float radius;
    [SerializeField] private float planetRadius;
    [SerializeField] private float rayLenght;
    [SerializeField] private float spawningAngleLimit;
    [SerializeField] private int stoneLimit;
    [SerializeField] private int bushLimit;
    [SerializeField] private int treeLimit;
    [SerializeField] private string seed;
    [SerializeField] private float treeLine;

    [SerializeField] private Object[] trees = new Object[4];
    [SerializeField] private Object[] bushes = new Object[2];
    [SerializeField] private Object[] grass = new Object[2];
    [SerializeField] private Object[] stones = new Object[2];

    // Start is called before the first frame update
    void Start()
    {
        plantStones();
        plantBushes();
        plantTrees();
    }

    void plantTrees()
    {
        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        Vector3 position;
        RaycastHit hit;

        for (int i = 0; i < treeLimit; i++)
        {
            position = Random.onUnitSphere * radius;
            if (Physics.Raycast(position, -position, out hit, rayLenght))
            {
                if (Mathf.Abs(Vector3.Angle(position, hit.normal)) > 30 ||
                    hit.distance < (radius - planetRadius) - treeLine)
                {
                    i--;
                    continue;
                }
                position = hit.point;
            }
            else continue;

            Quaternion rotation = Quaternion.LookRotation(position) * Quaternion.Euler(90, 0, 0);
            int use;
            if(hit.distance < (radius - planetRadius) - treeLine / 3) use = (Random.value > 0.5f) ? 0 : 1;
            else use = (Random.value > 0.5f) ? 2 : 3;
            Object newTree = Instantiate(trees[use], position, rotation);
            newTree.GetComponent<Transform>().parent = this.transform;
        }
    }

    void plantBushes()
    {
        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        Vector3 position;
        RaycastHit hit;

        for (int i = 0; i < bushLimit; i++)
        {
            position = Random.onUnitSphere * radius;
            if (Physics.Raycast(position, -position, out hit, rayLenght))
            {
                if (Mathf.Abs(Vector3.Angle(position, hit.normal)) > spawningAngleLimit + 10 ||
                    hit.distance < (radius - planetRadius) - treeLine - 20)
                {
                    i--;
                    continue;
                }
                position = hit.point;
            }
            else continue;

            Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
            int use = (Random.value > 0.5f) ? 1 : 0;
            Object newBush = Instantiate(bushes[use], position, rotation);
            newBush.GetComponent<Transform>().parent = this.transform;
        }
    }

    //Manfred ville ha denna
    void plantStones()
    {
        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        Vector3 position;
        RaycastHit hit;

        for (int i = 0; i < stoneLimit; i++)
        {
            position = Random.onUnitSphere * radius;
            if (Physics.Raycast(position, -position, out hit, rayLenght))
            {
                if (Mathf.Abs(Vector3.Angle(position, hit.normal)) < spawningAngleLimit)
                {
                    i--;
                    continue;
                }
                position = hit.point;
            }
            else continue;

            Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
            int use = (Random.value > 0.5f) ? 1 : 0;
            Object newBush = Instantiate(stones[use], position, rotation);
            newBush.GetComponent<Transform>().parent = this.transform;
        }
    }
}