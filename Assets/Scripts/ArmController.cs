using UnityEngine;

public class ArmController : MonoBehaviour
{
    public JointController Elbow;
    public JointController Wrist;
    public JointController ClawBase;

    public JointController Finger1;
    public JointController Finger2;

    void Update()
    {
        float elbowInput = 0;
        float wristInput = 0;
        float clawInput = 0;
        float fingerInput = 0;

        //--------------------------------
        // Локоть
        //--------------------------------

        if (Input.GetKey(KeyCode.Alpha1))
            elbowInput = 1;

        if (Input.GetKey(KeyCode.Z))
            elbowInput = -1;

        //--------------------------------
        // Кисть
        //--------------------------------

        if (Input.GetKey(KeyCode.Alpha2))
            wristInput = 1;

        if (Input.GetKey(KeyCode.X))
            wristInput = -1;

        //--------------------------------
        // Основание клешни
        //--------------------------------

        if (Input.GetKey(KeyCode.Alpha3))
            clawInput = 1;

        if (Input.GetKey(KeyCode.C))
            clawInput = -1;

        //--------------------------------
        // Пальцы
        //--------------------------------

        if (Input.GetKey(KeyCode.Alpha4))
            fingerInput = 1;

        if (Input.GetKey(KeyCode.V))
            fingerInput = -1;

        //--------------------------------

        Elbow.SetInput(elbowInput);

        Wrist.SetInput(wristInput);

        ClawBase.SetInput(clawInput);

        Finger1.SetInput(fingerInput);

        Finger2.SetInput(-fingerInput);
    }
}
