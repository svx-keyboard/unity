using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// Обновленный мозг ML-агента (Шаг 3 и Шаг 4).
/// Строго упорядочены 15 наблюдений, настроены непрерывные/дискретные действия и номинальные награды.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(RobotDrive))]
[RequireComponent(typeof(RobotSensors))]
public class RobotBrain : Agent
{
    [Header("Базовые компоненты платформы")]
    private Rigidbody rb;
    private RobotDrive drive;
    private RobotSensors sensors;

    [Header("Внешние компоненты трекинга и зрения")]
    public SimulatedYoloCamera yoloCamera;
    public RobotPositionTracker positionTracker;
    public CameraRotationTracker cameraRotationTracker;

    [Header("Компоненты клешни (Gripper)")]
    public JointController gripperJoint;
    public Transform gripperPoint;
    public Collider finger1;
    public Collider finger2;
    public Collider arm;
    public float grabRadius = 0.03f;
    public LayerMask grabbableMask;
    public bool drawSphere = true;

    [Header("Дополнительные датчики")]
    [Tooltip("ИК-датчик клешни (если нет в RobotSensors, можно настраивать здесь)")]
    public float clawIRSensorValue = 0.0f; 

    [Header("Сервопривод камеры")]
    public JointController cameraServoJoint;

    [Header("Sim2Real: Параметры надежности")]
    [Range(8, 13)]
    public int wifiDelaySteps = 10;
    private Queue<float[]> actionQueue = new Queue<float[]>();

    [Range(0f, 0.1f)]
    public float frameDropProbability = 0.05f;
    private bool isYoloFrozen = false;
    private int freezeTimer = 0;

    // Внутренние переменные для реализации пунктов 7 и 15 вектора наблюдений
    private float lastKnownDirectionToBall = 0.0f;
    private float timeSinceLastDetection = 0.0f;
    private bool holding = false;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        drive = GetComponent<RobotDrive>();
        sensors = GetComponent<RobotSensors>();

        // Автоматический поиск компонентов, если они не привязаны вручную
        if (yoloCamera == null) yoloCamera = GetComponentInChildren<SimulatedYoloCamera>();
        if (positionTracker == null) positionTracker = GetComponent<RobotPositionTracker>();
        if (cameraRotationTracker == null) cameraRotationTracker = GetComponent<CameraRotationTracker>();
    }

    public override void OnEpisodeBegin()
    {
        if (holding)
        {
            ReleaseBallInternal();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        drive.Stop();

        actionQueue.Clear();

        // Сброс параметров памяти детекции мяча
        lastKnownDirectionToBall = 0.0f;
        timeSinceLastDetection = 0.0f;

        // Перемещаем мяч в случайную точку (если он есть на сцене)
        if (yoloCamera != null && yoloCamera.target != null)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(1.0f, 3.5f));
            yoloCamera.target.position = transform.position + (transform.rotation * randomOffset);
            
            Rigidbody ballRb = yoloCamera.target.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = false;
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }
        }
    }

    void Update()
    {
        // Обновление состояния памяти детекции YOLO
        UpdateYoloTrackingState();

        // Логика автоматического физического подбора объекта клешнёй
        if (gripperJoint == null || gripperPoint == null) return;

        if (!holding)
        {
            SearchCandidateInternal();
            if (candidateObject != null && gripperJoint.IsClosed)
            {
                GrabBallInternal();
            }
        }
        else
        {
            if (!gripperJoint.IsClosed)
            {
                ReleaseBallInternal();
            }
        }
    }

    void LateUpdate()
    {
        if (holding && heldBody != null && gripperPoint != null)
        {
            heldBody.transform.position = gripperPoint.position;
            heldBody.transform.rotation = gripperPoint.rotation;
        }
    }

    /// <summary>
    /// Обновляет таймеры детекции и сохраняет последнее направление на цель.
    /// </summary>
    private void UpdateYoloTrackingState()
    {
        UpdateYoloDropout();

        bool isVisibleNow = yoloCamera != null && yoloCamera.IsTargetVisible && !isYoloFrozen;

        if (isVisibleNow)
        {
            timeSinceLastDetection = 0.0f;
            // Вычисляем горизонтальное смещение от центра экрана [-0.5; 0.5] и переводим в [-1.0; 1.0]
            lastKnownDirectionToBall = (yoloCamera.TargetScreenPos.x - 0.5f) * 2.0f;
        }
        else
        {
            timeSinceLastDetection += Time.deltaTime;
        }
    }

    // =================================================================================
    // 💻 ШАГ 3.1: МЕТОД СБОРА НАБЛЮДЕНИЙ (СТРОГИЙ ПОРЯДОК ИЗ 15 ПАРАМЕТРОВ)
    // =================================================================================
    public override void CollectObservations(VectorSensor sensor)
    {
        // Базовые флаги видимости
        bool ballVisible = yoloCamera != null && yoloCamera.IsTargetVisible && !isYoloFrozen;

        // 1. Нормализованное расстояние с УЗ-дальномера
        float rawUltrasonic = sensors != null ? sensors.UltrasonicValue : 1.0f;
        sensor.AddObservation(Mathf.Clamp01(rawUltrasonic));

        // 2. Левый ИК-датчик препятствий (0/1)
        float leftIR = (sensors != null && sensors.LeftIR > 0.5f) ? 1.0f : 0.0f;
        sensor.AddObservation(leftIR);

        // 3. Правый ИК-датчик препятствий (0/1)
        float rightIR = (sensors != null && sensors.RightIR > 0.5f) ? 1.0f : 0.0f;
        sensor.AddObservation(rightIR);

        // 4. ИК-датчик клешни (0/1)
        float clawIR = (clawIRSensorValue > 0.5f) ? 1.0f : 0.0f;
        sensor.AddObservation(clawIR);

        // 5. Относительный горизонтальный угол до мяча по камере (0, если мяч не виден)
        float currentHorizontalAngle = 0.0f;
        if (ballVisible)
        {
            currentHorizontalAngle = (yoloCamera.TargetScreenPos.x - 0.5f) * 2.0f;
        }
        sensor.AddObservation(currentHorizontalAngle);

        // 6. Нормализованное расстояние до мяча по камере (1, если мяч не виден)
        float normalizedDistanceToBall = 1.0f;
        if (ballVisible)
        {
            float rawDist = Vector3.Distance(yoloCamera.robotCamera.transform.position, yoloCamera.target.position);
            normalizedDistanceToBall = Mathf.Clamp01(rawDist / yoloCamera.maxDetectionRange);
        }
        sensor.AddObservation(normalizedDistanceToBall);

        // 7. Последнее известное направление на мяч (после утери из кадра)
        sensor.AddObservation(lastKnownDirectionToBall);

        // 8. Флаг видимости мяча (0.0 или 1.0)
        sensor.AddObservation(ballVisible ? 1.0f : 0.0f);

        // 9. Текущий поворот сервопривода камеры (из CameraRotationTracker.cs)
        float cameraYaw = cameraRotationTracker != null ? cameraRotationTracker.GetYaw() : 0.0f;
        sensor.AddObservation(cameraYaw);

        // 10. Статус захвата мяча клешнёй (hasBall -> 0.0 или 1.0)
        sensor.AddObservation(holding ? 1.0f : 0.0f);

        // 11. Относительное смещение робота по оси X от точки старта (из RobotPositionTracker.cs)
        float relX = positionTracker != null ? positionTracker.RelativeX : 0.0f;
        sensor.AddObservation(relX);

        // 12. Относительное смещение робота по оси Z от точки старта (из RobotPositionTracker.cs)
        float relZ = positionTracker != null ? positionTracker.RelativeZ : 0.0f;
        sensor.AddObservation(relZ);

        // 13. Нормализованный угол направления взгляда робота (Heading) [-1.0; 1.0]
        float robotHeading = NormalizeAngle(transform.localEulerAngles.y) / 180f;
        sensor.AddObservation(robotHeading);

        // 14. Текущая скорость движения робота от Rigidbody
        float currentSpeed = rb != null ? rb.linearVelocity.magnitude : 0.0f;
        sensor.AddObservation(currentSpeed);

        // 15. Время, прошедшее с момента последней детекции мяча
        sensor.AddObservation(timeSinceLastDetection);
    }

    // =================================================================================
    // 💻 ШАГ 3.2: МЕТОД ПРИЕМА ДЕЙСТВИЙ (ОБРАБОТКА ДИСКРЕТНЫХ И НЕПРЕРЫВНЫХ КОМАНД)
    // =================================================================================
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Собираем непрерывные действия во фрейм
        float[] currentActionFrame = new float[actions.ContinuousActions.Length];
        for (int i = 0; i < actions.ContinuousActions.Length; i++)
        {
            currentActionFrame[i] = actions.ContinuousActions[i];
        }

        // Имитируем сетевую Wi-Fi задержку
        actionQueue.Enqueue(currentActionFrame);
        if (actionQueue.Count < wifiDelaySteps)
        {
            drive.Stop();
            return;
        }

        float[] delayedAction = actionQueue.Dequeue();

        // Чтение сигналов непрерывных действий
        float gas = delayedAction[0];    // continuous[0] -> линейное движение
        float steer = delayedAction[1];  // continuous[1] -> угловое вращение
        float camInput = delayedAction[2]; // continuous[2] -> поворот камеры

        // Передаем движение гусеничной платформе (в TrackController / RobotDrive)
        drive.Drive(gas, steer);

        // Передаем сигнал поворота сервоприводу камеры
        if (cameraServoJoint != null)
        {
            cameraServoJoint.SetInput(camInput);
        }

        // Чтение сигнала дискретного действия (discrete[0] -> команда клешне)
        if (gripperJoint != null)
        {
            int clawCommand = actions.DiscreteActions[0];
            
            switch (clawCommand)
            {
                case 0: // Стоять / Ничего не делать
                    break;
                case 1: // Закрыть клешню
                    gripperJoint.SetInput(1.0f);
                    break;
                case 2: // Открыть клешню
                    gripperJoint.SetInput(-1.0f);
                    break;
            }
        }

        // =================================================================================
        // 💻 ШАГ 4: РАЗРАБОТКА ФУНКЦИИ НАГРАД (НОМИНАЛЬНЫЕ НАГРАДЫ)
        // =================================================================================
        CalculateNominalRewards();
    }

    private void CalculateNominalRewards()
    {
        AddReward(-0.0005f);

        if (holding)
        {
            SetReward(1.0f);
            EndEpisode();
        }
    }

    // --- ФУНКЦИИ ВЗАИМОДЕЙСТВИЯ КЛЕШНИ С ОБЪЕКТОМ (МЯЧОМ) ---
    private GameObject candidateObject;
    private Rigidbody heldBody;
    private Collider heldCollider;

    private void SearchCandidateInternal()
    {
        candidateObject = null;
        Collider[] hits = Physics.OverlapSphere(gripperPoint.position, grabRadius, grabbableMask);
        if (hits.Length > 0)
            candidateObject = hits[0].gameObject;
    }

    private void GrabBallInternal()
    {
        heldBody = candidateObject.GetComponent<Rigidbody>();
        heldCollider = candidateObject.GetComponent<Collider>();

        if (heldBody == null || heldCollider == null) return;

        if (finger1 != null) Physics.IgnoreCollision(finger1, heldCollider, true);
        if (finger2 != null) Physics.IgnoreCollision(finger2, heldCollider, true);
        if (arm != null) Physics.IgnoreCollision(arm, heldCollider, true);

        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.isKinematic = true;
        heldBody.transform.SetParent(gripperPoint);
        heldBody.transform.localPosition = Vector3.zero;
        heldBody.transform.localRotation = Quaternion.identity;

        holding = true;
    }

    private void ReleaseBallInternal()
    {
        if (heldBody != null)
        {
            heldBody.transform.SetParent(null);
            heldBody.isKinematic = false;

            if (finger1 != null && heldCollider != null) Physics.IgnoreCollision(finger1, heldCollider, false);
            if (finger2 != null && heldCollider != null) Physics.IgnoreCollision(finger2, heldCollider, false);
        }

        holding = false;
        heldBody = null;
        heldCollider = null;
        candidateObject = null;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    private void UpdateYoloDropout()
    {
        if (isYoloFrozen)
        {
            freezeTimer--;
            if (freezeTimer <= 0) isYoloFrozen = false;
        }
        else
        {
            if (Random.value < frameDropProbability)
            {
                isYoloFrozen = true;
                freezeTimer = Random.Range(3, 8);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawSphere || gripperPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gripperPoint.position, grabRadius);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");   
        continuousActions[1] = Input.GetAxis("Horizontal"); 
        continuousActions[2] = Input.GetKey(KeyCode.C) ? 1.0f : (Input.GetKey(KeyCode.X) ? -1.0f : 0.0f); 

        var discreteActions = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.Alpha1)) discreteActions[0] = 1;      
        else if (Input.GetKey(KeyCode.Alpha2)) discreteActions[0] = 2; 
        else discreteActions[0] = 0;                                   
    }
}
