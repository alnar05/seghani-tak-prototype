using UnityEngine;

public partial class SeghaniTakSceneBuilder
{
    static readonly Color Oak = new Color(.63f, .42f, .25f);
    static readonly Color Walnut = new Color(.33f, .21f, .15f);
    static readonly Color Ink = new Color(.13f, .19f, .21f);
    static readonly Color Cloth = new Color(.23f, .42f, .45f);
    static readonly Color Linen = new Color(.83f, .77f, .63f);
    static readonly Color Rust = new Color(.65f, .32f, .23f);
    static readonly Color Moss = new Color(.28f, .43f, .32f);
    static readonly Color Brass = new Color(.79f, .58f, .29f);
    static readonly Color Ivory = new Color(.91f, .88f, .78f);
    static readonly Color[] BookColors = { Cloth, Rust, Brass, Linen, Moss };

    Transform Node(string name, Transform parent, Vector3 position, float yaw = 0f)
    {
        var node = new GameObject(name).transform;
        node.SetParent(parent, false);
        node.localPosition = position;
        node.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return node;
    }

    GameObject Model(Transform parent, string name, Vector3 position, Vector3 size, Color color,
        PrimitiveType primitive = PrimitiveType.Cube)
    {
        var item = GameObject.CreatePrimitive(primitive);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = size;
        DestroyImmediate(item.GetComponent<Collider>());
        Tint(item, color);
        return item;
    }

    BoxCollider Solid(Transform parent, Vector3 position, Vector3 size)
    {
        var box = parent.gameObject.AddComponent<BoxCollider>();
        box.center = position;
        box.size = size;
        return box;
    }

    Transform BuildCover(string name, Vector3 position, float yaw, float width = 2f, float depth = 1.2f)
    {
        var table = Node(name, root, position, yaw);
        const float clearance = 1.04f, leg = .105f;
        Vector3 top = new Vector3(0f, clearance + .052f, 0f);
        Vector3 size = new Vector3(width, .104f, depth);
        Solid(table, top, size);
        Model(table, "Solid wood top", top, size, Oak);

        float xInset = (width - .16f) * .5f;
        float zInset = (depth - .16f) * .5f;
        foreach (float x in new[] { -xInset, xInset })
        foreach (float z in new[] { -zInset, zInset })
        {
            var foot = new Vector3(x, clearance * .5f, z);
            var post = new Vector3(leg, clearance, leg);
            Solid(table, foot, post);
            Model(table, "Tapered oak leg", foot, post, Walnut);
        }
        Model(table, "Top lip", new Vector3(0f, clearance + .085f, 0f),
            new Vector3(width + .015f, .035f, depth + .015f), Oak * .91f);
        // The controller's contact skin can be slightly below the rendered floor.
        const float floorTolerance = .035f;
        var volume = Node("Shelter clearance", table, new Vector3(0f, (clearance - floorTolerance) * .5f, 0f));
        var trigger = Solid(volume, Vector3.zero, new Vector3(width - .26f, clearance + floorTolerance, depth - .26f));
        trigger.isTrigger = true;
        shelters.Add(trigger);

        if (name.Contains("Desk"))
        {
            Model(table, "Writing pad", new Vector3(-.34f, clearance + .112f, -.17f),
                new Vector3(.55f, .016f, .34f), Moss);
            Model(table, "Notebook", new Vector3(-.38f, clearance + .132f, -.19f),
                new Vector3(.24f, .026f, .29f), Linen);
            Model(table, "Desk lamp base", new Vector3(.67f, clearance + .14f, -.32f),
                new Vector3(.25f, .055f, .25f), Ink, PrimitiveType.Cylinder);
            Model(table, "Desk lamp stem", new Vector3(.67f, clearance + .32f, -.32f),
                new Vector3(.04f, .32f, .04f), Brass);
            Model(table, "Desk lamp shade", new Vector3(.67f, clearance + .51f, -.21f),
                new Vector3(.36f, .16f, .33f), Moss);
        }
        else
        {
            Model(table, "Table runner", new Vector3(0f, clearance + .113f, 0f),
                new Vector3(width - .2f, .019f, .36f), Cloth);
            Model(table, "Ceramic centerpiece", new Vector3(0f, clearance + .2f, 0f),
                new Vector3(.34f, .16f, .34f), Linen, PrimitiveType.Sphere);
        }
        return table;
    }

    void BuildCouch(Vector3 pos, float yaw, float width = 2.2f)
    {
        var couch = Node("Couch", root, pos, yaw);
        Solid(couch, new Vector3(0f, .28f, 0f), new Vector3(width, .56f, .79f));
        Solid(couch, new Vector3(0f, .6f, -.3f), new Vector3(width, .72f, .2f));
        Model(couch, "Upholstered base", new Vector3(0f, .25f, 0f), new Vector3(width, .36f, .78f), Cloth);
        Model(couch, "Sofa back", new Vector3(0f, .62f, -.3f), new Vector3(width, .68f, .2f), Cloth);
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * (width * .5f - .12f);
            Model(couch, "Armrest", new Vector3(x, .52f, .03f), new Vector3(.24f, .37f, .72f), Cloth);
            Model(couch, "Seat cushion", new Vector3(side * width * .215f, .46f, .06f),
                new Vector3(width * .41f, .145f, .55f), new Color(.32f, .53f, .54f));
            Model(couch, "Sofa pillow", new Vector3(side * width * .27f, .66f, -.13f),
                new Vector3(.35f, .34f, .16f), side > 0 ? Linen : Rust).transform.localRotation =
                    Quaternion.Euler(15f, 0f, side * 13f);
            foreach (float z in new[] { -.27f, .27f })
                Model(couch, "Sofa foot", new Vector3(x, .055f, z), new Vector3(.11f, .11f, .11f), Walnut);
        }
    }

    void BuildBed(Vector3 pos, float yaw)
    {
        var bed = Node("Bed", root, pos, yaw);
        Solid(bed, new Vector3(0f, .31f, 0f), new Vector3(1.55f, .62f, 2.15f));
        Model(bed, "Timber bed frame", new Vector3(0f, .235f, 0f), new Vector3(1.55f, .32f, 2.15f), Walnut);
        Model(bed, "Headboard", new Vector3(0f, .5f, -.99f), new Vector3(1.55f, 1f, .16f), Oak);
        Model(bed, "Mattress", new Vector3(0f, .46f, 0f), new Vector3(1.49f, .21f, 1.98f), Ivory);
        Model(bed, "Woven bed cover", new Vector3(0f, .58f, .35f), new Vector3(1.48f, .08f, 1.25f), Cloth);
        Model(bed, "Folded cover", new Vector3(0f, .625f, -.24f), new Vector3(1.48f, .045f, .12f), Rust);
        foreach (float x in new[] { -.36f, .36f })
            Model(bed, "Pillow", new Vector3(x, .61f, -.65f), new Vector3(.6f, .14f, .41f), Linen);
    }

    void BuildRug(Vector3 pos, Vector2 size, Color border)
    {
        var rug = Node("Woven rug", root, pos);
        Model(rug, "Rug border", new Vector3(0f, .004f, 0f), new Vector3(size.x, .006f, size.y), border);
        Model(rug, "Rug center", new Vector3(0f, .01f, 0f), new Vector3(size.x - .17f, .005f, size.y - .17f),
            Color.Lerp(border, Linen, .45f));
        foreach (float side in new[] { -1f, 1f })
            Model(rug, "Woven stripe", new Vector3(0f, .013f, side * (size.y * .5f - .19f)),
                new Vector3(size.x - .22f, .004f, .044f), Linen);
    }

    FallingProp BuildBookcase(string name, Vector3 floor, float yaw, bool moving = true)
    {
        const float width = 1.7f, depth = .65f;
        var shelf = Node(name, root, floor + Vector3.up * .95f, yaw);
        Vector3 baseCenter = new Vector3(0f, -.775f, 0f);
        Vector3 baseSize = new Vector3(width, .35f, depth);
        Solid(shelf, baseCenter, baseSize);
        Solid(shelf, new Vector3(0f, .175f, 0f), new Vector3(width, 1.55f, depth));
        var model = Node("Bookcase model", shelf, Vector3.zero);
        Model(model, "Low cabinet", baseCenter, baseSize, Walnut);
        Model(model, "Shelf back", new Vector3(0f, .17f, -.295f), new Vector3(width, 1.55f, .06f), Walnut);
        foreach (float x in new[] { -.805f, .805f })
            Model(model, "Shelf side", new Vector3(x, .175f, 0f),
                new Vector3(.09f, 1.55f, depth), Oak);

        for (int row = 0; row < 4; row++)
        {
            float boardY = -.585f + row * .5f;
            Model(model, "Shelf board", new Vector3(0f, boardY, 0f), new Vector3(width, .06f, depth), Oak);
            if (row == 3) continue;
            for (int col = 0; col < 6; col++)
            {
                float bookHeight = .26f + .045f * ((row + col) % 4);
                float x = -.66f + col * .217f;
                Model(model, "Book spine", new Vector3(x, boardY + .032f + bookHeight / 2f, .043f),
                    new Vector3(.19f, bookHeight, .47f), BookColors[(row * 3 + col) % BookColors.Length]);
                Model(model, "Spine rule", new Vector3(x, boardY + bookHeight - .012f, .281f),
                    new Vector3(.12f, .016f, .008f), Linen);
            }
        }
        if (!moving) return null;

        Vector3 push = shelf.forward * 35f;
        var prop = MakeProp(shelf, model, 50f, name, push, Vector3.Cross(Vector3.up, push).normalized * 12f);
        prop.pushPointLocal = new Vector3(0f, .85f, 0f);
        var body = shelf.GetComponent<Rigidbody>();
        body.solverIterations = 12;
        body.solverVelocityIterations = 4;
        body.maxDepenetrationVelocity = 2f;
        return prop;
    }

    FallingProp BuildFixture(string name, Vector3 position, Vector3 size, float yaw = 0f, bool pendant = false)
    {
        var housing = Node(name, root, position, yaw);
        Solid(housing, Vector3.zero, size);
        var model = Node("Light housing", housing, Vector3.zero);
        Model(model, "Steel rim", Vector3.zero, size, Ink);
        Model(model, "Warm diffuser", new Vector3(0f, -size.y * .5f, 0f),
            new Vector3(size.x * .86f, .036f, size.z * .85f), new Color(1f, .92f, .71f));
        float ceiling = Mathf.Floor(position.y / Level1Configuration.UpperFloor) * Level1Configuration.UpperFloor + roomHeight;
        if (pendant)
        {
            float cable = ceiling - position.y;
            if (cable > .1f)
                Model(root, "Pendant cable", position + Vector3.up * (cable * .5f),
                    new Vector3(.025f, cable, .025f), Ink);
        }
        else
            Model(root, "Light mount", position + Vector3.up * (ceiling - position.y - .022f),
                new Vector3(.21f, .044f, .19f), Ink);
        var prop = MakeProp(housing, model, 5f, name, Vector3.zero, Vector3.zero);
        prop.routeObstacle = false;
        housing.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
        return prop;
    }

    FallingProp BuildWindow(string name, Vector3 pos, float yaw)
    {
        var frame = Node("Window frame", root, pos, yaw);
        Model(frame, "Sill", new Vector3(0f, -.58f, .055f), new Vector3(1.68f, .13f, .2f), Ivory);
        Model(frame, "Window header", new Vector3(0f, .59f, .01f), new Vector3(1.61f, .12f, .14f), Ivory);
        foreach (float x in new[] { -.75f, .75f })
            Model(frame, "Window jamb", new Vector3(x, 0f, .01f), new Vector3(.12f, 1.24f, .14f), Ivory);
        Model(frame, "Outdoor light", new Vector3(0f, 0f, -.03f), new Vector3(1.38f, 1.09f, .015f),
            new Color(.52f, .67f, .70f));

        var sash = Node(name, frame, new Vector3(0f, 0f, .088f));
        var model = Node("Window sash model", sash, Vector3.zero);
        Solid(sash, Vector3.zero, new Vector3(1.37f, 1.06f, .065f));
        Model(model, "Blue glazing", Vector3.zero, new Vector3(1.35f, 1.04f, .053f),
            new Color(.46f, .65f, .75f));
        foreach (float x in new[] { -.65f, 0f, .65f })
            Model(model, "Sash stile", new Vector3(x, 0f, .036f), new Vector3(.048f, 1.04f, .033f), Ivory);
        Model(model, "Sash rail", new Vector3(0f, 0f, .036f), new Vector3(1.36f, .05f, .034f), Ivory);
        return MakeProp(sash, model, 4f, name, frame.forward * 2.6f, frame.right * 1.1f);
    }

    FallingProp BuildTelevision(Vector3 pos, float yaw)
    {
        var console = Node("Media console", root, pos, yaw);
        Solid(console, new Vector3(0f, .275f, 0f), new Vector3(1.55f, .55f, .53f));
        Model(console, "Console carcass", new Vector3(0f, .29f, 0f), new Vector3(1.55f, .47f, .53f), Walnut);
        Model(console, "Console top", new Vector3(0f, .525f, 0f), new Vector3(1.6f, .05f, .54f), Oak);
        foreach (float x in new[] { -.38f, .38f })
        {
            Model(console, "Console doors", new Vector3(x, .3f, .27f), new Vector3(.71f, .36f, .035f), Oak);
            Model(console, "Drawer pull", new Vector3(x, .34f, .295f),
                new Vector3(.14f, .028f, .025f), Brass);
        }
        var tv = Node("Television", console, new Vector3(0f, .9f, 0f));
        var model = Node("TV model", tv, Vector3.zero);
        Solid(tv, Vector3.zero, new Vector3(1.27f, .64f, .12f));
        Model(model, "Dark glass", new Vector3(0f, 0f, .066f), new Vector3(1.18f, .56f, .023f),
            new Color(.13f, .22f, .26f));
        Model(model, "Plastic shell", Vector3.zero, new Vector3(1.27f, .64f, .12f), Ink);
        Model(model, "Status light", new Vector3(.52f, -.3f, .066f), new Vector3(.043f, .017f, .013f), Moss);
        foreach (float x in new[] { -.43f, .43f })
            Model(model, "TV stand feet", new Vector3(x, -.34f, 0f), new Vector3(.09f, .025f, .32f), Ink);
        return MakeProp(tv, model, 12f, "Television", console.forward * 6f, -console.right * 2f);
    }

    void BuildCounter(Vector3 pos, float yaw, float width)
    {
        var counter = Node("Kitchen counter", root, pos, yaw);
        Solid(counter, new Vector3(0f, .52f, 0f), new Vector3(width, 1.04f, .65f));
        Model(counter, "Lower cabinets", new Vector3(0f, .48f, 0f), new Vector3(width, .96f, .63f), Cloth);
        Model(counter, "Stone worktop", new Vector3(0f, 1.015f, 0f), new Vector3(width + .06f, .05f, .72f), Linen);
        int doors = Mathf.Max(1, Mathf.RoundToInt(width / .66f));
        float door = width / doors;
        for (int i = 0; i < doors; i++)
        {
            float x = -width * .5f + (i + .5f) * door;
            Model(counter, "Painted cabinet fronts", new Vector3(x, .5f, .323f),
                new Vector3(door - .034f, .77f, .034f), Moss);
            Model(counter, "Brass handle", new Vector3(x, .78f, .351f), new Vector3(.16f, .035f, .026f), Brass);
        }
        Model(counter, "Cutting board", new Vector3(width * -.21f, 1.055f, -.04f),
            new Vector3(.45f, .035f, .34f), Oak);
    }

    FallingProp BuildCupboardContents(Vector3 pos, float yaw)
    {
        var frame = Node("Open wall cupboard", root, pos, yaw);
        var board = new Vector3(1.38f, .08f, .46f);
        Solid(frame, Vector3.zero, board);
        Model(frame, "Cupboard base", Vector3.zero, board, Oak);
        Model(frame, "Cupboard top", new Vector3(0f, .70f, 0f), board, Oak);
        Model(frame, "Cupboard back", new Vector3(0f, .35f, -.21f), new Vector3(1.38f, .68f, .06f), Moss);
        foreach (float x in new[] { -.655f, .655f })
            Model(frame, "Cupboard side", new Vector3(x, .35f, 0f), new Vector3(.07f, .68f, .46f), Oak);
        var crock = Node("Ceramic mixing bowl", frame, new Vector3(0f, .20f, .13f));
        var model = Node("Ceramic model", crock, Vector3.zero);
        Solid(crock, Vector3.zero, new Vector3(.39f, .31f, .34f));
        Model(model, "Ceramic bowl", Vector3.zero, new Vector3(.39f, .31f, .34f), Rust, PrimitiveType.Sphere);
        Model(model, "Ceramic lip", new Vector3(0f, .083f, 0f), new Vector3(.38f, .05f, .33f), Ivory,
            PrimitiveType.Cylinder);
        return MakeProp(crock, model, 2f, "Ceramic bowl", frame.forward * 3.1f, frame.right * .35f);
    }

    void BuildArt(Vector3 pos, float yaw, Color field)
    {
        var art = Node("Framed print", root, pos, yaw);
        Model(art, "Print frame", Vector3.zero, new Vector3(.85f, .61f, .05f), Walnut);
        Model(art, "Print mat", new Vector3(0f, 0f, .032f), new Vector3(.77f, .53f, .014f), Ivory);
        Model(art, "Print field", new Vector3(0f, 0f, .044f), new Vector3(.59f, .37f, .009f), field);
        Model(art, "Print sun", new Vector3(.106f, .067f, .05f), new Vector3(.13f, .13f, .019f), Brass,
            PrimitiveType.Sphere);
        Model(art, "Print horizon", new Vector3(0f, -.086f, .052f), new Vector3(.58f, .08f, .009f), Walnut);
    }

    FallingProp MakeProp(Transform body, Transform model, float mass, string label, Vector3 push, Vector3 torque)
    {
        var rb = body.gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var prop = body.gameObject.AddComponent<FallingProp>();
        prop.label = label;
        prop.pushImpulse = push;
        prop.torqueImpulse = torque;
        prop.warningVisual = model;
        return prop;
    }
}
