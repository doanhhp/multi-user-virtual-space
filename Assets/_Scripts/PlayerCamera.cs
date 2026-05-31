using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("An empty GameObject placed around the character's chest/head height.")]
    [SerializeField] private Transform m_cameraTarget;

    private Transform m_mainCamera;

    public Transform CameraTransform => m_mainCamera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            m_mainCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        // LEGACY: Removed all manual camera manipulation here.
        // Cinemachine (via PlayerInputs / AdvancedPlayerController) now fully handles the camera.
        // If we leave this active, it violently fights Cinemachine and causes spinning glitches!
    }
}