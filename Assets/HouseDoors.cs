using UnityEngine;

public partial class SeghaniTakSceneBuilder
{
    HouseInteractions houseInteractions;

    void BuildHouseDoors(float upper)
    {
        houseInteractions = new GameObject("House interactions").AddComponent<HouseInteractions>();
        houseInteractions.shortcutHinge = BuildDoor("Locked shortcut", new Vector3(2.12f, 0f, -.52f), false);
        houseInteractions.jammedDoor = BuildDoor("Jammed study door", new Vector3(2.12f, upper, -.52f), true);
        // A conspicuous brass key hangs on the entry wall during the orientation period.
        Model(root, "Key hook backplate", new Vector3(-7.46f, 1.33f, -1.31f),
            new Vector3(.061f, .29f, .24f), Walnut);
        var key = Node("Brass shortcut key", root, new Vector3(-7.397f, 1.34f, -1.31f), 90f);
        houseInteractions.key = key;
        var bow = Model(key, "Key bow", new Vector3(0f, .055f, 0f), new Vector3(.13f, .024f, .13f), Brass,
            PrimitiveType.Cylinder);
        bow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Model(key, "Key shaft", new Vector3(0f, -.046f, 0f), new Vector3(.038f, .15f, .035f), Brass);
        Model(key, "Key teeth", new Vector3(.024f, -.112f, 0f), new Vector3(.074f, .04f, .035f), Brass);
    }

    Transform BuildDoor(string label, Vector3 hingePosition, bool jammed)
    {
        var pivot = Node(label, root, hingePosition);
        Vector3 mid = new Vector3(0f, 1.115f, .52f);
        Vector3 size = new Vector3(.112f, 2.2f, 1.06f);
        Solid(pivot, mid, size);
        Model(pivot, "Painted door slab", mid, size, jammed ? Walnut : Moss);
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * .063f;
            Model(pivot, "Inset panel", new Vector3(x, 1.51f, .52f), new Vector3(.025f, 1.14f, .77f),
                jammed ? Oak : StudyWall);
            Model(pivot, "Lower panel", new Vector3(x, .5f, .52f), new Vector3(.025f, .57f, .77f),
                jammed ? Oak : StudyWall);
            Model(pivot, "Door handle", new Vector3(side * .088f, .983f, .882f),
                new Vector3(.087f, .041f, .156f), Brass);
            if (jammed)
                Model(pivot, "Splintered brace", new Vector3(side * .089f, .44f, .48f),
                    new Vector3(.043f, .12f, 1.05f), Rust).transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        }
        return pivot;
    }
}
