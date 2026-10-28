using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public void SaveInitialTransform()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void ResetObstacle()
    {
        transform.SetPositionAndRotation(
            initialPosition,
            initialRotation
        );
    }

    public void SetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}