using System.IO;
using MyLittleCaveheart;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyLittleCaveheart.EditorTools
{
    public static class CaveheartWhiteboxSceneBuilder
    {
        private const string ScenePath = "Assets/level1.unity";
        private const string SpritePath = "Assets/WhiteboxGenerated/whitebox_square.png";
        private const string ClickFeedbackPrefabPath = "Assets/ClickFeedback.prefab";

        [InitializeOnLoadMethod]
        private static void AutoBuildWhiteboxDemoOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath))
                {
                    return;
                }

                RebuildWhiteboxDemo();
            };
        }

        [MenuItem("Tools/My Little Caveheart/Rebuild Whitebox Demo")]
        public static void RebuildWhiteboxDemo()
        {
            EnsureClickFeedbackPrefab();
            var square = EnsureWhiteboxSprite();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "level1";

            var camera = CreateCamera();
            var controller = new GameObject("GameController").AddComponent<CaveheartGameController>();
            var audioFeedback = controller.gameObject.AddComponent<CaveheartAudioFeedback>();
            audioFeedback.GetComponent<AudioSource>().playOnAwake = false;
            controller.gameObject.AddComponent<CaveheartEnvironmentFeedback>();
            new GameObject("ClickFeedback").AddComponent<CaveheartClickFeedback>();

            CreateEnvironment(square);
            var characterRoot = CreateCharacter(square, out var body, out var blanket, out var face, out var leftArm, out var rightArm, out var tear);
            var coldOverlay = CreateSpriteObject("Cold Stress Overlay", square, new Vector3(0f, 0f, 5f), new Vector3(24f, 14f, 1f), new Color(0.22f, 0.35f, 0.56f, 0.12f), 20).GetComponent<SpriteRenderer>();
            var warmLight = CreateSpriteObject("Warm Morning Light", square, new Vector3(4.6f, 2.2f, 4.5f), new Vector3(5.5f, 8f, 1f), new Color(1f, 0.8f, 0.42f, 0.18f), 19).GetComponent<SpriteRenderer>();
            warmLight.transform.rotation = Quaternion.Euler(0f, 0f, -22f);
            var stressPulse = CreateSpriteObject("Stress Pulse Overlay", square, new Vector3(0f, 0f, 4.8f), new Vector3(24f, 14f, 1f), new Color(1f, 0.25f, 0.18f, 0f), 21).GetComponent<SpriteRenderer>();

            CreateInteractable(square, controller, CaveheartInteractionType.Alarm, "Alarm", new Vector3(-6.1f, -0.9f, 0f), new Vector2(1.15f, 1.15f), new Color(0.95f, 0.23f, 0.23f));
            CreateInteractable(square, controller, CaveheartInteractionType.GentleTouch, "Gentle Touch", new Vector3(0.1f, 0.55f, 0f), new Vector2(2.0f, 0.75f), new Color(0.9f, 0.62f, 0.52f));
            CreateInteractable(square, controller, CaveheartInteractionType.OfferWater, "Water", new Vector3(5.55f, -1.1f, 0f), new Vector2(1.0f, 1.25f), new Color(0.34f, 0.72f, 0.9f));
            CreateInteractable(square, controller, CaveheartInteractionType.OpenCurtain, "Curtain", new Vector3(5.9f, 1.8f, 0f), new Vector2(1.4f, 3.4f), new Color(0.95f, 0.79f, 0.28f));

            var ui = CreateUi(camera, out var stateText, out var reactionText, out var timeText, out var endingText, out var debugText, out var whiteboxValuesText);
            var presenter = controller.gameObject.AddComponent<CaveheartWhiteboxPresenter>();
            presenter.Configure(controller, body, blanket, face, leftArm, rightArm, tear, coldOverlay, warmLight, stressPulse, characterRoot.transform, stateText, reactionText, timeText, endingText, debugText, whiteboxValuesText);
            Selection.activeObject = controller.gameObject;

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"Built My Little Caveheart whitebox scene: {ScenePath}. UI root: {ui.name}");
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.09f, 0.085f, 0.08f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateEnvironment(Sprite square)
        {
            CreateSpriteObject("Cave Bedroom Back Wall", square, new Vector3(0f, 0f, 2.5f), new Vector3(18f, 10f, 1f), new Color(0.18f, 0.16f, 0.14f), -10);
            CreateSpriteObject("Floor", square, new Vector3(0f, -3.75f, 1f), new Vector3(18f, 2f, 1f), new Color(0.25f, 0.2f, 0.16f), -8);
            CreateSpriteObject("Modern Wall Strip", square, new Vector3(-3.4f, 2.8f, 1f), new Vector3(5f, 0.3f, 1f), new Color(0.42f, 0.47f, 0.5f), -7);
            CreateSpriteObject("Cave Stone Arch", square, new Vector3(0f, 3.45f, 1f), new Vector3(9f, 0.8f, 1f), new Color(0.32f, 0.29f, 0.26f), -6);
            CreateSpriteObject("Bed Base", square, new Vector3(0f, -2.15f, 0f), new Vector3(5.4f, 0.85f, 1f), new Color(0.35f, 0.2f, 0.12f), -2);
            CreateSpriteObject("Window", square, new Vector3(5.9f, 1.8f, 0f), new Vector3(1.7f, 3.7f, 1f), new Color(0.13f, 0.18f, 0.22f), -4);
            CreateSpriteObject("Window Glow", square, new Vector3(5.9f, 1.8f, -0.1f), new Vector3(1.25f, 3.25f, 1f), new Color(0.98f, 0.8f, 0.42f, 0.25f), -3);
        }

        private static GameObject CreateCharacter(Sprite square, out SpriteRenderer body, out SpriteRenderer blanket, out SpriteRenderer face, out SpriteRenderer leftArm, out SpriteRenderer rightArm, out SpriteRenderer tear)
        {
            var root = new GameObject("Little Caveheart Character");
            root.transform.position = new Vector3(0f, -0.4f, -0.2f);

            body = CreateSpriteObject("Body", square, new Vector3(0f, -0.15f, 0f), new Vector3(1.25f, 0.88f, 1f), new Color(0.93f, 0.72f, 0.48f), 3).GetComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform, false);

            face = CreateSpriteObject("Face Mood Mark", square, new Vector3(0.22f, 0.1f, -0.02f), new Vector3(0.45f, 0.18f, 1f), new Color(0.12f, 0.08f, 0.07f), 4).GetComponent<SpriteRenderer>();
            face.transform.SetParent(root.transform, false);

            leftArm = CreateSpriteObject("Left Arm", square, new Vector3(-0.8f, -0.08f, -0.03f), new Vector3(0.85f, 0.18f, 1f), new Color(0.86f, 0.61f, 0.42f), 4).GetComponent<SpriteRenderer>();
            leftArm.transform.SetParent(root.transform, false);

            rightArm = CreateSpriteObject("Right Arm", square, new Vector3(0.8f, -0.08f, -0.03f), new Vector3(0.85f, 0.18f, 1f), new Color(0.86f, 0.61f, 0.42f), 4).GetComponent<SpriteRenderer>();
            rightArm.transform.SetParent(root.transform, false);

            tear = CreateSpriteObject("Tear Marker", square, new Vector3(0.56f, -0.05f, -0.04f), new Vector3(0.12f, 0.35f, 1f), new Color(0.38f, 0.72f, 1f), 5).GetComponent<SpriteRenderer>();
            tear.transform.SetParent(root.transform, false);

            blanket = CreateSpriteObject("Blanket", square, new Vector3(0.1f, -0.18f, -0.05f), new Vector3(1.95f, 1.05f, 1f), new Color(0.47f, 0.66f, 0.9f), 6).GetComponent<SpriteRenderer>();
            blanket.transform.SetParent(root.transform, false);

            return root;
        }

        private static void CreateInteractable(Sprite square, CaveheartGameController controller, CaveheartInteractionType type, string label, Vector3 position, Vector2 size, Color color)
        {
            var item = CreateSpriteObject($"Interactable - {label}", square, position, new Vector3(size.x, size.y, 1f), color, 10);
            var collider = item.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            item.AddComponent<CaveheartClickable>().Configure(controller, type);
            CreateWorldLabel(label, position + new Vector3(0f, -size.y * 0.62f, -0.1f), 0.28f, item.transform);
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return obj;
        }

        private static void CreateWorldLabel(string text, Vector3 position, float size, Transform parent)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, true);
            label.transform.position = position;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 42;
            mesh.color = Color.white;
            label.GetComponent<MeshRenderer>().sortingOrder = 30;
        }

        private static GameObject CreateUi(Camera camera, out Text stateText, out Text reactionText, out Text timeText, out Text endingText, out Text debugText, out Text whiteboxValuesText)
        {
            var canvasObject = new GameObject("Whitebox UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
            canvasObject.AddComponent<GraphicRaycaster>();

            stateText = CreateUiText("State Text", canvasObject.transform, new Vector2(24f, -24f), new Vector2(360f, 48f), TextAnchor.UpperLeft, 24, "State: Sleeping");
            reactionText = CreateUiText("Reaction Text", canvasObject.transform, new Vector2(24f, -76f), new Vector2(660f, 150f), TextAnchor.UpperLeft, 21, "Last: none\nClick an object to test an interaction.");
            timeText = CreateUiText("Time Text", canvasObject.transform, new Vector2(-24f, -24f), new Vector2(220f, 44f), TextAnchor.UpperRight, 22, "Time 00:00");
            whiteboxValuesText = CreateUiText("Whitebox Values Text", canvasObject.transform, new Vector2(-24f, -72f), new Vector2(240f, 120f), TextAnchor.UpperRight, 20, "WHITEBOX VALUES\nAwake  0\nTrust  1\nStress 1");
            debugText = CreateUiText("Debug Text Toggle Mirror", canvasObject.transform, new Vector2(-24f, -200f), new Vector2(240f, 110f), TextAnchor.UpperRight, 16, string.Empty);
            debugText.gameObject.SetActive(true);
            endingText = CreateUiText("Ending Text", canvasObject.transform, Vector2.zero, new Vector2(760f, 160f), TextAnchor.MiddleCenter, 26, "Thank you... this way I am less afraid.\n\nSometimes, slower is not failure.\nIt is standing with yourself again.");
            endingText.gameObject.SetActive(false);
            return canvasObject;
        }

        private static Text CreateUiText(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor anchor, int fontSize, string text)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (anchor == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            var uiText = obj.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.alignment = anchor;
            uiText.color = Color.white;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        private static Sprite EnsureWhiteboxSprite()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[16 * 16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(SpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        [MenuItem("Tools/My Little Caveheart/Create ClickFeedback Prefab")]
        public static void EnsureClickFeedbackPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ClickFeedbackPrefabPath);
            if (existing != null && existing.GetComponent<CaveheartClickFeedback>() != null)
            {
                Selection.activeObject = existing;
                return;
            }

            var prefabObject = new GameObject("ClickFeedback");
            prefabObject.AddComponent<CaveheartClickFeedback>();
            PrefabUtility.SaveAsPrefabAsset(prefabObject, ClickFeedbackPrefabPath);
            Object.DestroyImmediate(prefabObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ClickFeedbackPrefabPath);
            Debug.Log($"Created ClickFeedback prefab: {ClickFeedbackPrefabPath}");
        }
    }
}
