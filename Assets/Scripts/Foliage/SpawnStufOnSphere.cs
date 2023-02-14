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
    [SerializeField] private int spawningLimit;
    [SerializeField] private string seed;
    [SerializeField] private float treeLine;

    [SerializeField] private Object prefab;
    [SerializeField] private Object prefab1;
    [SerializeField] private Object prefab2;
    // Start is called before the first frame update
    void Start()
    {
        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        Vector3 position;
        RaycastHit hit;

        Perlin.SetSeed(seed);
		for(int i = 0; i < spawningLimit; i++)
        {

            position = Random.onUnitSphere * radius;

            if (Physics.Raycast(position, -position, out hit, rayLenght))
            {
                if (Mathf.Abs(Vector3.Angle(position, hit.normal)) > spawningAngleLimit ||
                    hit.distance < (radius - planetRadius) - treeLine)
                {
                    i--;
                    continue;
                }
                position = hit.point;
            }
            else continue;


            Quaternion rotation = Quaternion.LookRotation(position) * Quaternion.Euler(90, 0, 0);
            float noise = Perlin.Noise(position / 5);
            Object use;

            if (noise > 0.1)
            {
                use = prefab;
            }
            else if(noise < -0.1)
            {
                use = prefab1;
            }
            else
            {
                use = prefab2;
            }

            Object newTree = Instantiate(use, position, rotation);
            newTree.GetComponent<Transform>().parent = this.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
