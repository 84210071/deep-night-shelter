using UnityEditor;
using UnityEngine;

/// <summary>
/// DEPRECATED. No Editor menu. Do not rebuild production levels.
/// Historical walkable U-stairs connecting 1F, 2F and B1.
/// Parent: Environment/FloorConnections. Scene Transform is now the source of truth.
/// </summary>
public static partial class ShelterGreyboxBuilder
{
    const float StairRiser = 0.182f;
    const float StairTread = 0.28f;
    const int StairStepsPerFlight = 11;
    const float StairGap = 0.15f;

    const float Stair2XMin = -9.85f;
    const float Stair2XMax = -6.40f;
    const float Stair2ZMin = 1.70f;
    const float Stair2ZMax = 6.45f;
    const float Stair2DoorZ = 2.50f;
    const float Stair2DoorW = 1.40f;
    const float Stair2FlightWidth = 1.55f;
    const float Stair2HallZMin = 1.75f;
    const float Stair2HallZMax = 3.25f;

    const float StairBXMin = 9.2f;
    const float StairBXMax = 12.5f;
    const float StairBZMin = 5.4f;
    const float StairBZMax = 11.5f;
    const float StairBDoorZ = 6.2f;
    const float StairBDoorW = 1.2f;
    const float StairBFlightWidth = 1.48f;

    static void BuildFloorConnectionsInternal()
    {
        EnsureMaterials();
        Transform environment = FindOrCreateRoot("Environment");
        ClearNamedRoot("FloorConnections");

        Transform greybox = environment.Find("Greybox");
        if (greybox != null)
        {
            _levelY = 0f;
            PatchLobbyWestStairDoor(greybox);
            RebuildNamedArea(greybox, "Stairwell_To2F");
            RebuildNamedArea(greybox, "Stairwell_To2F_Hall");
            BuildStairwellTo2FRoom(greybox);
            PatchB1EntranceEastDoor(greybox);
            RebuildNamedArea(greybox, "Stairwell_ToB1");
            BuildStairwellToB1Room(greybox);
        }

        Transform second = environment.Find("SecondFloor");
        if (second != null)
        {
            PatchSecondFloorStairLanding(second);
        }

        Transform basement = environment.Find("Basement");
        if (basement != null)
        {
            PatchBasementEntranceEastDoor(basement);
            Transform architecture = basement.Find("Architecture");
            if (architecture != null)
            {
                RebuildNamedArea(architecture, "Stairwell");
                _levelY = BasementY;
                BuildBasementStairwellRoom(architecture);
                _levelY = 0f;
            }
        }

        Transform connections = CreateGroup("FloorConnections", environment);
        Material stepMat = GetOrCreateMaterial("Greybox_Stair", new Color(0.40f, 0.40f, 0.38f));
        Material railMat = GetOrCreateMaterial("Greybox_Handrail", new Color(0.28f, 0.28f, 0.30f));
        AssetDatabase.SaveAssets();

        BuildUStair(
            connections, "Stair_1F_To_2F",
            Stair2XMin + 0.12f, Stair2XMax - 0.05f, Stair2ZMin + 0.12f, Stair2ZMax - 0.12f,
            0f, SecondFloorY, true, true, Stair2FlightWidth, stepMat, railMat);

        BuildUStair(
            connections, "Stair_1F_To_B1",
            StairBXMin + 0.12f, StairBXMax - 0.12f, 6.55f, StairBZMax - 0.15f,
            BasementY, 0f, false, true, StairBFlightWidth, stepMat, railMat);

        Selection.activeGameObject = connections.gameObject;
        Debug.Log("Deep Night Shelter: floor connections rebuilt (1F↔2F and 1F↔B1 U-stairs). Teleport markers were left in place.");
    }

    static void BuildStairwellTo2FRoom(Transform greybox)
    {
        Transform area = CreateArea(greybox, "Stairwell_To2F");
        Transform floor = area.Find("Floor");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "Stairwell_To2F_Floor", Stair2XMin, Stair2XMax, Stair2ZMin, Stair2ZMax, FloorCenterY, _floorMat);
        CreateSlab(floor, "Stairwell_To2F_Floor_Hall", Stair2XMax, LobbyXMin, Stair2HallZMin, Stair2HallZMax, FloorCenterY, _floorMat);

        BuildWallAlongX(walls, doorways, "Stairwell_To2F_Wall_South", Stair2XMin, Stair2XMax, Stair2ZMin, -1f, coverCorners: true);
        BuildWallAlongX(walls, doorways, "Stairwell_To2F_Wall_North", Stair2XMin, Stair2XMax, Stair2ZMax, 1f, coverCorners: true);
        BuildWallAlongZ(walls, doorways, "Stairwell_To2F_Wall_West", Stair2ZMin, Stair2ZMax, Stair2XMin, -1f, coverCorners: false);
        BuildWallAlongZ(
            walls, doorways, "Stairwell_To2F_Wall_East",
            Stair2ZMin, Stair2ZMax, Stair2XMax, 1f, coverCorners: false,
            Cut("Stairwell_To2F_Door_Hall", Stair2DoorZ, Stair2DoorW, DoorHeight));

        Transform hall = CreateArea(greybox, "Stairwell_To2F_Hall");
        Transform hallFloor = hall.Find("Floor");
        Transform hallWalls = hall.Find("Walls");
        Transform hallDoorways = hall.Find("Doorways");
        CreateSlab(hallFloor, "StairHall_Floor", Stair2XMax, LobbyXMin, Stair2HallZMin, Stair2HallZMax, FloorCenterY, _floorMat);
        BuildWallAlongX(hallWalls, hallDoorways, "StairHall_Wall_South", Stair2XMax, LobbyXMin, Stair2HallZMin, -1f, coverCorners: false);
        BuildWallAlongX(hallWalls, hallDoorways, "StairHall_Wall_North", Stair2XMax, LobbyXMin, Stair2HallZMax, 1f, coverCorners: false);
    }

    static void BuildStairwellToB1Room(Transform greybox)
    {
        Transform area = CreateArea(greybox, "Stairwell_ToB1");
        Transform floor = area.Find("Floor");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        // Keep a west walk strip from B1Entrance; leave the well open so the player can walk down.
        CreateSlab(floor, "Stairwell_ToB1_Floor_TopLanding", StairBXMin, StairBXMax, StairBZMin, 6.55f, FloorCenterY, _floorMat);
        CreateSlab(floor, "Stairwell_ToB1_Floor_B1Join", B1XMax, StairBXMin, StairBDoorZ - StairBDoorW * 0.5f, StairBDoorZ + StairBDoorW * 0.5f, FloorCenterY, _floorMat);
        CreateCube(
            walls, "Stairwell_ToB1_HoleRail",
            new Vector3(11.7f, 0.5f, 6.55f),
            new Vector3(1.6f, 0.9f, 0.08f),
            GetOrCreateMaterial("Greybox_Handrail", new Color(0.28f, 0.28f, 0.30f)),
            addCollider: true);

        BuildWallAlongX(walls, doorways, "Stairwell_ToB1_Wall_South", StairBXMin, StairBXMax, StairBZMin, -1f, coverCorners: true);
        BuildWallAlongX(walls, doorways, "Stairwell_ToB1_Wall_North", StairBXMin, StairBXMax, StairBZMax, 1f, coverCorners: true);
        BuildWallAlongZ(walls, doorways, "Stairwell_ToB1_Wall_East", StairBZMin, StairBZMax, StairBXMax, 1f, coverCorners: false);
        BuildWallAlongZ(
            walls, doorways, "Stairwell_ToB1_Wall_West",
            StairBZMin, StairBZMax, StairBXMin, -1f, coverCorners: false,
            Cut("Stairwell_ToB1_Door_B1Entrance", StairBDoorZ, StairBDoorW, DoorHeight));
    }

    static void BuildBasementStairwellRoom(Transform architecture)
    {
        Transform area = CreateArea(architecture, "Stairwell");
        Transform floor = area.Find("Floor");
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");

        CreateSlab(floor, "BasementStairwell_Floor", StairBXMin, StairBXMax, StairBZMin, StairBZMax, FloorCenterY, _floorMat);
        CreateSlab(floor, "BasementStairwell_Floor_Join", 9.0f, StairBXMin, StairBDoorZ - StairBDoorW * 0.5f, StairBDoorZ + StairBDoorW * 0.5f, FloorCenterY, _floorMat);

        BuildWallAlongX(walls, doorways, "BasementStairwell_Wall_South", StairBXMin, StairBXMax, StairBZMin, -1f, coverCorners: true);
        BuildWallAlongX(walls, doorways, "BasementStairwell_Wall_North", StairBXMin, StairBXMax, StairBZMax, 1f, coverCorners: true);
        BuildWallAlongZ(walls, doorways, "BasementStairwell_Wall_East", StairBZMin, StairBZMax, StairBXMax, 1f, coverCorners: false);
        BuildWallAlongZ(
            walls, doorways, "BasementStairwell_Wall_West",
            StairBZMin, StairBZMax, StairBXMin, -1f, coverCorners: false,
            Cut("BasementStairwell_Door_Entrance", StairBDoorZ, StairBDoorW, DoorHeight));
    }

    static void PatchLobbyWestStairDoor(Transform greybox)
    {
        Transform lobby = greybox.Find("Lobby");
        if (lobby == null)
        {
            return;
        }

        Transform walls = lobby.Find("Walls");
        Transform doorways = lobby.Find("Doorways");
        if (walls == null || doorways == null)
        {
            return;
        }

        DestroyChildrenPrefixed(walls, "Lobby_Wall_West");
        BuildWallAlongZ(
            walls, doorways, "Lobby_Wall_West",
            LobbyZMin, LobbyZMax, LobbyXMin, -1f, coverCorners: false,
            Cut("Lobby_Door_StairwellTo2F", Stair2DoorZ, Stair2DoorW, DoorHeight));
    }

    static void PatchB1EntranceEastDoor(Transform greybox)
    {
        Transform area = greybox.Find("B1Entrance");
        if (area == null)
        {
            return;
        }

        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");
        Transform floor = area.Find("Floor");
        if (walls == null || doorways == null)
        {
            return;
        }

        DestroyChildrenPrefixed(walls, "B1Entrance_Wall_East");
        BuildWallAlongZ(
            walls, doorways, "B1Entrance_Wall_East",
            B1ZMin, B1ZMax, B1XMax, 1f, coverCorners: false,
            Cut("B1Entrance_Door_StairwellToB1", StairBDoorZ, StairBDoorW, DoorHeight));

        if (floor != null)
        {
            CreateSlab(floor, "B1Entrance_Floor_StairJoin", B1XMax, StairBXMin, StairBDoorZ - StairBDoorW * 0.5f, StairBDoorZ + StairBDoorW * 0.5f, FloorCenterY, _floorMat);
        }
    }

    static void PatchSecondFloorStairLanding(Transform secondFloor)
    {
        Transform architecture = secondFloor.Find("Architecture");
        if (architecture == null)
        {
            return;
        }

        Transform landing = architecture.Find("StairLanding");
        if (landing == null)
        {
            return;
        }

        _levelY = SecondFloorY;
        Transform floor = landing.Find("Floor");
        Transform walls = landing.Find("Walls");
        Transform doorways = landing.Find("Doorways");
        if (floor != null)
        {
            DestroyChildrenPrefixed(floor, "StairLanding_Floor");
            CreateSlab(floor, "StairLanding_Floor", -6.2f, -2.2f, 1.8f, 5.8f, FloorCenterY, _floorMat);
            CreateSlab(floor, "StairLanding_Floor_NorthJoin", -5.0f, -2.2f, 5.78f, 5.82f, FloorCenterY, _floorMat);
            CreateSlab(floor, "StairLanding_Floor_StairJoin", Stair2XMax, -6.2f, Stair2DoorZ - Stair2DoorW * 0.5f, Stair2DoorZ + Stair2DoorW * 0.5f, FloorCenterY, _floorMat);
        }

        if (walls != null && doorways != null)
        {
            DestroyNamed(walls, "StairLanding_StairWellBlock");
            DestroyChildrenPrefixed(walls, "StairLanding_Wall_West");
            DestroyChildrenPrefixed(walls, "StairLanding_Wall_South");
            DestroyChildrenPrefixed(walls, "StairLanding_Wall_SouthEast");
            BuildWallAlongX(walls, doorways, "StairLanding_Wall_South", -6.2f, -2.2f, 1.8f, -1f, coverCorners: true);
            BuildWallAlongZ(
                walls, doorways, "StairLanding_Wall_West",
                1.8f, 5.8f, -6.2f, -1f, coverCorners: false,
                Cut("StairLanding_Door_Stairwell", Stair2DoorZ, Stair2DoorW, DoorHeight));
        }

        RebuildNamedArea(architecture, "Stairwell");
        Transform well = CreateArea(architecture, "Stairwell");
        Transform wellWalls = well.Find("Walls");
        Transform wellDoorways = well.Find("Doorways");
        Transform wellCeiling = well.Find("Ceiling");
        CreateSlab(wellCeiling, "Stairwell_2F_Ceiling", Stair2XMin, Stair2XMax, Stair2ZMin, Stair2ZMax, CeilingCenterY, _ceilingMat);
        BuildWallAlongX(wellWalls, wellDoorways, "Stairwell_2F_Wall_South", Stair2XMin, Stair2XMax, Stair2ZMin, -1f, coverCorners: true);
        BuildWallAlongX(wellWalls, wellDoorways, "Stairwell_2F_Wall_North", Stair2XMin, Stair2XMax, Stair2ZMax, 1f, coverCorners: true);
        BuildWallAlongZ(wellWalls, wellDoorways, "Stairwell_2F_Wall_West", Stair2ZMin, Stair2ZMax, Stair2XMin, -1f, coverCorners: false);
        BuildWallAlongZ(
            wellWalls, wellDoorways, "Stairwell_2F_Wall_East",
            Stair2ZMin, Stair2ZMax, Stair2XMax, 1f, coverCorners: false,
            Cut("Stairwell_2F_Door_Landing", Stair2DoorZ, Stair2DoorW, DoorHeight));

        Transform ceiling = landing.Find("Ceiling");
        if (ceiling != null)
        {
            DestroyChildrenPrefixed(ceiling, "StairLanding_Ceiling");
            CreateSlab(ceiling, "StairLanding_Ceiling", -6.2f, -2.2f, 1.8f, 5.8f, CeilingCenterY, _ceilingMat);
        }

        _levelY = 0f;
    }

    static void PatchBasementEntranceEastDoor(Transform basement)
    {
        Transform architecture = basement.Find("Architecture");
        if (architecture == null)
        {
            return;
        }

        Transform area = architecture.Find("BasementEntrance");
        if (area == null)
        {
            return;
        }

        _levelY = BasementY;
        Transform walls = area.Find("Walls");
        Transform doorways = area.Find("Doorways");
        Transform floor = area.Find("Floor");
        if (walls != null && doorways != null)
        {
            DestroyChildrenPrefixed(walls, "BasementEntrance_Wall_East");
            BuildWallAlongZ(
                walls, doorways, "BasementEntrance_Wall_East",
                5.6f, 8.6f, 9.0f, 1f, coverCorners: false,
                Cut("BasementEntrance_Door_Stairwell", StairBDoorZ, StairBDoorW, DoorHeight));
        }

        if (floor != null)
        {
            CreateSlab(floor, "BasementEntrance_Floor_StairJoin", 9.0f, StairBXMin, StairBDoorZ - StairBDoorW * 0.5f, StairBDoorZ + StairBDoorW * 0.5f, FloorCenterY, _floorMat);
        }

        _levelY = 0f;
    }

    static void BuildUStair(
        Transform parent, string name,
        float xMin, float xMax, float zMin, float zMax,
        float bottomY, float topY,
        bool firstFlightOnWest, bool firstGoesPositiveZ,
        float flightWidth, Material stepMat, Material railMat)
    {
        Transform root = CreateGroup(name, parent);
        float midY = bottomY + StairRiser * StairStepsPerFlight;
        float run = StairTread * StairStepsPerFlight;
        float landingDepth = Mathf.Max(1.5f, (zMax - zMin) - run);

        float westX0 = xMin;
        float westX1 = xMin + flightWidth;
        float eastX0 = xMax - flightWidth;
        float eastX1 = xMax;

        float flightZ0;
        float flightZ1;
        float landZ0;
        float landZ1;
        if (firstGoesPositiveZ)
        {
            flightZ0 = zMin;
            flightZ1 = zMin + run;
            landZ0 = flightZ1;
            landZ1 = Mathf.Min(zMax, flightZ1 + landingDepth);
        }
        else
        {
            flightZ1 = zMax;
            flightZ0 = zMax - run;
            landZ1 = flightZ0;
            landZ0 = Mathf.Max(zMin, flightZ0 - landingDepth);
        }

        float flight1X0 = firstFlightOnWest ? westX0 : eastX0;
        float flight1X1 = firstFlightOnWest ? westX1 : eastX1;
        float flight2X0 = firstFlightOnWest ? eastX0 : westX0;
        float flight2X1 = firstFlightOnWest ? eastX1 : westX1;

        int zSign1 = firstGoesPositiveZ ? 1 : -1;
        int zSign2 = -zSign1;

        BuildStairFlight(root, "Flight_01", flight1X0, flight1X1, flightZ0, flightZ1, bottomY, midY, zSign1, stepMat, railMat);
        BuildStairFlight(root, "Flight_02", flight2X0, flight2X1, flightZ0, flightZ1, midY, topY, zSign2, stepMat, railMat);

        Transform landing = CreateGroup("Landing", root);
        CreateCube(
            landing, "Landing_Slab",
            new Vector3((xMin + xMax) * 0.5f, midY + 0.05f, (landZ0 + landZ1) * 0.5f),
            new Vector3(xMax - xMin, 0.1f, Mathf.Abs(landZ1 - landZ0)),
            stepMat, addCollider: true);

        BuildRailing(landing, "Landing_Rail_North", (xMin + xMax) * 0.5f, landZ0, landZ1, false);
        CreateCube(
            landing, "Landing_Rail_Outer",
            new Vector3((xMin + xMax) * 0.5f, midY + 0.5f, firstGoesPositiveZ ? landZ1 - 0.05f : landZ0 + 0.05f),
            new Vector3(xMax - xMin, 0.9f, 0.08f),
            railMat, addCollider: true);
    }

    static void BuildStairFlight(
        Transform parent, string name,
        float xMin, float xMax, float zMin, float zMax,
        float yBottom, float yTop, int zSign,
        Material stepMat, Material railMat)
    {
        Transform flight = CreateGroup(name, parent);
        float width = xMax - xMin;
        float x = (xMin + xMax) * 0.5f;
        float run = Mathf.Abs(zMax - zMin);

        for (int i = 0; i < StairStepsPerFlight; i++)
        {
            float y = yBottom + (i + 0.5f) * StairRiser;
            float z;
            if (zSign > 0)
            {
                z = zMin + (i + 0.5f) * StairTread;
            }
            else
            {
                z = zMax - (i + 0.5f) * StairTread;
            }

            CreateCube(
                flight, "Step_" + (i + 1).ToString("00"),
                new Vector3(x, y, z),
                new Vector3(width, StairRiser, StairTread),
                stepMat, addCollider: false);
        }

        Vector3 start = new Vector3(x, yBottom, zSign > 0 ? zMin : zMax);
        Vector3 end = new Vector3(x, yTop, zSign > 0 ? zMax : zMin);
        Vector3 along = end - start;
        float length = along.magnitude;
        Quaternion rot = Quaternion.LookRotation(along.normalized, Vector3.up);
        GameObject ramp = CreateCube(
            flight, "WalkRamp",
            (start + end) * 0.5f,
            new Vector3(width * 0.92f, 0.08f, length),
            stepMat, addCollider: true);
        ramp.transform.rotation = rot;
        MeshRenderer rampRenderer = ramp.GetComponent<MeshRenderer>();
        if (rampRenderer != null)
        {
            rampRenderer.enabled = false;
        }

        float railZ = (zMin + zMax) * 0.5f;
        CreateCube(
            flight, "Handrail_Outer",
            new Vector3(xMin + 0.04f, (yBottom + yTop) * 0.5f + 0.45f, railZ),
            new Vector3(0.06f, 0.9f, run),
            railMat, addCollider: true);
        CreateCube(
            flight, "Handrail_Inner",
            new Vector3(xMax - 0.04f, (yBottom + yTop) * 0.5f + 0.45f, railZ),
            new Vector3(0.06f, 0.9f, run),
            railMat, addCollider: true);
    }

    static void BuildRailing(Transform parent, string name, float x, float zMin, float zMax, bool alongZ)
    {
        Material railMat = GetOrCreateMaterial("Greybox_Handrail", new Color(0.28f, 0.28f, 0.30f));
        if (alongZ)
        {
            CreateCube(
                parent, name,
                new Vector3(x, _levelY + 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(0.08f, 0.9f, Mathf.Abs(zMax - zMin)),
                railMat, addCollider: true);
        }
        else
        {
            CreateCube(
                parent, name,
                new Vector3(x, _levelY + 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(0.08f, 0.9f, Mathf.Abs(zMax - zMin)),
                railMat, addCollider: true);
        }
    }

    static void RebuildNamedArea(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }

    static void DestroyNamed(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    static void DestroyChildrenPrefixed(Transform parent, string prefix)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith(prefix))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }
}
