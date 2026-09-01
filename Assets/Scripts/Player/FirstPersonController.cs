using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person over-the-shoulder locomotion for Deep Night Shelter.
/// Reuses the existing Input System Move/Look map and CharacterController.
/// Class name is kept so the scene Player does not lose its component.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    const string PlayerMapName = "Player";
    const string MoveActionName = "Move";
    const string LookActionName = "Look";

    [Header("Look")]
    [SerializeField] float mouseSensitivity = 0.12f;
    [SerializeField] float minPitch = -35f;
    [SerializeField] float maxPitch = 65f;

    [Header("Camera")]
    [SerializeField] float cameraHeight = 1.55f;
    [SerializeField] Vector3 cameraOffset = new Vector3(0.55f, 0.15f, -3.0f);
    [SerializeField] float cameraCollisionRadius = 0.18f;
    [SerializeField] float cameraMinDistance = 0.45f;
    [SerializeField] float cameraCollisionBuffer = 0.08f;
    [SerializeField] LayerMask cameraCollisionMask = ~0;
    [SerializeField] float cameraFov = 65f;

    [Header("Move")]
    [SerializeField] float walkSpeed = 3.2f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float groundedStickVelocity = -2f;
    [SerializeField] float turnSpeed = 10f;

    [Header("References")]
    [SerializeField] Transform cameraPivot;
    [SerializeField] Camera playerCamera;
    [SerializeField] Transform interactionOrigin;
    [SerializeField] Transform visualRoot;
    [SerializeField] InputActionAsset inputActions;

    CharacterController _controller;
    InputActionMap _playerMap;
    InputAction _moveAction;
    InputAction _lookAction;
    float _cameraYaw;
    float _pitch;
    float _verticalVelocity;
    float _currentCameraDistance;
    bool _cursorLocked = true;

    public Transform InteractionOrigin => interactionOrigin;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        ApplyLegacyDefaults();
        ResolveActions();
        EnsureHierarchy();
        ApplyCameraDefaults();
        _cameraYaw = transform.eulerAngles.y;
        _currentCameraDistance = Mathf.Abs(cameraOffset.z);
    }

    void OnEnable()
    {
        if (_playerMap != null)
        {
            _playerMap.Enable();
        }

        SetCursorLocked(true);
    }

    void OnDisable()
    {
        if (_playerMap != null)
        {
            _playerMap.Disable();
        }

        SetCursorLocked(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(!_cursorLocked);
        }

        HandleLook();
        HandleMove();
    }

    void LateUpdate()
    {
        UpdateCamera();
    }

    void ApplyLegacyDefaults()
    {
        if (minPitch < -50f)
        {
            minPitch = -35f;
        }

        if (maxPitch > 70f)
        {
            maxPitch = 65f;
        }

        if (cameraOffset.sqrMagnitude < 0.01f)
        {
            cameraOffset = new Vector3(0.55f, 0.15f, -3.0f);
        }
    }

    void HandleLook()
    {
        if (_lookAction == null || !_cursorLocked)
        {
            return;
        }

        Vector2 look = _lookAction.ReadValue<Vector2>();
        _cameraYaw += look.x * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - look.y * mouseSensitivity, minPitch, maxPitch);
    }

    void HandleMove()
    {
        Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 planar = CameraPlanarForward() * move.y + CameraPlanarRight() * move.x;
        planar.y = 0f;
        if (planar.sqrMagnitude > 1f)
        {
            planar.Normalize();
        }

        if (planar.sqrMagnitude > 0.0001f && visualRoot != null)
        {
            Quaternion target = Quaternion.LookRotation(planar, Vector3.up);
            visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, target, turnSpeed * Time.deltaTime);
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

    Vector3 CameraPlanarForward()
    {
        Vector3 forward = Quaternion.Euler(0f, _cameraYaw, 0f) * Vector3.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    Vector3 CameraPlanarRight()
    {
        Vector3 right = Quaternion.Euler(0f, _cameraYaw, 0f) * Vector3.right;
        right.y = 0f;
        return right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
    }

    void UpdateCamera()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 pivot = transform.position + Vector3.up * cameraHeight;
        Quaternion orbit = Quaternion.Euler(_pitch, _cameraYaw, 0f);
        Vector3 desiredOffset = orbit * cameraOffset;
        Vector3 desiredPosition = pivot + desiredOffset;
        Vector3 toCamera = desiredPosition - pivot;
        float desiredDistance = toCamera.magnitude;
        Vector3 direction = desiredDistance > 0.001f ? toCamera / desiredDistance : orbit * Vector3.back;
        float distance = desiredDistance;

        RaycastHit[] hits = Physics.SphereCastAll(
            pivot + direction * 0.15f,
            cameraCollisionRadius,
            direction,
            Mathf.Max(0.01f, desiredDistance - 0.15f),
            cameraCollisionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == null || hit.transform.IsChildOf(transform) || hit.collider == _controller)
            {
                continue;
            }

            float hitDistance = Mathf.Max(cameraMinDistance, hit.distance + 0.15f - cameraCollisionBuffer);
            if (hitDistance < distance)
            {
                distance = hitDistance;
            }
        }

        _currentCameraDistance = distance;
        Vector3 cameraPosition = pivot + direction * _currentCameraDistance;
        playerCamera.transform.position = cameraPosition;
        playerCamera.transform.rotation = Quaternion.LookRotation((pivot + Vector3.up * 0.1f) - cameraPosition, Vector3.up);

        if (interactionOrigin != null)
        {
            interactionOrigin.position = playerCamera.transform.position;
            interactionOrigin.rotation = playerCamera.transform.rotation;
        }
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

        cameraPivot.localPosition = new Vector3(0f, cameraHeight, 0f);
        cameraPivot.localRotation = Quaternion.identity;

        if (visualRoot == null)
        {
            Transform model = transform.Find("Model");
            visualRoot = model != null ? model : transform.Find("Body");
        }

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
            playerCamera.transform.SetParent(transform, true);
        }

        if (interactionOrigin == null)
        {
            Transform existingOrigin = transform.Find("InteractionOrigin");
            if (existingOrigin == null)
            {
                existingOrigin = new GameObject("InteractionOrigin").transform;
                existingOrigin.SetParent(transform, false);
            }

            interactionOrigin = existingOrigin;
        }
    }

    void ApplyCameraDefaults()
    {
        if (playerCamera == null)
        {
            return;
        }

        playerCamera.fieldOfView = cameraFov;
        playerCamera.nearClipPlane = 0.05f;
        playerCamera.farClipPlane = 150f;

        Behaviour brain = playerCamera.GetComponent("CinemachineBrain") as Behaviour;
        if (brain != null)
        {
            brain.enabled = false;
        }
    }

    void SetCursorLocked(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
