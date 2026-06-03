using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LowPolyDriftPrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/DriftPrototype.unity";
    private const string StarterCarPrefabPath = "Assets/Prefabs/Vehicles/StarterCar.prefab";
    private const string DriftCarPrefabPath = "Assets/Prefabs/Vehicles/DriftCar.prefab";
    private const string RallyCarPrefabPath = "Assets/Prefabs/Vehicles/RallyCar.prefab";
    private const string SpeedCarPrefabPath = "Assets/Prefabs/Vehicles/SpeedCar.prefab";
    private const string CoinPrefabPath = "Assets/Prefabs/Pickups/Coin.prefab";

    private struct CarTuning
    {
        public float Acceleration;
        public float ReverseAcceleration;
        public float MaxForwardSpeed;
        public float MaxReverseSpeed;
        public float SteeringAcceleration;
        public float DriftStartSpeed;
        public float NormalLateralGrip;
        public float DriftLateralGrip;
        public float DriftSteeringMultiplier;
    }

    private struct LevelSpec
    {
        public string DisplayName;
        public int TargetCoins;
        public float RoadWidth;
        public int ObstacleCount;
        public Vector3[] RoutePoints;
    }

    private struct BuiltLevel
    {
        public string DisplayName;
        public int TargetCoins;
        public Transform Root;
        public Transform SpawnPoint;
    }

    private struct UiRefs
    {
        public GameObject HomePanel;
        public GameObject ShopPanel;
        public GameObject RankPanel;
        public GameObject LevelSelectPanel;
        public GameObject GameplayHudPanel;
        public GameObject CompletionPanel;
        public GameObject SettingsPopup;
        public GameObject SpinPopup;
        public GameObject DailyPopup;
        public Text SpeedText;
        public Text DriftStateText;
        public Text DriftScoreText;
        public Text GuidanceText;
        public Text LevelText;
        public Text CoinText;
        public Text TotalCoinsText;
        public Text GarageText;
        public Text MessageText;
        public Text CoinBalanceText;
        public Text HomeLevelText;
        public Button GameplayHomeButton;
        public Text ShopStatusText;
        public Text RankListText;
        public Text CompletionTitleText;
        public Text CompletionStatsText;
        public Text CompletionBestText;
        public Text LevelSelectStatusText;
        public Text SettingsStatusText;
        public Text SpinStatusText;
        public Text DailyStatusText;
        public Button CoinButton;
        public Button SettingsButton;
        public Button SpinButton;
        public Button DailyButton;
        public Button LevelButton;
        public Button HomeTabButton;
        public Button ShopTabButton;
        public Button RankTabButton;
        public Button LevelSelectBackButton;
        public Button ShopBackButton;
        public Button RankBackButton;
        public Button CompletionNextButton;
        public Button CompletionReplayButton;
        public Button CompletionHomeButton;
        public Button CompletionLevelSelectButton;
        public Button CloseSettingsButton;
        public Button CloseSpinButton;
        public Button CloseDailyButton;
        public Button ClaimDailyButton;
        public Button SpinRewardButton;
        public Toggle MusicToggle;
        public Toggle SoundToggle;
        public Button[] ShopCarButtons;
        public Text[] ShopCarTitleTexts;
        public Text[] ShopCarCostTexts;
        public Button[] LevelButtons;
        public Text[] LevelTitleTexts;
        public Text[] LevelMetaTexts;
        public CanvasGroup TransitionFadeGroup;
    }

    [MenuItem("Tools/PolyCar/Create Drift Prototype Scene")]
    public static void Build()
    {
        EnsureFolders();

        Material groundMaterial = GetOrCreateMaterial("MAT_Ground", new Color(0.27f, 0.34f, 0.27f));
        Material roadMaterial = GetOrCreateMaterial("MAT_Road", new Color(0.13f, 0.14f, 0.15f));
        Material curbMaterial = GetOrCreateMaterial("MAT_Curb", new Color(0.86f, 0.88f, 0.86f));
        Material routeGuideMaterial = GetOrCreateMaterial("MAT_RouteGuide", new Color(0.12f, 0.86f, 0.95f));
        Material checkpointMaterial = GetOrCreateMaterial("MAT_Checkpoint", new Color(0.95f, 0.58f, 0.18f));
        Material barrierMaterial = GetOrCreateMaterial("MAT_Barrier", new Color(0.9f, 0.18f, 0.12f));
        Material coneMaterial = GetOrCreateMaterial("MAT_TrafficCone", new Color(1f, 0.45f, 0.08f));
        Material starterBodyMaterial = GetOrCreateMaterial("MAT_Car_Starter_Body", new Color(0.96f, 0.24f, 0.18f));
        Material driftBodyMaterial = GetOrCreateMaterial("MAT_Car_Drift_Body", new Color(0.13f, 0.68f, 0.72f));
        Material rallyBodyMaterial = GetOrCreateMaterial("MAT_Car_Rally_Body", new Color(0.98f, 0.72f, 0.18f));
        Material speedBodyMaterial = GetOrCreateMaterial("MAT_Car_Speed_Body", new Color(0.25f, 0.55f, 1f));
        Material carGlassMaterial = GetOrCreateMaterial("MAT_Car_Glass", new Color(0.16f, 0.32f, 0.46f));
        Material tireMaterial = GetOrCreateMaterial("MAT_Tire", new Color(0.04f, 0.04f, 0.045f));
        Material coinMaterial = GetOrCreateMaterial("MAT_Coin", new Color(1f, 0.78f, 0.18f));

        GameObject starterCarPrefab = CreateCarPrefab(
            "StarterCar",
            StarterCarPrefabPath,
            starterBodyMaterial,
            carGlassMaterial,
            tireMaterial,
            new Vector3(1.8f, 0.55f, 3.4f),
            new Vector3(1.2f, 0.55f, 1.35f),
            new CarTuning
            {
                Acceleration = 18f,
                ReverseAcceleration = 10f,
                MaxForwardSpeed = 42f,
                MaxReverseSpeed = 12f,
                SteeringAcceleration = 3.4f,
                DriftStartSpeed = 12f,
                NormalLateralGrip = 9f,
                DriftLateralGrip = 2.1f,
                DriftSteeringMultiplier = 1.25f
            });

        GameObject driftCarPrefab = CreateCarPrefab(
            "DriftCar",
            DriftCarPrefabPath,
            driftBodyMaterial,
            carGlassMaterial,
            tireMaterial,
            new Vector3(1.95f, 0.45f, 3.25f),
            new Vector3(1.35f, 0.45f, 1.2f),
            new CarTuning
            {
                Acceleration = 18f,
                ReverseAcceleration = 10f,
                MaxForwardSpeed = 43f,
                MaxReverseSpeed = 12f,
                SteeringAcceleration = 4.7f,
                DriftStartSpeed = 8f,
                NormalLateralGrip = 6.8f,
                DriftLateralGrip = 0.9f,
                DriftSteeringMultiplier = 1.85f
            });

        GameObject rallyCarPrefab = CreateCarPrefab(
            "RallyCar",
            RallyCarPrefabPath,
            rallyBodyMaterial,
            carGlassMaterial,
            tireMaterial,
            new Vector3(2.05f, 0.58f, 3.55f),
            new Vector3(1.35f, 0.52f, 1.25f),
            new CarTuning
            {
                Acceleration = 19f,
                ReverseAcceleration = 11f,
                MaxForwardSpeed = 41f,
                MaxReverseSpeed = 12f,
                SteeringAcceleration = 3.9f,
                DriftStartSpeed = 12f,
                NormalLateralGrip = 12.5f,
                DriftLateralGrip = 3.1f,
                DriftSteeringMultiplier = 1.15f
            });

        GameObject speedCarPrefab = CreateCarPrefab(
            "SpeedCar",
            SpeedCarPrefabPath,
            speedBodyMaterial,
            carGlassMaterial,
            tireMaterial,
            new Vector3(1.7f, 0.42f, 3.8f),
            new Vector3(1.05f, 0.42f, 1.15f),
            new CarTuning
            {
                Acceleration = 25f,
                ReverseAcceleration = 9f,
                MaxForwardSpeed = 56f,
                MaxReverseSpeed = 10f,
                SteeringAcceleration = 2.8f,
                DriftStartSpeed = 16f,
                NormalLateralGrip = 8f,
                DriftLateralGrip = 1.8f,
                DriftSteeringMultiplier = 1.25f
            });

        GameObject coinPrefab = CreateCoinPrefab(coinMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.68f, 0.72f);

        CreateGround(groundMaterial);
        BuiltLevel[] builtLevels = CreateRoadLevels(coinPrefab, roadMaterial, curbMaterial, routeGuideMaterial, checkpointMaterial, barrierMaterial, coneMaterial, tireMaterial);
        CameraFollow cameraFollow = CreateCamera();
        UiRefs uiRefs = CreateCanvas();

        AudioManager audioManager = CreateAudioManager();
        GameManager gameManager = CreateGameManager(uiRefs.SpeedText, uiRefs.DriftStateText, uiRefs.DriftScoreText, audioManager);
        LevelManager levelManager = CreateLevelManager(builtLevels, uiRefs.LevelText, uiRefs.CoinText, uiRefs.TotalCoinsText, uiRefs.MessageText, uiRefs.GuidanceText, gameManager);
        Transform initialSpawn = builtLevels.Length > 0 ? builtLevels[0].SpawnPoint : null;
        CarGarage carGarage = CreateCarGarage(
            new[] { starterCarPrefab, driftCarPrefab, rallyCarPrefab, speedCarPrefab },
            initialSpawn,
            cameraFollow,
            gameManager,
            levelManager,
            uiRefs.GarageText);
        AssignLevelGarage(levelManager, carGarage);
        CreateMainMenuUI(uiRefs, carGarage, levelManager, audioManager, cameraFollow);
        CreateEventSystem();
        CreateLighting();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(StarterCarPrefabPath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Low Poly drift prototype scene created at {ScenePath}. Road levels: {builtLevels.Length}.");
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < currentScenes.Length; i++)
        {
            if (currentScenes[i].path == ScenePath)
            {
                currentScenes[i].enabled = true;
                EditorBuildSettings.scenes = currentScenes;
                return;
            }
        }

        EditorBuildSettingsScene[] updatedScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
        for (int i = 0; i < currentScenes.Length; i++)
        {
            updatedScenes[i] = currentScenes[i];
        }

        updatedScenes[updatedScenes.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updatedScenes;
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Prefabs/Vehicles");
        Directory.CreateDirectory("Assets/Prefabs/Pickups");
    }

    private static Material GetOrCreateMaterial(string materialName, Color color)
    {
        string path = $"Assets/Materials/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (material.shader == null)
        {
            material.shader = Shader.Find("Standard");
        }

        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject CreateCarPrefab(
        string carName,
        string prefabPath,
        Material bodyMaterial,
        Material glassMaterial,
        Material tireMaterial,
        Vector3 bodyScale,
        Vector3 cabinScale,
        CarTuning tuning)
    {
        GameObject root = new GameObject(carName);

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 900f;
        body.linearDamping = 0.05f;
        body.angularDamping = 1.2f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.2f, 0f);
        collider.size = new Vector3(1.8f, 0.8f, 3.6f);

        CarController controller = root.AddComponent<CarController>();
        ConfigureController(controller, tuning);

        GameObject visuals = new GameObject("LowPolyVisuals");
        visuals.transform.SetParent(root.transform, false);

        AddCube("Body", visuals.transform, new Vector3(0f, 0.15f, 0f), bodyScale, bodyMaterial);
        AddCube("Cabin", visuals.transform, new Vector3(0f, 0.62f, -0.35f), cabinScale, glassMaterial);
        AddCube("Nose", visuals.transform, new Vector3(0f, 0.28f, 1.2f), new Vector3(bodyScale.x * 0.8f, 0.28f, 0.75f), bodyMaterial);
        AddCube("RearSpoiler", visuals.transform, new Vector3(0f, 0.62f, -1.55f), new Vector3(bodyScale.x * 0.85f, 0.12f, 0.18f), bodyMaterial);

        AddWheel("FrontWheel_L", visuals.transform, new Vector3(-0.95f, -0.18f, 1.05f), tireMaterial);
        AddWheel("FrontWheel_R", visuals.transform, new Vector3(0.95f, -0.18f, 1.05f), tireMaterial);
        AddWheel("RearWheel_L", visuals.transform, new Vector3(-0.95f, -0.18f, -1.1f), tireMaterial);
        AddWheel("RearWheel_R", visuals.transform, new Vector3(0.95f, -0.18f, -1.1f), tireMaterial);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    private static void ConfigureController(CarController controller, CarTuning tuning)
    {
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("acceleration").floatValue = tuning.Acceleration;
        serializedController.FindProperty("reverseAcceleration").floatValue = tuning.ReverseAcceleration;
        serializedController.FindProperty("maxForwardSpeed").floatValue = tuning.MaxForwardSpeed;
        serializedController.FindProperty("maxReverseSpeed").floatValue = tuning.MaxReverseSpeed;
        serializedController.FindProperty("steeringAcceleration").floatValue = tuning.SteeringAcceleration;
        serializedController.FindProperty("driftStartSpeed").floatValue = tuning.DriftStartSpeed;
        serializedController.FindProperty("normalLateralGrip").floatValue = tuning.NormalLateralGrip;
        serializedController.FindProperty("driftLateralGrip").floatValue = tuning.DriftLateralGrip;
        serializedController.FindProperty("driftSteeringMultiplier").floatValue = tuning.DriftSteeringMultiplier;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject AddCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static GameObject AddWheel(string name, Transform parent, Vector3 localPosition, Material material)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPosition;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = new Vector3(0.38f, 0.18f, 0.38f);
        wheel.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(wheel.GetComponent<Collider>());
        return wheel;
    }

    private static GameObject CreateCoinPrefab(Material coinMaterial)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Coin";
        coin.transform.localScale = new Vector3(0.45f, 0.08f, 0.45f);
        coin.GetComponent<Renderer>().sharedMaterial = coinMaterial;
        coin.AddComponent<CoinPickup>();

        PrefabUtility.SaveAsPrefabAsset(coin, CoinPrefabPath);
        Object.DestroyImmediate(coin);
        return AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
    }

    private static void CreateGround(Material groundMaterial)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GrassGround";
        ground.transform.localScale = new Vector3(8f, 1f, 8f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
    }

    private static BuiltLevel[] CreateRoadLevels(
        GameObject coinPrefab,
        Material roadMaterial,
        Material curbMaterial,
        Material routeGuideMaterial,
        Material checkpointMaterial,
        Material barrierMaterial,
        Material coneMaterial,
        Material tireMaterial)
    {
        GameObject mapsRoot = new GameObject("LevelMaps");
        LevelSpec[] specs =
        {
            new LevelSpec
            {
                DisplayName = "Harbor Loop",
                TargetCoins = 10,
                RoadWidth = 5.2f,
                ObstacleCount = 0,
                RoutePoints = new[]
                {
                    Point(-12f, -7f),
                    Point(-2f, -8f),
                    Point(8f, -5f),
                    Point(11f, 2f),
                    Point(5f, 9f),
                    Point(-6f, 8f),
                    Point(-12f, 2f),
                    Point(-12f, -7f)
                }
            },
            new LevelSpec
            {
                DisplayName = "S-Curve Sprint",
                TargetCoins = 12,
                RoadWidth = 4.9f,
                ObstacleCount = 6,
                RoutePoints = new[]
                {
                    Point(-15f, -10f),
                    Point(-8f, -5f),
                    Point(-12f, 1f),
                    Point(-4f, 7f),
                    Point(4f, 4f),
                    Point(0f, -2f),
                    Point(8f, -7f),
                    Point(15f, -3f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Switchback Climb",
                TargetCoins = 14,
                RoadWidth = 4.6f,
                ObstacleCount = 12,
                RoutePoints = new[]
                {
                    Point(-14f, -9f),
                    Point(12f, -9f),
                    Point(12f, -4f),
                    Point(-10f, -4f),
                    Point(-10f, 1f),
                    Point(14f, 1f),
                    Point(14f, 7f),
                    Point(-14f, 7f),
                    Point(-14f, 11f),
                    Point(8f, 11f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Quarry Figure Eight",
                TargetCoins = 16,
                RoadWidth = 4.4f,
                ObstacleCount = 16,
                RoutePoints = new[]
                {
                    Point(-16f, -2f),
                    Point(-10f, -10f),
                    Point(-1f, -6f),
                    Point(8f, -11f),
                    Point(16f, -3f),
                    Point(8f, 4f),
                    Point(0f, 0f),
                    Point(-8f, 6f),
                    Point(-16f, 0f),
                    Point(-10f, -6f),
                    Point(0f, 0f),
                    Point(10f, 7f),
                    Point(17f, 1f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Sunset Slalom",
                TargetCoins = 18,
                RoadWidth = 4.2f,
                ObstacleCount = 20,
                RoutePoints = new[]
                {
                    Point(-20f, -12f),
                    Point(-13f, -8f),
                    Point(-18f, -2f),
                    Point(-10f, 3f),
                    Point(-14f, 10f),
                    Point(-4f, 13f),
                    Point(3f, 7f),
                    Point(10f, 12f),
                    Point(18f, 5f),
                    Point(12f, -2f),
                    Point(20f, -8f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Dockside Spiral",
                TargetCoins = 20,
                RoadWidth = 4f,
                ObstacleCount = 24,
                RoutePoints = new[]
                {
                    Point(-18f, -12f),
                    Point(-4f, -12f),
                    Point(8f, -8f),
                    Point(14f, 2f),
                    Point(8f, 12f),
                    Point(-4f, 14f),
                    Point(-14f, 8f),
                    Point(-16f, -2f),
                    Point(-8f, -8f),
                    Point(2f, -5f),
                    Point(6f, 2f),
                    Point(1f, 7f),
                    Point(-5f, 4f),
                    Point(-3f, -1f),
                    Point(5f, -1f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Canyon Needles",
                TargetCoins = 22,
                RoadWidth = 3.9f,
                ObstacleCount = 28,
                RoutePoints = new[]
                {
                    Point(-22f, -8f),
                    Point(-14f, -13f),
                    Point(-5f, -8f),
                    Point(-10f, -1f),
                    Point(-1f, 4f),
                    Point(-6f, 12f),
                    Point(5f, 15f),
                    Point(12f, 8f),
                    Point(6f, 2f),
                    Point(16f, -3f),
                    Point(21f, -11f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Ridge Hairpins",
                TargetCoins = 24,
                RoadWidth = 3.8f,
                ObstacleCount = 32,
                RoutePoints = new[]
                {
                    Point(-22f, -14f),
                    Point(18f, -14f),
                    Point(18f, -9f),
                    Point(-18f, -9f),
                    Point(-18f, -4f),
                    Point(18f, -4f),
                    Point(18f, 1f),
                    Point(-18f, 1f),
                    Point(-18f, 6f),
                    Point(18f, 6f),
                    Point(18f, 12f),
                    Point(-10f, 12f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Industrial Gauntlet",
                TargetCoins = 26,
                RoadWidth = 3.6f,
                ObstacleCount = 36,
                RoutePoints = new[]
                {
                    Point(-23f, -13f),
                    Point(-12f, -13f),
                    Point(-8f, -4f),
                    Point(-18f, -1f),
                    Point(-20f, 8f),
                    Point(-8f, 13f),
                    Point(3f, 8f),
                    Point(-1f, 0f),
                    Point(8f, -5f),
                    Point(18f, -1f),
                    Point(22f, 8f),
                    Point(12f, 14f),
                    Point(1f, 12f),
                    Point(9f, 3f),
                    Point(23f, -10f)
                }
            },
            new LevelSpec
            {
                DisplayName = "Midnight Pinball",
                TargetCoins = 30,
                RoadWidth = 3.4f,
                ObstacleCount = 42,
                RoutePoints = new[]
                {
                    Point(-24f, -15f),
                    Point(-14f, -6f),
                    Point(-22f, 4f),
                    Point(-10f, 14f),
                    Point(0f, 6f),
                    Point(-7f, -2f),
                    Point(3f, -12f),
                    Point(14f, -7f),
                    Point(7f, 2f),
                    Point(18f, 12f),
                    Point(25f, 4f),
                    Point(15f, -4f),
                    Point(24f, -14f),
                    Point(5f, -16f),
                    Point(-8f, -10f),
                    Point(-24f, -15f)
                }
            }
        };

        List<BuiltLevel> levels = new List<BuiltLevel>();
        for (int i = 0; i < specs.Length; i++)
        {
            BuiltLevel builtLevel = CreateRoadLevel(i, specs[i], coinPrefab, roadMaterial, curbMaterial, routeGuideMaterial, checkpointMaterial, barrierMaterial, coneMaterial, tireMaterial, mapsRoot.transform);
            builtLevel.Root.gameObject.SetActive(i == 0);
            levels.Add(builtLevel);
        }

        return levels.ToArray();
    }

    private static BuiltLevel CreateRoadLevel(
        int index,
        LevelSpec spec,
        GameObject coinPrefab,
        Material roadMaterial,
        Material curbMaterial,
        Material routeGuideMaterial,
        Material checkpointMaterial,
        Material barrierMaterial,
        Material coneMaterial,
        Material tireMaterial,
        Transform parent)
    {
        GameObject levelRoot = new GameObject($"Level_{index + 1:00}_{spec.DisplayName.Replace(" ", string.Empty).Replace("-", string.Empty)}");
        levelRoot.transform.SetParent(parent);

        GameObject roadRoot = new GameObject("Road");
        roadRoot.transform.SetParent(levelRoot.transform);

        GameObject coinsRoot = new GameObject("RouteCoins");
        coinsRoot.transform.SetParent(levelRoot.transform);

        GameObject guidanceRoot = new GameObject("Guidance");
        guidanceRoot.transform.SetParent(levelRoot.transform);

        GameObject obstaclesRoot = new GameObject("Obstacles");
        obstaclesRoot.transform.SetParent(levelRoot.transform);

        Transform spawnPoint = CreateLevelSpawnPoint(levelRoot.transform, spec.RoutePoints);
        for (int i = 0; i < spec.RoutePoints.Length - 1; i++)
        {
            CreateRoadSegment($"Road_{i + 1:00}", roadRoot.transform, spec.RoutePoints[i], spec.RoutePoints[i + 1], spec.RoadWidth, roadMaterial, curbMaterial);
        }

        CreateRouteCoins(spec.RoutePoints, spec.TargetCoins, coinPrefab, coinsRoot.transform);
        CreateRouteGuides(spec.RoutePoints, spec.TargetCoins, spec.RoadWidth, routeGuideMaterial, checkpointMaterial, guidanceRoot.transform);
        CreateRouteObstacles(index, spec.RoutePoints, spec.ObstacleCount, spec.RoadWidth, barrierMaterial, coneMaterial, tireMaterial, obstaclesRoot.transform);
        CreateStartGate(levelRoot.transform, spec.RoutePoints, spec.RoadWidth, curbMaterial);

        return new BuiltLevel
        {
            DisplayName = spec.DisplayName,
            TargetCoins = spec.TargetCoins,
            Root = levelRoot.transform,
            SpawnPoint = spawnPoint
        };
    }

    private static Transform CreateLevelSpawnPoint(Transform levelRoot, Vector3[] routePoints)
    {
        GameObject spawnObject = new GameObject("PlayerSpawnPoint");
        spawnObject.transform.SetParent(levelRoot);
        spawnObject.transform.position = routePoints[0] + Vector3.up * 0.45f;

        Vector3 forward = routePoints.Length > 1 ? routePoints[1] - routePoints[0] : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }

        spawnObject.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        return spawnObject.transform;
    }

    private static void CreateRoadSegment(string name, Transform parent, Vector3 start, Vector3 end, float roadWidth, Material roadMaterial, Material curbMaterial)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.01f)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        Vector3 center = (start + end) * 0.5f;
        CreateBox(name, parent, center + Vector3.up * 0.025f, rotation, new Vector3(roadWidth, 0.05f, length + roadWidth), roadMaterial);

        Vector3 right = rotation * Vector3.right;
        float curbOffset = roadWidth * 0.5f + 0.08f;
        CreateBox($"{name}_Curb_L", parent, center + right * curbOffset + Vector3.up * 0.13f, rotation, new Vector3(0.18f, 0.24f, length + 0.4f), curbMaterial);
        CreateBox($"{name}_Curb_R", parent, center - right * curbOffset + Vector3.up * 0.13f, rotation, new Vector3(0.18f, 0.24f, length + 0.4f), curbMaterial);
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.SetPositionAndRotation(position, rotation);
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static GameObject CreateVisualBox(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject box = CreateBox(name, parent, position, rotation, scale, material);
        Object.DestroyImmediate(box.GetComponent<Collider>());
        return box;
    }

    private static void CreateRouteCoins(Vector3[] routePoints, int coinCount, GameObject coinPrefab, Transform parent)
    {
        float routeLength = GetRouteLength(routePoints);
        for (int i = 0; i < coinCount; i++)
        {
            float distance = routeLength * ((i + 1f) / (coinCount + 1f));
            Vector3 routePosition = SampleRoute(routePoints, distance);

            GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
            coin.name = $"Coin_{i + 1:00}";
            coin.transform.SetParent(parent);
            coin.transform.position = routePosition + Vector3.up * 0.75f;
        }
    }

    private static void CreateRouteGuides(
        Vector3[] routePoints,
        int checkpointCount,
        float roadWidth,
        Material routeGuideMaterial,
        Material checkpointMaterial,
        Transform parent)
    {
        float routeLength = GetRouteLength(routePoints);
        int arrowCount = Mathf.Max(5, Mathf.CeilToInt(routeLength / 8f));
        for (int i = 0; i < arrowCount; i++)
        {
            float distance = Mathf.Clamp(routeLength * ((i + 0.55f) / arrowCount), 1.4f, Mathf.Max(1.4f, routeLength - 1.4f));
            SampleRouteFrame(routePoints, distance, out Vector3 routePosition, out Quaternion routeRotation);
            CreateRoadArrow($"RouteArrow_{i + 1:00}", parent, routePosition + Vector3.up * 0.09f, routeRotation, roadWidth, routeGuideMaterial);
        }

        for (int i = 0; i < checkpointCount; i++)
        {
            float distance = routeLength * ((i + 1f) / (checkpointCount + 1f));
            SampleRouteFrame(routePoints, distance, out Vector3 routePosition, out Quaternion routeRotation);
            CreateCheckpointMarker($"Checkpoint_{i + 1:00}", parent, routePosition + Vector3.up * 0.075f, routeRotation, roadWidth, checkpointMaterial);
        }
    }

    private static void CreateRoadArrow(string name, Transform parent, Vector3 position, Quaternion rotation, float roadWidth, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(position, rotation);

        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        float width = Mathf.Clamp(roadWidth * 0.12f, 0.32f, 0.5f);

        CreateVisualBox("Shaft", root.transform, position - forward * 0.24f, rotation, new Vector3(width, 0.035f, 1.25f), material);
        CreateVisualBox("Head_L", root.transform, position + forward * 0.42f - right * 0.22f, rotation * Quaternion.Euler(0f, -36f, 0f), new Vector3(width, 0.035f, 0.78f), material);
        CreateVisualBox("Head_R", root.transform, position + forward * 0.42f + right * 0.22f, rotation * Quaternion.Euler(0f, 36f, 0f), new Vector3(width, 0.035f, 0.78f), material);
    }

    private static void CreateCheckpointMarker(string name, Transform parent, Vector3 position, Quaternion rotation, float roadWidth, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(position, rotation);

        Vector3 right = rotation * Vector3.right;
        float halfWidth = roadWidth * 0.34f;
        CreateVisualBox("CenterLine", root.transform, position, rotation, new Vector3(roadWidth * 0.62f, 0.04f, 0.13f), material);
        CreateVisualBox("LeftTick", root.transform, position - right * halfWidth, rotation, new Vector3(0.12f, 0.045f, 0.7f), material);
        CreateVisualBox("RightTick", root.transform, position + right * halfWidth, rotation, new Vector3(0.12f, 0.045f, 0.7f), material);
    }

    private static void CreateRouteObstacles(
        int routeIndex,
        Vector3[] routePoints,
        int obstacleCount,
        float roadWidth,
        Material barrierMaterial,
        Material coneMaterial,
        Material tireMaterial,
        Transform parent)
    {
        if (obstacleCount <= 0)
        {
            return;
        }

        float routeLength = GetRouteLength(routePoints);
        for (int i = 0; i < obstacleCount; i++)
        {
            float distance = routeLength * ((i + 1.4f) / (obstacleCount + 2.6f));
            SampleRouteFrame(routePoints, distance, out Vector3 routePosition, out Quaternion routeRotation);

            Vector3 right = routeRotation * Vector3.right;
            float side = i % 2 == 0 ? 1f : -1f;

            int pattern = (i + routeIndex) % 5;
            if (pattern == 0)
            {
                Vector3 barrierPosition = routePosition + right * side * (roadWidth * 0.22f) + Vector3.up * 0.33f;
                GameObject barrier = CreateBox(
                    $"Barrier_{i + 1:00}",
                    parent,
                    barrierPosition,
                    routeRotation,
                    new Vector3(roadWidth * 0.34f, 0.62f + routeIndex * 0.01f, 0.32f),
                    barrierMaterial);
                AttachObstacleFeedback(barrier, 2, new Color(1f, 0.42f, 0.18f), 1.1f);
            }
            else if (pattern == 1 || pattern == 2)
            {
                Vector3 conePosition = routePosition + right * side * (roadWidth * 0.28f) + Vector3.up * 0.35f;
                GameObject cone = CreateTrafficCone($"Cone_{i + 1:00}", parent, conePosition, routeRotation, coneMaterial);
                AttachObstacleFeedback(cone, 1, new Color(1f, 0.74f, 0.22f), 1.16f);
            }
            else if (pattern == 3)
            {
                Vector3 tirePosition = routePosition + right * side * (roadWidth * 0.24f) + Vector3.up * 0.18f;
                CreateTireStack($"TireStack_{i + 1:00}", parent, tirePosition, routeRotation, tireMaterial);
            }
            else
            {
                Vector3 blockPosition = routePosition + right * side * (roadWidth * 0.18f) + Vector3.up * 0.18f;
                GameObject block = CreateBox(
                    $"LowBlock_{i + 1:00}",
                    parent,
                    blockPosition,
                    routeRotation,
                    new Vector3(roadWidth * 0.26f, 0.32f, 0.85f),
                    barrierMaterial);
                AttachObstacleFeedback(block, 1, new Color(1f, 0.55f, 0.24f), 1.12f);
            }
        }
    }

    private static GameObject CreateTrafficCone(string name, Transform parent, Vector3 position, Quaternion rotation, Material material)
    {
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = name;
        cone.transform.SetParent(parent);
        cone.transform.SetPositionAndRotation(position, rotation);
        cone.transform.localScale = new Vector3(0.34f, 0.35f, 0.34f);
        cone.GetComponent<Renderer>().sharedMaterial = material;

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "Reflector";
        cap.transform.SetParent(cone.transform, false);
        cap.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        cap.transform.localScale = new Vector3(0.42f, 0.08f, 0.42f);
        cap.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(cap.GetComponent<Collider>());
        return cone;
    }

    private static void CreateTireStack(string name, Transform parent, Vector3 position, Quaternion rotation, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.SetPositionAndRotation(position, rotation);

        for (int i = 0; i < 3; i++)
        {
            GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = $"Tire_{i + 1:00}";
            tire.transform.SetParent(root.transform);
            tire.transform.SetPositionAndRotation(position + Vector3.up * (0.22f * i), rotation);
            tire.transform.localScale = new Vector3(0.52f, 0.14f, 0.52f);
            tire.GetComponent<Renderer>().sharedMaterial = material;
            AttachObstacleFeedback(tire, 2, new Color(0.45f, 0.95f, 1f), 1.1f);
        }
    }

    private static void AttachObstacleFeedback(GameObject obstacle, int coinPenalty, Color flashColor, float pulseScale)
    {
        if (obstacle == null)
        {
            return;
        }

        ObstacleFeedback feedback = obstacle.AddComponent<ObstacleFeedback>();
        feedback.Configure(coinPenalty, flashColor, pulseScale);
    }

    private static void CreateStartGate(Transform levelRoot, Vector3[] routePoints, float roadWidth, Material material)
    {
        if (routePoints.Length < 2)
        {
            return;
        }

        Vector3 start = routePoints[0];
        Vector3 forward = (routePoints[1] - routePoints[0]).normalized;
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 right = rotation * Vector3.right;

        GameObject gateRoot = new GameObject("StartGate");
        gateRoot.transform.SetParent(levelRoot);

        float postOffset = roadWidth * 0.5f + 0.65f;
        CreateBox("StartPost_L", gateRoot.transform, start + right * postOffset + Vector3.up * 1.1f, rotation, new Vector3(0.22f, 2.2f, 0.22f), material);
        CreateBox("StartPost_R", gateRoot.transform, start - right * postOffset + Vector3.up * 1.1f, rotation, new Vector3(0.22f, 2.2f, 0.22f), material);
        CreateBox("StartBeam", gateRoot.transform, start + Vector3.up * 2.25f, rotation, new Vector3(roadWidth + 1.6f, 0.2f, 0.2f), material);
    }

    private static float GetRouteLength(Vector3[] routePoints)
    {
        float length = 0f;
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            length += Vector3.Distance(routePoints[i], routePoints[i + 1]);
        }

        return length;
    }

    private static Vector3 SampleRoute(Vector3[] routePoints, float distance)
    {
        float remaining = distance;
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            float segmentLength = Vector3.Distance(routePoints[i], routePoints[i + 1]);
            if (remaining <= segmentLength)
            {
                return Vector3.Lerp(routePoints[i], routePoints[i + 1], remaining / segmentLength);
            }

            remaining -= segmentLength;
        }

        return routePoints[routePoints.Length - 1];
    }

    private static void SampleRouteFrame(Vector3[] routePoints, float distance, out Vector3 position, out Quaternion rotation)
    {
        float remaining = distance;
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            Vector3 segment = routePoints[i + 1] - routePoints[i];
            float segmentLength = segment.magnitude;
            if (remaining <= segmentLength)
            {
                position = Vector3.Lerp(routePoints[i], routePoints[i + 1], remaining / segmentLength);
                rotation = Quaternion.LookRotation(segment.normalized, Vector3.up);
                return;
            }

            remaining -= segmentLength;
        }

        Vector3 finalSegment = routePoints[routePoints.Length - 1] - routePoints[routePoints.Length - 2];
        position = routePoints[routePoints.Length - 1];
        rotation = Quaternion.LookRotation(finalSegment.normalized, Vector3.up);
    }

    private static Vector3 Point(float x, float z)
    {
        return new Vector3(x, 0f, z);
    }

    private static CameraFollow CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 4f, -12f);
        cameraObject.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 250f;

        cameraObject.AddComponent<AudioListener>();
        return cameraObject.AddComponent<CameraFollow>();
    }

    private static UiRefs CreateCanvas()
    {
        UiRefs ui = new UiRefs();

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        ui.GameplayHudPanel = CreateFullPanel("GameplayHUD", canvasObject.transform, new Color(0f, 0f, 0f, 0f), false, false);
        ui.SpeedText = CreateText("SpeedText", ui.GameplayHudPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(300f, 44f), "0 km/h", 34, TextAnchor.UpperLeft);
        ui.DriftStateText = CreateText("DriftStateText", ui.GameplayHudPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -68f), new Vector2(300f, 34f), "Normal", 24, TextAnchor.UpperLeft);
        ui.GuidanceText = CreateText("GuidanceText", ui.GameplayHudPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(620f, 46f), "Next Coin: ready", 30, TextAnchor.UpperCenter, new Color(0.45f, 0.95f, 1f));
        ui.DriftScoreText = CreateText("DriftScoreText", ui.GameplayHudPanel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -132f), new Vector2(620f, 80f), "Drift Score: 0\nCombo x1.0", 24, TextAnchor.UpperRight, new Color(0.92f, 0.96f, 1f));
        ui.LevelText = CreateText("LevelText", ui.GameplayHudPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -106f), new Vector2(560f, 34f), "Route 1/10", 24, TextAnchor.UpperLeft);
        ui.CoinText = CreateText("CoinText", ui.GameplayHudPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -144f), new Vector2(300f, 34f), "Coins: 0 / 10", 24, TextAnchor.UpperLeft);
        ui.TotalCoinsText = CreateText("TotalCoinsText", ui.GameplayHudPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -182f), new Vector2(360f, 34f), "Total Coins: 0", 24, TextAnchor.UpperLeft);
        ui.GarageText = CreateText("GarageText", ui.GameplayHudPanel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(620f, 110f), "Garage", 22, TextAnchor.UpperRight);
        ui.MessageText = CreateText("MessageText", ui.GameplayHudPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(820f, 170f), "Collect the coins on the road", 30, TextAnchor.MiddleCenter);
        ui.GameplayHomeButton = CreateButton("GameplayHomeButton", ui.GameplayHudPanel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -42f), new Vector2(150f, 68f), "MENU", 24, new Color(0.22f, 0.25f, 0.34f), Color.white);

        ui.HomePanel = CreateFullPanel("HomePanel", canvasObject.transform, new Color(0.035f, 0.04f, 0.055f, 0.98f), true, true);
        CreateRacingBackdrop(ui.HomePanel.transform);
        CreateRect("HomeTopRail", ui.HomePanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1920f, 118f), new Color(0.07f, 0.09f, 0.12f, 0.96f));
        CreateRect("HomeTopAccent", ui.HomePanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(1920f, 5f), new Color(0.95f, 0.58f, 0.18f));
        CreateText("GameTitle", ui.HomePanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(900f, 92f), "POLY DRIFT", 72, TextAnchor.MiddleCenter, new Color(0.92f, 0.98f, 1f));
        CreateText("GameSubtitle", ui.HomePanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(900f, 44f), "arcade low-poly racing", 28, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
        ui.CoinButton = CreateButton("CoinButton", ui.HomePanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -28f), new Vector2(280f, 70f), "Coins 0", 30, new Color(0.95f, 0.58f, 0.18f), Color.white);
        ui.CoinBalanceText = ui.CoinButton.GetComponentInChildren<Text>();
        ui.SettingsButton = CreateButton("SettingsButton", ui.HomePanel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -28f), new Vector2(140f, 70f), "SET", 26, new Color(0.2f, 0.23f, 0.29f), Color.white);
        ui.SpinButton = CreateButton("SpinButton", ui.HomePanel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 105f), new Vector2(230f, 86f), "SPIN", 30, new Color(0.53f, 0.26f, 0.9f), Color.white);
        ui.DailyButton = CreateButton("DailyButton", ui.HomePanel.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-58f, 105f), new Vector2(230f, 86f), "DAILY", 30, new Color(0.08f, 0.56f, 0.62f), Color.white);
        CreateVehicleHero(ui.HomePanel.transform);
        CreateText("HomeRouteHint", ui.HomePanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 332f), new Vector2(820f, 42f), "Collect coins, unlock cars, master all 10 routes", 26, TextAnchor.MiddleCenter, new Color(0.78f, 0.88f, 0.92f));
        ui.LevelButton = CreateButton("LevelButton", ui.HomePanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(650f, 108f), "LEVEL SELECT", 42, new Color(0.38f, 0.82f, 0.23f), Color.white);
        ui.HomeLevelText = ui.LevelButton.GetComponentInChildren<Text>();

        ui.LevelSelectPanel = CreateFullPanel("LevelSelectPanel", canvasObject.transform, new Color(0.035f, 0.04f, 0.055f, 0.98f), false, true);
        CreateRacingBackdrop(ui.LevelSelectPanel.transform);
        CreateRect("LevelSelectTopRail", ui.LevelSelectPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1920f, 118f), new Color(0.07f, 0.09f, 0.12f, 0.96f));
        CreateText("LevelSelectTitle", ui.LevelSelectPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(760f, 76f), "LEVEL SELECT", 56, TextAnchor.MiddleCenter);
        ui.LevelSelectBackButton = CreateButton("LevelSelectBackButton", ui.LevelSelectPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -28f), new Vector2(150f, 70f), "HOME", 26, new Color(0.2f, 0.23f, 0.29f), Color.white);
        ui.LevelSelectStatusText = CreateText("LevelSelectStatusText", ui.LevelSelectPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1000f, 42f), "Choose a route", 26, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
        CreateText("LevelSelectHint", ui.LevelSelectPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(1050f, 40f), "Finish a route to unlock the next one", 24, TextAnchor.MiddleCenter, new Color(0.78f, 0.88f, 0.92f));

        ui.LevelButtons = new Button[10];
        ui.LevelTitleTexts = new Text[10];
        ui.LevelMetaTexts = new Text[10];
        string[] routeNames =
        {
            "Harbor Loop",
            "S-Curve Sprint",
            "Switchback Climb",
            "Quarry Figure Eight",
            "Sunset Slalom",
            "Dockside Spiral",
            "Canyon Needles",
            "Ridge Hairpins",
            "Industrial Gauntlet",
            "Midnight Pinball"
        };
        int[] routeCoinTargets = { 10, 12, 14, 16, 18, 20, 22, 24, 26, 30 };
        for (int i = 0; i < 10; i++)
        {
            int column = i % 2;
            int row = i / 2;
            float x = column == 0 ? -300f : 300f;
            float y = -220f - row * 128f;
            Color accentColor = Color.Lerp(new Color(0.45f, 0.95f, 1f), new Color(0.95f, 0.58f, 0.18f), i / 9f);
            Button card = CreateButton($"LevelButton_{i + 1:00}", ui.LevelSelectPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(540f, 106f), string.Empty, 1, new Color(0.09f, 0.14f, 0.2f), Color.white);
            ui.LevelButtons[i] = card;
            CreateRect($"LevelAccent_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(12f, 106f), accentColor);
            CreateText($"LevelNumber_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 14f), new Vector2(82f, 54f), $"{i + 1:00}", 38, TextAnchor.MiddleCenter, accentColor);
            ui.LevelTitleTexts[i] = CreateText($"LevelTitle_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(124f, 20f), new Vector2(360f, 38f), $"Route {i + 1}: {routeNames[i]}", 24, TextAnchor.MiddleLeft);
            ui.LevelMetaTexts[i] = CreateText($"LevelMeta_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(124f, -22f), new Vector2(360f, 32f), $"{routeCoinTargets[i]} coins objective", 20, TextAnchor.MiddleLeft, new Color(0.45f, 0.95f, 1f));
        }

        ui.ShopPanel = CreateFullPanel("ShopPanel", canvasObject.transform, new Color(0.035f, 0.04f, 0.055f, 0.98f), false, true);
        CreateRacingBackdrop(ui.ShopPanel.transform);
        CreateRect("ShopTopRail", ui.ShopPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1920f, 118f), new Color(0.07f, 0.09f, 0.12f, 0.96f));
        CreateText("ShopTitle", ui.ShopPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(700f, 76f), "GARAGE SHOP", 54, TextAnchor.MiddleCenter);
        ui.ShopBackButton = CreateButton("ShopBackButton", ui.ShopPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -28f), new Vector2(150f, 70f), "HOME", 26, new Color(0.2f, 0.23f, 0.29f), Color.white);
        ui.ShopStatusText = CreateText("ShopStatusText", ui.ShopPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(900f, 42f), "Unlock cars with route coins", 26, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
        ui.ShopCarButtons = new Button[4];
        ui.ShopCarTitleTexts = new Text[4];
        ui.ShopCarCostTexts = new Text[4];
        string[] shopSubtitles = { "Balanced starter tune", "Low grip drift specialist", "Stable obstacle runner", "High speed route hunter" };
        Color[] carColors =
        {
            new Color(0.96f, 0.24f, 0.18f),
            new Color(0.13f, 0.68f, 0.72f),
            new Color(0.98f, 0.72f, 0.18f),
            new Color(0.25f, 0.55f, 1f)
        };
        float[,] carStats =
        {
            { 0.62f, 0.58f, 0.62f },
            { 0.56f, 0.9f, 0.42f },
            { 0.7f, 0.55f, 0.82f },
            { 0.95f, 0.45f, 0.5f }
        };
        for (int i = 0; i < 4; i++)
        {
            float y = -230f - i * 162f;
            Button card = CreateButton($"ShopCarButton_{i + 1:00}", ui.ShopPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1140f, 138f), string.Empty, 1, new Color(0.095f, 0.12f, 0.17f), Color.white);
            ui.ShopCarButtons[i] = card;
            CreateRect($"ShopAccent_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(12f, 138f), carColors[i]);
            CreateMiniCar($"ShopCarIcon_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 0f), carColors[i]);
            ui.ShopCarTitleTexts[i] = CreateText($"ShopCarTitle_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, 34f), new Vector2(430f, 46f), "Car", 32, TextAnchor.MiddleLeft);
            CreateText($"ShopCarSubtitle_{i + 1:00}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, -5f), new Vector2(460f, 34f), shopSubtitles[i], 20, TextAnchor.MiddleLeft, new Color(0.67f, 0.78f, 0.82f));
            CreateStatBar(card.transform, "SPD", carStats[i, 0], new Vector2(620f, 34f), carColors[i]);
            CreateStatBar(card.transform, "DRF", carStats[i, 1], new Vector2(620f, -8f), carColors[i]);
            CreateStatBar(card.transform, "GRP", carStats[i, 2], new Vector2(620f, -50f), carColors[i]);
            ui.ShopCarCostTexts[i] = CreateText($"ShopCarCost_{i + 1:00}", card.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-42f, -2f), new Vector2(300f, 58f), "Unlock", 26, TextAnchor.MiddleRight, new Color(0.45f, 0.95f, 1f));
        }

        ui.RankPanel = CreateFullPanel("RankPanel", canvasObject.transform, new Color(0.035f, 0.04f, 0.055f, 0.98f), false, true);
        CreateRacingBackdrop(ui.RankPanel.transform);
        CreateRect("RankTopRail", ui.RankPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1920f, 118f), new Color(0.07f, 0.09f, 0.12f, 0.96f));
        CreateText("RankTitle", ui.RankPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(700f, 76f), "RANK", 58, TextAnchor.MiddleCenter);
        ui.RankBackButton = CreateButton("RankBackButton", ui.RankPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -28f), new Vector2(150f, 70f), "HOME", 26, new Color(0.2f, 0.23f, 0.29f), Color.white);
        CreateRankPodium(ui.RankPanel.transform);
        CreateRect("RankListCard", ui.RankPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -520f), new Vector2(1060f, 410f), new Color(0.09f, 0.12f, 0.17f, 0.95f));
        CreateText("RankHeader", ui.RankPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -542f), new Vector2(960f, 42f), "POS        DRIVER                         BEST ROUTE", 24, TextAnchor.MiddleLeft, new Color(0.95f, 0.58f, 0.18f));
        for (int i = 0; i < 5; i++)
        {
            Color rowColor = i == 0 ? new Color(0.12f, 0.22f, 0.24f, 0.95f) : new Color(0.11f, 0.14f, 0.2f, 0.9f);
            CreateRect($"RankRow_{i + 1:00}", ui.RankPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -608f - i * 62f), new Vector2(960f, 48f), rowColor);
        }
        ui.RankListText = CreateText("RankListText", ui.RankPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -590f), new Vector2(900f, 320f), "Rank", 27, TextAnchor.UpperLeft);

        CreateBottomNav(ui.HomePanel.transform, out ui.HomeTabButton, out ui.ShopTabButton, out ui.RankTabButton);

        ui.SettingsPopup = CreatePopup("SettingsPopup", canvasObject.transform, "SETTINGS", out ui.CloseSettingsButton);
        ui.MusicToggle = CreateToggle("MusicToggle", ui.SettingsPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), "Music");
        ui.SoundToggle = CreateToggle("SoundToggle", ui.SettingsPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), "Sound");
        ui.SettingsStatusText = CreateText("SettingsStatusText", ui.SettingsPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -115f), new Vector2(520f, 40f), "Audio settings", 24, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));

        ui.SpinPopup = CreatePopup("SpinPopup", canvasObject.transform, "SPIN WHEEL", out ui.CloseSpinButton);
        CreateText("SpinWheelGraphic", ui.SpinPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(480f, 150f), "10 | 15 | 20 | 25", 38, TextAnchor.MiddleCenter, new Color(0.95f, 0.58f, 0.18f));
        ui.SpinStatusText = CreateText("SpinStatusText", ui.SpinPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(560f, 44f), "Spin once for a coin bonus", 24, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
        ui.SpinRewardButton = CreateButton("SpinRewardButton", ui.SpinPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -145f), new Vector2(300f, 70f), "SPIN", 28, new Color(0.53f, 0.26f, 0.9f), Color.white);

        ui.DailyPopup = CreatePopup("DailyPopup", canvasObject.transform, "DAILY CHECK-IN", out ui.CloseDailyButton);
        ui.DailyStatusText = CreateText("DailyStatusText", ui.DailyPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(620f, 80f), "Claim today's +20 coin reward", 28, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
        ui.ClaimDailyButton = CreateButton("ClaimDailyButton", ui.DailyPopup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(340f, 74f), "CLAIM", 30, new Color(0.38f, 0.82f, 0.23f), Color.white);

        ui.CompletionPanel = CreateFullPanel("CompletionPanel", canvasObject.transform, new Color(0f, 0f, 0f, 0.62f), false, true);
        GameObject completeCard = CreateRect("CompletionCard", ui.CompletionPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 620f), new Color(0.075f, 0.1f, 0.15f, 0.98f));
        CreateRect("CompletionAccent", completeCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(840f, 7f), new Color(0.95f, 0.58f, 0.18f));
        ui.CompletionTitleText = CreateText("CompletionTitleText", completeCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(760f, 76f), "ROUTE COMPLETE", 46, TextAnchor.MiddleCenter, new Color(0.92f, 0.98f, 1f));
        ui.CompletionStatsText = CreateText("CompletionStatsText", completeCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(680f, 230f), "Stats", 28, TextAnchor.MiddleCenter, new Color(0.78f, 0.88f, 0.92f));
        ui.CompletionBestText = CreateText("CompletionBestText", completeCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(720f, 54f), "Best", 24, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
        ui.CompletionNextButton = CreateButton("CompletionNextButton", completeCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-220f, 36f), new Vector2(190f, 70f), "NEXT", 26, new Color(0.38f, 0.82f, 0.23f), Color.white);
        ui.CompletionReplayButton = CreateButton("CompletionReplayButton", completeCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(190f, 70f), "REPLAY", 26, new Color(0.1f, 0.58f, 0.75f), Color.white);
        ui.CompletionLevelSelectButton = CreateButton("CompletionLevelSelectButton", completeCard.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 36f), new Vector2(190f, 70f), "ROUTES", 26, new Color(0.53f, 0.26f, 0.9f), Color.white);
        ui.CompletionHomeButton = CreateButton("CompletionHomeButton", completeCard.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(120f, 58f), "HOME", 22, new Color(0.22f, 0.25f, 0.34f), Color.white);

        GameObject transitionFade = CreateFullPanel("TransitionFade", canvasObject.transform, new Color(0f, 0f, 0f, 0.72f), true, true);
        ui.TransitionFadeGroup = transitionFade.AddComponent<CanvasGroup>();
        ui.TransitionFadeGroup.alpha = 0f;
        ui.TransitionFadeGroup.blocksRaycasts = false;

        return ui;
    }

    private static void CreateRacingBackdrop(Transform parent)
    {
        GameObject road = CreateRect("BackdropRoad", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(520f, 1800f), new Color(0.08f, 0.085f, 0.09f, 0.62f));
        road.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

        for (int i = 0; i < 7; i++)
        {
            GameObject stripe = CreateRect($"BackdropStripe_{i + 1:00}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240f + i * 80f, -450f + i * 105f), new Vector2(16f, 120f), new Color(0.95f, 0.58f, 0.18f, 0.34f));
            stripe.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        }

        GameObject cyan = CreateRect("BackdropCyanLane", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-530f, 80f), new Vector2(12f, 1300f), new Color(0.1f, 0.85f, 0.95f, 0.24f));
        cyan.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject orange = CreateRect("BackdropOrangeLane", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(530f, -70f), new Vector2(12f, 1300f), new Color(0.95f, 0.58f, 0.18f, 0.24f));
        orange.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
    }

    private static void CreateVehicleHero(Transform parent)
    {
        GameObject hero = CreateRect("VehicleHeroPanel", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(860f, 330f), new Color(0.08f, 0.12f, 0.17f, 0.92f));
        CreateRect("VehicleHeroAccent", hero.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(860f, 6f), new Color(0.45f, 0.95f, 1f));
        CreateText("VehicleHeroTitle", hero.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(700f, 42f), "SELECTED GARAGE", 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.58f, 0.18f));

        GameObject pad = CreateRect("VehiclePad", hero.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(620f, 170f), new Color(0.045f, 0.055f, 0.07f, 0.95f));
        CreateRect("PadLineA", pad.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-160f, 0f), new Vector2(16f, 150f), new Color(0.45f, 0.95f, 1f, 0.34f));
        CreateRect("PadLineB", pad.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 0f), new Vector2(16f, 150f), new Color(0.95f, 0.58f, 0.18f, 0.34f));
        CreateMiniCar("HeroCar", hero.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Color(0.13f, 0.68f, 0.72f), 1.8f);

        CreateText("VehicleRoster", hero.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(780f, 42f), "Starter | Drift | Rally | Speed", 24, TextAnchor.MiddleCenter, new Color(0.78f, 0.88f, 0.92f));
    }

    private static void CreateMiniCar(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Color bodyColor)
    {
        CreateMiniCar(name, parent, anchor, pivot, anchoredPosition, bodyColor, 1f);
    }

    private static void CreateMiniCar(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Color bodyColor, float scale)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f * scale, 92f * scale);

        CreateRect("Shadow", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f * scale), new Vector2(210f * scale, 18f * scale), new Color(0f, 0f, 0f, 0.42f));
        CreateRect("Body", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(188f * scale, 54f * scale), bodyColor);
        CreateRect("Nose", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(88f * scale, 0f), new Vector2(50f * scale, 40f * scale), Color.Lerp(bodyColor, Color.white, 0.12f));
        CreateRect("Cabin", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-16f * scale, 9f * scale), new Vector2(70f * scale, 34f * scale), new Color(0.12f, 0.24f, 0.34f));
        CreateRect("WheelFL", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f * scale, -34f * scale), new Vector2(42f * scale, 20f * scale), new Color(0.03f, 0.035f, 0.04f));
        CreateRect("WheelFR", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-62f * scale, -34f * scale), new Vector2(42f * scale, 20f * scale), new Color(0.03f, 0.035f, 0.04f));
        CreateRect("Highlight", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6f * scale, 29f * scale), new Vector2(130f * scale, 6f * scale), Color.Lerp(bodyColor, Color.white, 0.34f));
    }

    private static void CreateStatBar(Transform parent, string label, float value, Vector2 anchoredPosition, Color fillColor)
    {
        CreateText($"{label}_Label", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), anchoredPosition, new Vector2(70f, 26f), label, 17, TextAnchor.MiddleLeft, new Color(0.78f, 0.88f, 0.92f));
        CreateRect($"{label}_Track", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), anchoredPosition + new Vector2(58f, 0f), new Vector2(210f, 16f), new Color(0.035f, 0.045f, 0.06f));
        CreateRect($"{label}_Fill", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), anchoredPosition + new Vector2(58f, 0f), new Vector2(210f * Mathf.Clamp01(value), 16f), fillColor);
    }

    private static void CreateRankPodium(Transform parent)
    {
        GameObject podium = CreateRect("RankPodium", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(920f, 250f), new Color(0.08f, 0.12f, 0.17f, 0.92f));
        CreateText("PodiumTitle", podium.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(760f, 38f), "ROUTE LEADERS", 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.58f, 0.18f));
        CreateRect("PodiumSecond", podium.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 18f), new Vector2(170f, 116f), new Color(0.38f, 0.46f, 0.54f));
        CreateRect("PodiumFirst", podium.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(190f, 156f), new Color(0.95f, 0.58f, 0.18f));
        CreateRect("PodiumThird", podium.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 18f), new Vector2(170f, 92f), new Color(0.58f, 0.32f, 0.18f));
        CreateText("First", podium.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(180f, 56f), "1", 44, TextAnchor.MiddleCenter);
        CreateText("Second", podium.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 82f), new Vector2(160f, 46f), "2", 34, TextAnchor.MiddleCenter);
        CreateText("Third", podium.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 58f), new Vector2(160f, 46f), "3", 34, TextAnchor.MiddleCenter);
    }

    private static void CreateBottomNav(Transform parent, out Button homeButton, out Button shopButton, out Button rankButton)
    {
        GameObject nav = CreateRect("BottomNav", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1100f, 128f), new Color(0.07f, 0.1f, 0.18f, 0.95f));
        homeButton = CreateButton("HomeTabButton", nav.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(260f, 82f), "HOME", 30, new Color(0.1f, 0.58f, 0.75f), Color.white);
        shopButton = CreateButton("ShopTabButton", nav.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360f, 0f), new Vector2(260f, 82f), "SHOP", 30, new Color(0.22f, 0.25f, 0.34f), Color.white);
        rankButton = CreateButton("RankTabButton", nav.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 0f), new Vector2(260f, 82f), "RANK", 30, new Color(0.22f, 0.25f, 0.34f), Color.white);
    }

    private static GameObject CreatePopup(string name, Transform parent, string title, out Button closeButton)
    {
        GameObject popupRoot = CreateFullPanel(name, parent, new Color(0f, 0f, 0f, 0.58f), false, true);
        CreateRect("Dialog", popupRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 520f), new Color(0.08f, 0.11f, 0.18f, 0.98f));
        CreateText("Title", popupRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 185f), new Vector2(600f, 70f), title, 42, TextAnchor.MiddleCenter);
        closeButton = CreateButton("CloseButton", popupRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(285f, 185f), new Vector2(82f, 62f), "X", 30, new Color(0.22f, 0.25f, 0.34f), Color.white);
        return popupRoot;
    }

    private static GameObject CreateFullPanel(string name, Transform parent, Color color, bool active, bool withImage)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (withImage)
        {
            Image image = panel.AddComponent<Image>();
            image.color = color;
        }

        panel.SetActive(active);
        return panel;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject box = new GameObject(name);
        box.transform.SetParent(parent, false);
        RectTransform rect = box.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = box.AddComponent<Image>();
        image.color = color;
        return box;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string label, int fontSize, Color backgroundColor, Color textColor)
    {
        GameObject buttonObject = CreateRect(name, parent, anchor, pivot, anchoredPosition, size, backgroundColor);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            CreateText("Label", buttonObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, label, fontSize, TextAnchor.MiddleCenter, textColor);
        }

        return button;
    }

    private static Toggle CreateToggle(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, string label)
    {
        GameObject toggleObject = CreateRect(name, parent, anchor, pivot, anchoredPosition, new Vector2(420f, 58f), new Color(0.13f, 0.17f, 0.25f, 1f));
        Toggle toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = toggleObject.GetComponent<Image>();

        GameObject checkmark = CreateRect("Checkmark", toggleObject.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(46f, 38f), new Color(0.38f, 0.82f, 0.23f));
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.isOn = true;

        CreateText("Label", toggleObject.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(300f, 48f), label, 28, TextAnchor.MiddleLeft);
        return toggle;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        string value,
        int fontSize,
        TextAnchor alignment)
    {
        return CreateText(name, parent, anchor, pivot, anchoredPosition, size, value, fontSize, alignment, Color.white);
    }

    private static Text CreateText(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        string value,
        int fontSize,
        TextAnchor alignment,
        Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return text;
    }

    private static AudioManager CreateAudioManager()
    {
        GameObject audioObject = new GameObject("AudioManager");
        return audioObject.AddComponent<AudioManager>();
    }

    private static GameManager CreateGameManager(Text speedText, Text driftStateText, Text driftScoreText, AudioManager audioManager)
    {
        GameObject managerObject = new GameObject("GameManager");
        GameManager manager = managerObject.AddComponent<GameManager>();

        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("speedText").objectReferenceValue = speedText;
        serializedManager.FindProperty("driftStateText").objectReferenceValue = driftStateText;
        serializedManager.FindProperty("driftScoreText").objectReferenceValue = driftScoreText;
        serializedManager.FindProperty("audioManager").objectReferenceValue = audioManager;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        return manager;
    }

    private static LevelManager CreateLevelManager(BuiltLevel[] builtLevels, Text levelText, Text coinText, Text totalCoinsText, Text messageText, Text guidanceText, GameManager gameManager)
    {
        GameObject levelObject = new GameObject("LevelManager");
        LevelManager levelManager = levelObject.AddComponent<LevelManager>();

        SerializedObject serializedLevel = new SerializedObject(levelManager);
        SerializedProperty levelsProperty = serializedLevel.FindProperty("levels");
        levelsProperty.arraySize = builtLevels.Length;

        for (int i = 0; i < builtLevels.Length; i++)
        {
            SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(i);
            levelProperty.FindPropertyRelative("displayName").stringValue = builtLevels[i].DisplayName;
            levelProperty.FindPropertyRelative("root").objectReferenceValue = builtLevels[i].Root;
            levelProperty.FindPropertyRelative("spawnPoint").objectReferenceValue = builtLevels[i].SpawnPoint;
            levelProperty.FindPropertyRelative("targetCoins").intValue = builtLevels[i].TargetCoins;
        }

        serializedLevel.FindProperty("currentLevelIndex").intValue = 0;
        serializedLevel.FindProperty("levelText").objectReferenceValue = levelText;
        serializedLevel.FindProperty("coinText").objectReferenceValue = coinText;
        serializedLevel.FindProperty("totalCoinsText").objectReferenceValue = totalCoinsText;
        serializedLevel.FindProperty("messageText").objectReferenceValue = messageText;
        serializedLevel.FindProperty("guidanceText").objectReferenceValue = guidanceText;
        serializedLevel.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedLevel.ApplyModifiedPropertiesWithoutUndo();
        return levelManager;
    }

    private static CarGarage CreateCarGarage(
        GameObject[] carPrefabs,
        Transform spawnPoint,
        CameraFollow cameraFollow,
        GameManager gameManager,
        LevelManager levelManager,
        Text garageText)
    {
        GameObject garageObject = new GameObject("CarGarage");
        if (spawnPoint != null)
        {
            garageObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }

        CarGarage garage = garageObject.AddComponent<CarGarage>();

        SerializedObject serializedGarage = new SerializedObject(garage);
        SerializedProperty carsProperty = serializedGarage.FindProperty("cars");
        string[] ids = { "starter", "drift", "rally", "speed" };
        string[] names = { "Starter Car", "Drift Car", "Rally Car", "Speed Car" };
        carsProperty.arraySize = carPrefabs.Length;

        for (int i = 0; i < carPrefabs.Length; i++)
        {
            SerializedProperty car = carsProperty.GetArrayElementAtIndex(i);
            car.FindPropertyRelative("id").stringValue = i < ids.Length ? ids[i] : $"car_{i + 1}";
            car.FindPropertyRelative("displayName").stringValue = i < names.Length ? names[i] : $"Car {i + 1}";
            car.FindPropertyRelative("unlockCost").intValue = GameBalance.GetCarUnlockCost(i);
            car.FindPropertyRelative("prefab").objectReferenceValue = carPrefabs[i];
        }

        serializedGarage.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        serializedGarage.FindProperty("cameraFollow").objectReferenceValue = cameraFollow;
        serializedGarage.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedGarage.FindProperty("levelManager").objectReferenceValue = levelManager;
        serializedGarage.FindProperty("garageText").objectReferenceValue = garageText;
        serializedGarage.ApplyModifiedPropertiesWithoutUndo();

        return garage;
    }

    private static void CreateMainMenuUI(UiRefs ui, CarGarage carGarage, LevelManager levelManager, AudioManager audioManager, CameraFollow cameraFollow)
    {
        GameObject menuObject = new GameObject("MainMenuUI");
        MainMenuUI menu = menuObject.AddComponent<MainMenuUI>();

        SerializedObject serializedMenu = new SerializedObject(menu);
        serializedMenu.FindProperty("homePanel").objectReferenceValue = ui.HomePanel;
        serializedMenu.FindProperty("shopPanel").objectReferenceValue = ui.ShopPanel;
        serializedMenu.FindProperty("rankPanel").objectReferenceValue = ui.RankPanel;
        serializedMenu.FindProperty("levelSelectPanel").objectReferenceValue = ui.LevelSelectPanel;
        serializedMenu.FindProperty("gameplayHudPanel").objectReferenceValue = ui.GameplayHudPanel;
        serializedMenu.FindProperty("completionPanel").objectReferenceValue = ui.CompletionPanel;
        serializedMenu.FindProperty("settingsPopup").objectReferenceValue = ui.SettingsPopup;
        serializedMenu.FindProperty("spinPopup").objectReferenceValue = ui.SpinPopup;
        serializedMenu.FindProperty("dailyPopup").objectReferenceValue = ui.DailyPopup;
        serializedMenu.FindProperty("coinBalanceText").objectReferenceValue = ui.CoinBalanceText;
        serializedMenu.FindProperty("homeLevelText").objectReferenceValue = ui.HomeLevelText;
        serializedMenu.FindProperty("coinButton").objectReferenceValue = ui.CoinButton;
        serializedMenu.FindProperty("settingsButton").objectReferenceValue = ui.SettingsButton;
        serializedMenu.FindProperty("spinButton").objectReferenceValue = ui.SpinButton;
        serializedMenu.FindProperty("dailyButton").objectReferenceValue = ui.DailyButton;
        serializedMenu.FindProperty("levelButton").objectReferenceValue = ui.LevelButton;
        serializedMenu.FindProperty("gameplayHomeButton").objectReferenceValue = ui.GameplayHomeButton;
        serializedMenu.FindProperty("homeTabButton").objectReferenceValue = ui.HomeTabButton;
        serializedMenu.FindProperty("shopTabButton").objectReferenceValue = ui.ShopTabButton;
        serializedMenu.FindProperty("rankTabButton").objectReferenceValue = ui.RankTabButton;
        serializedMenu.FindProperty("levelSelectStatusText").objectReferenceValue = ui.LevelSelectStatusText;
        serializedMenu.FindProperty("levelSelectBackButton").objectReferenceValue = ui.LevelSelectBackButton;
        serializedMenu.FindProperty("completionTitleText").objectReferenceValue = ui.CompletionTitleText;
        serializedMenu.FindProperty("completionStatsText").objectReferenceValue = ui.CompletionStatsText;
        serializedMenu.FindProperty("completionBestText").objectReferenceValue = ui.CompletionBestText;
        serializedMenu.FindProperty("completionNextButton").objectReferenceValue = ui.CompletionNextButton;
        serializedMenu.FindProperty("completionReplayButton").objectReferenceValue = ui.CompletionReplayButton;
        serializedMenu.FindProperty("completionHomeButton").objectReferenceValue = ui.CompletionHomeButton;
        serializedMenu.FindProperty("completionLevelSelectButton").objectReferenceValue = ui.CompletionLevelSelectButton;
        serializedMenu.FindProperty("shopStatusText").objectReferenceValue = ui.ShopStatusText;
        serializedMenu.FindProperty("shopBackButton").objectReferenceValue = ui.ShopBackButton;
        serializedMenu.FindProperty("rankListText").objectReferenceValue = ui.RankListText;
        serializedMenu.FindProperty("rankBackButton").objectReferenceValue = ui.RankBackButton;
        serializedMenu.FindProperty("musicToggle").objectReferenceValue = ui.MusicToggle;
        serializedMenu.FindProperty("soundToggle").objectReferenceValue = ui.SoundToggle;
        serializedMenu.FindProperty("settingsStatusText").objectReferenceValue = ui.SettingsStatusText;
        serializedMenu.FindProperty("closeSettingsButton").objectReferenceValue = ui.CloseSettingsButton;
        serializedMenu.FindProperty("spinStatusText").objectReferenceValue = ui.SpinStatusText;
        serializedMenu.FindProperty("spinRewardButton").objectReferenceValue = ui.SpinRewardButton;
        serializedMenu.FindProperty("closeSpinButton").objectReferenceValue = ui.CloseSpinButton;
        serializedMenu.FindProperty("dailyStatusText").objectReferenceValue = ui.DailyStatusText;
        serializedMenu.FindProperty("claimDailyButton").objectReferenceValue = ui.ClaimDailyButton;
        serializedMenu.FindProperty("closeDailyButton").objectReferenceValue = ui.CloseDailyButton;
        serializedMenu.FindProperty("carGarage").objectReferenceValue = carGarage;
        serializedMenu.FindProperty("levelManager").objectReferenceValue = levelManager;
        serializedMenu.FindProperty("audioManager").objectReferenceValue = audioManager;
        serializedMenu.FindProperty("cameraFollow").objectReferenceValue = cameraFollow;
        serializedMenu.FindProperty("transitionFadeGroup").objectReferenceValue = ui.TransitionFadeGroup;
        AssignObjectArray(serializedMenu.FindProperty("levelButtons"), ui.LevelButtons);
        AssignObjectArray(serializedMenu.FindProperty("levelTitleTexts"), ui.LevelTitleTexts);
        AssignObjectArray(serializedMenu.FindProperty("levelMetaTexts"), ui.LevelMetaTexts);
        AssignObjectArray(serializedMenu.FindProperty("shopCarButtons"), ui.ShopCarButtons);
        AssignObjectArray(serializedMenu.FindProperty("shopCarTitleTexts"), ui.ShopCarTitleTexts);
        AssignObjectArray(serializedMenu.FindProperty("shopCarCostTexts"), ui.ShopCarCostTexts);
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignObjectArray(SerializedProperty property, Object[] objects)
    {
        property.arraySize = objects != null ? objects.Length : 0;
        for (int i = 0; i < property.arraySize; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        }
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (actions != null)
        {
            inputModule.actionsAsset = actions;
        }
    }

    private static void AssignLevelGarage(LevelManager levelManager, CarGarage carGarage)
    {
        SerializedObject serializedLevel = new SerializedObject(levelManager);
        serializedLevel.FindProperty("carGarage").objectReferenceValue = carGarage;
        serializedLevel.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }
}
