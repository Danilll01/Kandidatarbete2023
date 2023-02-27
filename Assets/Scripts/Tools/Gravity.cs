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
        double r2 = Vector3.Distance(entityPos, attractingBodyPos);
        r2 *= r2;

        Vector3 attractionDirection = (attractingBodyPos - entityPos).normalized;

        entityRigidbody.velocity += attractionDirection * (float)((attractingBodyMass * Time.deltaTime) / r2);
    }
}
