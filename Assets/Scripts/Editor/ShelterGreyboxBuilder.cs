using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DEPRECATED. Scene layout is now edited directly in Unity Scene.
/// No Editor menu. Do not rebuild production levels.
/// Historical first-floor greybox generator for Deep Night Shelter.
///
/// World: +X east, +Y up, +Z north. Walkable floor top at Y = 0. Storey height 3m.
///
/// L-shaped plan so the lobby cannot see the Director Office:
///
///                         [Pharmacy 5x5]
///                              |
///   [NurseStation 4x4] --- [Rear corridor] --- [DirectorOffice 7x6]
///          |
///   [Front corridor]  [Ward03 6x5]
///          |          [Ward02 5x6 south of rear]
///      [Ward01 6x6]
///          |
///   [Lobby 10x9] -- [B1]
///
/// Inner volumes:
///   Lobby            X[-4.0, 6.0]   Z[0.0, 9.0]     10 x 9
///   Front corridor    X[-2.2, 1.0]   Z[9.0, 17.5]    3.2 x 8.5
///   NurseStation      X[-3.0, 1.0]   Z[17.5, 21.5]   4 x 4
///   Rear corridor     X[1.0, 12.5]   Z[18.3, 21.5]   11.5 x 3.2
///   Ward01            X[-8.4, -2.4]  Z[9.7, 15.7]    6 x 6   west of front
///   Ward03            X[1.2, 7.2]     Z[12.3, 17.3]   6 x 5   east of front, staggered north
///   Ward02            X[7.4, 12.3]    Z[12.1, 18.1]   ~5 x 6  south of rear
///   Pharmacy          X[2.4, 7.4]     Z[21.7, 26.7]   5 x 5   north of rear
///   Director Office   X[12.7, 19.7]  Z[16.9, 22.9]   7 x 6
///   B1 Entrance        X[6.2, 9.0]    Z[5.6, 8.6]     ~3 x 3  lobby east, north side
/// </summary>
public static partial class ShelterGreyboxBuilder
{
    static string _undoName = "Build First Floor Greybox";
    const string MaterialFolder = "Assets/Materials/Greybox";
    static float _levelY;

    const float WallThickness = 0.2f;
    const float WallHeight = 3.0f;
    const float SlabThickness = 0.2f;
    const float DoorWidth = 1.1f;
    const float DoorHeight = 2.2f;
    const float DoubleDoorWidth = 2.2f;
    const float PharmacyDoorWidth = 0.9f;
    const float PlaceholderThickness = 0.06f;

    const float FloorCenterY = -SlabThickness * 0.5f;
    const float CeilingCenterY = WallHeight + SlabThickness * 0.5f;
    const float WallCenterY = WallHeight * 0.5f;

    // Lobby 10 x 9, extra width on the east (waiting), corridor mouth offset west.
    const float LobbyXMin = -4.0f;
    const float LobbyXMax = 6.0f;
    const float LobbyZMin = 0.0f;
    const float LobbyZMax = 9.0f;
    const float LobbyMainDoorX = 0.0f;

    // Front corridor 3.2m, north from the west-of-center lobby mouth.
    const float FrontXMin = -2.2f;
    const float FrontXMax = 1.0f;
    const float FrontZMin = 9.0f;
    const float FrontZMax = 17.5f;

    // 4 x 4 junction: expands west of the front corridor so the turn is a real node.
    const float NodeXMin = -3.0f;
    const float NodeXMax = 1.0f;
    const float NodeZMin = 17.5f;
    const float NodeZMax = 21.5f;

    // Rear corridor 3.2m, east from the north half of the node.
    const float RearXMin = 1.0f;
    const float RearXMax = 12.5f;
    const float RearZMin = 18.3f;
    const float RearZMax = 21.5f;

    const float Ward01XMin = -8.4f;
    const float Ward01XMax = -2.4f;
    const float Ward01ZMin = 9.7f;
    const float Ward01ZMax = 15.7f;
    const float Ward01DoorZ = 11.0f;

    const float Ward03XMin = 1.2f;
    const float Ward03XMax = 7.2f;
    const float Ward03ZMin = 12.3f;
    const float Ward03ZMax = 17.3f;
    const float Ward03DoorZ = 16.2f;

    const float Ward02XMin = 7.4f;
    const float Ward02XMax = 12.3f;
    const float Ward02ZMin = 12.1f;
    const float Ward02ZMax = 18.1f;
    const float Ward02DoorX = 10.7f;

    const float PharmacyXMin = 2.4f;
    const float PharmacyXMax = 7.4f;
    const float PharmacyZMin = 21.7f;
    const float PharmacyZMax = 26.7f;
    const float PharmacyDoorX = 3.35f;

    const float OfficeXMin = 12.7f;
    const float OfficeXMax = 19.7f;
    const float OfficeZMin = 16.9f;
    const float OfficeZMax = 22.9f;
    const float OfficeDoorZ = 19.9f;

    const float B1XMin = 6.2f;
    const float B1XMax = 9.0f;
    const float B1ZMin = 5.6f;
    const float B1ZMax = 8.6f;
    const float B1DoorZ = 7.2f;

    const string PathBed = "Assets/Model/HospitalBed.fbx";
    const string PathCabinet = "Assets/Model/BedsideCabinet.fbx";
    const string PathWardDoor = "Assets/Model/Door_Ward.fbx";
    const string PathPharmacyDoor = "Assets/Model/Door_Pharmacy.fbx";
    const string PathOfficeDoor = "Assets/Model/Door_DirectorOffice.fbx";

    const float TargetBedLength = 2.1f;
    const float TargetBedWidth = 0.95f;
    const float TargetCabinetWidth = 0.45f;
    const float TargetDoorHeight = 2.1f;
    const float DoorOpeningClearanceW = 0.04f;
    const float DoorOpeningClearanceH = 0.03f;

    static Material _floorMat;
    static Material _wallMat;
    static Material _ceilingMat;
    static Material _doorMat;
    static Material _lockedDoorMat;

    static FittedDoor _wardDoor;
    static FittedDoor _pharmacyDoor;
    static FittedDoor _officeDoor;
    static FittedProp _bed;
    static FittedProp _cabinet;

    enum AttachSide
    {
        West,
        East,
        South,
        North
    }

    // No Editor menu. Production layout lives in SampleScene; do not rebuild.
    public static void BuildFirstFloorGreybox()
    {
        if (BlockDeprecatedRebuild("Build First Floor Greybox"))
        {
            return;
        }

        _undoName = "Build First Floor Greybox";
        _levelY = 0f;
        Undo.SetCurrentGroupName(_undoName);
        int undoGroup = Undo.GetCurrentGroup();

        EnsureMaterials();
        ResolveImportedModels();

        float wardW = OpeningWidth(_wardDoor, DoorWidth);
        float wardH = OpeningHeight(_wardDoor, DoorHeight);
        float pharmW = OpeningWidth(_pharmacyDoor, PharmacyDoorWidth);
        float pharmH = OpeningHeight(_pharmacyDoor, DoorHeight);
        float officeW = OpeningWidth(_officeDoor, DoorWidth);
        float officeH = OpeningHeight(_officeDoor, DoorHeight);

        ClearPreviousGreybox();

        Transform environment = FindOrCreateRoot("Environment");
        Transform greybox = CreateGroup("Greybox", environment);

        BuildLobby(greybox);
        BuildMainCorridor(greybox, wardW, wardH, pharmW, pharmH);
        BuildNurseStation(greybox);

        Transform ward01 = BuildSideRoom("Ward01", greybox, Ward01XMin, Ward01XMax, Ward01ZMin, Ward01ZMax, AttachSide.West, Ward01DoorZ, wardW, FrontXMin);
        Transform ward02 = BuildSideRoom("Ward02", greybox, Ward02XMin, Ward02XMax, Ward02ZMin, Ward02ZMax, AttachSide.South, Ward02DoorX, wardW, RearZMin);
        Transform ward03 = BuildSideRoom("Ward03", greybox, Ward03XMin, Ward03XMax, Ward03ZMin, Ward03ZMax, AttachSide.East, Ward03DoorZ, wardW, FrontXMax);
        Transform pharmacy = BuildSideRoom("Pharmacy", greybox, PharmacyXMin, PharmacyXMax, PharmacyZMin, PharmacyZMax, AttachSide.North, PharmacyDoorX, pharmW, RearZMax);
        Transform office = BuildDirectorOffice(greybox, officeW, officeH);
        BuildB1Entrance(greybox);
        BuildStairwellTo2FRoom(greybox);
        BuildStairwellToB1Room(greybox);

        PlaceRoomDoor(ward01, "Ward01_DoorRoot", AttachSide.West, Ward01DoorZ, Ward01XMin, Ward01XMax, Ward01ZMin, Ward01ZMax, _wardDoor);
        PlaceRoomDoor(ward02, "Ward02_DoorRoot", AttachSide.South, Ward02DoorX, Ward02XMin, Ward02XMax, Ward02ZMin, Ward02ZMax, _wardDoor);
        PlaceRoomDoor(ward03, "Ward03_DoorRoot", AttachSide.East, Ward03DoorZ, Ward03XMin, Ward03XMax, Ward03ZMin, Ward03ZMax, _wardDoor);
        PlaceRoomDoor(pharmacy, "Pharmacy_DoorRoot", AttachSide.North, PharmacyDoorX, PharmacyXMin, PharmacyXMax, PharmacyZMin, PharmacyZMax, _pharmacyDoor);
        PlaceRoomDoor(office, "DirectorOffice_DoorRoot", AttachSide.East, OfficeDoorZ, OfficeXMin, OfficeXMax, OfficeZMin, OfficeZMax, _officeDoor);

        PlaceWard01Furniture(ward01);
        PlaceWard02Furniture(ward02);
        PlaceWard03Furniture(ward03);

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = greybox.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log(
            "Deep Night Shelter: first-floor greybox rebuilt with Meshy props. " +
            DescribeFit(_wardDoor, "Ward door") + " " +
            DescribeFit(_pharmacyDoor, "Pharmacy door") + " " +
            DescribeFit(_officeDoor, "Office door") + " " +
            DescribeFit(_bed, "Bed") + " " +
            DescribeFit(_cabinet, "Cabinet"));
    }

    static bool BlockDeprecatedRebuild(string commandName)
    {
        EditorUtility.DisplayDialog(
            "Builder deprecated",
            commandName + " is deprecated.\n\n" +
            "Production layout lives in SampleScene (Environment/Greybox, SecondFloor, Basement).\n" +
            "Edit Transform in the Scene. Do not rebuild floors.\n\n" +
            "This command was blocked so it cannot wipe the saved level.",
            "OK");
        Debug.LogWarning("Deep Night Shelter: " + commandName + " is deprecated and was blocked. Edit the Scene instead.");
        return true;
    }

    static void ClearPreviousGreybox()
    {
        GameObject environment = GameObject.Find("Environment");
        if (environment == null)
        {
            return;
        }

        Transform greybox = environment.transform.Find("Greybox");
        if (greybox != null)
        {
            Undo.DestroyObjectImmediate(greybox.gameObject);
        }
    }

    static void BuildLobby(Transform greybox)
    {
        Transform lobby = CreateArea(greybox, "Lobby");
        Transform floor = lobby.Find("Floor");
        Transform ceiling = lobby.Find("Ceiling");
        Transform walls = lobby.Find("Walls");
        Transform doorways = lobby.Find("Doorways");

        CreateSlab(floor, "Lobby_Floor", LobbyXMin, LobbyXMax, LobbyZMin, LobbyZMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "Lobby_Ceiling", LobbyXMin, LobbyXMax, LobbyZMin, LobbyZMax, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "Lobby_Floor_CorridorJoin", FrontXMin, FrontXMax, LobbyZMax - 0.02f, LobbyZMax + 0.02f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "Lobby_Ceiling_CorridorJoin", FrontXMin, FrontXMax, LobbyZMax - 0.02f, LobbyZMax + 0.02f, CeilingCenterY, _ceilingMat);

        BuildWallAlongX(
            walls, doorways, "Lobby_Wall_South",
            LobbyXMin, LobbyXMax, LobbyZMin, -1f, coverCorners: true,
            Cut("Lobby_Door_MainEntrance", LobbyMainDoorX, DoubleDoorWidth, DoorHeight));

        PlaceInvisibleBlocker(
            doorways,
            "Lobby_MainEntrance_Blocker",
            true,
            LobbyMainDoorX,
            LobbyZMin - WallThickness * 0.5f,
            DoubleDoorWidth,
            DoorHeight);

        BuildWallAlongZ(
            walls, doorways, "Lobby_Wall_West",
            LobbyZMin, LobbyZMax, LobbyXMin, -1f, coverCorners: false,
            Cut("Lobby_Door_StairwellTo2F", Stair2DoorZ, Stair2DoorW, DoorHeight));

        BuildWallAlongZ(
            walls, doorways, "Lobby_Wall_East",
            LobbyZMin, LobbyZMax, LobbyXMax, 1f, coverCorners: false,
            Cut("Lobby_Door_B1Entrance", B1DoorZ, DoorWidth, DoorHeight));

        BuildWallAlongX(
            walls, doorways, "Lobby_Wall_NorthWest",
            LobbyXMin, FrontXMin, LobbyZMax, 1f, coverCorners: false);
        ExtendWestCorner(walls, "Lobby_Wall_NorthWest_Corner", LobbyXMin, LobbyZMax, 1f);

        BuildWallAlongX(
            walls, doorways, "Lobby_Wall_NorthEast",
            FrontXMax, LobbyXMax, LobbyZMax, 1f, coverCorners: false);
        ExtendEastCorner(walls, "Lobby_Wall_NorthEast_Corner", LobbyXMax, LobbyZMax, 1f);

        Transform playerStart = CreateGroup("PlayerStart", lobby);
        playerStart.position = new Vector3(0.3f, 0f, LobbyZMin + 1.4f);
        playerStart.rotation = Quaternion.identity;

        Transform reception = CreateGroup("ReceptionDesk_Reserved", lobby);
        reception.position = new Vector3(-2.6f, 0f, 4.6f);

        Transform waiting = CreateGroup("WaitingArea_Reserved", lobby);
        waiting.position = new Vector3(3.6f, 0f, 4.0f);
    }

    static void BuildMainCorridor(Transform greybox, float wardW, float wardH, float pharmW, float pharmH)
    {
        Transform corridor = CreateArea(greybox, "MainCorridor");
        Transform floor = corridor.Find("Floor");
        Transform ceiling = corridor.Find("Ceiling");
        Transform walls = corridor.Find("Walls");
        Transform doorways = corridor.Find("Doorways");

        CreateSlab(floor, "MainCorridor_Front_Floor", FrontXMin, FrontXMax, FrontZMin, FrontZMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "MainCorridor_Front_Ceiling", FrontXMin, FrontXMax, FrontZMin, FrontZMax, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "MainCorridor_Rear_Floor", RearXMin, RearXMax, RearZMin, RearZMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "MainCorridor_Rear_Ceiling", RearXMin, RearXMax, RearZMin, RearZMax, CeilingCenterY, _ceilingMat);

        CreateSlab(floor, "MainCorridor_Floor_NodeJoin", FrontXMin, FrontXMax, FrontZMax - 0.02f, FrontZMax + 0.02f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "MainCorridor_Ceiling_NodeJoin", FrontXMin, FrontXMax, FrontZMax - 0.02f, FrontZMax + 0.02f, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "MainCorridor_Floor_RearJoin", RearXMin - 0.02f, RearXMin + 0.02f, RearZMin, RearZMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "MainCorridor_Ceiling_RearJoin", RearXMin - 0.02f, RearXMin + 0.02f, RearZMin, RearZMax, CeilingCenterY, _ceilingMat);

        float frontWallZMin = FrontZMin + WallThickness;

        BuildWallAlongZ(
            walls, doorways, "MainCorridor_Front_Wall_West",
            frontWallZMin, FrontZMax, FrontXMin, -1f, coverCorners: false,
            Cut("MainCorridor_Door_Ward01", Ward01DoorZ, wardW, wardH));

        BuildWallAlongZ(
            walls, doorways, "MainCorridor_Front_Wall_East",
            frontWallZMin, FrontZMax, FrontXMax, 1f, coverCorners: false,
            Cut("MainCorridor_Door_Ward03", Ward03DoorZ, wardW, wardH));

        BuildWallAlongX(
            walls, doorways, "MainCorridor_Rear_Wall_South",
            RearXMin, RearXMax, RearZMin, -1f, coverCorners: false,
            Cut("MainCorridor_Door_Ward02", Ward02DoorX, wardW, wardH));

        BuildWallAlongX(
            walls, doorways, "MainCorridor_Rear_Wall_North",
            RearXMin, RearXMax, RearZMax, 1f, coverCorners: false,
            Cut("MainCorridor_Door_Pharmacy", PharmacyDoorX, pharmW, pharmH));
    }

    static void BuildNurseStation(Transform greybox)
    {
        Transform area = CreateArea(greybox, "NurseStation");
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "NurseStation_Floor", NodeXMin, NodeXMax, NodeZMin, NodeZMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "NurseStation_Ceiling", NodeXMin, NodeXMax, NodeZMin, NodeZMax, CeilingCenterY, _ceilingMat);

        // West pocket south stub: node is wider than the front corridor.
        BuildWallAlongX(
            walls, doorways, "NurseStation_Wall_SouthWest",
            NodeXMin, FrontXMin, NodeZMin, -1f, coverCorners: false);
        ExtendWestCorner(walls, "NurseStation_Wall_SouthWest_Corner", NodeXMin, NodeZMin, -1f);

        // East stub south of the rear-corridor mouth — this is the actual 90-degree return.
        BuildWallAlongZ(
            walls, doorways, "NurseStation_Wall_EastStub",
            NodeZMin, RearZMin, NodeXMax, 1f, coverCorners: false);

        BuildWallAlongZ(
            walls, doorways, "NurseStation_Wall_West",
            NodeZMin, NodeZMax, NodeXMin, -1f, coverCorners: false);

        BuildWallAlongX(
            walls, doorways, "NurseStation_Wall_North",
            NodeXMin, NodeXMax, NodeZMax, 1f, coverCorners: false);
        ExtendWestCorner(walls, "NurseStation_Wall_North_Corner", NodeXMin, NodeZMax, 1f);

        Transform reserved = CreateGroup("EventSpace_Reserved", area);
        reserved.position = new Vector3((NodeXMin + NodeXMax) * 0.5f, 0f, (NodeZMin + NodeZMax) * 0.5f);
        reserved.rotation = Quaternion.identity;
    }

    static Transform BuildSideRoom(
        string areaName,
        Transform greybox,
        float xMin, float xMax, float zMin, float zMax,
        AttachSide attach,
        float doorAlong,
        float doorWidth,
        float corridorInner)
    {
        Transform area = CreateArea(greybox, areaName);
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, areaName + "_Floor", xMin, xMax, zMin, zMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, areaName + "_Ceiling", xMin, xMax, zMin, zMax, CeilingCenterY, _ceilingMat);

        float halfDoor = doorWidth * 0.5f;

        if (attach == AttachSide.West)
        {
            CreateSlab(floor, areaName + "_Floor_DoorThreshold", xMax, corridorInner, doorAlong - halfDoor, doorAlong + halfDoor, FloorCenterY, _floorMat);
            CreateSlab(ceiling, areaName + "_Ceiling_DoorThreshold", xMax, corridorInner, doorAlong - halfDoor, doorAlong + halfDoor, CeilingCenterY, _ceilingMat);
            BuildWallAlongZ(walls, doorways, areaName + "_Wall_West", zMin, zMax, xMin, -1f, coverCorners: false);
            BuildWallAlongX(walls, doorways, areaName + "_Wall_South", xMin, xMax, zMin, -1f, coverCorners: false);
            BuildWallAlongX(walls, doorways, areaName + "_Wall_North", xMin, xMax, zMax, 1f, coverCorners: false);
            ExtendWestCorner(walls, areaName + "_Wall_South_Corner", xMin, zMin, -1f);
            ExtendWestCorner(walls, areaName + "_Wall_North_Corner", xMin, zMax, 1f);
        }
        else if (attach == AttachSide.East)
        {
            CreateSlab(floor, areaName + "_Floor_DoorThreshold", corridorInner, xMin, doorAlong - halfDoor, doorAlong + halfDoor, FloorCenterY, _floorMat);
            CreateSlab(ceiling, areaName + "_Ceiling_DoorThreshold", corridorInner, xMin, doorAlong - halfDoor, doorAlong + halfDoor, CeilingCenterY, _ceilingMat);
            BuildWallAlongZ(walls, doorways, areaName + "_Wall_East", zMin, zMax, xMax, 1f, coverCorners: false);
            BuildWallAlongX(walls, doorways, areaName + "_Wall_South", xMin, xMax, zMin, -1f, coverCorners: false);
            BuildWallAlongX(walls, doorways, areaName + "_Wall_North", xMin, xMax, zMax, 1f, coverCorners: false);
            ExtendEastCorner(walls, areaName + "_Wall_South_Corner", xMax, zMin, -1f);
            ExtendEastCorner(walls, areaName + "_Wall_North_Corner", xMax, zMax, 1f);
        }
        else if (attach == AttachSide.South)
        {
            CreateSlab(floor, areaName + "_Floor_DoorThreshold", doorAlong - halfDoor, doorAlong + halfDoor, zMax, corridorInner, FloorCenterY, _floorMat);
            CreateSlab(ceiling, areaName + "_Ceiling_DoorThreshold", doorAlong - halfDoor, doorAlong + halfDoor, zMax, corridorInner, CeilingCenterY, _ceilingMat);
            BuildWallAlongX(walls, doorways, areaName + "_Wall_South", xMin, xMax, zMin, -1f, coverCorners: false);
            BuildWallAlongZ(walls, doorways, areaName + "_Wall_West", zMin, zMax, xMin, -1f, coverCorners: false);
            BuildWallAlongZ(walls, doorways, areaName + "_Wall_East", zMin, zMax, xMax, 1f, coverCorners: false);
            ExtendWestCorner(walls, areaName + "_Wall_South_Corner", xMin, zMin, -1f);
            ExtendEastCorner(walls, areaName + "_Wall_South_CornerEast", xMax, zMin, -1f);
        }
        else
        {
            CreateSlab(floor, areaName + "_Floor_DoorThreshold", doorAlong - halfDoor, doorAlong + halfDoor, corridorInner, zMin, FloorCenterY, _floorMat);
            CreateSlab(ceiling, areaName + "_Ceiling_DoorThreshold", doorAlong - halfDoor, doorAlong + halfDoor, corridorInner, zMin, CeilingCenterY, _ceilingMat);
            BuildWallAlongX(walls, doorways, areaName + "_Wall_North", xMin, xMax, zMax, 1f, coverCorners: false);
            BuildWallAlongZ(walls, doorways, areaName + "_Wall_West", zMin, zMax, xMin, -1f, coverCorners: false);
            BuildWallAlongZ(walls, doorways, areaName + "_Wall_East", zMin, zMax, xMax, 1f, coverCorners: false);
            ExtendWestCorner(walls, areaName + "_Wall_North_Corner", xMin, zMax, 1f);
            ExtendEastCorner(walls, areaName + "_Wall_North_CornerEast", xMax, zMax, 1f);
        }

        return area;
    }

    static Transform BuildDirectorOffice(Transform greybox, float officeW, float officeH)
    {
        Transform area = CreateArea(greybox, "DirectorOffice");
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "DirectorOffice_Floor", OfficeXMin, OfficeXMax, OfficeZMin, OfficeZMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "DirectorOffice_Ceiling", OfficeXMin, OfficeXMax, OfficeZMin, OfficeZMax, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "DirectorOffice_Floor_DoorThreshold",
            RearXMax, OfficeXMin,
            OfficeDoorZ - officeW * 0.5f, OfficeDoorZ + officeW * 0.5f,
            FloorCenterY, _floorMat);
        CreateSlab(ceiling, "DirectorOffice_Ceiling_DoorThreshold",
            RearXMax, OfficeXMin,
            OfficeDoorZ - officeW * 0.5f, OfficeDoorZ + officeW * 0.5f,
            CeilingCenterY, _ceilingMat);

        BuildWallAlongZ(
            walls, doorways, "DirectorOffice_Wall_West",
            OfficeZMin, OfficeZMax, OfficeXMin, -1f, coverCorners: false,
            Cut("DirectorOffice_Door", OfficeDoorZ, officeW, officeH));

        BuildWallAlongX(walls, doorways, "DirectorOffice_Wall_South", OfficeXMin, OfficeXMax, OfficeZMin, -1f, coverCorners: true);
        BuildWallAlongX(walls, doorways, "DirectorOffice_Wall_North", OfficeXMin, OfficeXMax, OfficeZMax, 1f, coverCorners: true);
        BuildWallAlongZ(walls, doorways, "DirectorOffice_Wall_East", OfficeZMin, OfficeZMax, OfficeXMax, 1f, coverCorners: false);

        return area;
    }

    static void BuildB1Entrance(Transform greybox)
    {
        Transform area = CreateArea(greybox, "B1Entrance");
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "B1Entrance_Floor", B1XMin, B1XMax, B1ZMin, B1ZMax, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "B1Entrance_Ceiling", B1XMin, B1XMax, B1ZMin, B1ZMax, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "B1Entrance_Floor_DoorThreshold",
            LobbyXMax, B1XMin,
            B1DoorZ - DoorWidth * 0.5f, B1DoorZ + DoorWidth * 0.5f,
            FloorCenterY, _floorMat);
        CreateSlab(ceiling, "B1Entrance_Ceiling_DoorThreshold",
            LobbyXMax, B1XMin,
            B1DoorZ - DoorWidth * 0.5f, B1DoorZ + DoorWidth * 0.5f,
            CeilingCenterY, _ceilingMat);

        BuildWallAlongX(walls, doorways, "B1Entrance_Wall_South", B1XMin, B1XMax, B1ZMin, -1f, coverCorners: false);
        BuildWallAlongX(walls, doorways, "B1Entrance_Wall_North", B1XMin, B1XMax, B1ZMax, 1f, coverCorners: false);
        ExtendEastCorner(walls, "B1Entrance_Wall_South_Corner", B1XMax, B1ZMin, -1f);
        ExtendEastCorner(walls, "B1Entrance_Wall_North_Corner", B1XMax, B1ZMax, 1f);
        BuildWallAlongZ(
            walls, doorways, "B1Entrance_Wall_East",
            B1ZMin, B1ZMax, B1XMax, 1f, coverCorners: false,
            Cut("B1Entrance_Door_StairwellToB1", StairBDoorZ, StairBDoorW, DoorHeight));

        CreateSlab(floor, "B1Entrance_Floor_StairJoin", B1XMax, StairBXMin, StairBDoorZ - StairBDoorW * 0.5f, StairBDoorZ + StairBDoorW * 0.5f, FloorCenterY, _floorMat);

        Transform reserved = CreateGroup("B1_StairsDown_Reserved", area);
        reserved.position = new Vector3((B1XMin + B1XMax) * 0.5f, 0f, (B1ZMin + B1ZMax) * 0.5f);
        reserved.rotation = Quaternion.identity;
    }

    struct FittedDoor
    {
        public bool valid;
        public GameObject source;
        public float uniformScale;
        public Quaternion visualLocalRot;
        public Vector3 visualLocalPos;
        public Vector3 size;
        public float openingWidth;
        public float openingHeight;
        public string note;
    }

    struct FittedProp
    {
        public bool valid;
        public GameObject source;
        public float uniformScale;
        public Quaternion visualLocalRot;
        public Vector3 visualLocalPos;
        public Vector3 size;
        public string note;
    }

    static float OpeningWidth(FittedDoor fit, float fallback)
    {
        return fit.valid ? fit.openingWidth : fallback;
    }

    static float OpeningHeight(FittedDoor fit, float fallback)
    {
        return fit.valid ? fit.openingHeight : fallback;
    }

    static string DescribeFit(FittedDoor fit, string label)
    {
        if (!fit.valid)
        {
            return label + ": MISSING.";
        }

        return label + " scale=" + fit.uniformScale.ToString("0.###") +
               " size=" + fit.size.x.ToString("0.00") + "x" + fit.size.y.ToString("0.00") + "x" + fit.size.z.ToString("0.00") +
               " opening=" + fit.openingWidth.ToString("0.00") + "x" + fit.openingHeight.ToString("0.00") + ".";
    }

    static string DescribeFit(FittedProp fit, string label)
    {
        if (!fit.valid)
        {
            return label + ": MISSING.";
        }

        return label + " scale=" + fit.uniformScale.ToString("0.###") +
               " size=" + fit.size.x.ToString("0.00") + "x" + fit.size.y.ToString("0.00") + "x" + fit.size.z.ToString("0.00") + ".";
    }

    static void ResolveImportedModels()
    {
        _wardDoor = FitDoor(PathWardDoor, "Ward door");
        _pharmacyDoor = FitDoor(PathPharmacyDoor, "Pharmacy door");
        _officeDoor = FitDoor(PathOfficeDoor, "Director office door");
        _bed = FitBed(PathBed);
        _cabinet = FitCabinet(PathCabinet);

        if (!_wardDoor.valid) Debug.LogError("Deep Night Shelter: ward door FBX not found at " + PathWardDoor);
        if (!_pharmacyDoor.valid) Debug.LogError("Deep Night Shelter: pharmacy door FBX not found at " + PathPharmacyDoor);
        if (!_officeDoor.valid) Debug.LogError("Deep Night Shelter: director office door FBX not found at " + PathOfficeDoor);
        if (!_bed.valid) Debug.LogError("Deep Night Shelter: bed FBX not found at " + PathBed);
        if (!_cabinet.valid) Debug.LogError("Deep Night Shelter: cabinet FBX not found at " + PathCabinet);
    }

    static FittedDoor FitDoor(string path, string label)
    {
        FittedDoor fit = new FittedDoor();
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null)
        {
            return fit;
        }

        GameObject tmp = null;
        try
        {
            tmp = UnityEngine.Object.Instantiate(source);
            tmp.hideFlags = HideFlags.HideAndDontSave;
            tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            tmp.transform.localScale = Vector3.one;

            Bounds native = EncapsulateWorldBounds(tmp);
            Vector3 size = native.size;
            int thicknessAxis = SmallestAxis(size);
            int heightAxis = 1;
            if (heightAxis == thicknessAxis)
            {
                heightAxis = LargestAxis(size);
            }

            int widthAxis = 3 - thicknessAxis - heightAxis;
            float nativeHeight = size[heightAxis];
            float nativeWidth = size[widthAxis];
            float nativeThickness = size[thicknessAxis];
            if (nativeHeight < 0.001f)
            {
                return fit;
            }

            float scale = TargetDoorHeight / nativeHeight;
            float scaledWidth = nativeWidth * scale;
            float scaledHeight = nativeHeight * scale;
            float scaledThickness = nativeThickness * scale;

            Quaternion align = RotationMappingAxisTo(heightAxis, thicknessAxis);
            tmp.transform.rotation = align;
            tmp.transform.localScale = Vector3.one * scale;
            Bounds aligned = EncapsulateWorldBounds(tmp);

            fit.valid = true;
            fit.source = source;
            fit.uniformScale = scale;
            fit.visualLocalRot = align;
            fit.visualLocalPos = new Vector3(-aligned.min.x, -aligned.min.y, -aligned.center.z);
            fit.size = new Vector3(scaledWidth, scaledHeight, scaledThickness);
            fit.openingWidth = scaledWidth + DoorOpeningClearanceW;
            fit.openingHeight = Mathf.Min(WallHeight - 0.15f, scaledHeight + DoorOpeningClearanceH);
            fit.note = label + " native " + size + " -> scale " + scale;
            return fit;
        }
        finally
        {
            if (tmp != null)
            {
                UnityEngine.Object.DestroyImmediate(tmp);
            }
        }
    }

    static FittedProp FitBed(string path)
    {
        FittedProp fit = new FittedProp();
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null)
        {
            return fit;
        }

        GameObject tmp = UnityEngine.Object.Instantiate(source);
        tmp.hideFlags = HideFlags.HideAndDontSave;
        tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        tmp.transform.localScale = Vector3.one;

        Bounds native = EncapsulateWorldBounds(tmp);
        Vector3 size = native.size;
        int longAxis = LargestAxis(size);
        Quaternion align = Quaternion.identity;
        if (longAxis == 1)
        {
            align = Quaternion.Euler(90f, 0f, 0f);
            tmp.transform.rotation = align;
            native = EncapsulateWorldBounds(tmp);
            size = native.size;
        }

        if (size.x > size.z)
        {
            align = align * Quaternion.Euler(0f, -90f, 0f);
            tmp.transform.rotation = align;
            native = EncapsulateWorldBounds(tmp);
            size = native.size;
        }

        // Meshy bed imports with Y-down after length alignment; roll 180 so legs sit on the floor.
        align = align * Quaternion.Euler(0f, 0f, 180f);
        tmp.transform.rotation = align;

        float scale = TargetBedLength / Mathf.Max(size.z, 0.001f);
        float scaledWidth = size.x * scale;
        if (scaledWidth > 1.35f)
        {
            scale *= TargetBedWidth / scaledWidth;
        }

        tmp.transform.localScale = Vector3.one * scale;
        Bounds aligned = EncapsulateWorldBounds(tmp);
        Vector3 visualPos = new Vector3(-aligned.center.x, -aligned.min.y, -aligned.center.z);
        Vector3 scaled = aligned.size;

        UnityEngine.Object.DestroyImmediate(tmp);

        fit.valid = true;
        fit.source = source;
        fit.uniformScale = scale;
        fit.visualLocalRot = align;
        fit.visualLocalPos = visualPos;
        fit.size = scaled;
        fit.note = "bed native " + size + " scale " + scale;
        return fit;
    }

    static FittedProp FitCabinet(string path)
    {
        FittedProp fit = new FittedProp();
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null)
        {
            return fit;
        }

        GameObject tmp = UnityEngine.Object.Instantiate(source);
        tmp.hideFlags = HideFlags.HideAndDontSave;
        tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        tmp.transform.localScale = Vector3.one;

        Bounds native = EncapsulateWorldBounds(tmp);
        Vector3 size = native.size;
        float footprint = Mathf.Max(size.x, size.z);
        float scale = TargetCabinetWidth / Mathf.Max(footprint, 0.001f);
        float scaledHeight = size.y * scale;
        if (scaledHeight > 1.15f)
        {
            scale *= 0.75f / Mathf.Max(size.y * scale, 0.001f);
        }

        if (size.x > size.z * 1.15f)
        {
            tmp.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }

        Quaternion align = tmp.transform.rotation;
        tmp.transform.localScale = Vector3.one * scale;
        Bounds aligned = EncapsulateWorldBounds(tmp);
        Vector3 visualPos = new Vector3(-aligned.center.x, -aligned.min.y, -aligned.center.z);

        UnityEngine.Object.DestroyImmediate(tmp);

        fit.valid = true;
        fit.source = source;
        fit.uniformScale = scale;
        fit.visualLocalRot = align;
        fit.visualLocalPos = visualPos;
        fit.size = aligned.size;
        fit.note = "cabinet scale " + scale;
        return fit;
    }

    static Quaternion RotationMappingAxisTo(int fromY, int fromZ)
    {
        Vector3 srcY = AxisVector(fromY);
        Vector3 srcZ = AxisVector(fromZ);
        Quaternion q = Quaternion.LookRotation(srcZ, srcY);
        Quaternion target = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        return target * Quaternion.Inverse(q);
    }

    static Vector3 AxisVector(int axis)
    {
        if (axis == 0) return Vector3.right;
        if (axis == 1) return Vector3.up;
        return Vector3.forward;
    }

    static int SmallestAxis(Vector3 size)
    {
        if (size.x <= size.y && size.x <= size.z) return 0;
        if (size.y <= size.x && size.y <= size.z) return 1;
        return 2;
    }

    static int LargestAxis(Vector3 size)
    {
        if (size.x >= size.y && size.x >= size.z) return 0;
        if (size.y >= size.x && size.y >= size.z) return 1;
        return 2;
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

    static GameObject SpawnModel(GameObject source, Transform parent, string name)
    {
        GameObject go;
        if (PrefabUtility.GetPrefabAssetType(source) != PrefabAssetType.NotAPrefab)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        }
        else
        {
            go = UnityEngine.Object.Instantiate(source);
        }

        Undo.RegisterCreatedObjectUndo(go, _undoName);
        go.name = name;
        go.transform.SetParent(parent, false);
        return go;
    }

    static void PlaceRoomDoor(
        Transform area, string rootName, AttachSide attach, float doorAlong,
        float xMin, float xMax, float zMin, float zMax, FittedDoor fit)
    {
        Transform doors = area.Find("Doors");
        if (!fit.valid)
        {
            Debug.LogWarning("Deep Night Shelter: skipped " + rootName + " because the door model is missing.");
            return;
        }

        Vector3 outward;
        Vector3 wallCenter;
        if (attach == AttachSide.West)
        {
            outward = Vector3.right;
            wallCenter = new Vector3(xMax + WallThickness * 0.5f, 0f, doorAlong);
        }
        else if (attach == AttachSide.East)
        {
            outward = Vector3.left;
            wallCenter = new Vector3(xMin - WallThickness * 0.5f, 0f, doorAlong);
        }
        else if (attach == AttachSide.South)
        {
            outward = Vector3.forward;
            wallCenter = new Vector3(doorAlong, 0f, zMax + WallThickness * 0.5f);
        }
        else
        {
            outward = Vector3.back;
            wallCenter = new Vector3(doorAlong, 0f, zMin - WallThickness * 0.5f);
        }

        Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);
        Vector3 hingeToLatch = rot * Vector3.right;
        Vector3 hinge = wallCenter - hingeToLatch * (fit.openingWidth * 0.5f);
        hinge.y = _levelY;

        Transform doorRoot = CreateGroup(rootName, doors);
        doorRoot.position = hinge;
        doorRoot.rotation = rot;

        GameObject visual = SpawnModel(fit.source, doorRoot, "Visual");
        visual.transform.localRotation = fit.visualLocalRot;
        visual.transform.localScale = Vector3.one * fit.uniformScale;
        visual.transform.localPosition = fit.visualLocalPos;

        var box = doorRoot.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(fit.size.x * 0.5f, fit.size.y * 0.5f, 0f);
        box.size = new Vector3(fit.size.x, fit.size.y, Mathf.Max(fit.size.z, 0.04f));
    }

    static void PlaceProp(Transform props, string name, FittedProp fit, Vector3 position, float yawY)
    {
        if (!fit.valid)
        {
            return;
        }

        Transform root = CreateGroup(name, props);
        root.position = new Vector3(position.x, position.y + _levelY, position.z);
        root.rotation = Quaternion.Euler(0f, yawY, 0f);

        GameObject visual = SpawnModel(fit.source, root, "Visual");
        visual.transform.localRotation = fit.visualLocalRot;
        visual.transform.localScale = Vector3.one * fit.uniformScale;
        visual.transform.localPosition = fit.visualLocalPos;

        var box = root.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, fit.size.y * 0.5f, 0f);
        box.size = fit.size;
    }

    static void PlaceWard01Furniture(Transform area)
    {
        Transform props = area.Find("Props");
        float headClear = 0.18f;
        float bedLength = _bed.valid ? _bed.size.z : TargetBedLength;
        float bedWidth = _bed.valid ? _bed.size.x : TargetBedWidth;
        float x = Ward01XMin + headClear + bedLength * 0.5f;
        float z0 = Ward01ZMin + 0.55f + bedWidth * 0.5f;
        float gap = 1.05f;
        float z1 = z0 + bedWidth + gap;

        PlaceProp(props, "Bed_01", _bed, new Vector3(x, 0f, z0), 90f);
        PlaceProp(props, "Bed_02", _bed, new Vector3(x + 0.08f, 0f, z1), 93.5f);
        PlaceProp(props, "BedsideCabinet_01", _cabinet, new Vector3(Ward01XMin + 0.42f, 0f, (z0 + z1) * 0.5f), 8f);
        PlaceProp(props, "BedsideCabinet_02", _cabinet, new Vector3(Ward01XMin + 0.38f, 0f, z1 + bedWidth * 0.5f + 0.32f), -6f);
    }

    static void PlaceWard02Furniture(Transform area)
    {
        Transform props = area.Find("Props");
        float headClear = 0.18f;
        float bedLength = _bed.valid ? _bed.size.z : TargetBedLength;
        float bedWidth = _bed.valid ? _bed.size.x : TargetBedWidth;
        float z = Ward02ZMin + headClear + bedLength * 0.5f;
        float x0 = Ward02XMin + 0.7f + bedWidth * 0.5f;

        PlaceProp(props, "Bed_01", _bed, new Vector3(x0, 0f, z), 0f);
        PlaceProp(props, "Bed_02", _bed, new Vector3(x0 + bedWidth + 1.15f, 0f, z + 0.12f), 17f);
        PlaceProp(props, "BedsideCabinet_01", _cabinet, new Vector3(x0 + bedWidth * 0.5f + 0.28f, 0f, Ward02ZMin + 0.38f), 11f);
    }

    static void PlaceWard03Furniture(Transform area)
    {
        Transform props = area.Find("Props");
        float headClear = 0.2f;
        float bedLength = _bed.valid ? _bed.size.z : TargetBedLength;
        float bedWidth = _bed.valid ? _bed.size.x : TargetBedWidth;
        float x = Ward03XMax - headClear - bedLength * 0.5f;
        float z = Ward03ZMin + 0.7f + bedWidth * 0.5f;

        PlaceProp(props, "Bed_01", _bed, new Vector3(x, 0f, z), -90f);
        PlaceProp(props, "BedsideCabinet_01", _cabinet, new Vector3(Ward03XMax - 0.4f, 0f, z + bedWidth * 0.5f + 0.3f), -7f);
    }

    static void PlaceInvisibleBlocker(Transform parent, string name, bool alongX, float alongCenter, float depthCenter, float width, float height)
    {
        Vector3 center;
        Vector3 size;
        if (alongX)
        {
            center = new Vector3(alongCenter, _levelY + height * 0.5f, depthCenter);
            size = new Vector3(width, height, WallThickness);
        }
        else
        {
            center = new Vector3(depthCenter, _levelY + height * 0.5f, alongCenter);
            size = new Vector3(WallThickness, height, width);
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, _undoName);
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(center, Quaternion.identity);
        var box = go.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = size;
    }

    struct DoorOpening
    {
        public string Name;
        public float Center;
        public float Width;
        public float Height;
        public bool DoubleLeaf;
        public bool Locked;
        public bool PlacePlaceholder;
    }

    static DoorOpening Opening(string name, float center, float width, float height, bool doubleLeaf = false, bool locked = false)
    {
        return new DoorOpening
        {
            Name = name,
            Center = center,
            Width = width,
            Height = height,
            DoubleLeaf = doubleLeaf,
            Locked = locked,
            PlacePlaceholder = true
        };
    }

    static DoorOpening Cut(string name, float center, float width, float height)
    {
        return new DoorOpening
        {
            Name = name,
            Center = center,
            Width = width,
            Height = height,
            PlacePlaceholder = false
        };
    }

    static void BuildWallAlongX(
        Transform walls, Transform doorways, string name,
        float xMin, float xMax, float zInner, float thicknessSign,
        bool coverCorners,
        params DoorOpening[] openings)
    {
        if (coverCorners)
        {
            xMin -= WallThickness;
            xMax += WallThickness;
        }

        float zCenter = zInner + thicknessSign * WallThickness * 0.5f;
        BuildSplitWall(walls, doorways, name, xMin, xMax, alongIsX: true, depthCenter: zCenter, openings);
    }

    static void BuildWallAlongZ(
        Transform walls, Transform doorways, string name,
        float zMin, float zMax, float xInner, float thicknessSign,
        bool coverCorners,
        params DoorOpening[] openings)
    {
        if (coverCorners)
        {
            zMin -= WallThickness;
            zMax += WallThickness;
        }

        float xCenter = xInner + thicknessSign * WallThickness * 0.5f;
        BuildSplitWall(walls, doorways, name, zMin, zMax, alongIsX: false, depthCenter: xCenter, openings);
    }

    static void BuildSplitWall(
        Transform walls, Transform doorways, string name,
        float alongMin, float alongMax, bool alongIsX, float depthCenter,
        DoorOpening[] openings)
    {
        if (openings == null || openings.Length == 0)
        {
            CreateWallSegment(walls, name, alongMin, alongMax, alongIsX, depthCenter, 0f, WallHeight);
            return;
        }

        Array.Sort(openings, (a, b) => a.Center.CompareTo(b.Center));

        float cursor = alongMin;
        int segment = 0;
        for (int i = 0; i < openings.Length; i++)
        {
            DoorOpening door = openings[i];
            float doorMin = Mathf.Max(alongMin, door.Center - door.Width * 0.5f);
            float doorMax = Mathf.Min(alongMax, door.Center + door.Width * 0.5f);

            if (doorMin - cursor > 0.001f)
            {
                CreateWallSegment(walls, name + "_A" + segment, cursor, doorMin, alongIsX, depthCenter, 0f, WallHeight);
                segment++;
            }

            float lintelHeight = WallHeight - door.Height;
            if (lintelHeight > 0.001f && doorMax - doorMin > 0.001f)
            {
                CreateWallSegment(walls, name + "_Lintel_" + door.Name, doorMin, doorMax, alongIsX, depthCenter, door.Height, lintelHeight);
            }

            if (door.PlacePlaceholder)
            {
                CreateDoorPlaceholder(
                    doorways,
                    door.Name + (door.Locked ? "_LockedPlaceholder" : "_Placeholder"),
                    alongIsX,
                    door.Center,
                    depthCenter,
                    door.Width,
                    door.Height,
                    door.DoubleLeaf,
                    door.Locked);
            }

            cursor = doorMax;
        }

        if (alongMax - cursor > 0.001f)
        {
            CreateWallSegment(walls, name + "_B", cursor, alongMax, alongIsX, depthCenter, 0f, WallHeight);
        }
    }

    static void CreateWallSegment(
        Transform parent, string name,
        float alongMin, float alongMax, bool alongIsX, float depthCenter,
        float yMin, float height)
    {
        float alongSize = alongMax - alongMin;
        if (alongSize < 0.001f || height < 0.001f)
        {
            return;
        }

        float alongCenter = (alongMin + alongMax) * 0.5f;
        float yCenter = _levelY + yMin + height * 0.5f;
        Vector3 center = alongIsX
            ? new Vector3(alongCenter, yCenter, depthCenter)
            : new Vector3(depthCenter, yCenter, alongCenter);
        Vector3 size = alongIsX
            ? new Vector3(alongSize, height, WallThickness)
            : new Vector3(WallThickness, height, alongSize);

        CreateCube(parent, name, center, size, _wallMat, addCollider: true);
    }

    static void ExtendWestCorner(Transform walls, string name, float xInner, float zInner, float zSign)
    {
        CreateCube(
            walls, name,
            new Vector3(xInner - WallThickness * 0.5f, WallCenterY + _levelY, zInner + zSign * WallThickness * 0.5f),
            new Vector3(WallThickness, WallHeight, WallThickness),
            _wallMat, addCollider: true);
    }

    static void ExtendEastCorner(Transform walls, string name, float xInner, float zInner, float zSign)
    {
        CreateCube(
            walls, name,
            new Vector3(xInner + WallThickness * 0.5f, WallCenterY + _levelY, zInner + zSign * WallThickness * 0.5f),
            new Vector3(WallThickness, WallHeight, WallThickness),
            _wallMat, addCollider: true);
    }

    static void CreateDoorPlaceholder(
        Transform parent, string name,
        bool alongX, float alongCenter, float depthCenter,
        float width, float height, bool doubleLeaf, bool locked)
    {
        Material mat = locked ? _lockedDoorMat : _doorMat;
        float thickness = locked ? WallThickness : PlaceholderThickness;
        bool collider = locked;

        if (doubleLeaf)
        {
            float leaf = width * 0.5f - 0.02f;
            float offset = width * 0.25f;
            CreatePlaceholderLeaf(parent, name + "_Left", alongX, alongCenter - offset, depthCenter, leaf, height, thickness, mat, collider);
            CreatePlaceholderLeaf(parent, name + "_Right", alongX, alongCenter + offset, depthCenter, leaf, height, thickness, mat, collider);
        }
        else
        {
            CreatePlaceholderLeaf(parent, name, alongX, alongCenter, depthCenter, width, height, thickness, mat, collider);
        }
    }

    static void CreatePlaceholderLeaf(
        Transform parent, string name, bool alongX,
        float alongCenter, float depthCenter,
        float alongSize, float height, float thickness,
        Material mat, bool collider)
    {
        float yCenter = _levelY + height * 0.5f;
        Vector3 center = alongX
            ? new Vector3(alongCenter, yCenter, depthCenter)
            : new Vector3(depthCenter, yCenter, alongCenter);
        Vector3 size = alongX
            ? new Vector3(alongSize, height, thickness)
            : new Vector3(thickness, height, alongSize);

        CreateCube(parent, name, center, size, mat, addCollider: collider);
    }

    static void CreateSlab(Transform parent, string name, float xMin, float xMax, float zMin, float zMax, float yCenter, Material mat)
    {
        float w = xMax - xMin;
        float d = zMax - zMin;
        if (w < 0.001f || d < 0.001f)
        {
            return;
        }

        CreateCube(
            parent, name,
            new Vector3((xMin + xMax) * 0.5f, yCenter + _levelY, (zMin + zMax) * 0.5f),
            new Vector3(w, SlabThickness, d),
            mat, addCollider: true);
    }

    static Transform CreateArea(Transform greybox, string name)
    {
        Transform area = CreateGroup(name, greybox);
        CreateGroup("Floor", area);
        CreateGroup("Ceiling", area);
        CreateGroup("Walls", area);
        CreateGroup("Doorways", area);
        CreateGroup("Doors", area);
        CreateGroup("Props", area);
        return area;
    }

    static Transform CreateGroup(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, _undoName);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        return go.transform;
    }

    static Transform FindOrCreateRoot(string name)
    {
        GameObject found = GameObject.Find(name);
        if (found != null)
        {
            return found.transform;
        }

        return CreateGroup(name, null);
    }

    static GameObject CreateCube(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool addCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.isStatic = true;
        Undo.RegisterCreatedObjectUndo(go, _undoName);
        go.transform.SetParent(parent, true);
        go.transform.position = center;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = size;

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = mat;
        }

        var collider = go.GetComponent<Collider>();
        if (!addCollider && collider != null)
        {
            Undo.DestroyObjectImmediate(collider);
        }

        return go;
    }

    static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Greybox");
        }

        _floorMat = GetOrCreateMaterial("Greybox_Floor", new Color(0.22f, 0.22f, 0.24f));
        _wallMat = GetOrCreateMaterial("Greybox_Wall", new Color(0.48f, 0.48f, 0.50f));
        _ceilingMat = GetOrCreateMaterial("Greybox_Ceiling", new Color(0.16f, 0.16f, 0.18f));
        _doorMat = GetOrCreateMaterial("Greybox_DoorPlaceholder", new Color(0.78f, 0.50f, 0.22f));
        _lockedDoorMat = GetOrCreateMaterial("Greybox_DoorLocked", new Color(0.62f, 0.22f, 0.20f));
        AssetDatabase.SaveAssets();
    }

    static Material GetOrCreateMaterial(string assetName, Color color)
    {
        string path = MaterialFolder + "/" + assetName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var mat = new Material(shader) { name = assetName };
        ApplyGreyboxColor(mat, color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ApplyGreyboxColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.color = color;
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", 0.05f);
        }

        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", 0f);
        }
    }
}
