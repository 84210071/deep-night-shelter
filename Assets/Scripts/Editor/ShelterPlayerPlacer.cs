using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Places a first-person Player in the open scene without touching Greybox.
/// Menu: Tools / Deep Night Shelter / Place First Person Player
/// </summary>
public static class ShelterPlayerPlacer
{
    const string PlayerName = "Player";
    const string InputAssetPath = "Assets/Input/PlayerControls.inputactions";
    const float EyeHeight = 1.65f;
    const float BodyHeight = 1.8f;
    const float BodyRadius = 0.32f;

    [MenuItem("Tools/Deep Night Shelter/Place First Person Player")]
    public static void PlaceFirstPersonPlayer()
    {
        InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        if (input == null)
        {
            Debug.LogError("Missing Input Action Asset at " + InputAssetPath);
            return;
        }

        Undo.SetCurrentGroupName("Place First Person Player");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            player = new GameObject(PlayerName);
            Undo.RegisterCreatedObjectUndo(player, "Place First Person Player");
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<CharacterController>(player);
        }

        controller.height = BodyHeight;
        controller.radius = BodyRadius;
        controller.center = new Vector3(0f, BodyHeight * 0.5f, 0f);
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0f;

        EnsureBody(player.transform);
        Transform cameraPivot = EnsureChild(player.transform, "CameraPivot");
        cameraPivot.localPosition = new Vector3(0f, EyeHeight, 0f);
        cameraPivot.localRotation = Quaternion.identity;

        Camera playerCamera = AdoptOrCreateCamera(cameraPivot);
        Transform interaction = EnsureChild(playerCamera.transform, "InteractionOrigin");
        interaction.localPosition = Vector3.zero;
        interaction.localRotation = Quaternion.identity;

        FirstPersonController fps = player.GetComponent<FirstPersonController>();
        if (fps == null)
        {
            fps = Undo.AddComponent<FirstPersonController>(player);
        }

        SerializedObject so = new SerializedObject(fps);
        so.FindProperty("mouseSensitivity").floatValue = 0.12f;
        so.FindProperty("minPitch").floatValue = -80f;
        so.FindProperty("maxPitch").floatValue = 80f;
        so.FindProperty("eyeHeight").floatValue = EyeHeight;
        so.FindProperty("walkSpeed").floatValue = 3.2f;
        so.FindProperty("cameraPivot").objectReferenceValue = cameraPivot;
        so.FindProperty("playerCamera").objectReferenceValue = playerCamera;
        so.FindProperty("interactionOrigin").objectReferenceValue = interaction;
        so.FindProperty("inputActions").objectReferenceValue = input;
        so.ApplyModifiedPropertiesWithoutUndo();

        Vector3 spawnPosition = new Vector3(0.3f, 0f, 1.4f);
        Quaternion spawnRotation = Quaternion.identity;
        GameObject start = GameObject.Find("PlayerStart");
        if (start != null)
        {
            spawnPosition = start.transform.position;
            spawnRotation = Quaternion.Euler(0f, start.transform.eulerAngles.y, 0f);
        }

        player.transform.position = spawnPosition;
        player.transform.rotation = spawnRotation;

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = player;
        Debug.Log("Deep Night Shelter: first-person Player placed at " + spawnPosition + ". Play to walk with WASD and look with the mouse.");
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Place First Person Player");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsureBody(Transform player)
    {
        Transform body = player.Find("Body");
        if (body == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Undo.RegisterCreatedObjectUndo(capsule, "Place First Person Player");
            capsule.name = "Body";
            capsule.transform.SetParent(player, false);
            body = capsule.transform;
        }

        // Default capsule is 2m tall with radius 0.5. Fit the CharacterController.
        body.localPosition = new Vector3(0f, BodyHeight * 0.5f, 0f);
        body.localRotation = Quaternion.identity;
        body.localScale = new Vector3(BodyRadius * 2f, BodyHeight * 0.5f, BodyRadius * 2f);

        CapsuleCollider extraCollider = body.GetComponent<CapsuleCollider>();
        if (extraCollider != null)
        {
            Undo.DestroyObjectImmediate(extraCollider);
        }

        MeshRenderer renderer = body.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static Camera AdoptOrCreateCamera(Transform cameraPivot)
    {
        Camera cam = cameraPivot.GetComponentInChildren<Camera>(true);
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Place First Person Player");
            cam = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";
        }

        Undo.SetTransformParent(cam.transform, cameraPivot, "Place First Person Player");
        cam.gameObject.name = "Main Camera";
        cam.tag = "MainCamera";
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
        cam.transform.localScale = Vector3.one;
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 150f;

        Behaviour brain = cam.GetComponent("CinemachineBrain") as Behaviour;
        if (brain != null)
        {
            brain.enabled = false;
        }

        return cam;
    }
}
