using UnityEngine;
using Unity.Netcode;

public class AdvancedPlayerAnimation : NetworkBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float locomotionBlendSpeed = 4f;

    private PlayerInputs _inputs;
    private PlayerState _state;
    private AdvancedPlayerController _controller;

    private static readonly int inputXHash = Animator.StringToHash("inputX");
    private static readonly int inputYHash = Animator.StringToHash("inputY");
    private static readonly int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
    private static readonly int isIdlingHash = Animator.StringToHash("isIdling");
    private static readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private static readonly int isFallingHash = Animator.StringToHash("isFalling");
    private static readonly int isJumpingHash = Animator.StringToHash("isJumping");
    private static readonly int isRotatingToTargetHash = Animator.StringToHash("isRotatingToTarget");
    private static readonly int rotationMismatchHash = Animator.StringToHash("rotationMismatch");

    private Vector3 _currentBlendInput = Vector3.zero;

    private void Awake()
    {
        _inputs = GetComponent<PlayerInputs>();
        _state = GetComponent<PlayerState>();
        _controller = GetComponent<AdvancedPlayerController>();
    }

    private void Update()
    {
        if (!IsOwner || _animator == null) return; 
        
        bool isRunBlendValue = _state.CurrentPlayerMovementState == PlayerMovementState.Running || 
                               _state.CurrentPlayerMovementState == PlayerMovementState.Jumping || 
                               _state.CurrentPlayerMovementState == PlayerMovementState.Falling;

        Vector2 inputTarget = _state.CurrentPlayerMovementState == PlayerMovementState.Sprinting ? _inputs.MovementInput * 1.5f :
                              isRunBlendValue ? _inputs.MovementInput * 1.0f : _inputs.MovementInput * 0.5f;

        _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);

        _animator.SetBool(isGroundedHash, _state.InGroundedState());
        _animator.SetBool(isIdlingHash, _state.CurrentPlayerMovementState == PlayerMovementState.Idling);
        _animator.SetBool(isFallingHash, _state.CurrentPlayerMovementState == PlayerMovementState.Falling);
        _animator.SetBool(isJumpingHash, _state.CurrentPlayerMovementState == PlayerMovementState.Jumping);
        
        if (_controller != null)
        {
            _animator.SetBool(isRotatingToTargetHash, _controller.IsRotatingToTarget);
            _animator.SetFloat(rotationMismatchHash, _controller.RotationMismatch);
        }

        _animator.SetFloat(inputXHash, _currentBlendInput.x);
        _animator.SetFloat(inputYHash, _currentBlendInput.y);
        _animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);
    }
}