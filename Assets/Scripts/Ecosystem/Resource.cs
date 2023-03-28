using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour
{
    [SerializeField] private float respawnTime = 30f;

    private MeshRenderer meshRenderer;
    private Collider colliderComponent;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        colliderComponent = GetComponent<Collider>();
    }

    /// <summary>
    /// Starts a respawn timer for the resource
    /// </summary>
    public void ConsumeResource()
    {
        if (meshRenderer == null || colliderComponent == null) return;
        
        meshRenderer.enabled = false;
        colliderComponent.enabled = false;
        StopAllCoroutines();
        StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        // Start a timer that goes from respawn time to 0
        yield return new WaitForSeconds(respawnTime);

        meshRenderer.enabled = true;
        colliderComponent.enabled = true;
    }
}
