using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUIInit : MonoBehaviour
{
    // Start is called before the first frame update
    private void Awake()
    {
        Universe.GPUI = transform.GetComponent<GPUInstancer.GPUInstancerPrefabManager>();
    }
}
