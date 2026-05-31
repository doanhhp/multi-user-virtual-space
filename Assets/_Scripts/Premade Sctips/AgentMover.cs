using UnityEngine;

public class AgentMover : MonoBehaviour
{
    [SerializeField] private CharacterController m_characterController;
    [SerializeField] private Animator m_animator;

    [Header("Movement Settings")]
    [SerializeField] private float m_movementSpeed = 4f;
    [SerializeField] private float m_turnSmoothTime = 0.1f; 
    
    [Header("Jump & Gravity")]
    [SerializeField] private float m_jumpHeight = 1.5f;
    [SerializeField] private float m_gravity = -15f; // Stronger gravity feels less "floaty"
    
    private float m_turnSmoothVelocity;
    private float m_velocityY; // Tracks our up/down falling speed

    public void Move(Vector2 movementInput, Transform cameraTransform)
    {
        // 1. Handle Grounding & Gravity
       
        if (m_characterController.isGrounded && m_velocityY < 0)
        {
            m_velocityY = -2f; 
        }

        Vector3 targetDirection = Vector3.zero;

        // 2. Handle Horizontal Movement
        if (movementInput.sqrMagnitude >= 0.01f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            targetDirection = camForward * movementInput.y + camRight * movementInput.x;
            targetDirection.Normalize();

            float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref m_turnSmoothVelocity, m_turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

       
        if (m_animator != null)
        {
            m_animator.SetFloat("Movement", movementInput.magnitude);
        }

    
        Vector3 finalVelocity = targetDirection * m_movementSpeed;
        
        m_velocityY += m_gravity * Time.deltaTime;
        finalVelocity.y = m_velocityY;

        m_characterController.Move(finalVelocity * Time.deltaTime);
    }

    // NEW: Jump Function
    public void Jump()
    {
        if (m_characterController.isGrounded)
        {
            m_velocityY = Mathf.Sqrt(m_jumpHeight * -2f * m_gravity);
            
            // if (m_animator != null) m_animator.SetTrigger("Jump");
        }
    }
}