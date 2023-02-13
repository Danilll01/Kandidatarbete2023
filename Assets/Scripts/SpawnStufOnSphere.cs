using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;




public class SpawnStufOnSphere : MonoBehaviour
{
	public Object prefab;
	public Object prefab1;
	public Object prefab2;
    // Start is called before the first frame update
    void Start()
    {
        Perlin.SetSeed("1kdjfghld");
		for(int i = 0; i < 1000; i++)
        { 

            Vector3 pos = Random.onUnitSphere*100;
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
            newTree.GetComponent<Transform>().parent = this.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
