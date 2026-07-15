using UnityEngine;

[RequireComponent(typeof(RobotDrive))]
public class KeyboardDrive : MonoBehaviour
{
    RobotDrive drive;

    void Awake()
    {
        drive = GetComponent<RobotDrive>();
    }

    void FixedUpdate()
    {
        float gas = Input.GetAxisRaw("Vertical");
        float steer = Input.GetAxisRaw("Horizontal");

        drive.Drive(gas, steer);
    }
}
