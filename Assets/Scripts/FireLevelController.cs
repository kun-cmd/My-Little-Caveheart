using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MyLittleCaveheart
{
    [Serializable]
    public sealed class FireThoughtBinding
    {
        public string id;
        public TextMesh text;
        public Vector3 farPosition;
        public Vector3 nearPosition;
        public bool observeTarget;

        [NonSerialized] public bool observed;
        [NonSerialized] public float pressure;
    }

    [ExecuteAlways]
    public sealed class FireLevelController : MonoBehaviour
    {
        private enum FirePhase
        {
            OpeningSuccess,
            ComparisonFall,
            ObserveEcho,
            FireWork,
            Stable,
            Ending
        }

        private enum DragKind
        {
            None,
            AshPoker,
            Kindling,
            Stone,
            WarmLight
        }

        private enum FireCursorMode
        {
            Default,
            Hand,
            Grab
        }

        private sealed class DryingSlotState
        {
            public FireLevelInteractable slot;
            public FireLevelInteractable wood;
            public float progress;
        }

        private sealed class RendererSortingState
        {
            public SpriteRenderer renderer;
            public int sortingOrder;
        }

        private sealed class FallingDropState
        {
            public Transform target;
            public float fixedX;
            public float targetY;
            public float z;
            public float velocity;
        }

        [Header("Core Tuning")]
        [SerializeField, Min(0.1f)] private float observeHoldSeconds = 1f;
        [SerializeField, Range(0f, 100f)] private float startingFire = 16f;
        [SerializeField, Range(0f, 1f)] private float openingStirCompletion = 0.42f;
        [SerializeField, Range(0f, 100f)] private float openingSuccessFire = 44f;
        [SerializeField, Range(0f, 100f)] private float fireWorkStartFire = 38f;
        [SerializeField, Range(0f, 100f)] private float completionFire = 100f;
        [SerializeField, Min(0.1f)] private float hunterEnterDelay = 0.35f;
        [SerializeField, Min(0.1f)] private float hunterAnnounceDelay = 1.15f;
        [SerializeField, Min(0.1f)] private float cheerDelay = 1.85f;
        [SerializeField, Min(0.1f)] private float comparisonFirstLineDelay = 2.35f;
        [SerializeField, Min(0.1f)] private float comparisonLineInterval = 1.05f;
        [SerializeField, Range(0f, 100f)] private float fireDropPerComparisonLine = 5.5f;
        [SerializeField, Range(0f, 100f)] private float distractionFireDrop = 11f;

        [Header("Fire Work Tuning")]
        [SerializeField, Range(0f, 100f)] private float naturalFireDecayPerSecond = 0.8f;
        [SerializeField, Range(0f, 1f)] private float fullFireNaturalDecayMultiplier = 0.6f;
        [SerializeField, Range(0f, 100f)] private float windFireDecayPerSecond = 3.2f;
        [SerializeField, Range(0f, 100f)] private float blockedWindFireDecayPerSecond = 0.6f;
        [SerializeField, Range(0f, 100f)] private float ashFireDecayPerSecond = 1.8f;
        [SerializeField, Range(0f, 100f)] private float dryKindlingFireBoost = 18f;
        [SerializeField, Range(0f, 100f)] private float wetKindlingFirePenalty = 8f;
        [SerializeField, Range(0f, 100f)] private float ashPerKindling = 34f;
        [SerializeField, Min(0f)] private float ashAfterKindlingDelaySeconds = 3.5f;
        [SerializeField, Range(0f, 100f)] private float ashSlowThreshold = 45f;
        [SerializeField, Range(0f, 100f)] private float ashChokeThreshold = 70f;
        [SerializeField, Min(0f)] private float ashStirClearPerSecond = 28f;
        [SerializeField, Min(0f)] private float ashStirFireBoostPerSecond = 2f;
        [SerializeField, Range(0f, 100f)] private float ashStirBoostThreshold = 20f;
        [SerializeField, Min(0.01f)] private float carefulStirMaxSpeed = 3.4f;
        [SerializeField, Min(0.01f)] private float roughStirSparkSpeed = 6.2f;
        [SerializeField, Min(0.05f)] private float ashWakeFadePerSecond = 3.2f;
        [SerializeField, Range(0f, 1f)] private float ashWakeMaxAlpha = 0.55f;
        [SerializeField, Min(0.1f)] private float roastDrySecondsHot = 8f;
        [SerializeField, Min(0.1f)] private float roastDrySecondsWarm = 12f;
        [SerializeField, Min(0.1f)] private float roastDrySecondsLow = 14f;
        [SerializeField, Min(0.1f)] private float roastDrySecondsEmber = 26f;
        [FormerlySerializedAs("roastPauseBelowFire")]
        [SerializeField, Range(0f, 100f)] private float roastEmberBelowFire = 8f;
        [SerializeField, Range(0f, 100f)] private float roastHotAtFire = 55f;
        [SerializeField, Range(0f, 100f)] private float roastWarmAtFire = 28f;
        [SerializeField, Min(0.1f)] private float resupplyDelay = 2f;

        [Header("Wind Tuning")]
        [SerializeField, Min(0.1f)] private float windFirstDelay = 14f;
        [SerializeField, Min(0.1f)] private float windIntervalMin = 9f;
        [SerializeField, Min(0.1f)] private float windIntervalMax = 13f;
        [SerializeField, Min(0.1f)] private float windWarningSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float windDurationSeconds = 5f;
        [SerializeField, Range(0f, 1f)] private float windWarningAlpha = 0.28f;
        [SerializeField, Range(0f, 1f)] private float windActiveAlpha = 1f;
        [SerializeField, Min(0.1f)] private float windFadeSpeed = 3.8f;
        [SerializeField, Min(0f)] private float windBobAmplitude = 0.05f;
        [SerializeField, Min(0f)] private float windStretchAmplitude = 0.08f;

        [Header("Drag Feel")]
        [SerializeField, Min(1)] private int dragLiftSortingBoost = 1000;
        [SerializeField, Min(0f)] private float placementTouchPadding = 0.18f;
        [SerializeField, Min(0.01f)] private float dropFallInitialSpeed = 2.8f;
        [SerializeField, Min(0.01f)] private float dropFallAcceleration = 18f;
        [SerializeField, Min(0.001f)] private float dropFallSnapDistance = 0.025f;

        [Header("Scene References")]
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Transform sceneRoot;
        [SerializeField] private Transform fireRoot;
        [SerializeField] private Transform propRoot;
        [SerializeField] private Transform thoughtRoot;
        [SerializeField] private SpriteRenderer caveWall;
        [SerializeField] private SpriteRenderer caveFloor;
        [SerializeField] private SpriteRenderer fireGlow;
        [SerializeField] private SpriteRenderer emberGlow;
        [SerializeField] private SpriteRenderer ashLayer;
        [SerializeField] private SpriteRenderer ashStirWake;
        [SerializeField] private SpriteRenderer flameOuter;
        [SerializeField] private SpriteRenderer flameInner;
        [SerializeField] private SpriteRenderer windVisual;
        [SerializeField] private SpriteRenderer lionShadow;
        [SerializeField] private SpriteRenderer hunterCapsule;
        [SerializeField] private SpriteRenderer woodcutterCapsule;
        [SerializeField] private SpriteRenderer littleCaveheartRenderer;
        [SerializeField] private SpriteRenderer firePitRenderer;
        [SerializeField] private SpriteRenderer dryingSpotsArtRenderer;
        [FormerlySerializedAs("emberStickObject")]
        [SerializeField] private GameObject ashPokerObject;
        [SerializeField] private List<GameObject> kindlingObjects = new List<GameObject>();
        [SerializeField] private GameObject stoneObject;
        [SerializeField] private GameObject warmLightObject;
        [SerializeField] private FireLevelInteractable fireZone;
        [SerializeField] private FireLevelInteractable ashZone;
        [FormerlySerializedAs("windBlockSlot")]
        [SerializeField] private FireLevelInteractable rightWindBlockSlot;
        [SerializeField] private FireLevelInteractable leftWindBlockSlot;
        [SerializeField] private List<FireLevelInteractable> dryingSlots = new List<FireLevelInteractable>();

        [Header("Text References")]
        [SerializeField] private TextMesh caveTitle;
        [SerializeField] private TextMesh statusText;
        [SerializeField] private TextMesh actionLabel;
        [SerializeField] private TextMesh companionText;
        [SerializeField] private TextMesh finalThoughtText;
        [SerializeField] private TextMesh endingText;
        [SerializeField] private List<FireThoughtBinding> thoughts = new List<FireThoughtBinding>();

        [Header("Decorative References")]
        [SerializeField] private List<SpriteRenderer> dashedEchoSegments = new List<SpriteRenderer>();
        [SerializeField] private List<SpriteRenderer> sparkRenderers = new List<SpriteRenderer>();
        [SerializeField] private List<SpriteRenderer> wallArtRenderers = new List<SpriteRenderer>();

        [Header("Editable Text")]
        [SerializeField] private string levelTitle = "Level 2: The Fire I Keep";
        [SerializeField] private string companionLine = "I caught a lion.";
        [SerializeField] private string crowdCheerLine = "Cheers rise outside.";
        [SerializeField] private string finalBeforeText = "I caught a lion,\nyou cannot do it forever.";
        [SerializeField] private string finalAfterText = "I kept the fire alive.";
        [SerializeField, TextArea] private string endingCopy = "He caught a lion.\nI kept the fire alive.\n\nI do not have to be him\nto be worth something.";

        [Header("Little Caveheart Sprites")]
        [SerializeField] private string level2CavemanSpriteFolder = "Sprites/Caveheart/Level2Fire/States1254/";

        [Header("Scene Art")]
        [SerializeField] private bool useLevel2SceneArt = true;
        [SerializeField] private string level2SceneArtFolder = "Sprites/Caveheart/Level2Fire/SceneArt/";
        [SerializeField] private bool preferNoFireBackground = true;
        [SerializeField] private bool backgroundIncludesBakedFire;

        [Header("Editable Layout")]
        [FormerlySerializedAs("emberStickStart")]
        [SerializeField] private Vector3 ashPokerStart = new Vector3(-4.75f, -2.45f, -0.5f);
        [SerializeField] private Vector3 stoneStart = new Vector3(3.65f, -2.5f, -0.5f);
        [FormerlySerializedAs("stonePlacedPosition")]
        [SerializeField] private Vector3 rightStonePlacedPosition = new Vector3(2.55f, -2.02f, -0.45f);
        [SerializeField] private Vector3 leftStonePlacedPosition = new Vector3(-2.55f, -2.02f, -0.45f);
        [SerializeField] private Vector3 warmLightStart = new Vector3(0f, -1.23f, -0.85f);
        [SerializeField] private Vector3 littleCaveheartBasePosition = new Vector3(-4.2f, -1.72f, -0.5f);

        [Header("Hover Label")]
        [SerializeField] private Vector2 hoverLabelSize = new Vector2(260f, 58f);
        [SerializeField] private Vector2 hoverLabelOffset = new Vector2(18f, -18f);

        [Header("Cursor")]
        [SerializeField] private ExamCursorVisuals cursorVisuals;

        private readonly Dictionary<Transform, Vector3> dragHomePositions = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Vector3> kindlingSpawnPositions = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, List<RendererSortingState>> liftedRendererStates = new Dictionary<Transform, List<RendererSortingState>>();
        private readonly Dictionary<Transform, Vector3> windSlotBaseScales = new Dictionary<Transform, Vector3>();
        private readonly List<FallingDropState> fallingDrops = new List<FallingDropState>();
        private readonly List<DryingSlotState> dryingStates = new List<DryingSlotState>();
        private readonly List<float> pendingAshReadyTimes = new List<float>();
        private readonly HashSet<Transform> consumedWood = new HashSet<Transform>();

        private FireLevelInteractable activeObserveThought;
        private Vector3 dragOffset;
        private DragKind dragKind;
        private Transform draggedTransform;
        private bool releaseKeepsLiftedVisual;
        private FirePhase phase = FirePhase.OpeningSuccess;
        private float phaseTimer;
        private float fireLevel;
        private float ashLevel;
        private float openingStirProgress;
        private float observeHeldFor;
        private float tremble;
        private float thoughtPressure;
        private Vector3 lastStirPointer;
        private float ashWakeAlpha;
        private int nextThoughtIndex;
        private Sprite cavemanShameFireTiny;
        private Sprite cavemanAnxiousFireLow;
        private Sprite cavemanFocusingFireRecovering;
        private Sprite cavemanEngagedFireStable;
        private Sprite cavemanConfidentFireStrong;
        private Sprite cavemanRelievedEnding;
        private Sprite cavemanStartledComparison;
        private Sprite sceneArtDryKindling;
        private Sprite sceneArtWetKindling;
        private Sprite sceneArtWindLeft;
        private Sprite sceneArtWindRight;
        private bool stonePlaced;
        private int windDirection = 1;
        private int blockedWindDirection;
        private bool endingResolved;
        private bool hasStirPointer;
        private float nextWindAt;
        private float windEndsAt;
        private float windVisualAlpha;
        private int windVisualDirection = 1;
        private Vector3 windVisualBasePosition;
        private Vector3 windVisualBaseScale = Vector3.one;
        private bool windVisualHomeCached;
        private float resupplyTimer;
        private int batchIndex;
        private FireCursorMode currentCursorMode = (FireCursorMode)(-1);
#if UNITY_EDITOR
        private bool editorSceneArtRefreshQueued;
#endif

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                BindSceneReferencesByName(true);
                ApplyTypography();
                ScheduleEditorSceneArtRefresh();
            }
        }

        private void Awake()
        {
            BindSceneReferencesByName(false);
            InitializeRuntimeState();
            ApplyPhaseVisuals(true);
        }

        private void OnValidate()
        {
            BindSceneReferencesByName(true);
            ApplyTypography();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ScheduleEditorSceneArtRefresh();
                UnityEditor.EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
#endif
        }

#if UNITY_EDITOR
        private void ScheduleEditorSceneArtRefresh()
        {
            if (Application.isPlaying || editorSceneArtRefreshQueued)
            {
                return;
            }

            editorSceneArtRefreshQueued = true;
            UnityEditor.EditorApplication.delayCall += ApplyQueuedEditorSceneArtRefresh;
        }

        private void ApplyQueuedEditorSceneArtRefresh()
        {
            editorSceneArtRefreshQueued = false;
            if (this == null || Application.isPlaying)
            {
                return;
            }

            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            {
                return;
            }

            BindSceneReferencesByName(true);
            ApplySceneArtSprites();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#else
        private void ScheduleEditorSceneArtRefresh()
        {
            ApplySceneArtSprites();
        }
#endif

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            phaseTimer += Time.deltaTime;
            UpdatePhaseTimeline();
            HandlePointerInput();
            UpdateFallingDrops();
            UpdateFireWorkLoop();
            UpdateComparisonPressure();
            UpdateLittleCaveheart();
            ApplyPhaseVisuals(false);
        }

        private void OnDisable()
        {
            cursorVisuals?.SetCursorMode(ExamCursorMode.Default);
            currentCursorMode = (FireCursorMode)(-1);
        }

        private void InitializeRuntimeState()
        {
            if (sceneCamera == null)
            {
                sceneCamera = Camera.main;
            }

            ApplyTypography();
            LoadLittleCaveheartSprites();
            ApplySceneArtSprites();
            CacheWindVisualHome();
            fireLevel = FireTo01(startingFire);
            ashLevel = 0.65f;
            openingStirProgress = 0f;
            phase = FirePhase.OpeningSuccess;
            phaseTimer = 0f;
            observeHeldFor = 0f;
            tremble = 0f;
            thoughtPressure = 0f;
            nextThoughtIndex = 0;
            stonePlaced = false;
            blockedWindDirection = 0;
            windDirection = 1;
            windVisualDirection = windDirection;
            windVisualAlpha = 0f;
            endingResolved = false;
            hasStirPointer = false;
            activeObserveThought = null;
            draggedTransform = null;
            dragKind = DragKind.None;
            nextWindAt = windFirstDelay;
            windEndsAt = -1f;
            resupplyTimer = 0f;
            batchIndex = 0;
            dragHomePositions.Clear();
            kindlingSpawnPositions.Clear();
            liftedRendererStates.Clear();
            windSlotBaseScales.Clear();
            fallingDrops.Clear();
            pendingAshReadyTimes.Clear();
            consumedWood.Clear();
            BuildDryingStates();

            for (var i = 0; i < thoughts.Count; i++)
            {
                thoughts[i].observed = false;
                thoughts[i].pressure = 0f;
                if (thoughts[i].text != null)
                {
                    thoughts[i].text.transform.position = thoughts[i].farPosition;
                }
            }

            if (ashPokerObject != null)
            {
                ashPokerObject.SetActive(true);
                CacheHome(ashPokerObject.transform);
            }

            CacheKindlingHomes();
            SetKindlingActive(true);

            if (stoneObject != null)
            {
                stoneObject.SetActive(true);
                CacheHome(stoneObject.transform);
            }

            if (warmLightObject != null)
            {
                warmLightObject.SetActive(false);
                CacheHome(warmLightObject.transform);
            }

            if (companionText != null)
            {
                companionText.text = string.Empty;
            }

            SetRendererAlpha(hunterCapsule, 0f);
            SetRendererAlpha(woodcutterCapsule, 0f);
            ashWakeAlpha = 0f;
            SetRendererAlpha(ashStirWake, 0f);

            if (caveTitle != null)
            {
                caveTitle.text = levelTitle;
            }

            if (finalThoughtText != null)
            {
                finalThoughtText.text = finalBeforeText;
                finalThoughtText.color = new Color(0.28f, 0.22f, 0.18f, 0f);
            }

            if (endingText != null)
            {
                endingText.text = string.Empty;
            }
        }

        private void BindSceneReferencesByName(bool editorOnly)
        {
            if (editorOnly && Application.isPlaying)
            {
                return;
            }

            var scene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            sceneCamera = FindComponentByName<Camera>("Level 2 Camera") ?? sceneCamera ?? Camera.main;
            cursorVisuals = FindComponentByName<ExamCursorVisuals>("Exam Cursor Visuals") ?? cursorVisuals ?? GetComponent<ExamCursorVisuals>();
            sceneRoot = FindTransformByName("Fire Level Authored Scene") ?? sceneRoot;
            fireRoot = FindTransformByName("Fire Workbench") ?? fireRoot;
            propRoot = FindTransformByName("Fire Props") ?? propRoot;
            thoughtRoot = FindTransformByName("Comparison Echoes") ?? thoughtRoot;

            caveWall = FindComponentByName<SpriteRenderer>("Cave Wall") ?? caveWall;
            caveFloor = FindComponentByName<SpriteRenderer>("Cave Floor") ?? caveFloor;
            fireGlow = FindComponentByName<SpriteRenderer>("Fire Glow") ?? fireGlow;
            emberGlow = FindComponentByName<SpriteRenderer>("Red Embers") ?? emberGlow;
            ashLayer = FindComponentByName<SpriteRenderer>("Ash Layer") ?? ashLayer;
            ashStirWake = FindComponentByName<SpriteRenderer>("Ash Stir Wake") ?? ashStirWake;
            flameOuter = FindComponentByName<SpriteRenderer>("Outer Flame") ?? flameOuter;
            flameInner = FindComponentByName<SpriteRenderer>("Inner Flame") ?? flameInner;
            windVisual = FindComponentByName<SpriteRenderer>("Cave Wind") ?? windVisual;
            lionShadow = FindComponentByName<SpriteRenderer>("Lion Shadow") ?? lionShadow;
            hunterCapsule = FindComponentByName<SpriteRenderer>("Hero Hunter")
                ?? FindComponentByName<SpriteRenderer>("Hunter Capsule")
                ?? hunterCapsule;
            woodcutterCapsule = FindComponentByName<SpriteRenderer>("Woodcutter Capsule") ?? woodcutterCapsule;
            littleCaveheartRenderer = FindComponentByName<SpriteRenderer>("Little Caveheart") ?? littleCaveheartRenderer;
            firePitRenderer = FindComponentByName<SpriteRenderer>("Fire Pit") ?? firePitRenderer;
            dryingSpotsArtRenderer = FindComponentByName<SpriteRenderer>("Drying Spots Art") ?? dryingSpotsArtRenderer;

            ashPokerObject = FindSceneObject("Ash Poker")
                ?? FindSceneObject("Ember Stick")
                ?? ashPokerObject;
            BindKindlingObjectsByName();
            stoneObject = FindSceneObject("Wind Stone") ?? stoneObject;
            warmLightObject = FindSceneObject("Warm Light") ?? warmLightObject;

            fireZone = FindComponentByName<FireLevelInteractable>("Fire Interaction Zone") ?? fireZone;
            ashZone = FindComponentByName<FireLevelInteractable>("Ash Stir Zone") ?? ashZone;
            rightWindBlockSlot = FindComponentByName<FireLevelInteractable>("Right Wind Block Slot")
                ?? FindComponentByName<FireLevelInteractable>("Wind Block Slot")
                ?? FindComponentByName<FireLevelInteractable>("Stone Wind Target")
                ?? rightWindBlockSlot;
            leftWindBlockSlot = FindComponentByName<FireLevelInteractable>("Left Wind Block Slot") ?? leftWindBlockSlot;
            BindDryingSlotsByName();

            caveTitle = FindComponentByName<TextMesh>("Level Title") ?? caveTitle;
            statusText = FindComponentByName<TextMesh>("State Text") ?? statusText;
            actionLabel = FindComponentByName<TextMesh>("Action Label") ?? actionLabel;
            companionText = FindComponentByName<TextMesh>("Companion Voice") ?? companionText;
            finalThoughtText = FindComponentByName<TextMesh>("Final Thought") ?? finalThoughtText;
            endingText = FindComponentByName<TextMesh>("Ending Text") ?? endingText;

            BindThoughtsByName();
            BindRendererListByPrefix(wallArtRenderers, "Dim Wall Painting ");
            BindRendererListByPrefix(sparkRenderers, "Fire Spark ");
            BindRendererListByPrefix(dashedEchoSegments, "Observed Echo Dash ");
            UpdateEditableLayoutFromScene();
        }

        private void BindKindlingObjectsByName()
        {
            kindlingObjects.Clear();
            AddKindlingObject("Dry Kindling 0");
            AddKindlingObject("Dry Kindling 1");
            for (var i = 0; i < 6; i++)
            {
                AddKindlingObject("Wet Kindling " + i);
            }
        }

        private void AddKindlingObject(string objectName)
        {
            var found = FindSceneObject(objectName);
            if (found != null)
            {
                kindlingObjects.Add(found);
            }
        }

        private void BindDryingSlotsByName()
        {
            dryingSlots.Clear();
            AddDryingSlot("Roast Slot 0");
            AddDryingSlot("Roast Slot 1");
        }

        private void AddDryingSlot(string objectName)
        {
            var slot = FindComponentByName<FireLevelInteractable>(objectName);
            if (slot != null)
            {
                dryingSlots.Add(slot);
            }
        }

        private void BindThoughtsByName()
        {
            thoughts.Clear();
            AddExistingThought("comparison-strong", "Thought - He is so strong.", new Vector3(1.85f, 1.35f, -0.6f), false);
            AddExistingThought("comparison-never", "Thought - I could never do that.", new Vector3(-1.85f, 1.05f, -0.6f), false);
            AddExistingThought("comparison-weak", "Thought - I am weak.", new Vector3(1.35f, 0.42f, -0.6f), false);
            AddExistingThought("comparison-everything", "Thought - I am bad at everything.", new Vector3(-1.2f, 0.02f, -0.6f), false);
            AddExistingThought("comparison-try", "Thought - Why even try?", new Vector3(1.55f, -0.35f, -0.6f), false);
            AddExistingThought("taunt", "Thought - They are laughing at me.", new Vector3(0f, 1.72f, -0.6f), true);
        }

        private void AddExistingThought(string id, string objectName, Vector3 fallbackNearPosition, bool observeTarget)
        {
            var text = FindComponentByName<TextMesh>(objectName);
            if (text == null)
            {
                return;
            }

            thoughts.Add(new FireThoughtBinding
            {
                id = id,
                text = text,
                farPosition = text.transform.position,
                nearPosition = fallbackNearPosition,
                observeTarget = observeTarget
            });
        }

        private void BindRendererListByPrefix(List<SpriteRenderer> targetList, string prefix)
        {
            targetList.Clear();
            var index = 0;
            while (true)
            {
                var renderer = FindComponentByName<SpriteRenderer>(prefix + index);
                if (renderer == null)
                {
                    break;
                }

                targetList.Add(renderer);
                index++;
            }
        }

        private void UpdateEditableLayoutFromScene()
        {
            if (ashPokerObject != null)
            {
                ashPokerStart = ashPokerObject.transform.position;
            }

            if (stoneObject != null && !stonePlaced)
            {
                stoneStart = stoneObject.transform.position;
            }

            if (warmLightObject != null)
            {
                warmLightStart = warmLightObject.transform.position;
            }

            if (littleCaveheartRenderer != null)
            {
                littleCaveheartBasePosition = littleCaveheartRenderer.transform.position;
            }
        }

        private T FindComponentByName<T>(string objectName) where T : Component
        {
            var foundObject = FindSceneObject(objectName);
            return foundObject == null ? null : foundObject.GetComponent<T>();
        }

        private Transform FindTransformByName(string objectName)
        {
            var foundObject = FindSceneObject(objectName);
            return foundObject == null ? null : foundObject.transform;
        }

        private GameObject FindSceneObject(string objectName)
        {
            var scene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindInHierarchy(roots[i].transform, objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInHierarchy(Transform current, string objectName)
        {
            if (current.name == objectName)
            {
                return current;
            }

            for (var i = 0; i < current.childCount; i++)
            {
                var found = FindInHierarchy(current.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void UpdatePhaseTimeline()
        {
            if (phase == FirePhase.ComparisonFall)
            {
                SetRendererAlpha(hunterCapsule, Mathf.Clamp01((phaseTimer - hunterEnterDelay) * 2.2f));
                if (phaseTimer > hunterAnnounceDelay && companionText != null)
                {
                    companionText.text = phaseTimer > cheerDelay
                        ? companionLine + "\n" + crowdCheerLine
                        : companionLine;
                }

                if (phaseTimer > cheerDelay)
                {
                    fireLevel = Mathf.MoveTowards(fireLevel, FireTo01(10f), Time.deltaTime * 0.16f);
                    tremble = Mathf.Clamp01(tremble + Time.deltaTime * 0.06f);
                }

                var shouldShowCount = Mathf.Clamp(Mathf.FloorToInt((phaseTimer - comparisonFirstLineDelay) / comparisonLineInterval) + 1, 0, thoughts.Count);
                while (nextThoughtIndex < shouldShowCount)
                {
                    thoughts[nextThoughtIndex].pressure = 0.82f;
                    nextThoughtIndex++;
                    fireLevel = Mathf.Max(0.06f, fireLevel - FireTo01(fireDropPerComparisonLine));
                    tremble += 0.12f;
                }

                if (nextThoughtIndex >= thoughts.Count && phaseTimer > comparisonFirstLineDelay + comparisonLineInterval * thoughts.Count + 0.5f)
                {
                    SetPhase(FirePhase.ObserveEcho);
                }
            }
            else if (phase == FirePhase.FireWork && IsCompletionFireReached())
            {
                SetPhase(FirePhase.Stable);
            }
        }

        private void UpdateFireWorkLoop()
        {
            if (phase != FirePhase.FireWork)
            {
                return;
            }

            var decay = NaturalFireDecayForCurrentFire();
            if (ashLevel >= FireTo01(ashSlowThreshold))
            {
                decay += ashFireDecayPerSecond;
            }

            if (IsWindActive())
            {
                decay += IsCurrentWindBlocked() ? blockedWindFireDecayPerSecond : windFireDecayPerSecond;
            }

            fireLevel = Mathf.Max(0.02f, fireLevel - FireTo01(decay) * Time.deltaTime);

            UpdateDryingSlots();
            UpdatePendingAsh();
            UpdateResupply();

            if (Time.time > windEndsAt && Time.time > nextWindAt + windDurationSeconds)
            {
                ScheduleNextWind();
            }
        }

        private float NaturalFireDecayForCurrentFire()
        {
            return naturalFireDecayPerSecond * Mathf.Lerp(1f, fullFireNaturalDecayMultiplier, Mathf.Clamp01(fireLevel));
        }

        private bool IsCompletionFireReached()
        {
            var completionTolerance = FireTo01(0.5f);
            return fireLevel >= FireTo01(completionFire) - completionTolerance;
        }

        private void TryCompleteFireWork()
        {
            if (phase == FirePhase.FireWork && IsCompletionFireReached())
            {
                SetPhase(FirePhase.Stable);
            }
        }

        private void HandlePointerInput()
        {
            if (sceneCamera == null)
            {
                return;
            }

            var pointer = PointerWorld();
            var top = TopInteractableAt(pointer);
            UpdateActionLabel(top);

            if (dragKind != DragKind.None)
            {
                SetCursorMode(FireCursorMode.Grab);
                ContinueDrag(pointer);
                return;
            }

            SetCursorMode(CursorModeForHover(top));

            if (Input.GetMouseButtonDown(0))
            {
                BeginPointerAction(pointer, top);
            }

            if (Input.GetMouseButton(0))
            {
                ContinuePointerHold(pointer, top);
            }
            else
            {
                FinishPointerHold();
            }
        }

        private void BeginPointerAction(Vector3 pointer, FireLevelInteractable target)
        {
            if (target == null)
            {
                return;
            }

            if (target.kind == FireLevelInteractableKind.AshPoker)
            {
                BeginDrag(DragKind.AshPoker, target.transform, pointer);
                return;
            }

            if (target.kind == FireLevelInteractableKind.Kindling)
            {
                if (IsWetWoodDrying(target))
                {
                    return;
                }

                ClearDryingSlotFor(target);
                BeginDrag(DragKind.Kindling, target.transform, pointer);
                return;
            }

            if (target.kind == FireLevelInteractableKind.Stone)
            {
                stonePlaced = false;
                blockedWindDirection = 0;
                BeginDrag(DragKind.Stone, target.transform, pointer);

                return;
            }

            if (target.kind == FireLevelInteractableKind.WarmLight && phase == FirePhase.Stable)
            {
                BeginDrag(DragKind.WarmLight, target.transform, pointer);
                return;
            }

            if (target.kind == FireLevelInteractableKind.Thought)
            {
                if (phase == FirePhase.ObserveEcho && target.id == "taunt")
                {
                    activeObserveThought = target;
                    observeHeldFor = 0f;
                    return;
                }

                if (phase == FirePhase.FireWork)
                {
                    DistractFromFire();
                }

                return;
            }

            if (target.kind == FireLevelInteractableKind.LionShadow && phase == FirePhase.FireWork)
            {
                DistractFromFire();
            }
        }

        private void ContinuePointerHold(Vector3 pointer, FireLevelInteractable target)
        {
            if (activeObserveThought != null)
            {
                if (target == activeObserveThought)
                {
                    observeHeldFor = Mathf.MoveTowards(observeHeldFor, observeHoldSeconds, Time.deltaTime);
                    if (observeHeldFor >= observeHoldSeconds)
                    {
                        ResolveEchoObserve();
                    }
                }
                else
                {
                    observeHeldFor = Mathf.MoveTowards(observeHeldFor, 0f, Time.deltaTime * 2f);
                }
            }
        }

        private void FinishPointerHold()
        {
            if (activeObserveThought != null && observeHeldFor < observeHoldSeconds)
            {
                activeObserveThought = null;
                observeHeldFor = 0f;
            }

            hasStirPointer = false;
        }

        private void BeginDrag(DragKind kind, Transform target, Vector3 pointer)
        {
            dragKind = kind;
            draggedTransform = target;
            releaseKeepsLiftedVisual = false;
            CancelFallingDrop(target, false);
            LiftDragVisuals(target);
            dragOffset = target.position - pointer;
            target.SetAsLastSibling();
            SetCursorMode(FireCursorMode.Grab);
        }

        private void ContinueDrag(Vector3 pointer)
        {
            if (draggedTransform == null)
            {
                dragKind = DragKind.None;
                return;
            }

            draggedTransform.position = new Vector3(pointer.x + dragOffset.x, pointer.y + dragOffset.y, draggedTransform.position.z);

            if (dragKind == DragKind.AshPoker)
            {
                if (DraggedToolTouches(ashZone, pointer))
                {
                    StirAsh(pointer);
                    if (phase == FirePhase.OpeningSuccess && openingStirProgress >= openingStirCompletion)
                    {
                        fireLevel = FireTo01(openingSuccessFire);
                        SetStatus("The ember breathes.");
                        SetPhase(FirePhase.ComparisonFall);
                    }
                }
                else
                {
                    hasStirPointer = false;
                }
            }

            if (!Input.GetMouseButtonUp(0))
            {
                return;
            }

            if (dragKind == DragKind.AshPoker)
            {
                DropAshPoker();
            }
            else if (dragKind == DragKind.Kindling)
            {
                DropKindling(pointer);
            }
            else if (dragKind == DragKind.Stone)
            {
                DropStone(WindBlockDirectionAt(pointer));
            }
            else if (dragKind == DragKind.WarmLight)
            {
                var droppedOnFinal = finalThoughtText != null && IsPointInsideCollider(finalThoughtText.GetComponent<Collider2D>(), pointer);
                DropWarmLight(droppedOnFinal);
            }

            if (!releaseKeepsLiftedVisual)
            {
                RestoreDragVisuals(draggedTransform);
            }

            draggedTransform = null;
            dragKind = DragKind.None;
            releaseKeepsLiftedVisual = false;
            SetCursorMode(FireCursorMode.Default);
        }

        private void DropKindling(Vector3 pointer)
        {
            var kindling = draggedTransform == null ? null : draggedTransform.GetComponent<FireLevelInteractable>();
            if (kindling == null)
            {
                ResetDraggedTransform();
                return;
            }

            if (phase != FirePhase.FireWork)
            {
                StartFallingDrop(kindling.transform);
                return;
            }

            var dryingSlot = DryingSlotAt(pointer);
            if (!kindling.dryKindling && dryingSlot != null)
            {
                PlaceWetWoodInDryingSlot(kindling, dryingSlot);
                return;
            }

            if (!DraggedToolTouches(fireZone, pointer))
            {
                StartFallingDrop(kindling.transform);
                return;
            }

            if (!kindling.dryKindling)
            {
                StartFallingDrop(kindling.transform);
                fireLevel = Mathf.Max(0.02f, fireLevel - FireTo01(wetKindlingFirePenalty));
                ScatterSparks(0.2f);
                SetStatus("Wet wood smokes.");
                return;
            }

            var boost = dryKindlingFireBoost * (ashLevel >= FireTo01(ashChokeThreshold) ? 0.6f : 1f);
            fireLevel = Mathf.Clamp01(fireLevel + FireTo01(boost));
            TryCompleteFireWork();
            ScheduleAshFromKindling();
            ClearDryingSlotFor(kindling);
            consumedWood.Add(kindling.transform);
            kindling.gameObject.SetActive(false);
            PushComparisonAway(0.2f);
            ScatterSparks(0.16f);
        }

        private void ScheduleAshFromKindling()
        {
            if (HasKindlingAshWaitingOrPresent())
            {
                return;
            }

            pendingAshReadyTimes.Add(Time.time + ashAfterKindlingDelaySeconds);
        }

        private void UpdatePendingAsh()
        {
            for (var i = pendingAshReadyTimes.Count - 1; i >= 0; i--)
            {
                if (Time.time < pendingAshReadyTimes[i])
                {
                    continue;
                }

                ashLevel = Mathf.Max(ashLevel, KindlingAshLevel());
                pendingAshReadyTimes.RemoveAt(i);
            }
        }

        private bool HasKindlingAshWaitingOrPresent()
        {
            return pendingAshReadyTimes.Count > 0 || ashLevel >= FireTo01(ashSlowThreshold);
        }

        private float ClearedAshLevel()
        {
            return FireTo01(ashStirBoostThreshold);
        }

        private float KindlingAshLevel()
        {
            return Mathf.Clamp01(ClearedAshLevel() + FireTo01(ashPerKindling));
        }

        private void DropStone(int droppedBlockDirection)
        {
            if (phase == FirePhase.FireWork && droppedBlockDirection != 0)
            {
                stonePlaced = true;
                blockedWindDirection = droppedBlockDirection;
                var targetSlot = droppedBlockDirection < 0 ? leftWindBlockSlot : rightWindBlockSlot;
                stoneObject.transform.position = targetSlot == null
                    ? droppedBlockDirection < 0 ? leftStonePlacedPosition : rightStonePlacedPosition
                    : targetSlot.transform.position;
                SetStatus("The stone blocks the wind.");
                return;
            }

            if (stoneObject != null)
            {
                StartFallingDrop(stoneObject.transform);
            }
        }

        private void DropAshPoker()
        {
            ResetDraggedTransform();
            hasStirPointer = false;
        }

        private void DropWarmLight(bool droppedOnFinal)
        {
            if (phase == FirePhase.Stable && droppedOnFinal && !endingResolved)
            {
                endingResolved = true;
                finalThoughtText.text = finalAfterText;
                finalThoughtText.color = new Color(1f, 0.82f, 0.38f, 1f);
                warmLightObject.SetActive(false);
                endingText.text = endingCopy;
                SetPhase(FirePhase.Ending);
                return;
            }

            ResetWarmLight();
        }

        private void StirAsh(Vector3 pointer)
        {
            var distance = hasStirPointer ? Vector2.Distance(pointer, lastStirPointer) : 0f;
            var speed = Time.deltaTime > 0f ? distance / Time.deltaTime : 0f;
            var careful = !hasStirPointer || speed <= carefulStirMaxSpeed;
            var tooFast = hasStirPointer && speed >= roughStirSparkSpeed;
            var clearRate = tooFast ? ashStirClearPerSecond * 0.45f : careful ? ashStirClearPerSecond : ashStirClearPerSecond * 0.72f;
            lastStirPointer = pointer;
            hasStirPointer = true;
            ShowAshWake(pointer, tooFast);

            if (phase == FirePhase.OpeningSuccess)
            {
                openingStirProgress = Mathf.Clamp01(openingStirProgress + Time.deltaTime * 0.32f);
                ashLevel = Mathf.Lerp(0.65f, 0.28f, openingStirProgress);
                return;
            }

            if (phase != FirePhase.FireWork)
            {
                return;
            }

            var hadKindlingAsh = ashLevel >= FireTo01(ashSlowThreshold);
            ashLevel = hadKindlingAsh ? ClearedAshLevel() : Mathf.Max(0f, ashLevel - FireTo01(clearRate) * Time.deltaTime);
            if (hadKindlingAsh || ashLevel > FireTo01(ashStirBoostThreshold))
            {
                fireLevel = Mathf.Clamp01(fireLevel + FireTo01(ashStirFireBoostPerSecond) * Time.deltaTime);
                TryCompleteFireWork();
            }

            if (tooFast)
            {
                ScatterSparks(0.12f);
            }
        }

        private void ShowAshWake(Vector3 pointer, bool rough)
        {
            ashWakeAlpha = Mathf.Max(ashWakeAlpha, ashWakeMaxAlpha * (rough ? 0.72f : 1f));
            if (ashStirWake == null)
            {
                return;
            }

            ashStirWake.transform.position = new Vector3(pointer.x, pointer.y, ashStirWake.transform.position.z);
            ashStirWake.transform.localScale = rough
                ? new Vector3(0.62f, 0.18f, 1f)
                : new Vector3(0.44f, 0.13f, 1f);
        }

        private void ResolveEchoObserve()
        {
            for (var i = 0; i < thoughts.Count; i++)
            {
                var line = thoughts[i];
                line.observed = true;
                line.pressure = Mathf.Min(line.pressure, 0.28f);
                if (line.text != null)
                {
                    line.text.color = new Color(0.17f, 0.15f, 0.14f, 0.32f);
                }
            }

            activeObserveThought = null;
            observeHeldFor = 0f;
            SetStatus("The words fade back.");
            SetPhase(FirePhase.FireWork);
        }

        private void DistractFromFire()
        {
            thoughtPressure = Mathf.Clamp01(thoughtPressure + 0.22f);
            fireLevel = Mathf.Max(0.02f, fireLevel - FireTo01(distractionFireDrop));
            tremble += 0.08f;
            ScatterSparks(0.28f);
        }

        private void PushComparisonAway(float amount)
        {
            thoughtPressure = Mathf.Clamp01(thoughtPressure - amount);
        }

        private void SetPhase(FirePhase nextPhase)
        {
            phase = nextPhase;
            phaseTimer = 0f;

            if (phase == FirePhase.ComparisonFall)
            {
                if (ashPokerObject != null)
                {
                    ashPokerObject.SetActive(true);
                }

                if (stoneObject != null)
                {
                    stoneObject.SetActive(true);
                }

                if (companionText != null)
                {
                    companionText.text = string.Empty;
                }

                nextThoughtIndex = 0;
                thoughtPressure = 0.82f;
            }
            else if (phase == FirePhase.FireWork)
            {
                fireLevel = FireTo01(fireWorkStartFire);
                ashLevel = 0.2f;
                if (ashPokerObject != null)
                {
                    ashPokerObject.SetActive(true);
                    ResetTransformToHome(ashPokerObject.transform);
                }

                if (stoneObject != null)
                {
                    stoneObject.SetActive(true);
                    ResetTransformToHome(stoneObject.transform);
                }

                ResetBatch();
                thoughtPressure = 0.48f;
                ScheduleFirstWind();
            }
            else if (phase == FirePhase.Stable)
            {
                fireLevel = 1f;
                ashLevel = Mathf.Min(ashLevel, 0.2f);
                if (warmLightObject != null)
                {
                    warmLightObject.SetActive(true);
                    ResetTransformToHome(warmLightObject.transform);
                }

                if (finalThoughtText != null)
                {
                    finalThoughtText.color = new Color(0.28f, 0.22f, 0.18f, 1f);
                }
            }
        }

        private void UpdateDryingSlots()
        {
            for (var i = 0; i < dryingStates.Count; i++)
            {
                var state = dryingStates[i];
                if (state.wood == null || state.wood.dryKindling)
                {
                    continue;
                }

                var fire = Fire01ToValue(fireLevel);
                var drySeconds = ResolveRoastDrySeconds(
                    fire,
                    roastHotAtFire,
                    roastWarmAtFire,
                    roastEmberBelowFire,
                    roastDrySecondsHot,
                    roastDrySecondsWarm,
                    roastDrySecondsLow,
                    roastDrySecondsEmber);
                state.progress = Mathf.Clamp01(state.progress + Time.deltaTime / drySeconds);
                TintKindling(state.wood);
                if (state.progress >= 1f)
                {
                    state.wood.dryKindling = true;
                    dragHomePositions[state.wood.transform] = state.wood.transform.position;
                    TintKindling(state.wood);
                }
            }
        }

        private void UpdateResupply()
        {
            if (HasAvailableWood() || HasWoodInDryingSlots() || fireLevel >= FireTo01(completionFire))
            {
                resupplyTimer = 0f;
                SetRendererAlpha(woodcutterCapsule, 0f);
                return;
            }

            resupplyTimer += Time.deltaTime;
            SetRendererAlpha(woodcutterCapsule, Mathf.Clamp01(resupplyTimer / resupplyDelay));
            if (resupplyTimer >= resupplyDelay)
            {
                batchIndex++;
                ResetBatch();
                resupplyTimer = 0f;
            }
        }

        private void ResetBatch()
        {
            consumedWood.Clear();
            BuildDryingStates();
            for (var i = 0; i < kindlingObjects.Count; i++)
            {
                if (kindlingObjects[i] == null)
                {
                    continue;
                }

                var interactable = kindlingObjects[i].GetComponent<FireLevelInteractable>();
                if (interactable != null)
                {
                    interactable.dryKindling = i < 2;
                    TintKindling(interactable);
                }

                kindlingObjects[i].SetActive(true);
                ResetKindlingToSpawn(kindlingObjects[i].transform);
            }

            SetRendererAlpha(woodcutterCapsule, 0f);
        }

        private void BuildDryingStates()
        {
            dryingStates.Clear();
            for (var i = 0; i < dryingSlots.Count; i++)
            {
                if (dryingSlots[i] == null)
                {
                    continue;
                }

                dryingStates.Add(new DryingSlotState
                {
                    slot = dryingSlots[i],
                    wood = null,
                    progress = 0f
                });
            }
        }

        private DryingSlotState DryingSlotAt(Vector3 pointer)
        {
            for (var i = 0; i < dryingStates.Count; i++)
            {
                var state = dryingStates[i];
                if (state.slot != null && state.wood == null && DraggedToolTouches(state.slot, pointer))
                {
                    return state;
                }
            }

            return null;
        }

        private void PlaceWetWoodInDryingSlot(FireLevelInteractable wood, DryingSlotState slot)
        {
            slot.wood = wood;
            slot.progress = 0f;
            if (slot.slot != null)
            {
                var slotPosition = slot.slot.transform.position;
                wood.transform.position = new Vector3(slotPosition.x, slotPosition.y, wood.transform.position.z);
                dragHomePositions[wood.transform] = wood.transform.position;
            }

            TintKindling(wood);
        }

        private void ClearDryingSlotFor(FireLevelInteractable wood)
        {
            for (var i = 0; i < dryingStates.Count; i++)
            {
                if (dryingStates[i].wood == wood)
                {
                    dryingStates[i].wood = null;
                    dryingStates[i].progress = 0f;
                    return;
                }
            }
        }

        private bool IsWetWoodDrying(FireLevelInteractable wood)
        {
            for (var i = 0; i < dryingStates.Count; i++)
            {
                if (dryingStates[i].wood == wood && !wood.dryKindling)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAvailableWood()
        {
            for (var i = 0; i < kindlingObjects.Count; i++)
            {
                if (kindlingObjects[i] != null && kindlingObjects[i].activeSelf)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasWoodInDryingSlots()
        {
            for (var i = 0; i < dryingStates.Count; i++)
            {
                if (dryingStates[i].wood != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void TintKindling(FireLevelInteractable wood)
        {
            if (wood == null)
            {
                return;
            }

            var renderer = wood.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            if (useLevel2SceneArt)
            {
                var kindlingSprite = wood.dryKindling ? sceneArtDryKindling : sceneArtWetKindling;
                SetRendererSprite(renderer, kindlingSprite);
            }

            if (wood.dryKindling)
            {
                renderer.color = Color.white;
                return;
            }

            renderer.color = Color.white;
        }

        private float DryingProgressFor(FireLevelInteractable wood)
        {
            for (var i = 0; i < dryingStates.Count; i++)
            {
                if (dryingStates[i].wood == wood)
                {
                    return dryingStates[i].progress;
                }
            }

            return 0f;
        }

        private bool IsWindWarning()
        {
            return phase == FirePhase.FireWork && Time.time >= nextWindAt - windWarningSeconds && Time.time < nextWindAt;
        }

        private bool IsWindActive()
        {
            if (phase != FirePhase.FireWork)
            {
                return false;
            }

            if (Time.time >= nextWindAt && Time.time <= windEndsAt)
            {
                return true;
            }

            if (Time.time > nextWindAt && windEndsAt < nextWindAt)
            {
                windEndsAt = Time.time + windDurationSeconds;
                return true;
            }

            return false;
        }

        private bool IsCurrentWindBlocked()
        {
            return stonePlaced && blockedWindDirection == windDirection;
        }

        private void ScheduleFirstWind()
        {
            PickNextWindDirection();
            nextWindAt = Time.time + windFirstDelay;
            windEndsAt = nextWindAt + windDurationSeconds;
        }

        private void ScheduleNextWind()
        {
            PickNextWindDirection();
            nextWindAt = Time.time + UnityEngine.Random.Range(windIntervalMin, windIntervalMax);
            windEndsAt = nextWindAt + windDurationSeconds;
        }

        private void PickNextWindDirection()
        {
            windDirection = UnityEngine.Random.value < 0.5f ? -1 : 1;
        }

        private void UpdateComparisonPressure()
        {
            if (phase == FirePhase.FireWork)
            {
                thoughtPressure = Mathf.MoveTowards(thoughtPressure, 0.42f, Time.deltaTime * 0.025f);
            }
            else if (phase == FirePhase.Stable || phase == FirePhase.Ending)
            {
                thoughtPressure = Mathf.MoveTowards(thoughtPressure, 0f, Time.deltaTime * 0.35f);
            }

            tremble = Mathf.MoveTowards(tremble, 0f, Time.deltaTime * 0.18f);
        }

        private void UpdateLittleCaveheart()
        {
            if (littleCaveheartRenderer == null)
            {
                return;
            }

            var shake = Mathf.Sin(Time.time * 28f) * tremble * 0.045f;
            littleCaveheartRenderer.transform.position = littleCaveheartBasePosition + new Vector3(shake, 0f, 0f);

            var targetSprite = SelectLittleCaveheartSprite();

            if (targetSprite != null)
            {
                littleCaveheartRenderer.sprite = targetSprite;
            }

            littleCaveheartRenderer.flipX = phase == FirePhase.ComparisonFall
                && phaseTimer > hunterEnterDelay
                && phaseTimer < comparisonFirstLineDelay;

            littleCaveheartRenderer.color = Color.white;
        }

        private void LoadLittleCaveheartSprites()
        {
            cavemanShameFireTiny = LoadCavemanStateSprite("Caveman_Shame_FireTiny");
            cavemanAnxiousFireLow = LoadCavemanStateSprite("Caveman_Anxious_FireLow");
            cavemanFocusingFireRecovering = LoadCavemanStateSprite("Caveman_Focusing_FireRecovering");
            cavemanEngagedFireStable = LoadCavemanStateSprite("Caveman_Engaged_FireStable");
            cavemanConfidentFireStrong = LoadCavemanStateSprite("Caveman_Confident_FireStrong");
            cavemanRelievedEnding = LoadCavemanStateSprite("Caveman_Relieved_Ending");
            cavemanStartledComparison = LoadCavemanStateSprite("Caveman_Startled_Comparison");
        }

        private Sprite LoadCavemanStateSprite(string assetName)
        {
            return Resources.Load<Sprite>(level2CavemanSpriteFolder + assetName);
        }

        private Sprite LoadSceneArtSprite(string assetName)
        {
            return Resources.Load<Sprite>(level2SceneArtFolder + assetName);
        }

        public static string SelectSceneArtBackgroundAssetName(bool preferNoFireBackground)
        {
            return preferNoFireBackground ? "CaveBackground_NoFire" : "CaveBackground_WarmInterior";
        }

        public static string SelectKindlingSceneArtAssetName(bool dryKindling)
        {
            return dryKindling ? "Firewood_Dry_Log" : "Firewood_Wet_Log";
        }

        private void ApplySceneArtSprites()
        {
            if (!useLevel2SceneArt)
            {
                return;
            }

            var backgroundAssetName = SelectSceneArtBackgroundAssetName(preferNoFireBackground);
            var backgroundSprite = LoadSceneArtSprite(backgroundAssetName);
            if (backgroundSprite == null && backgroundAssetName != "CaveBackground_WarmInterior")
            {
                backgroundSprite = LoadSceneArtSprite("CaveBackground_WarmInterior");
            }

            SetRendererSprite(caveWall, backgroundSprite);
            if (caveFloor != null)
            {
                caveFloor.enabled = false;
            }

            SetRendererSprite(firePitRenderer, LoadSceneArtSprite("Campfire_StoneRing"));
            SetRendererSprite(fireGlow, LoadSceneArtSprite("Campfire_Glow"));
            SetRendererSprite(emberGlow, LoadSceneArtSprite("Campfire_Embers"));
            SetRendererSprite(ashLayer, LoadSceneArtSprite("Campfire_AshLayer"));
            SetRendererSprite(flameOuter, LoadSceneArtSprite("Campfire_FlameOuter"));
            SetRendererSprite(flameInner, LoadSceneArtSprite("Campfire_FlameInner"));

            sceneArtWindLeft = LoadSceneArtSprite("CaveWind_Left");
            sceneArtWindRight = LoadSceneArtSprite("CaveWind_Right");
            SetRendererSprite(windVisual, windDirection < 0 ? sceneArtWindLeft : sceneArtWindRight);

            SetRendererSprite(lionShadow, LoadSceneArtSprite("LionShadow_WallProjection"));
            SetRendererSprite(hunterCapsule, LoadSceneArtSprite("HunterSilhouette_Event"));
            SetRendererSprite(dryingSpotsArtRenderer, LoadSceneArtSprite("DryingSpots_TwoWarmStones"));
            sceneArtDryKindling = LoadSceneArtSprite(SelectKindlingSceneArtAssetName(true));
            sceneArtWetKindling = LoadSceneArtSprite(SelectKindlingSceneArtAssetName(false));

            if (ashPokerObject != null)
            {
                SetRendererSprite(ashPokerObject.GetComponentInChildren<SpriteRenderer>(true), LoadSceneArtSprite("AshPoker"));
            }

            if (stoneObject != null)
            {
                SetRendererSprite(stoneObject.GetComponent<SpriteRenderer>(), LoadSceneArtSprite("WindBlockStone"));
            }

            for (var i = 0; i < kindlingObjects.Count; i++)
            {
                if (kindlingObjects[i] == null)
                {
                    continue;
                }

                TintKindling(kindlingObjects[i].GetComponent<FireLevelInteractable>());
            }
        }

        private static void SetRendererSprite(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer != null && sprite != null)
            {
                if (renderer.sprite == sprite)
                {
                    return;
                }

                renderer.sprite = sprite;
            }
        }

        private Sprite SelectLittleCaveheartSprite()
        {
            if (phase == FirePhase.Ending)
            {
                return cavemanRelievedEnding ?? cavemanConfidentFireStrong;
            }

            if (phase == FirePhase.Stable)
            {
                return cavemanConfidentFireStrong ?? cavemanEngagedFireStable;
            }

            if (phase == FirePhase.ComparisonFall)
            {
                return cavemanStartledComparison ?? cavemanShameFireTiny;
            }

            if (phase == FirePhase.ObserveEcho)
            {
                return cavemanShameFireTiny ?? cavemanAnxiousFireLow;
            }

            var fire = Fire01ToValue(fireLevel);
            if (fire < 24f)
            {
                return cavemanShameFireTiny ?? cavemanAnxiousFireLow;
            }

            if (fire < 42f)
            {
                return cavemanAnxiousFireLow ?? cavemanShameFireTiny;
            }

            if (fire < 62f)
            {
                return cavemanFocusingFireRecovering ?? cavemanEngagedFireStable;
            }

            if (fire < 84f)
            {
                return cavemanEngagedFireStable ?? cavemanFocusingFireRecovering;
            }

            return cavemanConfidentFireStrong ?? cavemanEngagedFireStable;
        }

        private void ApplyPhaseVisuals(bool force)
        {
            var warm = Mathf.Clamp01(fireLevel);
            if (useLevel2SceneArt)
            {
                SetRendererColor(caveWall, Color.Lerp(new Color(0.48f, 0.43f, 0.36f, 1f), Color.white, warm * 0.45f));
            }
            else
            {
                SetRendererColor(caveWall, Color.Lerp(new Color(0.1f, 0.095f, 0.09f, 1f), new Color(0.28f, 0.19f, 0.13f, 1f), warm * 0.75f));
                SetRendererColor(caveFloor, Color.Lerp(new Color(0.12f, 0.1f, 0.09f, 1f), new Color(0.31f, 0.22f, 0.15f, 1f), warm * 0.7f));
            }
            SetRendererColor(fireGlow, new Color(1f, 0.44f, 0.1f, Mathf.Lerp(0.06f, 0.52f, warm)));
            if (fireGlow != null)
            {
                fireGlow.transform.localScale = useLevel2SceneArt
                    ? Vector3.one
                    : Vector3.one * Mathf.Lerp(0.7f, 1.45f, warm);
            }

            var ashPulse = phase == FirePhase.FireWork && ashLevel >= FireTo01(ashSlowThreshold)
                ? Mathf.Sin(Time.time * 7.5f) * 0.07f
                : 0f;
            SetRendererColor(emberGlow, new Color(1f, 0.12f, 0.04f, Mathf.Lerp(0.16f, 0.82f, Mathf.Max(warm, 1f - ashLevel))));
            SetRendererColor(ashLayer, new Color(0.55f, 0.52f, 0.48f, Mathf.Clamp01(Mathf.Lerp(0.08f, 0.9f, ashLevel) + ashPulse)));

            var flameScale = Mathf.Lerp(0.42f, 1.16f, warm) + Mathf.Sin(Time.time * 8f) * 0.04f;
            if (flameOuter != null)
            {
                flameOuter.transform.localScale = useLevel2SceneArt ? Vector3.one : Vector3.one * flameScale;
            }

            if (flameInner != null)
            {
                flameInner.transform.localScale = useLevel2SceneArt
                    ? Vector3.one
                    : Vector3.one * Mathf.Lerp(0.5f, 1.2f, warm);
            }

            ApplyDynamicFireVisibility();

            var windBlocked = IsCurrentWindBlocked();
            var windTargetAlpha = windBlocked ? 0f : IsWindActive() ? windActiveAlpha : IsWindWarning() ? windWarningAlpha : 0f;
            var windFadeRate = windBlocked && windTargetAlpha < windVisualAlpha
                ? windFadeSpeed * 0.35f
                : windFadeSpeed;
            windVisualAlpha = force ? windTargetAlpha : Mathf.MoveTowards(windVisualAlpha, windTargetAlpha, windFadeRate * Time.deltaTime);
            UpdateWindVisual(windVisualAlpha, windBlocked);
            UpdateWindBlockSlotVisuals(windVisualAlpha, windBlocked);

            SetRendererColor(lionShadow, new Color(0.02f, 0.018f, 0.016f, phase == FirePhase.ComparisonFall ? Mathf.Clamp01(phaseTimer / 2.5f) * 0.78f : Mathf.Lerp(0.68f, 0.14f, 1f - thoughtPressure)));
            if (lionShadow != null)
            {
                lionShadow.transform.localScale = useLevel2SceneArt
                    ? Vector3.one
                    : Vector3.one * Mathf.Lerp(1.1f, 0.42f, phase == FirePhase.Stable || phase == FirePhase.Ending ? 1f : 1f - thoughtPressure);
            }

            if (companionText != null)
            {
                companionText.color = new Color(0.88f, 0.9f, 0.93f, phase == FirePhase.ComparisonFall ? Mathf.Clamp01(phaseTimer - 0.5f) : Mathf.Lerp(0.42f, 0f, 1f - thoughtPressure));
            }

            if (endingText != null)
            {
                endingText.color = new Color(0.97f, 0.86f, 0.64f, phase == FirePhase.Ending ? Mathf.Clamp01(phaseTimer * 0.75f) : 0f);
            }

            for (var i = 0; i < thoughts.Count; i++)
            {
                var line = thoughts[i];
                if (line.text == null)
                {
                    continue;
                }

                var visible = i < nextThoughtIndex || phase == FirePhase.ObserveEcho || phase == FirePhase.FireWork;
                if (line.observeTarget && (phase == FirePhase.FireWork || phase == FirePhase.Stable || phase == FirePhase.Ending))
                {
                    visible = true;
                }

                var pressure = Mathf.Clamp01(Mathf.Max(line.pressure, thoughtPressure));
                var target = Vector3.Lerp(line.farPosition, line.nearPosition, pressure);
                if (line.observed)
                {
                    target += new Vector3(Mathf.Sin(Time.time * 2.4f) * 0.035f, Mathf.Sin(Time.time * 1.7f) * 0.02f, 0f);
                }

                line.text.transform.position = Vector3.Lerp(line.text.transform.position, target, force ? 1f : Time.deltaTime * 3.5f);
                line.text.transform.localScale = Vector3.one * Mathf.Lerp(0.86f, line.observeTarget ? 1.04f : 1.22f, pressure);
                line.text.color = line.observed
                    ? new Color(0.17f, 0.15f, 0.14f, visible ? 0.32f : 0f)
                    : new Color(0.17f, 0.15f, 0.14f, visible ? Mathf.Lerp(0.32f, 0.98f, pressure) : 0f);

                line.pressure = Mathf.MoveTowards(line.pressure, phase == FirePhase.ComparisonFall ? line.pressure : 0f, Time.deltaTime * 0.08f);
            }

            for (var i = 0; i < dashedEchoSegments.Count; i++)
            {
                SetRendererColor(dashedEchoSegments[i], new Color(0.92f, 0.82f, 0.65f, 0f));
            }

            for (var i = 0; i < wallArtRenderers.Count; i++)
            {
                SetRendererColor(wallArtRenderers[i], new Color(0.8f, 0.55f, 0.25f, phase == FirePhase.Stable || phase == FirePhase.Ending ? 0.52f : warm * 0.12f));
            }

            UpdateStatusForPhase();
            UpdateSparks();
            UpdateAshWake();
        }

        private void UpdateStatusForPhase()
        {
            if (phase == FirePhase.OpeningSuccess)
            {
                SetStatus("Drag the ash poker slowly through the grey ash.");
            }
            else if (phase == FirePhase.ObserveEcho)
            {
                SetStatus(observeHeldFor > 0f ? string.Empty : "Hold on the loudest words.");
            }
            else if (phase == FirePhase.FireWork)
            {
                SetStatus(string.Empty);
            }
            else if (phase == FirePhase.Stable)
            {
                SetStatus("Drag warmth from the fire to the sentence.");
            }
        }

        private void ApplyDynamicFireVisibility()
        {
            var alpha = backgroundIncludesBakedFire ? 0f : 1f;
            SetRendererAlpha(firePitRenderer, alpha);
            if (backgroundIncludesBakedFire)
            {
                SetRendererAlpha(fireGlow, 0f);
                SetRendererAlpha(emberGlow, 0f);
                SetRendererAlpha(ashLayer, 0f);
                SetRendererAlpha(flameOuter, 0f);
                SetRendererAlpha(flameInner, 0f);
            }
        }

        private void ScatterSparks(float strength)
        {
            for (var i = 0; i < sparkRenderers.Count; i++)
            {
                if (sparkRenderers[i] == null)
                {
                    continue;
                }

                var angle = (i / Mathf.Max(1f, sparkRenderers.Count - 1f)) * Mathf.PI * 2f + Time.time;
                sparkRenderers[i].transform.position = new Vector3(Mathf.Cos(angle) * strength, -1.23f + Mathf.Sin(angle * 1.7f) * strength, -0.75f);
                sparkRenderers[i].color = new Color(1f, 1f, 1f, Mathf.Clamp01(strength * 2.2f));
            }
        }

        private void UpdateSparks()
        {
            for (var i = 0; i < sparkRenderers.Count; i++)
            {
                if (sparkRenderers[i] == null)
                {
                    continue;
                }

                var current = sparkRenderers[i].color;
                current.a = Mathf.MoveTowards(current.a, 0f, Time.deltaTime * 2.4f);
                sparkRenderers[i].color = current;
            }
        }

        private void UpdateAshWake()
        {
            ashWakeAlpha = Mathf.MoveTowards(ashWakeAlpha, 0f, Time.deltaTime * ashWakeFadePerSecond);
            SetRendererColor(ashStirWake, new Color(0.9f, 0.78f, 0.62f, ashWakeAlpha));
        }

        private string LabelFor(FireLevelInteractable target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            if (target.kind == FireLevelInteractableKind.Kindling)
            {
                return target.dryKindling ? "Dry Kindling" : "Wet Kindling";
            }

            if (target.kind == FireLevelInteractableKind.AshPoker && (phase == FirePhase.OpeningSuccess || phase == FirePhase.FireWork))
            {
                return "Ash Poker";
            }

            if (target.kind == FireLevelInteractableKind.Stone)
            {
                return "Place Stone";
            }

            if (target.kind == FireLevelInteractableKind.WarmLight && phase == FirePhase.Stable)
            {
                return "Warm Flame";
            }

            if (target.kind == FireLevelInteractableKind.Thought && phase == FirePhase.ObserveEcho && target.id == "taunt")
            {
                return "Observe";
            }

            return string.Empty;
        }

        private void UpdateActionLabel(FireLevelInteractable target)
        {
            if (actionLabel == null || sceneCamera == null)
            {
                return;
            }

            var label = LabelFor(target);
            actionLabel.text = label;
            actionLabel.anchor = TextAnchor.UpperLeft;
            actionLabel.alignment = TextAlignment.Left;
            var labelRenderer = actionLabel.GetComponent<MeshRenderer>();
            if (labelRenderer != null)
            {
                labelRenderer.sortingOrder = 10000;
            }

            actionLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            var screenPosition = ClampHoverLabelScreenPosition(
                Input.mousePosition,
                new Vector2(Screen.width, Screen.height),
                hoverLabelSize,
                hoverLabelOffset);
            var labelDepth = actionLabel.transform.position.z - sceneCamera.transform.position.z;
            actionLabel.transform.position = sceneCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, labelDepth));
        }

        public static Vector2 ClampHoverLabelScreenPosition(Vector2 pointerScreenPosition, Vector2 screenSize, Vector2 labelSize, Vector2 offset)
        {
            var screenPosition = pointerScreenPosition + offset;
            screenPosition.x = Mathf.Clamp(screenPosition.x, 8f, Mathf.Max(8f, screenSize.x - labelSize.x - 8f));
            screenPosition.y = Mathf.Clamp(screenPosition.y, labelSize.y + 8f, Mathf.Max(labelSize.y + 8f, screenSize.y - 8f));
            return screenPosition;
        }

        private bool IsPointerInteractive(FireLevelInteractable target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.kind == FireLevelInteractableKind.AshPoker)
            {
                return phase == FirePhase.OpeningSuccess || phase == FirePhase.FireWork;
            }

            if (target.kind == FireLevelInteractableKind.Kindling)
            {
                return !IsWetWoodDrying(target);
            }

            if (target.kind == FireLevelInteractableKind.Stone)
            {
                return true;
            }

            if (target.kind == FireLevelInteractableKind.WarmLight)
            {
                return phase == FirePhase.Stable;
            }

            if (target.kind == FireLevelInteractableKind.Thought)
            {
                return phase == FirePhase.ObserveEcho && target.id == "taunt";
            }

            return target.kind == FireLevelInteractableKind.LionShadow && phase == FirePhase.FireWork;
        }

        private FireCursorMode CursorModeForHover(FireLevelInteractable target)
        {
            if (!IsPointerInteractive(target))
            {
                return FireCursorMode.Default;
            }

            return FireCursorMode.Hand;
        }

        private void CacheHome(Transform target)
        {
            if (target != null)
            {
                dragHomePositions[target] = target.position;
            }
        }

        private void CacheKindlingHomes()
        {
            for (var i = 0; i < kindlingObjects.Count; i++)
            {
                if (kindlingObjects[i] != null)
                {
                    var target = kindlingObjects[i].transform;
                    kindlingSpawnPositions[target] = target.position;
                    CacheHome(target);
                }
            }
        }

        private void ResetKindlingToSpawn(Transform target)
        {
            if (target == null)
            {
                return;
            }

            CancelFallingDrop(target, true);
            if (!kindlingSpawnPositions.TryGetValue(target, out var spawnPosition))
            {
                spawnPosition = target.position;
                kindlingSpawnPositions[target] = spawnPosition;
            }

            target.position = spawnPosition;
            dragHomePositions[target] = spawnPosition;
        }

        private void SetKindlingActive(bool active)
        {
            for (var i = 0; i < kindlingObjects.Count; i++)
            {
                if (kindlingObjects[i] != null)
                {
                    kindlingObjects[i].SetActive(active);
                }
            }
        }

        private void ResetDraggedTransform()
        {
            ResetTransformToHome(draggedTransform);
        }

        private void StartFallingDrop(Transform target)
        {
            releaseKeepsLiftedVisual = false;
            if (target == null)
            {
                return;
            }

            if (!dragHomePositions.TryGetValue(target, out var home))
            {
                RestoreDragVisuals(target);
                return;
            }

            CancelFallingDrop(target, false);
            var position = target.position;
            var fixedX = position.x;
            var targetY = home.y;
            if (position.y <= targetY + dropFallSnapDistance)
            {
                target.position = new Vector3(fixedX, targetY, position.z);
                return;
            }

            fallingDrops.Add(new FallingDropState
            {
                target = target,
                fixedX = fixedX,
                targetY = targetY,
                z = position.z,
                velocity = dropFallInitialSpeed
            });
            releaseKeepsLiftedVisual = true;
        }

        private void UpdateFallingDrops()
        {
            for (var i = fallingDrops.Count - 1; i >= 0; i--)
            {
                var falling = fallingDrops[i];
                if (falling.target == null || !falling.target.gameObject.activeInHierarchy)
                {
                    if (falling.target != null)
                    {
                        RestoreDragVisuals(falling.target);
                    }

                    fallingDrops.RemoveAt(i);
                    continue;
                }

                falling.velocity += dropFallAcceleration * Time.deltaTime;
                var position = falling.target.position;
                var nextY = position.y - falling.velocity * Time.deltaTime;
                if (nextY <= falling.targetY + dropFallSnapDistance)
                {
                    falling.target.position = new Vector3(falling.fixedX, falling.targetY, falling.z);
                    RestoreDragVisuals(falling.target);
                    fallingDrops.RemoveAt(i);
                    continue;
                }

                falling.target.position = new Vector3(falling.fixedX, nextY, falling.z);
            }
        }

        private void LiftDragVisuals(Transform target)
        {
            if (target == null || liftedRendererStates.ContainsKey(target))
            {
                return;
            }

            var renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            var states = new List<RendererSortingState>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                states.Add(new RendererSortingState
                {
                    renderer = renderer,
                    sortingOrder = renderer.sortingOrder
                });
                renderer.sortingOrder += dragLiftSortingBoost;
            }

            liftedRendererStates[target] = states;
        }

        private void RestoreDragVisuals(Transform target)
        {
            if (target == null || !liftedRendererStates.TryGetValue(target, out var states))
            {
                return;
            }

            for (var i = 0; i < states.Count; i++)
            {
                if (states[i].renderer != null)
                {
                    states[i].renderer.sortingOrder = states[i].sortingOrder;
                }
            }

            liftedRendererStates.Remove(target);
        }

        private void CancelFallingDrop(Transform target, bool restoreVisuals)
        {
            if (target == null)
            {
                return;
            }

            for (var i = fallingDrops.Count - 1; i >= 0; i--)
            {
                if (fallingDrops[i].target == target)
                {
                    fallingDrops.RemoveAt(i);
                }
            }

            if (restoreVisuals)
            {
                RestoreDragVisuals(target);
            }
        }

        private void ResetTransformToHome(Transform target)
        {
            if (target == null)
            {
                return;
            }

            CancelFallingDrop(target, true);
            if (dragHomePositions.TryGetValue(target, out var home))
            {
                target.position = home;
            }
        }

        private void ResetStone()
        {
            if (!stonePlaced && stoneObject != null)
            {
                ResetTransformToHome(stoneObject.transform);
            }
        }

        private void ResetWarmLight()
        {
            if (warmLightObject != null)
            {
                ResetTransformToHome(warmLightObject.transform);
            }
        }

        private Vector3 PointerWorld()
        {
            var mouse = Input.mousePosition;
            mouse.z = -sceneCamera.transform.position.z;
            return sceneCamera.ScreenToWorldPoint(mouse);
        }

        private FireLevelInteractable TopInteractableAt(Vector3 pointer)
        {
            FireLevelInteractable best = null;
            var bestVisualPriority = float.NegativeInfinity;

            for (var i = 0; i < kindlingObjects.Count; i++)
            {
                if (kindlingObjects[i] == null || !kindlingObjects[i].activeInHierarchy)
                {
                    continue;
                }

                var interactable = kindlingObjects[i].GetComponent<FireLevelInteractable>();
                if (CanUsePointerTarget(interactable, pointer))
                {
                    ConsiderPointerTarget(interactable, sceneCamera, ref best, ref bestVisualPriority);
                }
            }

            var hits = Physics2D.OverlapPointAll(pointer);
            for (var i = 0; i < hits.Length; i++)
            {
                var interactable = hits[i].GetComponent<FireLevelInteractable>();
                if (!CanUsePointerTarget(interactable, pointer))
                {
                    continue;
                }

                ConsiderPointerTarget(interactable, sceneCamera, ref best, ref bestVisualPriority);
            }

            return best;
        }

        private static bool CanUsePointerTarget(FireLevelInteractable interactable, Vector3 pointer)
        {
            if (interactable == null)
            {
                return false;
            }

            if (interactable.kind == FireLevelInteractableKind.Ash
                || interactable.kind == FireLevelInteractableKind.WindStonesTarget
                || interactable.kind == FireLevelInteractableKind.StoneShelterTarget
                || interactable.kind == FireLevelInteractableKind.DryingSlot)
            {
                return false;
            }

            return interactable.ContainsPointer(pointer);
        }

        private static void ConsiderPointerTarget(FireLevelInteractable interactable, Camera camera, ref FireLevelInteractable best, ref float bestVisualPriority)
        {
            var priority = VisualPointerPriority(interactable, camera);
            if (priority > bestVisualPriority)
            {
                bestVisualPriority = priority;
                best = interactable;
            }
        }

        private static float VisualPointerPriority(FireLevelInteractable interactable, Camera camera)
        {
            if (interactable == null)
            {
                return float.NegativeInfinity;
            }

            var cameraPosition = camera == null ? Vector3.back * 10f : camera.transform.position;
            var renderers = interactable.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0)
            {
                return -Vector3.Distance(cameraPosition, interactable.transform.position);
            }

            var best = float.NegativeInfinity;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var layerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
                var distanceScore = -Vector3.Distance(cameraPosition, renderer.bounds.center);
                var priority = layerValue * 1000000f + renderer.sortingOrder * 1000f + distanceScore;
                best = Mathf.Max(best, priority);
            }

            return best;
        }

        private bool IsPointerInside(FireLevelInteractable target, Vector3 pointer)
        {
            return target != null && target.ContainsPointer(pointer);
        }

        private int WindBlockDirectionAt(Vector3 pointer)
        {
            if (DraggedToolTouches(leftWindBlockSlot, pointer))
            {
                return -1;
            }

            if (DraggedToolTouches(rightWindBlockSlot, pointer))
            {
                return 1;
            }

            return 0;
        }

        private void UpdateWindBlockSlotVisuals(float windAlpha, bool windBlocked)
        {
            SetWindBlockSlotColor(leftWindBlockSlot, -1, windAlpha, windBlocked);
            SetWindBlockSlotColor(rightWindBlockSlot, 1, windAlpha, windBlocked);
        }

        private void UpdateWindVisual(float windAlpha, bool windBlocked)
        {
            if (windVisual == null)
            {
                return;
            }

            if (!windVisualHomeCached)
            {
                CacheWindVisualHome();
            }

            var targetActive = IsWindActive() || IsWindWarning();
            windVisualDirection = ResolveWindVisualDirection(windVisualDirection, windDirection, windAlpha, targetActive);
            var sprite = windVisualDirection < 0 ? sceneArtWindLeft : sceneArtWindRight;
            var fallbackSprite = windVisualDirection < 0 ? sceneArtWindRight : sceneArtWindLeft;
            if (useLevel2SceneArt)
            {
                SetRendererSprite(windVisual, sprite ?? fallbackSprite);
                windVisual.flipX = sprite == null && fallbackSprite != null;
            }
            else
            {
                windVisual.flipX = windVisualDirection < 0;
            }

            SetRendererColor(windVisual, new Color(0.52f, 0.78f, 1f, windAlpha));

            var time = Time.time;
            var position = windVisualBasePosition;
            position.x = Mathf.Abs(windVisualBasePosition.x) * windVisualDirection;
            position.y += Mathf.Sin(time * 3.6f) * windBobAmplitude * Mathf.Clamp01(windAlpha);
            windVisual.transform.position = position;

            var pulse = Mathf.Sin(time * 4.2f);
            var stretch = 1f + pulse * windStretchAmplitude * Mathf.Clamp01(windAlpha);
            var squash = 1f - pulse * windStretchAmplitude * 0.35f * Mathf.Clamp01(windAlpha);
            windVisual.transform.localScale = new Vector3(windVisualBaseScale.x * stretch, windVisualBaseScale.y * squash, windVisualBaseScale.z);
        }

        public static int ResolveWindVisualDirection(int currentVisualDirection, int gameplayDirection, float currentAlpha, bool targetActive)
        {
            if (targetActive || currentAlpha <= 0.01f)
            {
                return gameplayDirection < 0 ? -1 : 1;
            }

            return currentVisualDirection < 0 ? -1 : 1;
        }

        public static float ResolveRoastDrySeconds(
            float fire,
            float hotAtFire,
            float warmAtFire,
            float emberMaxFire,
            float hotSeconds,
            float warmSeconds,
            float lowSeconds,
            float emberSeconds)
        {
            if (fire >= hotAtFire)
            {
                return Mathf.Max(0.1f, hotSeconds);
            }

            if (fire >= warmAtFire)
            {
                return Mathf.Max(0.1f, warmSeconds);
            }

            if (fire >= emberMaxFire)
            {
                return Mathf.Max(0.1f, lowSeconds);
            }

            return Mathf.Max(0.1f, emberSeconds);
        }

        private void CacheWindVisualHome()
        {
            if (windVisual == null)
            {
                return;
            }

            windVisualBasePosition = windVisual.transform.position;
            windVisualBasePosition.x = Mathf.Abs(windVisualBasePosition.x);
            windVisualBaseScale = windVisual.transform.localScale;
            windVisualHomeCached = true;
        }

        private void SetWindBlockSlotColor(FireLevelInteractable slot, int direction, float windAlpha, bool windBlocked)
        {
            if (slot == null)
            {
                return;
            }

            var draggingStone = dragKind == DragKind.Stone && draggedTransform != null;
            var blockedSide = windBlocked && blockedWindDirection == direction;
            var pulse = (Mathf.Sin(Time.time * 7f) + 1f) * 0.5f;
            var activeSide = draggingStone && windDirection == direction && (IsWindActive() || IsWindWarning());
            var alpha = draggingStone ? Mathf.Lerp(0.18f, activeSide ? 0.72f : 0.42f, pulse) : 0f;
            var color = blockedSide
                ? new Color(0.62f, 1f, 0.76f, draggingStone ? 0.62f : 0f)
                : new Color(0.42f, 0.9f, 1f, alpha);

            var renderer = slot.GetComponent<SpriteRenderer>();
            SetRendererColor(renderer, color);
            if (renderer != null)
            {
                renderer.sortingOrder = 60;
                if (!windSlotBaseScales.TryGetValue(renderer.transform, out var baseScale))
                {
                    baseScale = renderer.transform.localScale;
                    windSlotBaseScales[renderer.transform] = baseScale;
                }

                var scale = 1f + (draggingStone ? pulse * 0.08f : 0f);
                renderer.transform.localScale = baseScale * scale;
            }
        }

        private bool DraggedToolTouches(FireLevelInteractable target, Vector3 pointer)
        {
            if (target == null)
            {
                return false;
            }

            if (IsPointerInside(target, pointer))
            {
                return true;
            }

            if (draggedTransform == null)
            {
                return false;
            }

            var targetCollider = target.GetComponent<Collider2D>();
            var draggedCollider = draggedTransform.GetComponent<Collider2D>();
            if (targetCollider == null || draggedCollider == null)
            {
                return IsPointerInside(target, pointer);
            }

            if (BoundsOverlapXY(draggedCollider.bounds, targetCollider.bounds, placementTouchPadding))
            {
                return true;
            }

            var distance = draggedCollider.Distance(targetCollider);
            return distance.isOverlapped || distance.distance <= placementTouchPadding;
        }

        public static bool BoundsOverlapXY(Bounds first, Bounds second, float padding)
        {
            padding = Mathf.Max(0f, padding);
            return first.min.x <= second.max.x + padding
                && first.max.x >= second.min.x - padding
                && first.min.y <= second.max.y + padding
                && first.max.y >= second.min.y - padding;
        }

        private static bool IsPointInsideCollider(Collider2D collider, Vector3 pointer)
        {
            return collider != null && collider.OverlapPoint(pointer);
        }

        private static float FireTo01(float value)
        {
            return Mathf.Clamp01(value / 100f);
        }

        private static float Fire01ToValue(float value)
        {
            return Mathf.Clamp01(value) * 100f;
        }

        private void ApplyTypography()
        {
            CaveheartTypography.ApplyTo(caveTitle);
            CaveheartTypography.ApplyTo(statusText);
            CaveheartTypography.ApplyTo(actionLabel);
            CaveheartTypography.ApplyTo(companionText);
            CaveheartTypography.ApplyTo(finalThoughtText);
            CaveheartTypography.ApplyTo(endingText);
            for (var i = 0; i < thoughts.Count; i++)
            {
                CaveheartTypography.ApplyTo(thoughts[i].text);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private static void SetRendererColor(SpriteRenderer renderer, Color color)
        {
            if (renderer != null)
            {
                renderer.color = new Color(1f, 1f, 1f, color.a);
            }
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            var color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private void SetCursorMode(FireCursorMode mode)
        {
            if (currentCursorMode == mode)
            {
                return;
            }

            currentCursorMode = mode;
            if (cursorVisuals != null)
            {
                cursorVisuals.SetCursorMode(ToExamCursorMode(mode));
                return;
            }

        }

        private static ExamCursorMode ToExamCursorMode(FireCursorMode mode)
        {
            switch (mode)
            {
                case FireCursorMode.Hand:
                    return ExamCursorMode.Hand;
                case FireCursorMode.Grab:
                    return ExamCursorMode.Grab;
                default:
                    return ExamCursorMode.Default;
            }
        }

    }
}
