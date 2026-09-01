using UnityEditor;
using UnityEngine;

/// <summary>
/// DEPRECATED. No Editor menu. Do not rebuild production levels.
/// Historical second floor (Y=4) and basement (Y=-4) greybox generator.
/// </summary>
public static partial class ShelterGreyboxBuilder
{
    const float SecondFloorY = 4.0f;
    const float BasementY = -4.0f;

    const string PathDesk = "Assets/Model/OfficeDesk.fbx";
    const string PathChair = "Assets/Model/OfficeChair.fbx";
    const string PathFileCabinet = "Assets/Model/MetalFileCabinet.fbx";
    const string PathArchiveShelf = "Assets/Model/WoodenArchiveShelf.fbx";
    const string PathRecords = "Assets/Model/MedicalRecordStack.fbx";
    const string PathLamp = "Assets/Model/DeskLamp.fbx";
    const string PathGlassCabinet = "Assets/Model/GlassMedicineCabinet.fbx";
    const string PathCart = "Assets/Model/HospitalCart.fbx";
    const string PathIv = "Assets/Model/IVStand.fbx";
    const string PathGenerator = "Assets/Model/DieselGenerator.fbx";
    const string PathPanel = "Assets/Model/ElectricalPanel.fbx";
    const string PathToolbox = "Assets/Model/Toolbox.fbx";
    const string PathShelf = "Assets/Model/MetalShelf.fbx";
    const string PathCrate = "Assets/Model/WoodenCrate.fbx";
    const string PathWorkbench = "Assets/Model/Workbench.fbx";
    const string PathValve = "Assets/Model/PipeValve.fbx";
    const string PathFire = "Assets/Model/FireHoseCabinet.fbx";
    const string PathTrash = "Assets/Model/TrashBin.fbx";
    const string PathWheelchair = "Assets/Model/Wheelchair.fbx";
    const string PathThermos = "Assets/Model/Thermos.fbx";
    const string PathBottles = "Assets/Model/MedicineBottles.fbx";

    static FittedProp _desk;
    static FittedProp _chair;
    static FittedProp _fileCabinet;
    static FittedProp _archiveShelf;
    static FittedProp _records;
    static FittedProp _lamp;
    static FittedProp _glassCabinet;
    static FittedProp _cart;
    static FittedProp _iv;
    static FittedProp _generator;
    static FittedProp _panel;
    static FittedProp _toolbox;
    static FittedProp _shelf;
    static FittedProp _crate;
    static FittedProp _workbench;
    static FittedProp _valve;
    static FittedProp _fire;
    static FittedProp _trash;
    static FittedProp _wheelchair;
    static FittedProp _thermos;
    static FittedProp _bottles;

    static void BuildSecondFloorInternal()
    {
        _levelY = SecondFloorY;
        EnsureMaterials();
        ApplySecondFloorMaterials();
        ResolveUpperFloorModels();
        ClearNamedRoot("SecondFloor");

        Transform environment = FindOrCreateRoot("Environment");
        Transform root = CreateGroup("SecondFloor", environment);
        Transform architecture = CreateGroup("Architecture", root);
        Transform propsRoot = CreateGroup("Props", root);
        Transform markers = CreateGroup("GameplayMarkers", root);

        float wardW = OpeningWidth(_wardDoor, DoorWidth);
        float wardH = OpeningHeight(_wardDoor, DoorHeight);
        float officeW = OpeningWidth(_officeDoor, DoorWidth);
        float officeH = OpeningHeight(_officeDoor, DoorHeight);

        BuildStairLanding(architecture);
        BuildSecondFloorCorridor(architecture, wardW, wardH, officeW, officeH);

        Transform nurse = BuildSideRoom("NurseOffice", architecture, -5.0f, 0.0f, 8.8f, 12.8f, AttachSide.North, -2.5f, officeW, 8.6f);
        Transform archive = BuildSideRoom("ArchiveRoom", architecture, 0.4f, 6.4f, 8.8f, 13.8f, AttachSide.North, 3.4f, officeW, 8.6f);
        Transform smallWard = BuildSideRoom("SmallWard", architecture, -2.0f, 3.2f, 1.4f, 5.6f, AttachSide.South, 0.6f, wardW, 5.8f);
        BuildSideRoom("BlockedRoom", architecture, 3.8f, 6.6f, 2.8f, 5.6f, AttachSide.South, 5.2f, DoorWidth, 5.8f);
        Transform treatment = BuildSideRoom("TreatmentRoom", architecture, 11.4f, 16.4f, 9.2f, 13.2f, AttachSide.East, 11.2f, wardW, 11.2f);
        Transform doctor = BuildSideRoom("DoctorOffice", architecture, 11.4f, 15.9f, 16.0f, 20.0f, AttachSide.East, 18.0f, officeW, 11.2f);

        PlaceRoomDoor(nurse, "NurseOffice_DoorRoot", AttachSide.North, -2.5f, -5.0f, 0.0f, 8.8f, 12.8f, _officeDoor);
        PlaceRoomDoor(archive, "ArchiveRoom_DoorRoot", AttachSide.North, 3.4f, 0.4f, 6.4f, 8.8f, 13.8f, _officeDoor);
        PlaceRoomDoor(smallWard, "SmallWard_DoorRoot", AttachSide.South, 0.6f, -2.0f, 3.2f, 1.4f, 5.6f, _wardDoor);
        PlaceRoomDoor(treatment, "TreatmentRoom_DoorRoot", AttachSide.East, 11.2f, 11.4f, 16.4f, 9.2f, 13.2f, _wardDoor);
        PlaceRoomDoor(doctor, "DoctorOffice_DoorRoot", AttachSide.East, 18.0f, 11.4f, 15.9f, 16.0f, 20.0f, _officeDoor);

        PlaceSecondFloorProps(propsRoot);
        PlaceSecondFloorMarkers(markers);

        Selection.activeGameObject = root.gameObject;
        Debug.Log("Deep Night Shelter: second-floor greybox rebuilt at Y=" + SecondFloorY + ".");
    }

    static void BuildBasementInternal()
    {
        _levelY = BasementY;
        EnsureMaterials();
        ApplyBasementMaterials();
        ResolveUpperFloorModels();
        ClearNamedRoot("Basement");

        Transform environment = FindOrCreateRoot("Environment");
        Transform root = CreateGroup("Basement", environment);
        Transform architecture = CreateGroup("Architecture", root);
        Transform propsRoot = CreateGroup("Props", root);
        Transform markers = CreateGroup("GameplayMarkers", root);

        float doorW = OpeningWidth(_pharmacyDoor, DoorWidth);
        float doorH = OpeningHeight(_pharmacyDoor, DoorHeight);
        float officeW = OpeningWidth(_officeDoor, DoorWidth);
        float officeH = OpeningHeight(_officeDoor, DoorHeight);

        BuildBasementEntrance(architecture);
        BuildBasementCorridors(architecture, doorW, doorH);

        Transform generator = BuildSideRoom("GeneratorRoom", architecture, -5.2f, 1.8f, 5.0f, 11.0f, AttachSide.South, -1.8f, officeW, 11.2f);
        BuildWallAlongX(generator.Find("Walls"), generator.Find("Doorways"), "GeneratorRoom_Wall_NorthWest", -5.2f, -4.0f, 11.0f, 1f, coverCorners: false);

        Transform electrical = BuildSideRoom("ElectricalRoom", architecture, 9.2f, 13.2f, 9.2f, 13.2f, AttachSide.East, 11.2f, doorW, 9.0f);
        Transform workshop = BuildSideRoom("MaintenanceWorkshop", architecture, -5.2f, -0.2f, 14.2f, 18.7f, AttachSide.North, -2.7f, officeW, 14.0f);
        BuildWallAlongX(workshop.Find("Walls"), workshop.Find("Doorways"), "Workshop_Wall_SouthWest", -5.2f, -4.0f, 14.2f, -1f, coverCorners: false);

        Transform storage = BuildSideRoom("StorageRoom", architecture, 0.2f, 5.2f, 14.2f, 19.2f, AttachSide.North, 2.7f, officeW, 14.0f);
        Transform isolation = BuildSideRoom("IsolationArea", architecture, 6.2f, 12.2f, 19.7f, 24.7f, AttachSide.North, 7.6f, doorW, 19.5f);
        BuildWallAlongX(isolation.Find("Walls"), isolation.Find("Doorways"), "IsolationArea_Wall_SouthEast", 9.0f, 12.2f, 19.7f, -1f, coverCorners: false);
        ExtendEastCorner(isolation.Find("Walls"), "IsolationArea_Wall_SouthEast_Corner", 12.2f, 19.7f, -1f);

        PlaceRoomDoor(generator, "GeneratorRoom_DoorRoot", AttachSide.South, -1.8f, -5.2f, 1.8f, 5.0f, 11.0f, _officeDoor);
        PlaceRoomDoor(electrical, "ElectricalRoom_DoorRoot", AttachSide.East, 11.2f, 9.2f, 13.2f, 9.2f, 13.2f, _pharmacyDoor);
        PlaceRoomDoor(workshop, "Workshop_DoorRoot", AttachSide.North, -2.7f, -5.2f, -0.2f, 14.2f, 18.7f, _officeDoor);
        PlaceRoomDoor(storage, "StorageRoom_DoorRoot", AttachSide.North, 2.7f, 0.2f, 5.2f, 14.2f, 19.2f, _officeDoor);

        PlaceBasementProps(propsRoot);
        PlaceBasementMarkers(markers);

        Selection.activeGameObject = root.gameObject;
        Debug.Log("Deep Night Shelter: basement greybox rebuilt at Y=" + BasementY + ".");
    }

    static Transform BuildStairLanding(Transform architecture)
    {
        Transform area = CreateArea(architecture, "StairLanding");
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "StairLanding_Floor", -6.2f, -2.2f, 1.8f, 5.8f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "StairLanding_Ceiling", -6.2f, -2.2f, 1.8f, 5.8f, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "StairLanding_Floor_NorthJoin", -5.0f, -2.2f, 5.78f, 5.82f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "StairLanding_Ceiling_NorthJoin", -5.0f, -2.2f, 5.78f, 5.82f, CeilingCenterY, _ceilingMat);

        BuildWallAlongX(walls, doorways, "StairLanding_Wall_South", -6.2f, -2.2f, 1.8f, -1f, coverCorners: true);
        BuildWallAlongZ(
            walls, doorways, "StairLanding_Wall_West",
            1.8f, 5.8f, -6.2f, -1f, coverCorners: false,
            Cut("StairLanding_Door_Stairwell", Stair2DoorZ, Stair2DoorW, DoorHeight));
        BuildWallAlongZ(walls, doorways, "StairLanding_Wall_East", 1.8f, 5.8f, -2.2f, 1f, coverCorners: false);
        BuildWallAlongX(
            walls, doorways, "StairLanding_Wall_North",
            -6.2f, -2.2f, 5.8f, 1f, coverCorners: false,
            Cut("StairLanding_ToCorridor", -3.6f, 2.8f, DoorHeight));

        return area;
    }

    static Transform BuildSecondFloorCorridor(Transform architecture, float wardW, float wardH, float officeW, float officeH)
    {
        Transform area = CreateArea(architecture, "MainCorridor");
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "MainCorridor_EW_Floor", -5.0f, 8.4f, 5.8f, 8.6f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "MainCorridor_EW_Ceiling", -5.0f, 8.4f, 5.8f, 8.6f, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "MainCorridor_NS_Floor", 8.4f, 11.2f, 5.8f, 20.0f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "MainCorridor_NS_Ceiling", 8.4f, 11.2f, 5.8f, 20.0f, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "MainCorridor_Floor_LJoin", 8.38f, 8.42f, 5.8f, 8.6f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "MainCorridor_Ceiling_LJoin", 8.38f, 8.42f, 5.8f, 8.6f, CeilingCenterY, _ceilingMat);

        BuildWallAlongZ(walls, doorways, "MainCorridor_EW_Wall_West", 5.8f, 8.6f, -5.0f, -1f, coverCorners: false);

        BuildWallAlongX(
            walls, doorways, "MainCorridor_EW_Wall_South",
            -5.0f, 8.4f, 5.8f, -1f, coverCorners: false,
            Cut("MainCorridor_ToLanding", -3.6f, 2.8f, DoorHeight),
            Cut("MainCorridor_Door_SmallWard", 0.6f, wardW, wardH),
            Opening("MainCorridor_Door_BlockedRoom", 5.2f, DoorWidth, DoorHeight, locked: true));

        BuildWallAlongX(
            walls, doorways, "MainCorridor_EW_Wall_North",
            -5.0f, 8.4f, 8.6f, 1f, coverCorners: false,
            Cut("MainCorridor_Door_NurseOffice", -2.5f, officeW, officeH),
            Cut("MainCorridor_Door_Archive", 3.4f, officeW, officeH));

        BuildWallAlongX(walls, doorways, "MainCorridor_NS_Wall_South", 8.4f, 11.2f, 5.8f, -1f, coverCorners: false);
        ExtendEastCorner(walls, "MainCorridor_NS_Wall_South_Corner", 11.2f, 5.8f, -1f);

        BuildWallAlongZ(
            walls, doorways, "MainCorridor_NS_Wall_West",
            8.6f, 20.0f, 8.4f, -1f, coverCorners: false);

        BuildWallAlongZ(
            walls, doorways, "MainCorridor_NS_Wall_East",
            5.8f, 20.0f, 11.2f, 1f, coverCorners: false,
            Cut("MainCorridor_Door_Treatment", 11.2f, wardW, wardH),
            Cut("MainCorridor_Door_Doctor", 18.0f, officeW, officeH));

        BuildWallAlongX(walls, doorways, "MainCorridor_NS_Wall_North", 8.4f, 11.2f, 20.0f, 1f, coverCorners: false);
        ExtendWestCorner(walls, "MainCorridor_NS_Wall_North_Corner", 8.4f, 20.0f, 1f);
        ExtendEastCorner(walls, "MainCorridor_NS_Wall_North_CornerEast", 11.2f, 20.0f, 1f);

        return area;
    }

    static void BuildBasementEntrance(Transform architecture)
    {
        Transform area = CreateArea(architecture, "BasementEntrance");
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "BasementEntrance_Floor", 6.2f, 9.0f, 5.6f, 8.6f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "BasementEntrance_Ceiling", 6.2f, 9.0f, 5.6f, 8.6f, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "BasementEntrance_Floor_NorthJoin", 6.2f, 9.0f, 8.58f, 8.62f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "BasementEntrance_Ceiling_NorthJoin", 6.2f, 9.0f, 8.58f, 8.62f, CeilingCenterY, _ceilingMat);

        BuildWallAlongX(walls, doorways, "BasementEntrance_Wall_South", 6.2f, 9.0f, 5.6f, -1f, coverCorners: true);
        BuildWallAlongZ(walls, doorways, "BasementEntrance_Wall_West", 5.6f, 8.6f, 6.2f, -1f, coverCorners: false);
        BuildWallAlongZ(
            walls, doorways, "BasementEntrance_Wall_East",
            5.6f, 8.6f, 9.0f, 1f, coverCorners: false,
            Cut("BasementEntrance_Door_Stairwell", StairBDoorZ, StairBDoorW, DoorHeight));
        CreateSlab(floor, "BasementEntrance_Floor_StairJoin", 9.0f, StairBXMin, StairBDoorZ - StairBDoorW * 0.5f, StairBDoorZ + StairBDoorW * 0.5f, FloorCenterY, _floorMat);
        BuildWallAlongX(
            walls, doorways, "BasementEntrance_Wall_North",
            6.2f, 9.0f, 8.6f, 1f, coverCorners: false,
            Cut("BasementEntrance_ToUtility", 7.6f, 2.8f, DoorHeight));
    }

    static void BuildBasementCorridors(Transform architecture, float doorW, float doorH)
    {
        Transform area = CreateArea(architecture, "UtilityCorridor");
        Transform floor = area.Find("Floor");
        Transform ceiling = area.Find("Ceiling");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "Utility_NS_Floor", 6.2f, 9.0f, 8.6f, 14.0f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "Utility_NS_Ceiling", 6.2f, 9.0f, 8.6f, 14.0f, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "Utility_EW_Floor", -4.0f, 6.2f, 11.2f, 14.0f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "Utility_EW_Ceiling", -4.0f, 6.2f, 11.2f, 14.0f, CeilingCenterY, _ceilingMat);
        CreateSlab(floor, "Utility_Floor_TurnJoin", 6.18f, 6.22f, 11.2f, 14.0f, FloorCenterY, _floorMat);
        CreateSlab(ceiling, "Utility_Ceiling_TurnJoin", 6.18f, 6.22f, 11.2f, 14.0f, CeilingCenterY, _ceilingMat);

        BuildWallAlongZ(walls, doorways, "Utility_NS_Wall_West", 8.6f, 11.2f, 6.2f, -1f, coverCorners: false);
        BuildWallAlongZ(
            walls, doorways, "Utility_NS_Wall_East",
            8.6f, 14.0f, 9.0f, 1f, coverCorners: false,
            Cut("Utility_Door_Electrical", 11.2f, doorW, doorH));

        BuildWallAlongX(
            walls, doorways, "Utility_NS_Wall_North",
            6.2f, 9.0f, 14.0f, 1f, coverCorners: false,
            Opening("IsolationDoor", 7.6f, DoorWidth, DoorHeight, locked: true));

        BuildWallAlongX(
            walls, doorways, "Utility_EW_Wall_South",
            -4.0f, 6.2f, 11.2f, -1f, coverCorners: false,
            Cut("Utility_Door_Generator", -1.8f, OpeningWidth(_officeDoor, DoorWidth), OpeningHeight(_officeDoor, DoorHeight)));

        BuildWallAlongX(
            walls, doorways, "Utility_EW_Wall_North",
            -4.0f, 6.2f, 14.0f, 1f, coverCorners: false,
            Cut("Utility_Door_Workshop", -2.7f, OpeningWidth(_officeDoor, DoorWidth), OpeningHeight(_officeDoor, DoorHeight)),
            Cut("Utility_Door_Storage", 2.7f, OpeningWidth(_officeDoor, DoorWidth), OpeningHeight(_officeDoor, DoorHeight)));

        BuildWallAlongZ(walls, doorways, "Utility_EW_Wall_West", 11.2f, 14.0f, -4.0f, -1f, coverCorners: false);
        ExtendWestCorner(walls, "Utility_EW_Wall_West_SouthCorner", -4.0f, 11.2f, -1f);
        ExtendWestCorner(walls, "Utility_EW_Wall_West_NorthCorner", -4.0f, 14.0f, 1f);

        Transform isolationHall = CreateArea(architecture, "IsolationCorridor");
        Transform isoFloor = isolationHall.Find("Floor");
        Transform isoCeil = isolationHall.Find("Ceiling");
        Transform isoWalls = isolationHall.Find("Walls");
        Transform isoDoorways = isolationHall.Find("Doorways");

        CreateSlab(isoFloor, "IsolationCorridor_Floor", 6.2f, 9.0f, 14.2f, 19.5f, FloorCenterY, _floorMat);
        CreateSlab(isoCeil, "IsolationCorridor_Ceiling", 6.2f, 9.0f, 14.2f, 19.5f, CeilingCenterY, _ceilingMat);
        CreateSlab(isoFloor, "IsolationCorridor_Floor_SouthJoin", 6.2f, 9.0f, 14.18f, 14.22f, FloorCenterY, _floorMat);
        CreateSlab(isoFloor, "IsolationCorridor_Floor_NorthJoin", 6.2f, 9.0f, 19.48f, 19.52f, FloorCenterY, _floorMat);

        BuildWallAlongZ(isoWalls, isoDoorways, "IsolationCorridor_Wall_West", 14.2f, 19.5f, 6.2f, -1f, coverCorners: false);
        BuildWallAlongZ(isoWalls, isoDoorways, "IsolationCorridor_Wall_East", 14.2f, 19.5f, 9.0f, 1f, coverCorners: false);
        BuildWallAlongX(
            isoWalls, isoDoorways, "IsolationCorridor_Wall_North",
            6.2f, 9.0f, 19.5f, 1f, coverCorners: false,
            Cut("IsolationCorridor_ToArea", 7.6f, doorW, doorH));
    }

    static void PlaceSecondFloorProps(Transform propsRoot)
    {
        Transform landingProps = CreateGroup("StairLanding", propsRoot);
        PlaceOrPlaceholder(landingProps, "TrashBin", _trash, new Vector3(-2.55f, 0f, 5.25f), 8f, new Vector3(0.35f, 0.5f, 0.35f));
        Transform nurse = CreateGroup("NurseOffice", propsRoot);
        PlaceOrPlaceholder(nurse, "OfficeDesk", _desk, new Vector3(-3.6f, 0f, 11.6f), 8f, new Vector3(1.4f, 0.75f, 0.7f));
        PlaceOrPlaceholder(nurse, "OfficeChair", _chair, new Vector3(-3.5f, 0f, 10.85f), 188f, new Vector3(0.5f, 0.9f, 0.55f));
        PlaceOrPlaceholder(nurse, "MetalFileCabinet", _fileCabinet, new Vector3(-4.55f, 0f, 9.35f), 6f, new Vector3(0.7f, 1.3f, 0.4f));
        PlaceOrPlaceholder(nurse, "GlassMedicineCabinet", _glassCabinet, new Vector3(-0.7f, 0f, 11.9f), -4f, new Vector3(0.9f, 1.6f, 0.4f));
        PlaceOrPlaceholder(nurse, "MedicalRecordStack", _records, new Vector3(-3.1f, 0.76f, 11.85f), 12f, new Vector3(0.35f, 0.2f, 0.28f));
        PlaceOrPlaceholder(nurse, "Thermos", _thermos, new Vector3(-4.05f, 0.76f, 11.45f), 20f, new Vector3(0.18f, 0.28f, 0.18f));

        Transform archive = CreateGroup("ArchiveRoom", propsRoot);
        PlaceOrPlaceholder(archive, "WoodenArchiveShelf_01", _archiveShelf, new Vector3(1.15f, 0f, 9.55f), 92f, new Vector3(1.3f, 1.9f, 0.4f));
        PlaceOrPlaceholder(archive, "WoodenArchiveShelf_02", _archiveShelf, new Vector3(2.7f, 0f, 9.6f), 88f, new Vector3(1.3f, 1.9f, 0.4f));
        PlaceOrPlaceholder(archive, "WoodenArchiveShelf_03", _archiveShelf, new Vector3(1.3f, 0f, 12.95f), 86f, new Vector3(1.3f, 1.9f, 0.4f));
        PlaceOrPlaceholder(archive, "MetalFileCabinet", _fileCabinet, new Vector3(5.55f, 0f, 10.2f), 12f, new Vector3(0.7f, 1.3f, 0.4f));
        PlaceOrPlaceholder(archive, "MedicalRecordStack", _records, new Vector3(5.35f, 0f, 12.6f), -8f, new Vector3(0.35f, 0.2f, 0.28f));

        Transform doctor = CreateGroup("DoctorOffice", propsRoot);
        PlaceOrPlaceholder(doctor, "OfficeDesk", _desk, new Vector3(14.4f, 0f, 18.7f), -88f, new Vector3(1.4f, 0.75f, 0.7f));
        PlaceOrPlaceholder(doctor, "OfficeChair", _chair, new Vector3(13.65f, 0f, 18.55f), 95f, new Vector3(0.5f, 0.9f, 0.55f));
        PlaceOrPlaceholder(doctor, "DeskLamp", _lamp, new Vector3(14.55f, 0.76f, 19.15f), 40f, new Vector3(0.22f, 0.45f, 0.22f));
        PlaceOrPlaceholder(doctor, "MetalFileCabinet", _fileCabinet, new Vector3(15.35f, 0f, 16.55f), -6f, new Vector3(0.7f, 1.3f, 0.4f));
        PlaceOrPlaceholder(doctor, "MedicalRecordStack", _records, new Vector3(14.7f, 0.76f, 18.15f), 18f, new Vector3(0.35f, 0.2f, 0.28f));

        Transform treatment = CreateGroup("TreatmentRoom", propsRoot);
        PlaceOrPlaceholder(treatment, "HospitalBed", _bed, new Vector3(15.35f, 0f, 11.0f), -90f, new Vector3(0.95f, 0.7f, 2.1f));
        PlaceOrPlaceholder(treatment, "HospitalCart", _cart, new Vector3(12.2f, 0f, 10.05f), 18f, new Vector3(0.8f, 0.95f, 0.5f));
        PlaceOrPlaceholder(treatment, "WoodenCrate_Encounter", _crate, new Vector3(12.05f, 0f, 11.7f), 8f, new Vector3(0.45f, 0.4f, 0.45f));
        PlaceOrPlaceholder(treatment, "IVStand", _iv, new Vector3(14.55f, 0f, 12.35f), 10f, new Vector3(0.4f, 1.6f, 0.4f));
        PlaceOrPlaceholder(treatment, "GlassMedicineCabinet", _glassCabinet, new Vector3(13.2f, 0f, 12.7f), 4f, new Vector3(0.9f, 1.6f, 0.4f));
        PlaceOrPlaceholder(treatment, "MedicineBottles", _bottles, new Vector3(12.55f, 0f, 12.55f), -14f, new Vector3(0.3f, 0.2f, 0.25f));

        Transform ward = CreateGroup("SmallWard", propsRoot);
        float bedLength = _bed.valid ? _bed.size.z : TargetBedLength;
        float bedWidth = _bed.valid ? _bed.size.x : TargetBedWidth;
        float x = -1.55f + bedLength * 0.5f;
        float z0 = 2.05f + bedWidth * 0.5f;
        PlaceOrPlaceholder(ward, "Bed_01", _bed, new Vector3(x, 0f, z0), 90f, new Vector3(0.95f, 0.7f, 2.1f));
        PlaceOrPlaceholder(ward, "Bed_02", _bed, new Vector3(x + 0.06f, 0f, z0 + bedWidth + 1.0f), 94f, new Vector3(0.95f, 0.7f, 2.1f));
        PlaceOrPlaceholder(ward, "BedsideCabinet_01", _cabinet, new Vector3(-1.55f, 0f, z0 + bedWidth * 0.5f + 0.28f), 8f, new Vector3(0.45f, 0.7f, 0.45f));
        PlaceOrPlaceholder(ward, "BedsideCabinet_02", _cabinet, new Vector3(-1.5f, 0f, z0 + bedWidth + 1.35f), -10f, new Vector3(0.45f, 0.7f, 0.45f));
        PlaceOrPlaceholder(ward, "Wheelchair", _wheelchair, new Vector3(2.35f, 0f, 2.0f), 22f, new Vector3(0.65f, 0.95f, 1.0f));

        Transform corr = CreateGroup("MainCorridor", propsRoot);
        PlaceOrPlaceholder(corr, "FireHoseCabinet", _fire, new Vector3(7.7f, 0f, 8.48f), 0f, new Vector3(0.7f, 1.1f, 0.22f));
    }

    static void PlaceBasementProps(Transform propsRoot)
    {
        Transform gen = CreateGroup("GeneratorRoom", propsRoot);
        PlaceOrPlaceholder(gen, "DieselGenerator", _generator, new Vector3(-3.1f, 0f, 7.6f), 90f, new Vector3(2.4f, 1.5f, 1.2f));
        PlaceOrPlaceholder(gen, "PipeValve", _valve, new Vector3(-4.7f, 0f, 6.0f), 8f, new Vector3(0.5f, 0.9f, 0.5f));
        PlaceOrPlaceholder(gen, "Toolbox", _toolbox, new Vector3(0.7f, 0f, 6.1f), -12f, new Vector3(0.5f, 0.3f, 0.35f));

        Transform electrical = CreateGroup("ElectricalRoom", propsRoot);
        PlaceOrPlaceholder(electrical, "ElectricalPanel", _panel, new Vector3(12.7f, 0f, 11.2f), -90f, new Vector3(0.7f, 1.4f, 0.22f));
        PlaceOrPlaceholder(electrical, "Toolbox", _toolbox, new Vector3(10.1f, 0f, 10.0f), 16f, new Vector3(0.5f, 0.3f, 0.35f));
        PlaceOrPlaceholder(electrical, "MetalShelf", _shelf, new Vector3(10.0f, 0f, 12.55f), 94f, new Vector3(1.2f, 1.8f, 0.4f));

        Transform workshop = CreateGroup("MaintenanceWorkshop", propsRoot);
        PlaceOrPlaceholder(workshop, "Workbench", _workbench, new Vector3(-3.7f, 0f, 17.4f), 6f, new Vector3(1.6f, 0.9f, 0.7f));
        PlaceOrPlaceholder(workshop, "Toolbox", _toolbox, new Vector3(-2.2f, 0f, 17.15f), -18f, new Vector3(0.5f, 0.3f, 0.35f));
        PlaceOrPlaceholder(workshop, "MetalShelf", _shelf, new Vector3(-4.7f, 0f, 15.2f), 92f, new Vector3(1.2f, 1.8f, 0.4f));
        PlaceOrPlaceholder(workshop, "WoodenCrate_01", _crate, new Vector3(-1.1f, 0f, 15.0f), 14f, new Vector3(0.55f, 0.5f, 0.55f));

        Transform storage = CreateGroup("StorageRoom", propsRoot);
        PlaceOrPlaceholder(storage, "MetalShelf_01", _shelf, new Vector3(0.85f, 0f, 15.0f), 90f, new Vector3(1.3f, 1.8f, 0.4f));
        PlaceOrPlaceholder(storage, "MetalShelf_02", _shelf, new Vector3(0.9f, 0f, 17.35f), 88f, new Vector3(1.3f, 1.8f, 0.4f));
        PlaceOrPlaceholder(storage, "MetalShelf_03", _shelf, new Vector3(4.55f, 0f, 16.2f), -92f, new Vector3(1.3f, 1.8f, 0.4f));
        PlaceOrPlaceholder(storage, "WoodenCrate_01", _crate, new Vector3(2.55f, 0f, 15.15f), 10f, new Vector3(0.55f, 0.5f, 0.55f));
        PlaceOrPlaceholder(storage, "WoodenCrate_02", _crate, new Vector3(2.85f, 0f, 18.35f), -8f, new Vector3(0.55f, 0.5f, 0.55f));
        PlacePlaceholderCube(storage, "PLACEHOLDER_CardboardBox", new Vector3(4.35f, 0f, 18.5f), new Vector3(0.45f, 0.35f, 0.4f), 16f);
        PlaceOrPlaceholder(storage, "Toolbox", _toolbox, new Vector3(3.5f, 0f, 15.4f), 22f, new Vector3(0.5f, 0.3f, 0.35f));

        Transform utility = CreateGroup("UtilityCorridor", propsRoot);
        PlaceOrPlaceholder(utility, "FireHoseCabinet", _fire, new Vector3(6.42f, 0f, 9.7f), 90f, new Vector3(0.7f, 1.1f, 0.22f));
        PlaceOrPlaceholder(utility, "ElectricalPanel", _panel, new Vector3(8.78f, 0f, 9.4f), -90f, new Vector3(0.7f, 1.4f, 0.22f));
    }

    static void PlaceSecondFloorMarkers(Transform markers)
    {
        CreateMarker(markers, "Teleport_To1F", -4.2f, 3.5f);
        CreateMarker(markers, "B1CodeClue_A", 5.3f, 12.4f);
        CreateMarker(markers, "B1CodeClue_B", 14.6f, 18.2f);
        CreateMarker(markers, "PharmacyKeySpawn", 13.2f, 12.5f);

        Transform encounter = CreateGroup("EnemyEncounterZone", markers);
        CreateMarker(encounter, "EventTrigger", 9.8f, 13.4f);
        CreateMarker(encounter, "EnemySpawn", 10.2f, 15.4f);
        CreateMarker(encounter, "PatrolPoint_A", 9.5f, 12.2f);
        CreateMarker(encounter, "PatrolPoint_B", 9.9f, 17.6f);
    }

    static void PlaceBasementMarkers(Transform markers)
    {
        CreateMarker(markers, "Teleport_To1F", 7.6f, 7.1f);
        CreateMarker(markers, "GeneratorInteractPoint", -2.1f, 7.6f);
        CreateMarker(markers, "FuseInstallPoint", -2.4f, 8.2f);
        CreateMarker(markers, "FuseSpawnPoint", 2.7f, 16.8f);
        CreateMarker(markers, "IsolationDoor", 7.6f, 14.0f);
        CreateMarker(markers, "IsolationUnlockTrigger", 7.6f, 13.4f);
        CreateMarker(markers, "ChaseRoute_Start", 7.6f, 19.9f);
        CreateMarker(markers, "ChaseRoute_Mid", 2.0f, 12.6f);
        CreateMarker(markers, "ChaseRoute_Exit", 7.6f, 7.1f);
    }

    static void PlaceOrPlaceholder(Transform props, string name, FittedProp fit, Vector3 position, float yawY, Vector3 placeholderSize)
    {
        if (fit.valid)
        {
            PlaceProp(props, name, fit, position, yawY);
            return;
        }

        PlacePlaceholderCube(props, "PLACEHOLDER_" + name, position, placeholderSize, yawY);
    }

    static void PlacePlaceholderCube(Transform parent, string name, Vector3 position, Vector3 size, float yawY)
    {
        Transform root = CreateGroup(name, parent);
        root.position = new Vector3(position.x, _levelY, position.z);
        root.rotation = Quaternion.Euler(0f, yawY, 0f);
        GameObject visual = CreateCube(
            root, "Visual",
            new Vector3(position.x, _levelY + size.y * 0.5f, position.z),
            size,
            _doorMat, addCollider: true);
        visual.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
        visual.transform.localRotation = Quaternion.identity;
    }

    static Transform CreateMarker(Transform parent, string name, float x, float z)
    {
        Transform marker = CreateGroup(name, parent);
        marker.position = new Vector3(x, _levelY, z);
        marker.rotation = Quaternion.identity;
        return marker;
    }

    static void ClearNamedRoot(string childName)
    {
        GameObject environment = GameObject.Find("Environment");
        if (environment == null)
        {
            return;
        }

        Transform child = environment.transform.Find(childName);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    static void ApplySecondFloorMaterials()
    {
        _floorMat = GetOrCreateMaterial("Greybox_SecondFloor_Floor", new Color(0.24f, 0.21f, 0.21f));
        _wallMat = GetOrCreateMaterial("Greybox_SecondFloor_Wall", new Color(0.42f, 0.45f, 0.47f));
        _ceilingMat = GetOrCreateMaterial("Greybox_SecondFloor_Ceiling", new Color(0.13f, 0.13f, 0.15f));
        AssetDatabase.SaveAssets();
    }

    static void ApplyBasementMaterials()
    {
        _floorMat = GetOrCreateMaterial("Greybox_Basement_Floor", new Color(0.16f, 0.18f, 0.17f));
        _wallMat = GetOrCreateMaterial("Greybox_Basement_Wall", new Color(0.34f, 0.37f, 0.35f));
        _ceilingMat = GetOrCreateMaterial("Greybox_Basement_Ceiling", new Color(0.09f, 0.11f, 0.11f));
        AssetDatabase.SaveAssets();
    }

    static void ResolveUpperFloorModels()
    {
        _wardDoor = FitDoor(PathWardDoor, "Ward door");
        _pharmacyDoor = FitDoor(PathPharmacyDoor, "Pharmacy door");
        _officeDoor = FitDoor(PathOfficeDoor, "Office door");
        _bed = FitBed(PathBed);
        _cabinet = FitCabinet(PathCabinet);

        _desk = FitProp(PathDesk, 1.5f, 0.85f, 0.8f);
        _chair = FitProp(PathChair, 0.55f, 0.95f, 0.6f);
        _fileCabinet = FitProp(PathFileCabinet, 0.8f, 1.45f, 0.5f);
        _archiveShelf = FitProp(PathArchiveShelf, 1.4f, 2.0f, 0.45f);
        _records = FitProp(PathRecords, 0.4f, 0.28f, 0.32f);
        _lamp = FitProp(PathLamp, 0.28f, 0.5f, 0.28f);
        _glassCabinet = FitProp(PathGlassCabinet, 1.0f, 1.8f, 0.45f);
        _cart = FitProp(PathCart, 0.85f, 1.05f, 0.55f);
        _iv = FitProp(PathIv, 0.45f, 1.7f, 0.45f);
        _generator = FitProp(PathGenerator, 2.5f, 1.7f, 1.3f);
        _panel = FitProp(PathPanel, 0.8f, 1.5f, 0.28f);
        _toolbox = FitProp(PathToolbox, 0.55f, 0.35f, 0.4f);
        _shelf = FitProp(PathShelf, 1.4f, 2.0f, 0.45f);
        _crate = FitProp(PathCrate, 0.6f, 0.55f, 0.6f);
        _workbench = FitProp(PathWorkbench, 1.7f, 0.95f, 0.75f);
        _valve = FitProp(PathValve, 0.55f, 1.0f, 0.55f);
        _fire = FitProp(PathFire, 0.8f, 1.2f, 0.28f);
        _trash = FitProp(PathTrash, 0.4f, 0.6f, 0.4f);
        _wheelchair = FitProp(PathWheelchair, 0.7f, 1.05f, 1.1f);
        _thermos = FitProp(PathThermos, 0.2f, 0.32f, 0.2f);
        _bottles = FitProp(PathBottles, 0.35f, 0.25f, 0.3f);
    }

    static FittedProp FitProp(string path, float maxX, float maxY, float maxZ)
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
        if (size.x < 0.001f || size.y < 0.001f || size.z < 0.001f)
        {
            UnityEngine.Object.DestroyImmediate(tmp);
            return fit;
        }

        float scale = Mathf.Min(maxX / size.x, maxY / size.y, maxZ / size.z);
        tmp.transform.localScale = Vector3.one * scale;
        Bounds aligned = EncapsulateWorldBounds(tmp);

        fit.valid = true;
        fit.source = source;
        fit.uniformScale = scale;
        fit.visualLocalRot = Quaternion.identity;
        fit.visualLocalPos = new Vector3(-aligned.center.x, -aligned.min.y, -aligned.center.z);
        fit.size = aligned.size;

        UnityEngine.Object.DestroyImmediate(tmp);
        return fit;
    }
}
