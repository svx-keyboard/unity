using UnityEngine;

public class RobotSensors : MonoBehaviour
{
    [Header("Ultrasonic")]
    public Transform soundDetector;

    public float ultrasonicRange = 2.0f;
    public int ultrasonicRays = 5;
    public float ultrasonicAngle = 30f;

    [Header("IR Sensors")]
    public Transform leftIRPoint;
    public Transform rightIRPoint;
    public Transform centerPoint;

    public float irRange = 2f;  // TODO: Fix ranges & distances issue

    [Header("Debug")]
    public float debugOutput = 0f;


    public float UltrasonicValue { get; private set; }

    public float LeftIR { get; private set; }
    public float RightIR { get; private set; }
    public float CenterIR { get; private set; }

    void FixedUpdate()
    {
        UpdateUltrasonic();

        LeftIR = CheckIR(leftIRPoint);

        RightIR = CheckIR(rightIRPoint);

        CenterIR = CheckIR(centerPoint);


        if(debugOutput > 0.5f)
        {
            Debug.Log(
                $"US={UltrasonicValue:F2} | " +
                $"Left={LeftIR} | " +
                $"Center={CenterIR} | " +
                $"Right={RightIR}");
        }
    }


    void UpdateUltrasonic()
    {
        float minDistance = ultrasonicRange;


        for(int i = 0; i < ultrasonicRays; i++)
        {
            float t = (float)i / (ultrasonicRays - 1);


            float angle = Mathf.Lerp(
                    -ultrasonicAngle / 2,
                     ultrasonicAngle / 2,
                     t);


            Vector3 direction = Quaternion.Euler(
                    0,
                    angle,
                    0)
                *
                soundDetector.right;


            Ray ray = new Ray(
                    soundDetector.position,
                    direction);


            if(Physics.Raycast(
                ray,
                out RaycastHit hit,
                ultrasonicRange))
            {
                minDistance =
                    Mathf.Min(
                        minDistance,
                        hit.distance);
            }


            Debug.DrawRay(
                ray.origin,
                direction * ultrasonicRange,
                Color.yellow);
        }


        // 0 = рядом
        // 1 = свободно

        UltrasonicValue = Mathf.Clamp01(
                minDistance /
                ultrasonicRange);
    }


    float CheckIR(Transform sensor)
    {
        if(sensor == null)
            return 0;


        bool hit = Physics.Raycast(
                sensor.position,
                sensor.right,
                irRange);


        Debug.DrawRay(
            sensor.position,
            sensor.right * irRange,
            hit ? Color.red : Color.green);


        return hit ? 1f : 0f;
    }
}
