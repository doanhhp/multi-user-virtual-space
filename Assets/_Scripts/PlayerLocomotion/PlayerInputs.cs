using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Cinemachine;

[DefaultExecutionOrder(-2)]
public class PlayerInputs : NetworkBehaviour, PlayerControls.IPlayerLocomotionMapActions, PlayerControls.IThirdPersonMapActions
{
    public PlayerControls Controls { get; private set; }

    // --- INTERNAL RAW INPUTS ---
    private Vector2 _rawMovementInput;
    private Vector2 _rawLookInput;
    private bool _rawJumpPressed;
    private float _rawScrollInput;

    // --- PUBLIC INPUTS (These instantly become ZERO if the UI is open!) ---
    public Vector2 MovementInput => MultiplayerUI.IsUIActive ? Vector2.zero : _rawMovementInput;
    public Vector2 LookInput => MultiplayerUI.IsUIActive ? Vector2.zero : _rawLookInput;
    public bool JumpPressed => !MultiplayerUI.IsUIActive && _rawJumpPressed;
    public float ScrollInput => MultiplayerUI.IsUIActive ? 0f : _rawScrollInput;

    public bool SprintToggledOn { get; private set; }
    public bool WalkToggledOn { get; private set; }

    [Header("Camera Zoom Settings")]
    [SerializeField] private float _cameraMinZoom = 1.5f;
    [SerializeField] private float _cameraMaxZoom = 10f;
    [SerializeField] private float _scrollSensitivity = 0.05f; 
    [SerializeField] private float _zoomSmoothSpeed = 10f; 

    private float _targetCameraDistance;
    private Cinemachine3rdPersonFollow _thirdPersonFollow;

    // ... Keep your exact OnNetworkSpawn and AttachCameraDelayed here! ...
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        Controls = new PlayerControls();
        Controls.PlayerLocomotionMap.Enable();
        Controls.PlayerLocomotionMap.SetCallbacks(this);
        Controls.ThirdPersonMap.Enable();
        Controls.ThirdPersonMap.SetCallbacks(this);

        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));
            transform.position = spawnPoint.transform.position + randomOffset;
            transform.rotation = spawnPoint.transform.rotation;
            
            if (cc != null) cc.enabled = true;
        }

        StartCoroutine(AttachCameraDelayed());
    }

    private System.Collections.IEnumerator AttachCameraDelayed()
    {
        // Wait exactly 1 frame so MainMenuNetworker can disable the customization camera 
        // and enable the gameplay camera. This guarantees we grab the correct Cinemachine.
        yield return null; 
        
        CinemachineVirtualCamera vCam = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (vCam != null)
        {
            Transform target = transform.Find("CameraFollowTarget") ?? transform;
            vCam.Follow = target;
            vCam.LookAt = null; 
            vCam.PreviousStateIsValid = false;
            _thirdPersonFollow = vCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            
            if (_thirdPersonFollow != null)
                _targetCameraDistance = _thirdPersonFollow.CameraDistance;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        Controls?.Disable();
    }

    private void Update()
    {
        if (!IsOwner || _thirdPersonFollow == null) return;
        
        if (ScrollInput != 0)
        {
            _targetCameraDistance -= ScrollInput; 
            _targetCameraDistance = Mathf.Clamp(_targetCameraDistance, _cameraMinZoom, _cameraMaxZoom);
        }

        _thirdPersonFollow.CameraDistance = Mathf.Lerp(_thirdPersonFollow.CameraDistance, _targetCameraDistance, Time.deltaTime * _zoomSmoothSpeed);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        // Reset our raw inputs at the end of the frame
        _rawJumpPressed = false;
        _rawScrollInput = 0f; 
    }

    // --- Input Interfaces (Writes to the RAW variables) ---
    public void OnMovement(InputAction.CallbackContext ctx) => _rawMovementInput = ctx.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext ctx) => _rawLookInput = ctx.ReadValue<Vector2>();
    public void OnJump(InputAction.CallbackContext ctx) { if (ctx.performed) _rawJumpPressed = true; }
    public void OnScrollCamera(InputAction.CallbackContext ctx) => _rawScrollInput = ctx.ReadValue<Vector2>().y * _scrollSensitivity; 
    
    public void OnToggleSprint(InputAction.CallbackContext ctx) 
    { 
        if (ctx.performed) 
        {
            SprintToggledOn = true; 
            WalkToggledOn = false; 
        }
        else if (ctx.canceled)
        {
            SprintToggledOn = false;
        }
    }
    
    public void OnToggleWalk(InputAction.CallbackContext ctx) { if (ctx.performed) WalkToggledOn = !WalkToggledOn; }
}