using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private MyPlayerInput m_playerInput;
    [SerializeField] private AgentMover m_agentMover;
    [SerializeField] private PlayerCamera m_playerCamera; 

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            m_playerInput.OnJumpPressed += HandleJump;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            m_playerInput.OnJumpPressed -= HandleJump;
        }
    }

    private void HandleJump()
    {
        // --- NEW: UI LOCK FOR JUMPING ---
        // Prevents the player from jumping if they press spacebar while typing!
        if (MultiplayerUI.IsUIActive) return;

        m_agentMover.Jump();
    }

    private void Update()
    {
        if(IsOwner == false) return;
        
        // Completely ignores WASD inputs while the chat box is open
        if (MultiplayerUI.IsUIActive) return;

        Vector2 movementInput = m_playerInput.MovementInput;

        if (m_playerCamera.CameraTransform != null)
        {
            m_agentMover.Move(movementInput, m_playerCamera.CameraTransform);
        }
    }
}