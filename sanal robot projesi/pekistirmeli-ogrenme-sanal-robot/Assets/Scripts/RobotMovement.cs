using UnityEngine;

/// <summary>Shared physics movement for keyboard and ML-Agents continuous actions.</summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class RobotMovement : MonoBehaviour
{
    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // Called once per physics action by RobotObstacleAgent, including repeated decisions.
    public void Move(float forward, float turn, float moveSpeed, float turnSpeed)
    {
        if (body == null) body = GetComponent<Rigidbody>();
        float speed = Mathf.Clamp(forward, -1f, 1f) * Mathf.Max(0f, moveSpeed);
        Vector3 velocity = transform.forward * speed;
        velocity.y = body.linearVelocity.y;
        body.linearVelocity = velocity;
        body.MoveRotation(body.rotation * Quaternion.Euler(
            0f, Mathf.Clamp(turn, -1f, 1f) * Mathf.Max(0f, turnSpeed) * Time.fixedDeltaTime, 0f));
    }

    public void ResetPose(Vector3 position, Quaternion rotation)
    {
        if (body == null) body = GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = position;
        body.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
    }
}
