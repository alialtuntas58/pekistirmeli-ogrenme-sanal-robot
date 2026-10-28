using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public enum EnvironmentMode
    {
        Training,
        Test
    }

    public enum EpisodeEndReason
    {
        None,
        ObstacleCollision,
        TargetReached,
        OutOfBounds
    }

    [Header("Environment Mode")]
    [SerializeField]
    private EnvironmentMode mode = EnvironmentMode.Training;

    [Header("References")]
    [SerializeField]
    private Transform robot;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private Transform obstacleRoot;

    [Header("Arena Settings")]
    [SerializeField]
    private float minX = -8f;

    [SerializeField]
    private float maxX = 8f;

    [SerializeField]
    private float minZ = -8f;

    [SerializeField]
    private float maxZ = 8f;

    [Header("Obstacle Placement")]
    [SerializeField]
    private float minDistanceFromSpawn = 2.5f;

    [SerializeField]
    private float minDistanceFromTarget = 2.5f;

    [SerializeField]
    private float minDistanceBetweenObstacles = 2f;

    [SerializeField]
    private int maxPlacementAttempts = 100;

    [Header("Episode Settings")]
    [SerializeField]
    private float targetReachDistance = 1.2f;

    private readonly List<Obstacle> obstacles =
        new List<Obstacle>();

    private Rigidbody robotRigidbody;

    private bool obstacleHit = false;

    private Vector3 targetInitialPosition;
    private Quaternion targetInitialRotation;


    private void Awake()
    {
        // Robot henüz eklenmemiþ olabilir.
        if (robot != null)
        {
            robotRigidbody =
                robot.GetComponent<Rigidbody>();
        }

        // Target'ýn baþlangýç konumunu kaydet.
        if (target != null)
        {
            targetInitialPosition =
                target.position;

            targetInitialRotation =
                target.rotation;
        }

        // Obstacle ana nesnesi atanmýþ mý?
        if (obstacleRoot == null)
        {
            Debug.LogError(
                "EnvironmentManager: Obstacle Root atanmamýþ!"
            );

            return;
        }

        // Altýndaki tüm Obstacle componentlerini bul.
        Obstacle[] foundObstacles =
            obstacleRoot.GetComponentsInChildren<Obstacle>();

        obstacles.AddRange(foundObstacles);

        // Test modunda dönmek için baþlangýç
        // konumlarýný kaydet.
        foreach (Obstacle obstacle in obstacles)
        {
            obstacle.SaveInitialTransform();
        }

        Debug.Log(
            "EnvironmentManager hazýr. Bulunan obstacle sayýsý: "
            + obstacles.Count
        );
    }


    public void ResetEnvironment()
    {
        obstacleHit = false;

        ResetRobot();

        ResetTarget();

        if (mode == EnvironmentMode.Training)
        {
            RandomizeObstacles();
        }
        else
        {
            ResetObstaclesToInitialPositions();
        }

        Debug.Log(
            "Environment resetlendi. Mod: "
            + mode
        );
    }


    private void ResetRobot()
    {
        // Robot henüz projeye eklenmediyse
        // reset iþlemini atla.
        if (robot == null || spawnPoint == null)
        {
            return;
        }

        robot.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (robotRigidbody != null)
        {
            robotRigidbody.linearVelocity =
                Vector3.zero;

            robotRigidbody.angularVelocity =
                Vector3.zero;
        }
    }


    private void ResetTarget()
    {
        if (target == null)
        {
            return;
        }

        target.SetPositionAndRotation(
            targetInitialPosition,
            targetInitialRotation
        );
    }


    private void ResetObstaclesToInitialPositions()
    {
        foreach (Obstacle obstacle in obstacles)
        {
            obstacle.ResetObstacle();
        }
    }


    private void RandomizeObstacles()
    {
        List<Vector3> placedPositions =
            new List<Vector3>();

        foreach (Obstacle obstacle in obstacles)
        {
            bool positionFound = false;

            for (
                int attempt = 0;
                attempt < maxPlacementAttempts;
                attempt++
            )
            {
                Vector3 candidatePosition =
                    new Vector3(
                        Random.Range(minX, maxX),

                        // Obstacle yüksekliði deðiþmesin.
                        obstacle.transform.position.y,

                        Random.Range(minZ, maxZ)
                    );

                if (!IsValidObstaclePosition(
                        candidatePosition,
                        placedPositions))
                {
                    continue;
                }

                // DOÐRU METOT BURASI
                obstacle.SetPosition(
                    candidatePosition
                );

                placedPositions.Add(
                    candidatePosition
                );

                positionFound = true;

                Debug.Log(
                    obstacle.name
                    + " yeni pozisyon: "
                    + candidatePosition
                );

                break;
            }

            if (!positionFound)
            {
                Debug.LogWarning(
                    obstacle.name
                    + " için uygun rastgele pozisyon bulunamadý."
                );
            }
        }
    }


    private bool IsValidObstaclePosition(
        Vector3 candidate,
        List<Vector3> placedPositions)
    {
        // SpawnPoint'in üzerine obstacle gelmesin.
        if (spawnPoint != null)
        {
            if (HorizontalDistance(
                    candidate,
                    spawnPoint.position)
                < minDistanceFromSpawn)
            {
                return false;
            }
        }

        // Target'ýn üzerine obstacle gelmesin.
        if (target != null)
        {
            if (HorizontalDistance(
                    candidate,
                    target.position)
                < minDistanceFromTarget)
            {
                return false;
            }
        }

        // Obstacle'lar birbirinin çok yakýnýna gelmesin.
        foreach (
            Vector3 otherPosition
            in placedPositions
        )
        {
            if (HorizontalDistance(
                    candidate,
                    otherPosition)
                < minDistanceBetweenObstacles)
            {
                return false;
            }
        }

        return true;
    }


    private float HorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        Vector2 pointA =
            new Vector2(
                a.x,
                a.z
            );

        Vector2 pointB =
            new Vector2(
                b.x,
                b.z
            );

        return Vector2.Distance(
            pointA,
            pointB
        );
    }


    public void RegisterObstacleHit()
    {
        obstacleHit = true;
    }


    public EpisodeEndReason CheckEpisodeEnd()
    {
        // Engel veya duvar çarpýþmasý bildirildiyse.
        if (obstacleHit)
        {
            return
                EpisodeEndReason.ObstacleCollision;
        }

        // Robot henüz yoksa kontrol yapýlamaz.
        if (robot == null)
        {
            return EpisodeEndReason.None;
        }

        // Robot hedefe ulaþtý mý?
        if (target != null)
        {
            float distanceToTarget =
                HorizontalDistance(
                    robot.position,
                    target.position
                );

            if (
                distanceToTarget
                <= targetReachDistance
            )
            {
                return
                    EpisodeEndReason.TargetReached;
            }
        }

        // Robot arena dýþýna çýktý mý?
        if (
            robot.position.y < -1f ||
            robot.position.x < -10.5f ||
            robot.position.x > 10.5f ||
            robot.position.z < -10.5f ||
            robot.position.z > 10.5f
        )
        {
            return
                EpisodeEndReason.OutOfBounds;
        }

        return EpisodeEndReason.None;
    }


    // Klavye input'u kullanmadan reset test etmek için.
    [ContextMenu("TEST - Reset Environment")]
    private void TestResetEnvironment()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Önce Play tuþuna basmalýsýn."
            );

            return;
        }

        ResetEnvironment();
    }
}