using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInteractor : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("How close your character's body needs to be to the board/NPC to interact.")]
    [SerializeField] private float m_interactRange = 3f;

    private MultiplayerUI m_uiManager;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        m_uiManager = FindFirstObjectByType<MultiplayerUI>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Manual Cursor Toggle ( ~ Key )
        if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            ToggleCursorState();
        }

        // --- THE NEW INTERACTION: Press E ---
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteractProximity();
        }
    }

    private void ToggleCursorState()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void TryInteractProximity()
    {
        // Prevent clicking if a menu is already open
        if (MultiplayerUI.IsUIActive) return;

        Debug.Log("<color=cyan>--- PRESSING E: CHECKING AREA ---</color>");

        // Creates an invisible bubble around the player's body and grabs everything inside it
        Collider[] hits = Physics.OverlapSphere(transform.position, m_interactRange);


        foreach (Collider hit in hits)
        {
            // Ignore our own body
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

            // Check if one of the things in the bubble is the Media Board
            NetworkedMediaBoard board = hit.GetComponentInParent<NetworkedMediaBoard>();
            if (board != null)
            {
                Debug.Log("<color=green>SUCCESS: Opened Media Board!</color>");
                if (m_uiManager != null) m_uiManager.OpenClassroomUI();
                return; 
            }

            // Check if one of the things in the bubble is the NPC
            NpcBrain npc = hit.GetComponentInParent<NpcBrain>();
            if (npc != null)
            {
                Debug.Log("<color=green>SUCCESS: Opened NPC Chat!</color>");
                if (m_uiManager != null) m_uiManager.OpenNpcChatUI();
                return; 
            }
        }
        
        Debug.Log("<color=orange>Pressed E, but you are not close enough to anything interactable!</color>");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_interactRange);
    }
}