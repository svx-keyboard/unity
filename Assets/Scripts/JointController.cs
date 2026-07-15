using System.Collections.Generic;
using UnityEngine;

public class JointController : MonoBehaviour
{
    private ArmCollisionChecker collisionChecker;
    public enum Axis {X, Y, Z}

    [Header("Rotation")]
    public Axis rotationAxis = Axis.Z;

    [Header("Limits")]
    public float minAngle = -90;
    public float maxAngle = 90;

    [Header("Speed")]
    public float speed = 90;

    [Header("Collision")]
    public Collider[] ownColliders;
    public LayerMask forbiddenLayers;
    public Collider[] forbiddenColliders;

    float currentAngle;

    void Start()
    {
        collisionChecker = GetComponentInParent<ArmCollisionChecker>();
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

    public void SetInput(float value)
    {
        if (Mathf.Abs(value) < 0.001f)
            return;

        float oldAngle = currentAngle;

        currentAngle += value * speed * Time.deltaTime;
        currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

        ApplyRotation(currentAngle);

        if (collisionChecker != null &&
    collisionChecker.HasCollision())
        {
            currentAngle = oldAngle;
            ApplyRotation(currentAngle);
        }
    }

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

    float Normalize(float a)
    {
        while (a > 180)
            a -= 360;

        while (a < -180)
            a += 360;

        return a;
    }
}
