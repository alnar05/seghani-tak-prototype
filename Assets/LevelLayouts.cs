using System.Collections.Generic;
using UnityEngine;

public partial class SeghaniTakSceneBuilder
{
    readonly List<BoxCollider> shelters = new List<BoxCollider>();
    readonly List<FallingProp> props = new List<FallingProp>();
    readonly List<HazardCue> cues = new List<HazardCue>();
    static readonly Color Plaster = new Color(.79f, .74f, .64f);
    static readonly Color StudyWall = new Color(.59f, .67f, .59f);
    static readonly Color FloorWood = new Color(.45f, .31f, .24f);
    static readonly Color Trim = new Color(.31f, .27f, .23f);

    void BuildLayout(int index)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.47f, .48f, .51f);
        if (index < 3)
        {
            BuildShell(0f, true, true);
            if (index == 0) BuildLivingRoom();
            else if (index == 1) BuildOffice();
            else BuildKitchen();
        }
        else BuildTwoStoreys(index == 4);
    }

    void BuildShell(float floor, bool fullFloor, bool ceiling)
    {
        float w = roomWidth, d = roomDepth, h = roomHeight, t = wallThickness;
        if (fullFloor) FloorSection(floor, -w / 2f, w / 2f, -d / 2f, d / 2f);
        if (ceiling) Box("Ceiling", new Vector3(0f, floor + h + .05f, 0f),
            new Vector3(w + 2f * t, .1f, d + 2f * t), Ivory);
        Box("Outer_South", new Vector3(0f, floor + h / 2f, -d / 2f - t / 2f), new Vector3(w + 2f * t, h, t), Plaster);
        Box("Outer_North", new Vector3(0f, floor + h / 2f, d / 2f + t / 2f), new Vector3(w + 2f * t, h, t), Plaster);
        Box("Outer_West", new Vector3(-w / 2f - t / 2f, floor + h / 2f, 0f), new Vector3(t, h, d), Plaster);
        Box("Outer_East", new Vector3(w / 2f + t / 2f, floor + h / 2f, 0f), new Vector3(t, h, d), StudyWall);
        foreach (float side in new[] { -1f, 1f })
        {
            Model(root, "Long baseboard", new Vector3(0f, floor + .065f, side * (d / 2f - .024f)),
                new Vector3(w, .13f, .044f), Trim);
            Model(root, "Short baseboard", new Vector3(side * (w / 2f - .024f), floor + .065f, 0f),
                new Vector3(.044f, .13f, d), Trim);
        }
    }

    void FloorSection(float floor, float xMin, float xMax, float zMin, float zMax)
    {
        const float thickness = .2f;
        float width = xMax - xMin, depth = zMax - zMin;
        var section = Box("Oak floor", new Vector3((xMin + xMax) / 2f, floor - thickness / 2f, (zMin + zMax) / 2f),
            new Vector3(width, thickness, depth), FloorWood);
        section.GetComponent<Renderer>().sharedMaterial = FloorMaterial(width, depth, xMin, zMin);
    }

    // Door openings are pairs of z-center and width. Caller orders them south to north.
    void Partition(float x, float floor, Color wall, params Vector2[] openings)
    {
        float start = -roomDepth / 2f;
        const float header = 2.27f;
        foreach (var opening in openings)
        {
            float a = opening.x - opening.y / 2f, b = opening.x + opening.y / 2f;
            WallSection(x, floor, start, a, wall);
            Box("Door header", new Vector3(x, floor + (roomHeight + header) / 2f, opening.x),
                new Vector3(wallThickness, roomHeight - header, opening.y), wall);
            Model(root, "Door frame top", new Vector3(x, floor + header - .018f, opening.x),
                new Vector3(wallThickness + .07f, .048f, opening.y), Trim);
            foreach (float z in new[] { a, b })
                Model(root, "Door jamb", new Vector3(x, floor + header / 2f, z),
                    new Vector3(wallThickness + .07f, header, .065f), Trim);
            start = b;
        }
        WallSection(x, floor, start, roomDepth / 2f, wall);
    }

    void WallSection(float x, float floor, float a, float b, Color wall)
    {
        if (b <= a) return;
        Box("Partition", new Vector3(x, floor + roomHeight / 2f, (a + b) / 2f),
            new Vector3(wallThickness, roomHeight, b - a), wall);
        foreach (float side in new[] { -1f, 1f })
            Model(root, "Partition baseboard", new Vector3(x + side * (wallThickness / 2f + .017f),
                floor + .065f, (a + b) / 2f), new Vector3(.032f, .13f, b - a), Trim);
    }

    void RoomLight(float x, float floor, float z, float range = 6.3f, float intensity = 1.36f)
    {
        var light = Node("Warm room lighting", root, new Vector3(x, floor + roomHeight - .48f, z))
            .gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .91f, .78f);
        light.range = range;
        light.intensity = intensity;
    }

    void Register(FallingProp prop, Vector3 center, Vector3 size, float earliest, float latest,
        float warning, Bounds? approach = null)
    {
        prop.predictedArea = new Bounds(center, size);
        props.Add(prop);
        if (latest >= earliest) cues.Add(new HazardCue(prop, earliest, latest, warning, approach));
    }

    void RegisterShelf(FallingProp prop, float earliest, float latest, float warning)
    {
        Vector3 pos = prop.transform.position + prop.transform.forward * .72f;
        Vector3 size = Mathf.Abs(prop.transform.forward.x) > .5f ?
            new Vector3(2.16f, 1.92f, 1.79f) : new Vector3(1.79f, 1.92f, 2.16f);
        Register(prop, pos, size, earliest, latest, warning);
    }

    void RegisterLight(FallingProp prop, Vector2 footprint, float floor, float earliest, float latest,
        float warning, Bounds? approach = null)
    {
        Vector3 pos = prop.transform.position;
        Register(prop, new Vector3(pos.x, (pos.y + floor) / 2f, pos.z),
            new Vector3(footprint.x, pos.y - floor + .15f, footprint.y), earliest, latest, warning, approach);
    }

    void BuildLivingRoom()
    {
        var doors = new[] { new Vector2(-1.65f, 1.8f), new Vector2(1.65f, 1.8f) };
        Partition(-2f, 0f, Plaster, doors);
        Partition(2f, 0f, StudyWall, doors);
        Box("Passage divider", new Vector3(0f, 1.5f, 0f), new Vector3(4f, 3f, .8f), Plaster);
        foreach (float z in new[] { -3.025f, 3.025f })
            Box("Passage outer partition", new Vector3(0f, 1.5f, z), new Vector3(4f, 3f, .95f), Plaster);
        RoomLight(-4.15f, 0f, .14f);
        RoomLight(.0f, 0f, -1.65f, 4.3f, .92f);
        RoomLight(.0f, 0f, 1.65f, 4.3f, .92f);
        RoomLight(4.5f, 0f, -.5f);
        RoomLight(4.5f, 0f, 2.64f, 3.6f, .9f);

        BuildCouch(new Vector3(-3.5f, 0f, -3.02f), 0f, 1.79f);
        BuildRug(new Vector3(-4.2f, 0f, -.08f), new Vector2(2.65f, 2.9f), Cloth);
        BuildArt(new Vector3(-3.6f, 1.71f, -3.45f), 0f, Moss);
        BuildArt(new Vector3(-3.4f, 1.9f, 3.46f), 180f, Rust);
        BuildRug(new Vector3(4.92f, 0f, 0f), new Vector2(1.95f, 2.6f), Moss);
        BuildCover("Desk", new Vector3(5.06f, 0f, 0f), 90f);
        level.SetDeskPosition(new Vector3(5.06f, 0f, 0f), 4.46f);
        BuildBookcase("Study bookcase", new Vector3(5.62f, 0f, 2.34f), -90f, false);
        BuildArt(new Vector3(5.95f, 1.96f, -.18f), -90f, Cloth);

        var window = BuildWindow("Living room window sash", new Vector3(-5.99f, 1.62f, -2.51f), 90f);
        var tv = BuildTelevision(new Vector3(-3.4f, 0f, 3.17f), 180f);
        var south = BuildBookcase("South passage bookshelf", new Vector3(1f, 0f, -2.217f), 0f);
        var north = BuildBookcase("North passage bookshelf", new Vector3(1f, 0f, 2.217f), 180f);
        // The seed chooses which corridor falls first. Their fall directions are fixed by placement.
        int openingSide = new System.Random(level.seed).Next(2);
        RegisterShelf(south, level.openingDelayAfterOnset, openingSide == 0 ? 2.15f : -1f, level.openingWarningSeconds);
        RegisterShelf(north, level.openingDelayAfterOnset, openingSide == 1 ? 2.15f : -1f, level.openingWarningSeconds);

        var southLight = BuildFixture("South passage ceiling light", new Vector3(3.05f, 2.76f, -1.65f),
            new Vector3(.99f, .16f, 1.14f));
        var northLight = BuildFixture("North passage ceiling light", new Vector3(3.05f, 2.76f, 1.65f),
            new Vector3(.99f, .16f, 1.14f));
        RegisterLight(southLight, new Vector2(1.15f, 1.29f), 0f, 2.7f, 10f, 1.78f,
            new Bounds(new Vector3(-.43f, 1f, -1.65f), new Vector3(5.1f, 2.5f, 1.7f)));
        RegisterLight(northLight, new Vector2(1.15f, 1.29f), 0f, 2.7f, 10f, 1.78f,
            new Bounds(new Vector3(-.43f, 1f, 1.65f), new Vector3(5.1f, 2.5f, 1.7f)));
        var studyLight = BuildFixture("Study pendant", new Vector3(3.3f, 2.53f, 1.07f),
            new Vector3(.64f, .18f, .66f), pendant: true);
        RegisterLight(studyLight, new Vector2(.78f, .77f), 0f, 9f, 11f, 1.6f);
        Register(window, new Vector3(-5.29f, 1.13f, -2.51f), new Vector3(1.6f, 2.28f, 1.56f), 5.8f, 8.8f, 2.3f);
        Register(tv, new Vector3(-3.4f, .63f, 2.68f), new Vector3(1.5f, 1.32f, 1.6f), 6.2f, 9.4f, 2.2f);
    }

    void BuildOffice()
    {
        float h = roomHeight;
        Box("Central office partition", new Vector3(-.55f, h / 2f, -.55f), new Vector3(5.5f, h, 2.3f), StudyWall);
        Box("Filing room screen", new Vector3(2.95f, h / 2f, -1.78f), new Vector3(.18f, h, 2.15f), Plaster);
        BuildRug(new Vector3(4.81f, 0f, .41f), new Vector2(2.7f, 3.1f), Moss);
        BuildCover("Office Desk", new Vector3(5.33f, 0f, .32f), 90f, 2.32f, 1.27f);
        BuildCouch(new Vector3(-5.99f, 0f, 2.83f), 90f, 1.81f);
        BuildBookcase("Office record cabinet", new Vector3(5.36f, 0f, 3.78f), 180f, false);
        BuildArt(new Vector3(6.44f, 1.94f, .19f), -90f, Cloth);
        BuildArt(new Vector3(-1.8f, 1.7f, .638f), 0f, Rust);
        RoomLight(-5.2f, 0f, -.47f);
        RoomLight(-1.5f, 0f, 2.47f);
        RoomLight(-1.0f, 0f, -3.17f);
        RoomLight(4.76f, 0f, .4f);

        var shelf = BuildBookcase("Office archive bookshelf", new Vector3(-.95f, 0f, -2.054f), 180f);
        RegisterShelf(shelf, 1f, 4.15f, 2.95f);
        var pendant = BuildFixture("North office pendant", new Vector3(.36f, 2.48f, 2.44f),
            new Vector3(1.17f, .18f, 1.28f), pendant: true);
        RegisterLight(pendant, new Vector2(1.28f, 1.43f), 0f, 5.2f, 10.75f, 2f,
            new Bounds(new Vector3(-1.55f, 1f, 2.46f), new Vector3(5.9f, 2.5f, 3.55f)));
        var exitLight = BuildFixture("Office threshold light", new Vector3(3.38f, 2.65f, -3.4f),
            new Vector3(.85f, .16f, 1.25f));
        RegisterLight(exitLight, new Vector2(1f, 1.37f), 0f, 8.8f, 13.5f, 2.1f);
        var window = BuildWindow("Office window sash", new Vector3(-3.9f, 1.64f, 4.285f), 180f);
        Register(window, new Vector3(-3.9f, 1.2f, 3.55f), new Vector3(1.55f, 2.4f, 1.8f), 9.5f, 13.2f, 2.1f);
    }

    void BuildKitchen()
    {
        Partition(.76f, 0f, StudyWall, new Vector2(-1.02f, 3.6f), new Vector2(3.04f, 1.8f));
        BuildCounter(new Vector3(-3.28f, 0f, -3.94f), 0f, 4.95f);
        BuildCounter(new Vector3(-6.13f, 0f, -2.44f), 90f, 2.6f);
        BuildCounter(new Vector3(-2.76f, 0f, 2.02f), 180f, 2.32f);
        BuildRug(new Vector3(4.35f, 0f, .2f), new Vector2(3.54f, 2.74f), Rust);
        BuildCover("Dining Table", new Vector3(4.42f, 0f, .2f), 90f, 2.48f, 1.63f);
        BuildCouch(new Vector3(4.34f, 0f, 3.83f), 180f, 2.24f);
        BuildArt(new Vector3(6.45f, 1.95f, -.11f), -90f, Cloth);
        RoomLight(-4.43f, 0f, -1.2f);
        RoomLight(-1.73f, 0f, 2.9f);
        RoomLight(2.67f, 0f, -.8f);
        RoomLight(5.15f, 0f, 2.49f);

        var bowl = BuildCupboardContents(new Vector3(-1.05f, 1.62f, -3.92f), 0f);
        Register(bowl, new Vector3(-1.05f, .99f, -2.96f), new Vector3(.84f, 1.98f, 1.55f), .65f, 4f, 2.25f);
        var light = BuildFixture("Kitchen opening light", new Vector3(-.22f, 2.64f, -1.48f),
            new Vector3(.93f, .15f, 1.29f), pendant: true);
        RegisterLight(light, new Vector2(1.08f, 1.44f), 0f, 4.4f, 9.6f, 2.15f,
            new Bounds(new Vector3(-2.05f, 1f, -1.04f), new Vector3(5.1f, 2.5f, 3.3f)));
        var window = BuildWindow("Kitchen window sash", new Vector3(-1.89f, 1.65f, 4.28f), 180f);
        Register(window, new Vector3(-1.89f, 1.14f, 3.53f), new Vector3(1.55f, 2.3f, 1.65f), 7f, 12.1f, 2.35f);
        var sideLight = BuildFixture("Back passage light", new Vector3(1.41f, 2.68f, 3.05f),
            new Vector3(.89f, .156f, .98f));
        RegisterLight(sideLight, new Vector2(1.04f, 1.1f), 0f, 11.5f, 15.5f, 1.95f);
    }

    void BuildTwoStoreys(bool fullHouse)
    {
        const float upper = Level1Configuration.UpperFloor;
        float stairX = -.12f, startZ = -roomDepth / 2f + .82f;
        BuildShell(0f, true, false);
        BuildShell(upper, false, true);
        BuildStairs(stairX, startZ);
        var openings = new[] { new Vector2(-2.58f, 2.08f), new Vector2(2.65f, 2.44f) };
        Partition(-2.05f, upper, Plaster, openings);
        if (fullHouse) Partition(2.12f, upper, StudyWall, openings[0], new Vector2(0f, 1.12f), openings[1]);
        else Partition(2.12f, upper, StudyWall, openings);
        BuildBed(new Vector3(-4.66f, upper, -3.16f), 0f);
        BuildRug(new Vector3(-4.8f, upper, -.56f), new Vector2(2.7f, 1.84f), Cloth);
        BuildArt(new Vector3(-roomWidth / 2f + .05f, upper + 1.84f, -.2f), 90f, fullHouse ? Cloth : Moss);
        BuildCover("Study Desk", new Vector3(roomWidth / 2f - 1.04f, upper, .17f), 90f, 2.15f, 1.29f);
        BuildRug(new Vector3(roomWidth / 2f - 1.22f, upper, .17f), new Vector2(1.9f, 2.7f), Moss);
        BuildArt(new Vector3(roomWidth / 2f - .055f, upper + 1.97f, .15f), -90f, Brass);
        RoomLight(-4.63f, upper, .1f);
        RoomLight(.37f, upper, 2.75f);
        RoomLight(.27f, upper, -2.9f, 4.25f, 1f);
        RoomLight(roomWidth / 2f - 1.6f, upper, .1f);

        var upperShelf = BuildBookcase("Study entry bookshelf", new Vector3(3.38f, upper, fullHouse ? -3.58f : -3.72f), 0f);
        RegisterShelf(upperShelf, fullHouse ? 6f : 1.3f, fullHouse ? 13f : 5.5f, 2.92f);
        var hall = BuildFixture("Upper hall light", new Vector3(1.48f, upper + 2.68f, 2.56f),
            new Vector3(.83f, .15f, 1.14f));
        RegisterLight(hall, new Vector2(.97f, 1.29f), upper, 6f, 12f, 2.1f);
        var bedroomWindow = BuildWindow("Bedroom window sash", new Vector3(-4.65f, upper + 1.67f, roomDepth / 2f - .02f), 180f);
        Register(bedroomWindow, new Vector3(-4.65f, upper + 1.17f, roomDepth / 2f - .76f),
            new Vector3(1.57f, 2.38f, 1.62f), 12.5f, 16.1f, 2.2f);

        float tableX = roomWidth / 2f - 1.9f;
        BuildCover(fullHouse ? "Dining Table" : "Sitting Room Table", new Vector3(tableX, 0f, .51f), 90f, 2.28f, 1.56f);
        BuildRug(new Vector3(tableX, 0f, .51f), new Vector2(2.6f, 3.1f), Rust);
        BuildCouch(new Vector3(-4.84f, 0f, -roomDepth / 2f + .47f), 0f);
        BuildArt(new Vector3(-4.84f, 1.79f, -roomDepth / 2f + .045f), 0f, Moss);
        RoomLight(-4.63f, 0f, -.87f);
        RoomLight(-1.91f, 0f, 2.35f);
        RoomLight(3.46f, 0f, -2.54f);
        RoomLight(tableX, 0f, 1.17f);

        if (fullHouse)
        {
            Partition(2.12f, 0f, StudyWall, new Vector2(-2.55f, 2.28f),
                new Vector2(0f, 1.12f), new Vector2(2.55f, 2.28f));
            BuildCounter(new Vector3(-4.85f, 0f, roomDepth / 2f - .43f), 180f, 3.52f);
            var bowl = BuildCupboardContents(new Vector3(-3.99f, 1.62f, roomDepth / 2f - .4f), 180f);
            Register(bowl, new Vector3(-3.99f, .96f, roomDepth / 2f - 1.52f),
                new Vector3(.89f, 1.96f, 1.65f), .95f, 5.5f, 2.26f);
            var lowerShelf = BuildBookcase("Dining entry bookshelf", new Vector3(3.52f, 0f, -3.78f), 0f);
            RegisterShelf(lowerShelf, 4.85f, 11.55f, 2.93f);
            BuildHouseDoors(upper);
        }
        var lowerLight = BuildFixture("Sitting room pendant", new Vector3(3.27f, 2.47f, 2.54f),
            new Vector3(.9f, .18f, 1.03f), pendant: true);
        RegisterLight(lowerLight, new Vector2(1.05f, 1.15f), 0f, 9.2f, 17f, 2.38f);
    }

    void BuildStairs(float x, float startZ)
    {
        const int steps = 16;
        const float rise = .2f, tread = .31f, halfWidth = .75f;
        float upper = Level1Configuration.UpperFloor;
        float endZ = startZ + steps * tread;
        float w = roomWidth / 2f, d = roomDepth / 2f;
        FloorSection(upper, -w, x - halfWidth, -d, d);
        FloorSection(upper, x + halfWidth, w, -d, d);
        FloorSection(upper, x - halfWidth, x + halfWidth, -d, startZ);
        FloorSection(upper, x - halfWidth, x + halfWidth, endZ, d);

        for (int i = 0; i < steps; i++)
        {
            float top = (i + 1) * rise;
            float z = startZ + (i + .5f) * tread;
            Box("Timber stair tread", new Vector3(x, top / 2f, z),
                new Vector3(2f * halfWidth, top, tread), i % 2 == 0 ? Oak : Oak * .91f);
        }
        foreach (float side in new[] { -1f, 1f })
        {
            float railX = x + side * (halfWidth + .055f), midZ = (startZ + endZ) / 2f;
            var guard = Node("Upper landing guard", root, new Vector3(railX, upper + .5f, midZ));
            Solid(guard, Vector3.zero, new Vector3(.12f, 1f, endZ - startZ));
            Model(guard, "Top rail", new Vector3(0f, .46f, 0f), new Vector3(.12f, .09f, endZ - startZ), Walnut);
            for (int i = 0; i <= steps; i += 2)
                Model(guard, "Baluster", new Vector3(0f, 0f, startZ - midZ + i * tread),
                    new Vector3(.065f, .95f, .065f), Oak);
        }
    }

    Texture2D floorGrain;
    Material FloorMaterial(float width, float depth, float x, float z)
    {
        if (!floorGrain)
        {
            const int resolution = 128;
            floorGrain = new Texture2D(resolution, resolution);
            floorGrain.wrapMode = TextureWrapMode.Repeat;
            floorGrain.filterMode = FilterMode.Bilinear;
            for (int py = 0; py < resolution; py++)
            for (int px = 0; px < resolution; px++)
            {
                int row = py / 16;
                int seam = (px + row * 31) % resolution;
                float grain = Mathf.PerlinNoise(px * .04f, py * .22f);
                float varied = .88f + grain * .14f + (row % 3) * .035f;
                bool joint = py % 16 == 0 || seam < 1;
                floorGrain.SetPixel(px, py, FloorWood * (joint ? .71f : varied));
            }
            floorGrain.Apply();
        }

        var material = baseMaterial ? new Material(baseMaterial) : new Material(Shader.Find("Standard"));
        material.color = Color.white;
        material.mainTexture = floorGrain;
        material.mainTextureScale = new Vector2(width / 2f, depth / 2f);
        material.mainTextureOffset = new Vector2(x / 2f, z / 2f);
        material.SetFloat("_Glossiness", .15f);
        runtimeMaterials.Add(material);
        return material;
    }
}
