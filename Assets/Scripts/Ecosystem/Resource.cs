using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour
{
    [SerializeField] private float respawnTime = 30f;

    private MeshRenderer meshRenderer;
    private new Collider collider;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        collider = GetComponent<Collider>();
    }

    /// <summary>
    /// Starts a respawn timer for the resource
    /// </summary>
    public void ConsumeResource()
    {
        if (meshRenderer == null || collider == null) return;
        
        meshRenderer.enabled = false;
        collider.enabled = false;
        StopAllCoroutines();
        StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        // Start a timer that goes from respawn time to 0
        yield return new WaitForSeconds(respawnTime);

        meshRenderer.enabled = true;
        collider.enabled = true;
    }
}
