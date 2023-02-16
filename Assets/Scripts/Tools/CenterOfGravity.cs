using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterOfGravity
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="centerOfGravity"></param>
    public static void KeepUpright(Transform entity, Transform centerOfGravity)
    {
        Vector3 directionFromCenter = entity.position - centerOfGravity.transform.position;
        directionFromCenter = directionFromCenter.normalized;
        entity.rotation = Quaternion.FromToRotation(entity.up, directionFromCenter) * entity.rotation;
    }

    /// <summary>
    /// Attracts an entity towards a body. The attracting body is not affected.
    /// </summary>
    public static void Attract(Vector3 entityPos, Rigidbody entityRigidbody, Vector3 attractingBodyPos, float attractingBodyMass)
    {
        double r2 = Vector3.Distance(entityPos, attractingBodyPos);
        r2 *= r2;

        //THE DIVIDED BY TEN IS A HOTFIX TO KEEP GRAVITY DOWN
        Vector3 attractionDirection = (attractingBodyPos - entityPos).normalized / 10;

        entityRigidbody.velocity += attractionDirection * (float)((Universe.gravitationalConstant * attractingBodyMass * Time.deltaTime) / r2);
    }
}
