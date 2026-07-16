using UnityEngine;

/// <summary>
/// Симуляция работы нейросети YOLO для детекции цели (мяча) на кадре с камеры.
/// Переводит 3D-координаты в 2D (Viewport) и проверяет видимость с учетом препятствий.
/// </summary>
public class SimulatedYoloCamera : MonoBehaviour
{
    [Header("Компоненты")]
    public Camera robotCamera;
    public Transform target;

    [Header("Настройки детекции")]
    [Tooltip("Слои, которые перекрывают обзор (стены, препятствия, окружение)")]
    public LayerMask occlusionLayers;

    [Tooltip("Максимальная дистанция, на которой YOLO способна распознать объект")]
    public float maxDetectionRange = 7.0f;

    // Выходные данные для ML-агента (Будем забирать в CollectObservations)
    public bool IsTargetVisible { get; private set; }
    
    [Tooltip("Координаты центра объекта на экране в диапазоне [0; 1]")]
    public Vector2 TargetScreenPos { get; private set; }

    void Start()
    {
        // Подсмотрели логику поиска камеры в вашем скрипте CameraCapture.cs
        if (robotCamera == null)
        {
            GameObject camObj = GameObject.Find("CameraScript");
            if (camObj != null)
            {
                robotCamera = camObj.GetComponent<Camera>();
            }
            else
            {
                robotCamera = GetComponentInChildren<Camera>();
            }
        }

        // Автоматический поиск мяча на сцене, если забыли перетащить в инспекторе
        if (target == null)
        {
            GameObject ball = GameObject.FindWithTag("Ball");
            if (ball != null) 
                target = ball.transform;
        }
    }

    void FixedUpdate()
    {
        CalculateYoloDetection();
    }

    private void CalculateYoloDetection()
    {
        if (robotCamera == null || target == null)
        {
            ResetDetection();
            return;
        }

        // 1. Проверка дистанции: если объект слишком далеко, YOLO его не задетектит
        Vector3 camPosition = robotCamera.transform.position;
        Vector3 targetPosition = target.position;
        float distance = Vector3.Distance(camPosition, targetPosition);

        if (distance > maxDetectionRange)
        {
            ResetDetection();
            return;
        }

        // 2. Проекция 3D в 2D координаты Viewport
        // (0,0) — левый нижний угол, (1,1) — правый верхний угол экрана
        Vector3 viewportPos = robotCamera.WorldToViewportPoint(targetPosition);

        // Проверяем, попадает ли координата в поле видимости камеры. 
        // viewportPos.z > 0 показывает, что объект находится ПЕРЕД камерой, а не сзади нее.
        bool inFrustum = viewportPos.x >= 0f && viewportPos.x <= 1f &&
                         viewportPos.y >= 0f && viewportPos.y <= 1f &&
                         viewportPos.z > 0f;

        if (!inFrustum)
        {
            ResetDetection();
            return;
        }

        // 3. Проверка на препятствия (Occlusion) с помощью луча
        Vector3 direction = (targetPosition - camPosition).normalized;
        
        // Кидаем луч от камеры к мячу на расстояние дистанции между ними
        if (Physics.Raycast(camPosition, direction, out RaycastHit hit, distance, occlusionLayers))
        {
            // Если луч врезался во что-то, что не является целью (или ее дочерним объектом) — обзор закрыт
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                ResetDetection();
                return;
            }
        }

        // Если все проверки пройдены, YOLO успешно "выдало" bounding box
        IsTargetVisible = true;
        TargetScreenPos = new Vector2(viewportPos.x, viewportPos.y);
    }

    private void ResetDetection()
    {
        IsTargetVisible = false;
        TargetScreenPos = Vector2.zero;
    }

    // Отрисовка луча детекции в окне Scene для наглядности (зеленый - видит, красный - нет)
    void OnDrawGizmos()
    {
        if (robotCamera != null && target != null)
        {
            Gizmos.color = IsTargetVisible ? Color.green : Color.red;
            Gizmos.DrawLine(robotCamera.transform.position, target.position);
        }
    }
}