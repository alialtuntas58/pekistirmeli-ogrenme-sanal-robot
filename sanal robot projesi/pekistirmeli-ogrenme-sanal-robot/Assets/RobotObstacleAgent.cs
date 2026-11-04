using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(RobotMovement))]
public class RobotObstacleAgent : Agent
{
    [Header("Hedef ve Hareket Ayarları")]
    public Transform target;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    private Rigidbody rb;
    private RobotMovement movement;
    private Vector3 startPos;
    private Quaternion startRot;

    [Header("Ortam Yönetimi")]
    [SerializeField] private EnvironmentManager environmentManager;

    private bool episodeEnding;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<RobotMovement>();
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        episodeEnding = false;

        if (environmentManager == null)
        {
            Debug.LogError("EnvironmentManager atanmamış!");
            return;
        }

        environmentManager.ResetEnvironment();

        movement.ResetPose(

            transform.position,
            transform.rotation
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (target != null)
        {
            // 1. Hedefe olan yön vektörü (3 Gözlem: X, Y, Z)
            Vector3 dirToTarget = (target.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(dirToTarget);

            // 2. Hedefe olan mesafe (1 Gözlem)
            float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
            sensor.AddObservation(distanceToTarget);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        // 3. Robotun mevcut ileri yönü (3 Gözlem: X, Y, Z) -> Toplam = 7 Gözlem
        sensor.AddObservation(transform.forward);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Continuous Actions = 2 (İleri/Geri Hareket ve Dönüş)
        float moveInput = actions.ContinuousActions[0];
        float turnInput = actions.ContinuousActions[1];

        movement.Move(moveInput, turnInput, moveSpeed, turnSpeed);

        // Hedefe daha hızlı ulaşmasını teşvik etmek için adım başı ufak ceza
        AddReward(-0.001f);

        // Platformdan aşağı düşerse ceza ver ve bölümü bitir
        if (transform.localPosition.y < -1.0f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        continuousActions[0] = keyboard == null ? 0f :
            ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1f : 0f) -
            ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1f : 0f);
        continuousActions[1] = keyboard == null ? 0f :
            ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1f : 0f) -
            ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1f : 0f);
#else
        continuousActions[0] = Input.GetAxis("Vertical");   // W-S veya Yukarı-Aşağı Ok
        continuousActions[1] = Input.GetAxis("Horizontal"); // A-D veya Sol-Sağ Ok
#endif
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            AddReward(1.0f); // Hedefe ulaşma büyük pozitif ödül
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.tag == "Wall")
        {
            AddReward(-1.0f); // Engele çarpma cezası
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            AddReward(1.0f); // Hedefe ulaşma büyük pozitif ödül
            EndEpisode();
        }
        else if (other.CompareTag("Obstacle") || other.tag == "Wall")
        {
            AddReward(-1.0f); // Engele çarpma cezası
            EndEpisode();
        }
    }

    private void FinishEpisodeIfNeeded(EpisodeEndReason reason)
    {
        if (episodeEnding || reason == EpisodeEndReason.None)
            return;

        episodeEnding = true;

        if (reason == EpisodeEndReason.TargetReached)
        {
            AddReward(1.0f);
        }
        else if (
            reason == EpisodeEndReason.ObstacleCollision ||
            reason == EpisodeEndReason.OutOfBounds
        )
        {
            AddReward(-1.0f);
        }

        EndEpisode();
    }
}
