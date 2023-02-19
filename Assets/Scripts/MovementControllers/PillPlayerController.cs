using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PillPlayerController : MonoBehaviour
{
    public Planet attractor;
    public Camera firstPersonCamera;
    public float movementSpeed;
    [SerializeField] private float maxSpeed = 0;

    private Rigidbody body;
    private int collisionCount = 0;
    private bool grounded = false;
    [SerializeField] private float groundedSlowDownFactor = 0.9f;
    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        //A bit of a hack to give the player a starting planet
        attractor = GameObject.Find("Sun").transform.parent.transform.GetChild(1).GetComponent<Planet>();
        Vector3 directionNearestPlanet = attractor.transform.position - transform.position;
        Physics.Raycast(transform.position, directionNearestPlanet, out RaycastHit hit);
        transform.SetParent(attractor.transform);

        //Put the player above the ground
        transform.position = hit.point - (directionNearestPlanet.normalized) * 5;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        Gravity.KeepUpright(transform, attractor.transform);
        Gravity.Attract(transform.position, body, attractor.transform.position, attractor.mass);
    }

    private void HandleInput()
    {
        Vector3 movementVector = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Jump"), Input.GetAxisRaw("Vertical"));
        body.velocity += transform.rotation * movementVector * Time.deltaTime * movementSpeed;
        if (body.velocity.magnitude > maxSpeed)
        {
            body.velocity = body.velocity.normalized * maxSpeed;
        }

        if (grounded && movementVector == Vector3.zero)
        {
            body.velocity *= groundedSlowDownFactor;
        }

        Vector3 cameraRotationVector = new Vector3(Input.GetAxis("Mouse Y") * -1, 0);
        Vector3 playerRotationVector = new Vector3(0, Input.GetAxis("Mouse X"));
        firstPersonCamera.transform.Rotate(cameraRotationVector);
        transform.Rotate(playerRotationVector);
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisionCount++;
        grounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        collisionCount--;
        grounded = collisionCount != 0;
    }
}
