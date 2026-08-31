using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Stable first-person locomotion and look for Deep Night Shelter.
/// Body yaw on the Player transform; pitch only on CameraPivot. No roll, bob, or Cinemachine.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    const string PlayerMapName = "Player";
    const string MoveActionName = "Move";
    const string LookActionName = "Look";

    [Header("Look")]
    [SerializeField] float mouseSensitivity = 0.12f;
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;
    [SerializeField] float eyeHeight = 1.65f;

    [Header("Move")]
    [SerializeField] float walkSpeed = 3.2f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float groundedStickVelocity = -2f;

    [Header("References")]
    [SerializeField] Transform cameraPivot;
    [SerializeField] Camera playerCamera;
    [SerializeField] Transform interactionOrigin;
    [SerializeField] InputActionAsset inputActions;

    CharacterController _controller;
    InputActionMap _playerMap;
    InputAction _moveAction;
    InputAction _lookAction;
    float _pitch;
    float _verticalVelocity;

    public Transform InteractionOrigin => interactionOrigin;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        ResolveActions();
        EnsureHierarchy();
        ApplyCameraDefaults();
    }

    void OnEnable()
    {
        if (_playerMap != null)
        {
            _playerMap.Enable();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        if (_playerMap != null)
        {
            _playerMap.Disable();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        if (_lookAction == null || cameraPivot == null)
        {
            return;
        }

        Vector2 look = _lookAction.ReadValue<Vector2>();
        transform.Rotate(0f, look.x * mouseSensitivity, 0f, Space.World);

        _pitch = Mathf.Clamp(_pitch - look.y * mouseSensitivity, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void HandleMove()
    {
        Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 planar = transform.right * move.x + transform.forward * move.y;
        planar.y = 0f;
        if (planar.sqrMagnitude > 1f)
        {
            planar.Normalize();
        }

        if (_controller.isGrounded)
        {
            _verticalVelocity = groundedStickVelocity;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = planar * walkSpeed;
        velocity.y = _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    void ResolveActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("FirstPersonController is missing PlayerControls InputActionAsset.", this);
            return;
        }

        _playerMap = inputActions.FindActionMap(PlayerMapName, true);
        _moveAction = _playerMap.FindAction(MoveActionName, true);
        _lookAction = _playerMap.FindAction(LookActionName, true);
    }

    void EnsureHierarchy()
    {
        if (cameraPivot == null)
        {
            Transform existing = transform.Find("CameraPivot");
            cameraPivot = existing != null ? existing : new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(transform, false);
        }

        cameraPivot.localPosition = new Vector3(0f, eyeHeight, 0f);
        cameraPivot.localRotation = Quaternion.identity;

        if (playerCamera == null)
        {
            playerCamera = cameraPivot.GetComponentInChildren<Camera>(true);
        }

        if (playerCamera == null)
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                playerCamera = sceneCamera;
            }
        }

        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(cameraPivot, false);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
            playerCamera.transform.localScale = Vector3.one;
        }

        if (interactionOrigin == null)
        {
            Transform existingOrigin = cameraPivot.Find("InteractionOrigin");
            if (existingOrigin == null && playerCamera != null)
            {
                existingOrigin = playerCamera.transform.Find("InteractionOrigin");
            }

            if (existingOrigin == null)
            {
                existingOrigin = new GameObject("InteractionOrigin").transform;
                Transform parent = playerCamera != null ? playerCamera.transform : cameraPivot;
                existingOrigin.SetParent(parent, false);
            }

            interactionOrigin = existingOrigin;
        }

        interactionOrigin.localPosition = Vector3.zero;
        interactionOrigin.localRotation = Quaternion.identity;
    }

    void ApplyCameraDefaults()
    {
        if (playerCamera == null)
        {
            return;
        }

        playerCamera.fieldOfView = 70f;
        playerCamera.nearClipPlane = 0.05f;
        playerCamera.farClipPlane = 150f;

        Behaviour brain = playerCamera.GetComponent("CinemachineBrain") as Behaviour;
        if (brain != null)
        {
            brain.enabled = false;
        }
    }
}
