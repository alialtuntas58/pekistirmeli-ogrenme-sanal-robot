using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RobotObstacleAgent : Agent
{
    [Header("Hedef ve Hareket Ayarları")]
    public Transform target;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    private Rigidbody rb;
    private Vector3 startPos;
    private Quaternion startRot;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        // Ajanın hızını ve konumunu sıfırla
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.localPosition = startPos;
        transform.localRotation = startRot;

        // Hedefi platform üzerinde rastgele bir konuma taşı
        if (target != null)
        {
            target.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
        }
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

        Vector3 move = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        Quaternion turn = Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);

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
        continuousActions[0] = Input.GetAxis("Vertical");   // W-S veya Yukarı-Aşağı Ok
        continuousActions[1] = Input.GetAxis("Horizontal"); // A-D veya Sol-Sağ Ok
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            AddReward(1.0f); // Hedefe ulaşma büyük pozitif ödül
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
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
        else if (other.CompareTag("Obstacle"))
        {
            AddReward(-1.0f); // Engele çarpma cezası
            EndEpisode();
        }
    }
}