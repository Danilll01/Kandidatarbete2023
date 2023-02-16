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
}
