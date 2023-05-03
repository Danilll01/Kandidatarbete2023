using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancer
{
    public class GPUIGrass : MonoBehaviour
    {
        private GPUInstancerPrefabManager prefabManager;
        public GPUInstancerPrefab grassPrefab;
        public Transform parent;
        private int instantiatedCount;
        [HideInInspector]
        public bool initialized = false;
        private bool done = false;
        private float waterRadius;

        private Vector3[] positions;
        private int totalPositions;

        private List<GPUInstancerPrefab> grassInstances = new List<GPUInstancerPrefab>();

        private void Update()
        {
            if(initialized && !done)
            {
                StaggeredSpawning();
            }

        }

        public void Initialize(Vector3[] positions, Planet planet)
        {
            prefabManager = Universe.GPUI;
            instantiatedCount = 0;

            waterRadius = Mathf.Abs(planet.waterDiameter / 2);

            this.positions = positions;
            totalPositions = positions.Length;

            initialized = true;
        }

        private void StaggeredSpawning()
        {

            grassInstances.Clear();

            for (int i = 0; i < totalPositions; i++)
            {
                if(positions[i].magnitude > waterRadius && positions[i].magnitude < waterRadius + 50)
                {
                    grassInstances.Add(Instantiate(grassPrefab, positions[i], Quaternion.LookRotation(positions[i]) * Quaternion.Euler(90, 0, 0), parent));
                }
                instantiatedCount++;
                
                if(instantiatedCount >= totalPositions-1)
                {
                    RegisterInstances();
                    Debug.Log("Instantiated " + instantiatedCount + " objects.");
                    done = true;
                    positions = null;
                    break;
                }
            }
        }


        private void RegisterInstances()
        {
            if (prefabManager != null && prefabManager.gameObject.activeSelf && prefabManager.enabled)
            {
                GPUInstancerAPI.RegisterPrefabInstanceList(prefabManager, grassInstances);
                GPUInstancerAPI.InitializeGPUInstancer(prefabManager);
            }
        }
    }
}