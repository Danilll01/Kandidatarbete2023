using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity
{
    /// <summary>
    /// Forces entity to be upright by putting their positive y-axis facing away from centerOfGravity's position.
    /// </summary>
    public static void KeepUpright(Transform entity, Transform centerOfGravity)
    {
        Vector3 directionFromCenter = entity.position - centerOfGravity.transform.position;
        directionFromCenter = directionFromCenter.normalized;
        entity.rotation = Quaternion.FromToRotation(entity.up, directionFromCenter) * entity.rotation;
    }

    /// <summary>
    /// Returns the upright Vector rotation in global space.
    /// </summary>
    public static Quaternion UprightRotation(Transform entity, Transform centerOfGravity)
    {
        Vector3 directionFromCenter = entity.position - centerOfGravity.transform.position;
        directionFromCenter = directionFromCenter.normalized;
        return Quaternion.FromToRotation(entity.up, directionFromCenter) * entity.rotation;
    }

    /// <summary>
    /// Attracts an entity towards a body. The attracting body is not affected.
    /// </summary>
    public static void Attract(Vector3 entityPos, Rigidbody entityRigidbody, Vector3 attractingBodyPos, float attractingBodyMass)
    {
        // These calculation assume the player has a weight of 1
        float r2 = Vector3.SqrMagnitude(entityPos - attractingBodyPos);

        Vector3 attractionDirection = (attractingBodyPos - entityPos).normalized;
        float gravity = Mathf.Max(13, (Universe.gravitationalConstant * attractingBodyMass) / r2);

        entityRigidbody.velocity += attractionDirection * (gravity * Time.deltaTime);
    }
}
