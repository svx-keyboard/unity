using UnityEngine;

public class RobotPositionTracker : MonoBehaviour
{
    // Позиция в начале сцены
    private Vector3 startPosition;
    public float RelativeX { get; private set; }
    public float RelativeZ { get; private set; }

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        RelativeX = transform.position.x - startPosition.x;
        RelativeZ = transform.position.z - startPosition.z;
    }

    //сразу оба значения
    public Vector2 GetRelativePosition()
    {
        return new Vector2(RelativeX, RelativeZ);
    }
}