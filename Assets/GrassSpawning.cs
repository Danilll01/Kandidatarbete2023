using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtendedRandom;

namespace GPUInstancer
{
    public class GrassSpawning : MonoBehaviour
    {
        private List<GPUInstancerPrefab> grassInstances = new List<GPUInstancerPrefab>();
        public GPUInstancerPrefabManager prefabManager;
        public List<GPUInstancerPrefab> grassPrefabs = new();


        public Transform ship;
        private Vector3 lastUpdate = Vector3.zero;
        private RandomX random = new RandomX();

        private GameObject grass;

        void Update()
        {
            Vector3 pos = transform.position;

            if ((pos - lastUpdate).magnitude > 20)
            {
                Ray ray = new Ray(pos, -pos);
                Physics.Raycast(ray, out RaycastHit hit);

                if (hit.transform != ship && hit.distance < 20)
                {
                    Debug.Log(hit.transform);



                    RemoveInstances();
                    if(grass != null)
                        Destroy(grass);
                    grass = new GameObject("Grass");
                    grass.transform.parent = Universe.player.attractor.transform;
                    grassInstances.Clear();

                    for (int i = 0; i < 1000; i++)
                    {
                        float x = (float)random.Value() * 2 - 1;
                        float y = (float)random.Value() * 2 - 1;
                        float z = (float)random.Value() * 2 - 1;
                        Vector3 localpos = Quaternion.Euler(x, y, z) * (pos * 1.5f);

                        ray = new Ray(localpos, -localpos);
                        if(Physics.Raycast(ray, out RaycastHit hit2))
                            grassInstances.Add(Instantiate(grassPrefabs[random.Next(0, grassPrefabs.Count)], hit2.point, 
                                Quaternion.LookRotation(hit2.point) * Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, random.Next(0, 360), 0),
                                grass.transform));
                        
                    }
                    RegisterInstances();
                }
                lastUpdate = transform.position;
            }
        }

        private void RegisterInstances()
        {
            if (prefabManager != null && prefabManager.gameObject.activeSelf && prefabManager.enabled)
            {
                GPUInstancerAPI.RegisterPrefabInstanceList(prefabManager, grassInstances);
            }
        }

        private void RemoveInstances()
        {
            if (prefabManager != null && prefabManager.gameObject.activeSelf && prefabManager.enabled)
            {
                GPUInstancerAPI.ClearRegisteredPrefabInstances(prefabManager);
            }
        }


    }
}