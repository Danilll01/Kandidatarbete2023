using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;




public class SpawnStufOnSphere : MonoBehaviour
{
    [SerializeField] private Object prefab;
    [SerializeField] private Object prefab1;
    [SerializeField] private Object prefab2;
    // Start is called before the first frame update
    void Start()
    {

        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        Perlin.SetSeed("1kdjfghld");
		for(int i = 0; i < 1000; i++)
        {

            Vector3 pos = Random.onUnitSphere*150;

            RaycastHit hit;
            if (Physics.Raycast(pos, -pos, out hit, 100.0f))
            {
                pos = hit.point;
            }
            else
            {
                continue;
            }

            Quaternion rotation = Quaternion.LookRotation(pos) * Quaternion.Euler(90, 0, 0);
            float noise = Perlin.Noise(pos/5);
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

            Object newTree = Instantiate(use, pos, rotation);
            //newTree.GetComponent<Transform>().parent = this.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
