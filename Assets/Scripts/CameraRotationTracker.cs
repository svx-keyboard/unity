using UnityEngine;

public class CameraRotationTracker : MonoBehaviour
{
    // Текущий угол поворота камеры по оси Y
    public float Yaw { get; private set; }

    void Update()
    {
        Yaw = NormalizeAngle(transform.localEulerAngles.y);
    }

    /// <summary>
    /// Возвращает угол поворота камеры по оси Y в диапазоне [-180; 180]
    /// </summary>
    public float GetYaw()
    {
        return Yaw;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}