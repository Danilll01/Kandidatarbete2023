using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterOfGravity
{
    // Start is called before the first frame update
    public static void KeepUpright(Transform entity, Transform centerOfGravity)
    {
        Vector3 directionFromCenter = entity.position - centerOfGravity.transform.position;
        directionFromCenter = directionFromCenter.normalized;
        entity.rotation = Quaternion.FromToRotation(entity.up, directionFromCenter) * entity.rotation;
    }

    public static void Attract(Transform entity, Rigidbody entityBody, Transform centerOfGravity, float centerOfGravityMass)
    {
        double r2 = Vector3.Distance(entity.position, centerOfGravity.position);
        r2 *= r2;

        entityBody.velocity += entity.up * -1 * (float)((Universe.gravitationalConstant * centerOfGravityMass) / r2);
    }
}
