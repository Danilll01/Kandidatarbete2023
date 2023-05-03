using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour
{
    [SerializeField] private float respawnTime = 30f;

    private Vector3 originalScale;
    private new Collider collider;

    // Start is called before the first frame update
    void Start()
    {
        originalScale = transform.localScale;
        collider = GetComponent<Collider>();
    }

    /// <summary>
    /// Starts a respawn timer for the resource
    /// </summary>
    public void ConsumeResource()
    {
        if (collider == null) return;
        
        collider.enabled = false;
        StopAllCoroutines();
        StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        // Lerp the scale of the plant to make it "grow"
        for (float timePassed = 0f; timePassed < respawnTime; timePassed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, timePassed / respawnTime);

            yield return null;
        }
        
        collider.enabled = true;
    }
}
