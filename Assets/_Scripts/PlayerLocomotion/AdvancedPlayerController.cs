using UnityEngine;
using Unity.Netcode;

[DefaultExecutionOrder(-1)]
public class AdvancedPlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private CharacterController _characterController;
    private Transform _cameraTarget; 
    public float RotationMismatch { get; private set; } = 0f;
    public bool IsRotatingToTarget { get; private set; } = false;

    [Header("Movement Settings")]
    public float walkAcceleration = 25f; public float walkSpeed = 2f;
    public float runAcceleration = 35f;  public float runSpeed = 4f;
    public float sprintAcceleration = 50f; public float sprintSpeed = 7f;
    public float inAirAcceleration = 25f; public float drag = 20f; public float inAirDrag = 5f;
    public float gravity = 25f; public float terminalVelocity = 50f; public float jumpSpeed = 0.8f;
    
    [Header("Jump Settings")]
    public float jumpCooldown = 0.5f;
    private float _jumpCooldownTimer = 0f;

    [Header("Animation & Camera")]
    public float playerModelRotationSpeed = 10f;
    public float rotateToTargetTime = 0.67f;
    public float lookSenseH = 0.1f; public float lookSenseV = 0.1f; public float lookLimitV = 89f;
    [SerializeField] private LayerMask _groundLayers;

    private PlayerInputs _inputs;
    private PlayerState _state;
    private Vector2 _cameraRotation = Vector2.zero;
    private Vector2 _playerTargetRotation = Vector2.zero;
    private bool _jumpedLastFrame;
    private bool _isRotatingClockwise;
    private float _rotatingToTargetTimer, _verticalVelocity, _antiBump, _stepOffset;
    private PlayerMovementState _lastMovementState = PlayerMovementState.Falling;

    private void Awake()
    {
        _inputs = GetComponent<PlayerInputs>();
        _state = GetComponent<PlayerState>();
        _antiBump = sprintSpeed;
        _stepOffset = _characterController.stepOffset;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _cameraTarget = transform.Find("CameraFollowTarget");
    }

    private void Update()
    {
        // Removed the UI check so gravity and physics run forever!
        if (!IsOwner) return; 
        
        if (_jumpCooldownTimer > 0) _jumpCooldownTimer -= Time.deltaTime;

        UpdateMovementState();
        HandleVerticalMovement();
        HandleLateralMovement();
        
        if (_cameraTarget != null) UpdateCameraRotation();
    }


    private void UpdateMovementState()
    {
        _lastMovementState = _state.CurrentPlayerMovementState;

        bool canRun = _inputs.MovementInput.y >= Mathf.Abs(_inputs.MovementInput.x);
        bool isMovingLaterally = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude > 0.01f;
        bool isSprinting = _inputs.SprintToggledOn && isMovingLaterally;
        bool isWalking = isMovingLaterally && (!canRun || _inputs.WalkToggledOn);
        bool isGrounded = IsGrounded();

        PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking :
                                           isSprinting ? PlayerMovementState.Sprinting :
                                           isMovingLaterally || _inputs.MovementInput != Vector2.zero ? PlayerMovementState.Running : PlayerMovementState.Idling;

        _state.SetPlayerMovementState(lateralState);

        if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y > 0f) { _state.SetPlayerMovementState(PlayerMovementState.Jumping); _jumpedLastFrame = false; _characterController.stepOffset = 0f; }
        else if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y <= 0f) { _state.SetPlayerMovementState(PlayerMovementState.Falling); _jumpedLastFrame = false; _characterController.stepOffset = 0f; }
        else { _characterController.stepOffset = _stepOffset; }
    }

    private void HandleVerticalMovement()
    {
        bool isGrounded = _state.InGroundedState();
        _verticalVelocity -= gravity * Time.deltaTime;
        
        if (isGrounded && _verticalVelocity < 0) _verticalVelocity = -_antiBump;

        if (_inputs.JumpPressed && isGrounded && _jumpCooldownTimer <= 0f)
        {
            // 1. We keep your existing horizontal velocity (X and Z) untouched
            // 2. We reset ONLY the Y velocity to the jump force
            // This removes the "Slope Push" but keeps your forward running speed
            _verticalVelocity = Mathf.Sqrt(jumpSpeed * 3 * gravity); 
            
            _jumpedLastFrame = true;
            _jumpCooldownTimer = jumpCooldown;
        }

        if (_state.IsStateGroundedState(_lastMovementState) && !isGrounded) _verticalVelocity += _antiBump;
        if (Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity)) _verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
    }

    private void HandleLateralMovement()
    {
        bool isSprinting = _state.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
        bool isGrounded = _state.InGroundedState();
        bool isWalking = _state.CurrentPlayerMovementState == PlayerMovementState.Walking;

        float acc = !isGrounded ? inAirAcceleration : isWalking ? walkAcceleration : isSprinting ? sprintAcceleration : runAcceleration;
        float clamp = !isGrounded ? sprintSpeed : isWalking ? walkSpeed : isSprinting ? sprintSpeed : runSpeed;

        Vector3 camFwd = new Vector3(_cameraTarget.forward.x, 0f, _cameraTarget.forward.z).normalized;
        Vector3 camRight = new Vector3(_cameraTarget.right.x, 0f, _cameraTarget.right.z).normalized;
        Vector3 dir = camRight * _inputs.MovementInput.x + camFwd * _inputs.MovementInput.y;

        Vector3 newVel = _characterController.velocity + (dir * acc * Time.deltaTime);
        float dragMag = isGrounded ? drag : inAirDrag;
        Vector3 curDrag = newVel.normalized * dragMag * Time.deltaTime;
        
        newVel = (newVel.magnitude > dragMag * Time.deltaTime) ? newVel - curDrag : Vector3.zero;
        newVel = Vector3.ClampMagnitude(new Vector3(newVel.x, 0f, newVel.z), clamp);
        newVel.y += _verticalVelocity;

        _characterController.Move(newVel * Time.deltaTime);
    }

    private void UpdateCameraRotation()
    {
        _cameraRotation.x += lookSenseH * _inputs.LookInput.x;
        _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _inputs.LookInput.y, -lookLimitV, lookLimitV);
        _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _inputs.LookInput.x;

        // Rotate the invisible Camera Target, letting Cinemachine follow it smoothly
        _cameraTarget.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);

        float rotTol = 90f;
        IsRotatingToTarget = _rotatingToTargetTimer > 0;
        if (_state.CurrentPlayerMovementState != PlayerMovementState.Idling) RotatePlayerToTarget();
        else if (Mathf.Abs(RotationMismatch) > rotTol || IsRotatingToTarget)
        {
            if (Mathf.Abs(RotationMismatch) > rotTol) { _rotatingToTargetTimer = rotateToTargetTime; _isRotatingClockwise = RotationMismatch > rotTol; }
            _rotatingToTargetTimer -= Time.deltaTime;
            if (_isRotatingClockwise && RotationMismatch > 0f || !_isRotatingClockwise && RotationMismatch < 0f) RotatePlayerToTarget();
        }

        Vector3 camFwd = new Vector3(_cameraTarget.forward.x, 0f, _cameraTarget.forward.z).normalized;
        float sign = Mathf.Sign(Vector3.Dot(Vector3.Cross(transform.forward, camFwd), transform.up));
        RotationMismatch = sign * Vector3.Angle(transform.forward, camFwd);
    }

    private void RotatePlayerToTarget() => transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, _playerTargetRotation.x, 0f), playerModelRotationSpeed * Time.deltaTime);

    private bool IsGrounded()
    {
      
        if (_characterController.isGrounded) return true;

        // Find the exact bottom of the physics capsule
        Vector3 bottom = transform.position + _characterController.center;
        bottom.y -= (_characterController.height / 2f);

        float checkRadius = _characterController.radius * 0.8f; 
        
        // Place the sphere exactly at the feet and push it slightly into the floor
        Vector3 sphereCenter = bottom + (Vector3.up * checkRadius);
        return Physics.CheckSphere(sphereCenter - (Vector3.up * 0.1f), checkRadius, _groundLayers, QueryTriggerInteraction.Ignore);
    }
}