using System.Collections.Generic;
using UnityEngine;

public class JointController : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation")]
    public Axis rotationAxis = Axis.Z;

    [Header("Limits")]
    public float minAngle = -90;
    public float maxAngle = 90;

    [Header("Speed")]
    public float speed = 90;

    [Header("Collision")]

    // Все коллайдеры этого звена
    public Collider[] ownColliders;

    // С какими слоями запрещено пересекаться
    public LayerMask forbiddenLayers;

    // Коллайдеры другого пальца
    public Collider[] forbiddenColliders;

    float currentAngle;

    void Start()
    {
        Vector3 e = transform.localEulerAngles;

        switch (rotationAxis)
        {
            case Axis.X:
                currentAngle = Normalize(e.x);
                break;

            case Axis.Y:
                currentAngle = Normalize(e.y);
                break;

            case Axis.Z:
                currentAngle = Normalize(e.z);
                break;
        }
    }

    //-------------------------------------------------

    public void SetInput(float value)
    {
        if (Mathf.Abs(value) < 0.001f)
            return;

        float oldAngle = currentAngle;

        currentAngle += value * speed * Time.deltaTime;
        currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

        ApplyRotation(currentAngle);

        if (HasCollision())
        {
            currentAngle = oldAngle;
            ApplyRotation(currentAngle);
        }
    }

    //-------------------------------------------------

    void ApplyRotation(float angle)
    {
        Vector3 e = transform.localEulerAngles;

        switch (rotationAxis)
        {
            case Axis.X:
                e.x = angle;
                break;

            case Axis.Y:
                e.y = angle;
                break;

            case Axis.Z:
                e.z = angle;
                break;
        }

        transform.localEulerAngles = e;
    }

    //-------------------------------------------------

    bool HasCollision()
    {
        foreach (Collider own in ownColliders)
        {
            if (own == null)
                continue;

            //------------------------------
            // Проверка против пальцев
            //------------------------------

            foreach (Collider other in forbiddenColliders)
            {
                if (other == null)
                    continue;

                Vector3 dir;
                float dist;

                if (Physics.ComputePenetration(
                    own, own.transform.position, own.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out dir,
                    out dist))
                {
                    return true;
                }
            }

            //------------------------------
            // Проверка против мира
            //------------------------------

            Collider[] hits =
                Physics.OverlapBox(
                    own.bounds.center,
                    own.bounds.extents,
                    own.transform.rotation,
                    forbiddenLayers);

            foreach (Collider hit in hits)
            {
                if (hit.transform.IsChildOf(transform.root))
                    continue;

                Vector3 dir;
                float dist;

                if (Physics.ComputePenetration(
                    own, own.transform.position, own.transform.rotation,
                    hit, hit.transform.position, hit.transform.rotation,
                    out dir,
                    out dist))
                {
                    return true;
                }
            }
        }

        return false;
    }

    //-------------------------------------------------

    float Normalize(float a)
    {
        while (a > 180)
            a -= 360;

        while (a < -180)
            a += 360;

        return a;
    }
}