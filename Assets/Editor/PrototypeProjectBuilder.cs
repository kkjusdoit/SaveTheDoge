using System.IO;
using SaveTheDoge;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SaveTheDoge.Editor
{
    public static class PrototypeProjectBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string BeePrefabPath = "Assets/Prefabs/Bee.prefab";
        private const string LevelFolder = "Assets/Prefabs/Levels";

        [MenuItem("SaveTheDoge/Build Prototype")]
        public static void Build()
        {
            EnsureFolders();
            BeeController beePrefab = CreateBeePrefab();
            LevelInstaller[] levels = new[]
            {
                CreateLevelPrefab("Level_01", new Vector2(0f, -2.3f), new Vector2(0f, -0.2f), new Vector2(-3.6f, 2.9f), 5, 8f, 8f, 0),
                CreateLevelPrefab("Level_02", new Vector2(0f, -2.6f), new Vector2(1.3f, 1.2f), new Vector2(-3.8f, 2.8f), 6, 7f, 8.5f, 1),
                CreateLevelPrefab("Level_03", new Vector2(0f, -2.5f), new Vector2(-1.8f, 0.7f), new Vector2(3.7f, 3.2f), 8, 7f, 9f, 2),
            };

            for (int i = 0; i < levels.Length; i++)
            {
                BeeHiveController hive = levels[i].GetComponentInChildren<BeeHiveController>(true);
                SerializedObject serializedHive = new SerializedObject(hive);
                serializedHive.FindProperty("beePrefab").objectReferenceValue = beePrefab;
                serializedHive.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SavePrefabAsset(levels[i].gameObject);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSceneRoot(levels);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToPortrait = true;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildFromBatchMode()
        {
            Build();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory(LevelFolder);
            Directory.CreateDirectory("Assets/Art/Placeholders");
            Directory.CreateDirectory("Assets/Scripts");
        }

        private static BeeController CreateBeePrefab()
        {
            GameObject bee = new GameObject("Bee");
            SpriteRenderer renderer = bee.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBuiltinSprite();
            renderer.color = new Color(1f, 0.87f, 0.27f, 1f);
            bee.transform.localScale = new Vector3(0.45f, 0.32f, 1f);

            var body = bee.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 1.2f;
            body.angularDamping = 1f;
            body.freezeRotation = true;

            bee.AddComponent<CircleCollider2D>().radius = 0.45f;
            BeeController controller = bee.AddComponent<BeeController>();
            AssignField(controller, "body", body);
            AssignField(controller, "bodyRenderer", renderer);

            BeeController prefab = PrefabUtility.SaveAsPrefabAsset(bee, BeePrefabPath).GetComponent<BeeController>();
            Object.DestroyImmediate(bee);
            return prefab;
        }

        private static LevelInstaller CreateLevelPrefab(
            string assetName,
            Vector2 groundCenter,
            Vector2 dogPosition,
            Vector2 hivePosition,
            int beeCount,
            float survivalTime,
            float maxLineLength,
            int variant)
        {
            GameObject root = new GameObject(assetName);
            LevelInstaller installer = root.AddComponent<LevelInstaller>();
            LevelConfig config = root.AddComponent<LevelConfig>();
            AssignField(installer, "config", config);
            AssignField(config, "<LevelName>k__BackingField", assetName.Replace('_', ' '));
            AssignField(config, "<SurvivalDuration>k__BackingField", survivalTime);
            AssignField(config, "<MaxLineLength>k__BackingField", maxLineLength);
            AssignField(config, "<BeeCount>k__BackingField", beeCount);

            CreatePlatform(root.transform, "Ground", groundCenter, new Vector2(8.6f, 0.6f), new Color(0.44f, 0.32f, 0.2f, 1f));
            CreateHazard(root.transform, new Vector2(0f, -4.95f), new Vector2(10.5f, 1.1f));
            CreateDog(root.transform, dogPosition, config);
            CreateHive(root.transform, hivePosition, config);

            if (variant == 1)
            {
                CreatePlatform(root.transform, "Pedestal", new Vector2(1.3f, -0.2f), new Vector2(1.8f, 0.35f), new Color(0.56f, 0.39f, 0.23f, 1f));
                CreatePlatform(root.transform, "LeftWall", new Vector2(-3.2f, -0.4f), new Vector2(0.35f, 2.2f), new Color(0.49f, 0.35f, 0.22f, 1f));
            }
            else if (variant == 2)
            {
                CreatePlatform(root.transform, "Ceiling", new Vector2(-0.7f, 2.2f), new Vector2(3.5f, 0.35f), new Color(0.49f, 0.35f, 0.22f, 1f));
                CreatePlatform(root.transform, "RightTower", new Vector2(2.8f, 0.2f), new Vector2(0.45f, 2.4f), new Color(0.56f, 0.39f, 0.23f, 1f));
            }

            string prefabPath = $"{LevelFolder}/{assetName}.prefab";
            LevelInstaller prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath).GetComponent<LevelInstaller>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildSceneRoot(LevelInstaller[] levels)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.35f;
            camera.backgroundColor = new Color(0.95f, 0.96f, 0.88f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject bootstrap = new GameObject("Bootstrap");
            GameObject gameRoot = new GameObject("GameRoot");
            gameRoot.transform.SetParent(bootstrap.transform);

            GameObject gameplay = new GameObject("Gameplay");
            GameObject levelRoot = new GameObject("LevelRoot");
            GameObject drawLayer = new GameObject("DrawLayer");
            GameObject actorRoot = new GameObject("ActorRoot");
            GameObject beeSpawnRoot = new GameObject("BeeSpawnRoot");
            levelRoot.transform.SetParent(gameplay.transform);
            drawLayer.transform.SetParent(gameplay.transform);
            actorRoot.transform.SetParent(gameplay.transform);
            beeSpawnRoot.transform.SetParent(gameplay.transform);

            LineDrawController lineDraw = gameRoot.AddComponent<LineDrawController>();
            GameFlowController flow = gameRoot.AddComponent<GameFlowController>();

            Canvas canvas = CreateCanvas();
            GameHudController hud = CreateHud(canvas.transform, flow);

            AssignField(flow, "gameplayCamera", camera);
            AssignField(flow, "levelRoot", levelRoot.transform);
            AssignField(flow, "drawLayer", drawLayer.transform);
            AssignField(flow, "beeSpawnRoot", beeSpawnRoot.transform);
            AssignField(flow, "lineDrawController", lineDraw);
            AssignField(flow, "hudController", hud);
            AssignField(flow, "levelPrefabs", levels);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameHudController CreateHud(Transform canvasRoot, GameFlowController flow)
        {
            GameObject hudRoot = CreateUiObject("HUD", canvasRoot);
            Stretch(hudRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text countdown = CreateText("Countdown", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(360f, 100f), 50, TextAnchor.MiddleCenter, Color.black);
            Text length = CreateText("Length", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(420f, 70f), 28, TextAnchor.MiddleCenter, new Color(0.18f, 0.27f, 0.18f, 1f));
            Text hint = CreateText("Hint", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(900f, 120f), 34, TextAnchor.MiddleCenter, new Color(0.24f, 0.22f, 0.18f, 1f));

            GameObject resultsRoot = CreateUiObject("ResultPanels", canvasRoot);
            Stretch(resultsRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject winPanel = CreateResultPanel("WinPanel", resultsRoot.transform, "YOU WIN", new Color(0.30f, 0.65f, 0.35f, 0.95f), out Text winBody);
            GameObject losePanel = CreateResultPanel("LosePanel", resultsRoot.transform, "TRY AGAIN", new Color(0.83f, 0.31f, 0.28f, 0.95f), out Text loseBody);

            Button retryOnWin = CreateButton("RetryButton", winPanel.transform, "Retry", new Vector2(-120f, -170f));
            Button nextButton = CreateButton("NextButton", winPanel.transform, "Next", new Vector2(120f, -170f));
            Button retryOnLose = CreateButton("RetryButton", losePanel.transform, "Retry", new Vector2(0f, -170f));

            GameHudController hud = hudRoot.AddComponent<GameHudController>();
            AssignField(hud, "countdownText", countdown);
            AssignField(hud, "lengthText", length);
            AssignField(hud, "hintText", hint);
            AssignField(hud, "winPanel", winPanel);
            AssignField(hud, "losePanel", losePanel);
            AssignField(hud, "winBodyText", winBody);
            AssignField(hud, "loseBodyText", loseBody);

            UnityEventTools.AddPersistentListener(retryOnWin.onClick, flow.RetryLevel);
            UnityEventTools.AddPersistentListener(nextButton.onClick, flow.LoadNextLevel);
            UnityEventTools.AddPersistentListener(retryOnLose.onClick, flow.RetryLevel);

            winPanel.SetActive(false);
            losePanel.SetActive(false);
            return hud;
        }

        private static GameObject CreateResultPanel(string name, Transform parent, string title, Color color, out Text bodyText)
        {
            GameObject panel = CreateUiObject(name, parent, typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 520f);
            panel.GetComponent<Image>().color = color;

            CreateText("Title", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(620f, 90f), 48, TextAnchor.MiddleCenter, Color.white).text = title;
            bodyText = CreateText("Body", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(620f, 140f), 32, TextAnchor.MiddleCenter, Color.white);
            return panel;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 anchoredPosition)
        {
            GameObject buttonGo = CreateUiObject(name, parent, typeof(Image), typeof(Button));
            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(190f, 80f);

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);

            Text label = CreateText("Label", buttonGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 56f), 28, TextAnchor.MiddleCenter, new Color(0.18f, 0.18f, 0.18f, 1f));
            label.text = text;
            return buttonGo.GetComponent<Button>();
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject go = CreateUiObject(name, parent, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void CreateDog(Transform parent, Vector2 position, LevelConfig config)
        {
            GameObject dog = new GameObject("Dog");
            dog.transform.SetParent(parent);
            dog.transform.position = position;
            dog.transform.localScale = new Vector3(0.75f, 0.68f, 1f);

            SpriteRenderer renderer = dog.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBuiltinSprite();
            renderer.color = new Color(0.95f, 0.65f, 0.24f, 1f);
            renderer.sortingOrder = 2;

            Rigidbody2D body = dog.AddComponent<Rigidbody2D>();
            body.mass = 1.1f;
            body.angularDamping = 1f;

            BoxCollider2D collider = dog.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 1f);

            DogController dogController = dog.AddComponent<DogController>();
            AssignField(dogController, "body", body);
            AssignField(dogController, "bodyRenderer", renderer);
            AssignField(config, "<Dog>k__BackingField", dogController);
        }

        private static void CreateHive(Transform parent, Vector2 position, LevelConfig config)
        {
            GameObject hive = new GameObject("BeeHive");
            hive.transform.SetParent(parent);
            hive.transform.position = position;
            hive.transform.localScale = new Vector3(0.82f, 0.82f, 1f);

            SpriteRenderer renderer = hive.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBuiltinSprite();
            renderer.color = new Color(0.77f, 0.56f, 0.23f, 1f);
            renderer.sortingOrder = 1;

            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(hive.transform);
            spawnPoint.transform.localPosition = Vector3.down * 0.6f;

            BeeHiveController hiveController = hive.AddComponent<BeeHiveController>();
            AssignField(hiveController, "spawnPoint", spawnPoint.transform);
            AssignField(config, "<BeeHive>k__BackingField", hiveController);
        }

        private static void CreateHazard(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject hazard = new GameObject("Water");
            hazard.transform.SetParent(parent);
            hazard.transform.position = position;
            hazard.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = hazard.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBuiltinSprite();
            renderer.color = new Color(0.23f, 0.60f, 0.89f, 0.92f);
            renderer.sortingOrder = -1;

            BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            hazard.AddComponent<HazardController>();
        }

        private static void CreatePlatform(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject platform = new GameObject(name);
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            platform.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
            renderer.sprite = GetBuiltinSprite();
            renderer.color = color;

            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();

            Rigidbody2D body = platform.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            platform.AddComponent<PlatformMarker>();
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            System.Type[] finalTypes = new System.Type[components.Length + 1];
            finalTypes[0] = typeof(RectTransform);
            for (int i = 0; i < components.Length; i++)
            {
                finalTypes[i + 1] = components[i];
            }

            GameObject go = new GameObject(name, finalTypes);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rectTransform, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static Sprite GetBuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void AssignField(Object target, string fieldName, object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value as Object;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = (float)value;
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = (int)value;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = (string)value;
                    break;
                case SerializedPropertyType.Generic:
                    if (property.isArray && value is Object[] objects)
                    {
                        property.arraySize = objects.Length;
                        for (int i = 0; i < objects.Length; i++)
                        {
                            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                        }
                    }
                    break;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
