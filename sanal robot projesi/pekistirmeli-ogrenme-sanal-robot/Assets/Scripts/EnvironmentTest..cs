using UnityEngine;
using UnityEngine.InputSystem;

public class EnvironmentTest : MonoBehaviour
{
    [SerializeField]
    private EnvironmentManager environmentManager;

    private void Start()
    {
        Debug.Log("EnvironmentTest çalýþtý.");
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("R tuþuna basýldý.");

            if (environmentManager == null)
            {
                Debug.LogError("EnvironmentManager referansý BOÞ!");
                return;
            }

            environmentManager.ResetEnvironment();

            Debug.Log("ResetEnvironment çaðrýldý.");
        }
    }
}