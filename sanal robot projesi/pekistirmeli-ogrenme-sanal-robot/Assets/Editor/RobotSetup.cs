using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

/// <summary>Creates the team's robot from Unity primitives, with no external model dependency.</summary>
public static class RobotSetup
{
    private enum RobotStyle { Classic, Humanoid, Explorer }

    [MenuItem("Robot Projesi/Robotu Olustur ve Sahneye Ekle")]
    public static void CreateAndInstall()
    {
        Install(RobotStyle.Classic, false);
    }

    [MenuItem("Robot Projesi/Robot Sec/1 - Klasik Tekerlekli")]
    public static void SelectClassic() { Install(RobotStyle.Classic, true); }

    [MenuItem("Robot Projesi/Robot Sec/2 - Atlas Insansi")]
    public static void SelectHumanoid() { Install(RobotStyle.Humanoid, true); }

    [MenuItem("Robot Projesi/Robot Sec/3 - Nova Kesif Robotu")]
    public static void SelectExplorer() { Install(RobotStyle.Explorer, true); }

    private static void Install(RobotStyle style, bool replace)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Robot kurulumu Play modu disinda yapilmali.");

        // Validate scene dependencies before creating or changing assets.
        var manager = Object.FindAnyObjectByType<EnvironmentManager>();
        if (manager == null)
            throw new System.InvalidOperationException("Once TraningScene sahnesini acin.");
        var managerData = new SerializedObject(manager);
        var spawn = managerData.FindProperty("spawnPoint").objectReferenceValue as Transform;
        var target = managerData.FindProperty("target").objectReferenceValue as Transform;
        if (spawn == null || target == null)
            throw new System.InvalidOperationException("EnvironmentManager SpawnPoint ve Target referanslarini atayin.");
        var previous = managerData.FindProperty("robot").objectReferenceValue as Transform;
        if (previous != null && !replace)
        {
            Debug.Log("EnvironmentManager zaten bir robota bagli; ikinci robot olusturulmadi.");
            Selection.activeObject = managerData.FindProperty("robot").objectReferenceValue;
            return;
        }

        EnsureTag("Wall");
        string prefabPath = "Assets/Prefabs/" + (style == RobotStyle.Classic ? "Robot" : "Robot_" + style) + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var root = BuildRobot(style);
            try { prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }
            finally { Object.DestroyImmediate(root); }
        }
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Robot modeli sec");
        var robot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, manager.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(robot, "Robot ekle");
        robot.transform.SetParent(spawn.parent, false);
        robot.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        robot.GetComponent<RobotObstacleAgent>().target = target;
        // Keep the previous model in the scene so the user's work is preserved.
        if (previous != null)
        {
            Undo.RecordObject(previous.gameObject, "Onceki robotu pasiflestir");
            previous.gameObject.SetActive(false);
        }
        managerData.FindProperty("robot").objectReferenceValue = robot.transform;
        managerData.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Selection.activeGameObject = robot;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("Robot hazir. Sahneyi kaydedin, Play ile W/A/S/D testini yapin. Egitim icin Behavior Type = Default secin.");
    }

    private static GameObject BuildRobot(RobotStyle style)
    {
        var root = new GameObject(style == RobotStyle.Humanoid ? "Atlas_Insansi" :
            style == RobotStyle.Explorer ? "Nova_Kesif" : "Robot");
        var body = root.AddComponent<Rigidbody>();
        body.mass = 3f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, -0.05f, 0f);
        collider.size = new Vector3(1.2f, 0.9f, 1.5f);

        var blue = MaterialAsset("RobotBody", new Color(0.08f, 0.3f, 0.65f));
        var dark = MaterialAsset("RobotWheel", new Color(0.06f, 0.07f, 0.09f));
        var yellow = MaterialAsset("RobotForward", new Color(1f, 0.7f, 0.05f));
        if (style == RobotStyle.Humanoid)
        {
            BuildHumanoid(root, dark, yellow);
            collider.center = new Vector3(0f, 0.65f, 0f);
            collider.size = new Vector3(1.4f, 2.3f, 0.85f);
        }
        else if (style == RobotStyle.Explorer)
        {
            BuildExplorer(root, dark, yellow);
            collider.center = new Vector3(0f, 0.25f, 0f);
            collider.size = new Vector3(1.6f, 1.5f, 1.8f);
        }
        else
        {
        Part(root, "Body", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.9f, 0.5f, 1.2f), Quaternion.identity, blue);
        Part(root, "SensorHousing", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0.25f), new Vector3(0.5f, 0.2f, 0.3f), Quaternion.identity, dark);
        foreach (float side in new[] { -1f, 1f })
            foreach (float end in new[] { -1f, 1f })
                Part(root, "Wheel", PrimitiveType.Cylinder, new Vector3(side * 0.5f, -0.18f, end * 0.4f),
                    new Vector3(0.64f, 0.1f, 0.64f), Quaternion.Euler(0f, 0f, 90f), dark);
        Part(root, "ForwardMarker_Z+", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.48f), new Vector3(0.3f, 0.06f, 0.25f), Quaternion.identity, yellow);
        }

        root.AddComponent<RobotMovement>();
        var behavior = root.AddComponent<BehaviorParameters>();
        behavior.BehaviorName = "RobotAvoidance";
        behavior.BehaviorType = BehaviorType.HeuristicOnly;
        behavior.BrainParameters.VectorObservationSize = 7;
        behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(2);
        var sensor = root.AddComponent<RayPerceptionSensorComponent3D>();
        sensor.DetectableTags = new List<string> { "Obstacle", "Wall", "Target" };
        sensor.RaysPerDirection = 6;
        sensor.MaxRayDegrees = 90f;
        sensor.RayLength = 10f;
        sensor.SphereCastRadius = 0.1f;
        sensor.StartVerticalOffset = 0.1f;
        sensor.EndVerticalOffset = 0.1f;
        var agent = root.AddComponent<RobotObstacleAgent>();
        agent.MaxStep = 2000;
        var requester = root.AddComponent<DecisionRequester>();
        requester.DecisionPeriod = 5;
        requester.TakeActionsBetweenDecisions = true;
        return root;
    }

    private static void BuildHumanoid(GameObject root, Material dark, Material gold)
    {
        var white = MaterialAsset("AtlasPearl", new Color(0.8f, 0.87f, 0.93f));
        var cyan = MaterialAsset("AtlasCyan", new Color(0.05f, 0.8f, 1f));
        Shape(root, "Pelvis", PrimitiveType.Cube, 0, 0.48f, 0, .63f, .26f, .43f, dark);
        Shape(root, "ChestArmor", PrimitiveType.Cube, 0, .91f, 0, .85f, .67f, .49f, white);
        Shape(root, "ChestCore", PrimitiveType.Sphere, 0, 1f, .26f, .23f, .23f, .08f, cyan);
        Shape(root, "Neck", PrimitiveType.Cylinder, 0, 1.3f, 0, .2f, .09f, .2f, dark);
        Shape(root, "Head", PrimitiveType.Cube, 0, 1.56f, 0, .58f, .46f, .45f, white);
        Shape(root, "FaceVisor", PrimitiveType.Cube, 0, 1.59f, .235f, .48f, .19f, .035f, dark);
        foreach (float side in new[] { -1f, 1f })
        {
            Shape(root, "Eye", PrimitiveType.Cube, side * .13f, 1.61f, .258f, .13f, .055f, .02f, cyan);
            Shape(root, "Shoulder", PrimitiveType.Sphere, side * .53f, 1.13f, 0, .27f, .27f, .3f, gold);
            Shape(root, "UpperArm", PrimitiveType.Cube, side * .57f, .91f, 0, .22f, .36f, .26f, white);
            Shape(root, "ElbowJoint", PrimitiveType.Sphere, side * .57f, .7f, 0, .19f, .19f, .19f, dark);
            Shape(root, "Forearm", PrimitiveType.Cube, side * .57f, .49f, .04f, .25f, .31f, .27f, white);
            Shape(root, "Hand", PrimitiveType.Cube, side * .57f, .27f, .06f, .21f, .15f, .22f, dark);
            Shape(root, "Thigh", PrimitiveType.Cube, side * .22f, .2f, 0, .27f, .32f, .32f, white);
            Shape(root, "Knee", PrimitiveType.Sphere, side * .22f, 0, .02f, .24f, .24f, .26f, dark);
            Shape(root, "Shin", PrimitiveType.Cube, side * .22f, -.22f, 0, .25f, .3f, .29f, white);
            Shape(root, "Foot", PrimitiveType.Cube, side * .22f, -.435f, .12f, .33f, .13f, .58f, dark);
        }
    }

    private static void BuildExplorer(GameObject root, Material dark, Material gold)
    {
        var orange = MaterialAsset("NovaOrange", new Color(.95f, .29f, .06f));
        var metal = MaterialAsset("NovaMetal", new Color(.45f, .54f, .64f));
        var cyan = MaterialAsset("NovaLight", new Color(.1f, .85f, 1f));
        Shape(root, "LowerChassis", PrimitiveType.Cube, 0, -.02f, 0, 1.12f, .31f, 1.55f, dark);
        Shape(root, "ArmorDeck", PrimitiveType.Cube, 0, .23f, 0, 1.15f, .27f, 1.35f, orange);
        Shape(root, "BatteryPack", PrimitiveType.Cube, 0, .46f, -.28f, .69f, .22f, .55f, metal);
        Shape(root, "FrontBumper", PrimitiveType.Cube, 0, .03f, .83f, 1.37f, .15f, .14f, metal);
        Shape(root, "RearBumper", PrimitiveType.Cube, 0, .03f, -.83f, 1.37f, .15f, .14f, metal);
        Shape(root, "Mast", PrimitiveType.Cylinder, 0, .63f, .25f, .12f, .27f, .12f, metal);
        Shape(root, "CameraHead", PrimitiveType.Cube, 0, .91f, .25f, .55f, .21f, .3f, dark);
        Shape(root, "LidarBase", PrimitiveType.Cylinder, 0, .49f, -.25f, .32f, .1f, .32f, dark);
        Shape(root, "LidarCap", PrimitiveType.Cylinder, 0, .61f, -.25f, .28f, .035f, .28f, cyan);
        foreach (float side in new[] { -1f, 1f })
        {
            Shape(root, "Headlight", PrimitiveType.Cube, side * .4f, .24f, .69f, .19f, .1f, .035f, cyan);
            Shape(root, "CameraLens", PrimitiveType.Sphere, side * .15f, .92f, .41f, .12f, .12f, .04f, cyan);
            Shape(root, "SideRail", PrimitiveType.Cube, side * .64f, .08f, 0, .09f, .09f, 1.45f, gold);
            foreach (float z in new[] { -.57f, 0f, .57f })
            {
                Part(root, "AllTerrainWheel", PrimitiveType.Cylinder, new Vector3(side * .67f, -.18f, z),
                    new Vector3(.64f, .13f, .64f), Quaternion.Euler(0, 0, 90), dark);
                Part(root, "WheelHub", PrimitiveType.Cylinder, new Vector3(side * .81f, -.18f, z),
                    new Vector3(.3f, .025f, .3f), Quaternion.Euler(0, 0, 90), metal);
            }
        }
    }

    private static void Shape(GameObject root, string name, PrimitiveType type,
        float x, float y, float z, float sx, float sy, float sz, Material material)
    {
        Part(root, name, type, new Vector3(x, y, z), new Vector3(sx, sy, sz), Quaternion.identity, material);
    }

    private static void Part(GameObject root, string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Quaternion rotation, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale = scale;
        // A single collider on the root keeps contacts and observations consistent.
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureTag(string name)
    {
        var data = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = data.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == name) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = name;
        data.ApplyModifiedProperties();
    }
}
