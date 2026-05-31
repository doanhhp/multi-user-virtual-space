using UnityEngine;

public enum PlayerMovementState
{
    Idling = 0, Walking = 1, Running = 2, Sprinting = 3,
    Jumping = 4, Falling = 5, Strafing = 6,
}

public class PlayerState : MonoBehaviour
{
    [field: SerializeField] public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idling;

    public void SetPlayerMovementState(PlayerMovementState state) => CurrentPlayerMovementState = state;
    public bool InGroundedState() => IsStateGroundedState(CurrentPlayerMovementState);
    public bool IsStateGroundedState(PlayerMovementState state)
    {
        return state == PlayerMovementState.Idling || state == PlayerMovementState.Walking ||
               state == PlayerMovementState.Running || state == PlayerMovementState.Sprinting;
    }
}

public static class CharacterControllerUtils
{
    public static Vector3 GetNormalWithSphereCast(CharacterController characterController, LayerMask layerMask = default)
    {
        Vector3 normal = Vector3.up;
        Vector3 center = characterController.transform.position + characterController.center;
        float distance = characterController.height / 2f + characterController.stepOffset + 0.01f;

        if (Physics.SphereCast(center, characterController.radius, Vector3.down, out RaycastHit hit, distance, layerMask))
            normal = hit.normal;
        return normal;
    }
}