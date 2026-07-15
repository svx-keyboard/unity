using UnityEngine;

/// <summary>
/// Управление дифференциальным приводом.
/// На вход получает gas и steer в диапазоне [-1;1].
/// На выходе вычисляет скорости бортов, PWM и двигает Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RobotDrive : MonoBehaviour
{
    [Header("Base Motion")]
    public float moveSpeed = 0.57f;
    public float turnSpeed = 120f;
    public float turnK = 0.30f;

    [Header("Limits")]
    // public float maxLinearCmd = 0.25f;
    public float maxLinearCmd = 1f; // debug

    [Header("Motor Model")]
    public float motorDeadzone = 10f;
    public float minMotorPwm = 35f;
    public float maxPwmStep = 15f;

    [Header("Model")]
    public float modelForwardOffset = 90f;

    [Header("PWM Scale")]
    [Tooltip("Коэффициент перевода скорости (м/с) в PWM.")]
    public float speedToPwm = 200f;

    Rigidbody rb;

    float currentLeftPwm;
    float currentRightPwm;

    public float LeftTrackSpeed { get; private set; }
    public float RightTrackSpeed { get; private set; }

    public float LeftPWM { get; private set; }
    public float RightPWM { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Главная функция управления.
    /// gas и steer находятся в диапазоне [-1;1].
    /// </summary>
    public void Drive(float gas, float steer)
    {
        gas = Mathf.Clamp(gas, -1f, 1f);
        steer = Mathf.Clamp(steer, -1f, 1f);

        //--------------------------------------------------
        // 1. Линейная скорость
        //--------------------------------------------------

        float linearSpeed = gas * moveSpeed;
        linearSpeed = Mathf.Clamp(
            linearSpeed,
            -maxLinearCmd,
             maxLinearCmd);

        //--------------------------------------------------
        // 2. Поворот
        //--------------------------------------------------

        float turnComponent = steer * moveSpeed * turnK;

        //--------------------------------------------------
        // 3. Скорости гусениц
        //--------------------------------------------------

        LeftTrackSpeed = linearSpeed - turnComponent;
        RightTrackSpeed = linearSpeed + turnComponent;

        //--------------------------------------------------
        // 4. PWM
        //--------------------------------------------------

        float targetLeftPWM = SpeedToPWM(LeftTrackSpeed);
        float targetRightPWM = SpeedToPWM(RightTrackSpeed);

        currentLeftPwm = MoveTowardsPwm(currentLeftPwm, targetLeftPWM);
        currentRightPwm = MoveTowardsPwm(currentRightPwm, targetRightPWM);

        LeftPWM = currentLeftPwm;
        RightPWM = currentRightPwm;

        //--------------------------------------------------
        // 5. Движение Rigidbody
        //--------------------------------------------------

        float forwardSpeed = (LeftTrackSpeed + RightTrackSpeed) * 0.5f;

        float angularSpeed = steer * turnSpeed;

        Vector3 forward = Quaternion.Euler(0f, modelForwardOffset, 0f) * transform.forward;


        Debug.Log($"ForwardSpeed = {forwardSpeed}");


        rb.linearVelocity = forward * forwardSpeed;

        rb.angularVelocity = Vector3.up * angularSpeed * Mathf.Deg2Rad;
    }

    public void Stop()
    {
        Drive(0f, 0f);
    }

    //------------------------------------------------------
    // PWM MODEL
    //------------------------------------------------------

    float SpeedToPWM(float speed)
    {
        float pwm = Mathf.Abs(speed) * speedToPwm;

        if (pwm < motorDeadzone)
            pwm = 0;

        else if (pwm < minMotorPwm)
            pwm = minMotorPwm;

        return Mathf.Sign(speed) * pwm;
    }

    float MoveTowardsPwm(float current, float target)
    {
        return Mathf.MoveTowards(
            current,
            target,
            maxPwmStep);
    }
}
