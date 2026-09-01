using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Historical one-shot to place or upgrade the Player. No Editor menu.
/// Player already exists in SampleScene; edit that instance instead.
/// </summary>
public static class ShelterPlayerPlacer
{
    const string PlayerName = "Player";
    const string InputAssetPath = "Assets/Input/PlayerControls.inputactions";
    const string PlayerModelPath = "Assets/Model/PlayerModel.fbx";
    const float EyeHeight = 1.55f;
    const float BodyHeight = 1.8f;
    const float BodyRadius = 0.32f;

    public static void PlaceThirdPersonPlayer()
    {
        InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        if (input == null)
        {
            Debug.LogError("Missing Input Action Asset at " + InputAssetPath);
            return;
        }

        Undo.SetCurrentGroupName("Place Third Person Player");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            player = new GameObject(PlayerName);
            Undo.RegisterCreatedObjectUndo(player, "Place Third Person Player");
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<CharacterController>(player);
        }

        controller.height = BodyHeight;
        controller.radius = BodyRadius;
        controller.center = new Vector3(0f, BodyHeight * 0.5f, 0f);
        controller.slopeLimit = 50f;
        controller.stepOffset = 0.32f;
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0f;

        EnsureBody(player.transform);
        EnsurePlayerModel(player.transform);
        Transform cameraPivot = EnsureChild(player.transform, "CameraPivot");
        cameraPivot.localPosition = new Vector3(0f, EyeHeight, 0f);
        cameraPivot.localRotation = Quaternion.identity;

        Camera playerCamera = AdoptOrCreateCamera(player.transform);
        Transform interaction = EnsureChild(player.transform, "InteractionOrigin");
        interaction.localPosition = new Vector3(0f, EyeHeight, 0f);
        interaction.localRotation = Quaternion.identity;

        FirstPersonController fps = player.GetComponent<FirstPersonController>();
        if (fps == null)
        {
            fps = Undo.AddComponent<FirstPersonController>(player);
        }

        Transform visual = player.transform.Find("Model");
        if (visual == null)
        {
            visual = player.transform.Find("Body");
        }

        SerializedObject so = new SerializedObject(fps);
        so.FindProperty("mouseSensitivity").floatValue = 0.12f;
        so.FindProperty("minPitch").floatValue = -35f;
        so.FindProperty("maxPitch").floatValue = 65f;
        so.FindProperty("cameraHeight").floatValue = EyeHeight;
        so.FindProperty("cameraOffset").vector3Value = new Vector3(0.55f, 0.15f, -3.0f);
        so.FindProperty("cameraCollisionRadius").floatValue = 0.18f;
        so.FindProperty("cameraMinDistance").floatValue = 0.45f;
        so.FindProperty("cameraFov").floatValue = 65f;
        so.FindProperty("walkSpeed").floatValue = 3.2f;
        so.FindProperty("turnSpeed").floatValue = 10f;
        so.FindProperty("cameraPivot").objectReferenceValue = cameraPivot;
        so.FindProperty("playerCamera").objectReferenceValue = playerCamera;
        so.FindProperty("interactionOrigin").objectReferenceValue = interaction;
        so.FindProperty("visualRoot").objectReferenceValue = visual;
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
        Debug.Log("Deep Night Shelter: third-person Player placed at " + spawnPosition + ". WASD moves relative to camera, mouse looks, ESC unlocks cursor.");
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Place Third Person Player");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsureBody(Transform player)
    {
        Transform body = player.Find("Body");
        if (body == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Undo.RegisterCreatedObjectUndo(capsule, "Place Third Person Player");
            capsule.name = "Body";
            capsule.transform.SetParent(player, false);
            body = capsule.transform;
        }

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
            renderer.enabled = player.Find("Model") == null;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static void EnsurePlayerModel(Transform player)
    {
        Transform model = player.Find("Model");
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        if (source == null)
        {
            return;
        }

        if (model == null)
        {
            GameObject instance;
            if (PrefabUtility.GetPrefabAssetType(source) != PrefabAssetType.NotAPrefab)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            }
            else
            {
                instance = Object.Instantiate(source);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Place Third Person Player");
            instance.name = "Model";
            instance.transform.SetParent(player, false);
            model = instance.transform;
        }

        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;

        Bounds bounds = EncapsulateWorldBounds(model.gameObject);
        float height = Mathf.Max(0.01f, bounds.size.y);
        model.localScale = Vector3.one * (BodyHeight / height);

        bounds = EncapsulateWorldBounds(model.gameObject);
        Vector3 world = model.position;
        model.position = new Vector3(
            world.x + (player.position.x - bounds.center.x),
            world.y + (player.position.y - bounds.min.y),
            world.z + (player.position.z - bounds.center.z));

        Transform body = player.Find("Body");
        if (body != null)
        {
            MeshRenderer renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    static Bounds EncapsulateWorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        bool init = false;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!init)
            {
                b = renderers[i].bounds;
                init = true;
            }
            else
            {
                b.Encapsulate(renderers[i].bounds);
            }
        }

        return b;
    }

    static Camera AdoptOrCreateCamera(Transform player)
    {
        Camera cam = player.GetComponentInChildren<Camera>(true);
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Place Third Person Player");
            cam = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";
        }

        Undo.SetTransformParent(cam.transform, player, "Place Third Person Player");
        cam.gameObject.name = "Main Camera";
        cam.tag = "MainCamera";
        cam.fieldOfView = 65f;
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
