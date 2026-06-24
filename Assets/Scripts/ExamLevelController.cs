using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyLittleCaveheart
{
    public sealed class ExamLevelController : MonoBehaviour
    {
        private const int TimeoutMinutes = 7;
        private const string ExamStateSpriteRoot = "Sprites/Caveheart/Level3School/States1254/";
        private const string OverwhelmedSpriteName = "caveheart_level2_state_overwhelmed_v1_1254";
        private const string SittingSpriteName = "caveheart_level2_state_sitting_v1_1254";
        private const string ObservingSpriteName = "caveheart_level2_state_observing_v1_1254";
        private const string GroundedSpriteName = "caveheart_level2_state_grounded_v1_1254";
        private const string PressureMusicPath = "Audio/Caveheart/level3_cave_exam_pressure";
        private const string GroundedMusicPath = "Audio/Caveheart/level3_stone_marimba_grounded";
        private const string ObserveHoldStartSfxPath = "Audio/Caveheart/action_observe_breath";
        private const string WrongActionSfxPath = "Audio/Caveheart/feedback_negative_3";
        private const string FragmentPickUpSfxPath = "Audio/Caveheart/action_click";
        private static readonly int ToneTintId = Shader.PropertyToID("_ToneTint");
        private static readonly int ToneTintStrengthId = Shader.PropertyToID("_ToneTintStrength");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");

        [SerializeField] private int grounding;
        [SerializeField] private int trust = 1;
        [SerializeField] private int stress = 6;
        [SerializeField] private int usedMinutes;

        [SerializeField] private bool bodyConnected;
        [SerializeField] private bool realityConnected;
        [SerializeField] private bool futureConnected;
        [SerializeField] private bool pencilPlaced;

        [Header("Scene References")]
        [SerializeField] private ExamThoughtText thoughtText;
        [SerializeField] private ExamThoughtText[] thoughtSlots;
        [SerializeField] private ExamFeedbackPresenter feedback;
        [SerializeField] private TextMesh paperQuestionText;
        [SerializeField] private Collider2D paperDropCollider;
        [SerializeField] private TextMesh thoughtCursorHintText;
        [SerializeField] private ExamCursorVisuals cursorVisuals;
        [SerializeField] private ExamThoughtPointerInteraction thoughtPointerInteraction;
        [SerializeField] private ExamEndScreenPresenter endScreen;
        [SerializeField] private Transform fragmentRoot;
        [SerializeField] private Transform fallingSkyArea;
        [SerializeField] private Transform littleCaveheart;
        [SerializeField] private SpriteRenderer littleCaveheartRenderer;
        [SerializeField] private SpriteRenderer deskRenderer;
        [SerializeField] private SpriteRenderer paperRenderer;

        [Header("Inspector-Tunable Thought Layouts")]
        [SerializeField] private ThoughtStage[] stageLayouts;

        [Header("Music")]
        [SerializeField, Range(0f, 1f)] private float pressureMusicVolume = 0.32f;
        [SerializeField, Range(0f, 1f)] private float groundedMusicVolume = 0.34f;
        [SerializeField, Min(0.1f)] private float musicCrossfadeSeconds = 2f;
        [SerializeField] private AudioClip pressureMusicClip;
        [SerializeField] private AudioClip groundedMusicClip;

        [Header("Interaction Sounds")]
        [SerializeField, Range(0f, 1f)] private float interactionSfxVolume = 0.78f;
        [SerializeField] private AudioClip observeHoldStartSfx;
        [SerializeField] private AudioClip wrongActionSfx;
        [SerializeField] private AudioClip fragmentPickUpSfx;

        [Header("Level 3 Full-Screen Tone")]
        [SerializeField] private Material examHandDrawnMaterial;
        [SerializeField] private Color anxiousToneTint = new Color(0.52f, 0.78f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float anxiousToneTintStrength = 0.55f;
        [SerializeField, Range(0f, 2f)] private float anxiousSaturation = 0.45f;
        [SerializeField] private Color groundedToneTint = new Color(1f, 0.96f, 0.84f, 1f);
        [SerializeField, Range(0f, 1f)] private float groundedToneTintStrength = 0.12f;
        [SerializeField, Range(0f, 2f)] private float groundedSaturation = 1.05f;
        [SerializeField, Min(0.1f)] private float toneTransitionSeconds = 4f;

        private enum LittleCaveheartVisualState
        {
            Overwhelmed,
            Sitting,
            Observing,
            Grounded
        }

        private enum ExamLevelEnding
        {
            None,
            Victory,
            Failure
        }

        private readonly List<ExamThoughtText> thoughtViews = new List<ExamThoughtText>();
        private readonly List<ActiveThought> activeThoughts = new List<ActiveThought>();
        private readonly List<ExamFragmentText> activeFragments = new List<ExamFragmentText>();
        private readonly Dictionary<ExamFragmentText, ActiveThought> fragmentOwners = new Dictionary<ExamFragmentText, ActiveThought>();
        private readonly Dictionary<string, string> futureReframes = new Dictionary<string, string>();
        private Sprite overwhelmedStateSprite;
        private Sprite sittingStateSprite;
        private Sprite observingStateSprite;
        private Sprite groundedStateSprite;
        private LittleCaveheartVisualState currentLittleCaveheartVisualState = (LittleCaveheartVisualState)(-1);
        private AudioSource pressureMusicSource;
        private AudioSource groundedMusicSource;
        private AudioSource interactionSfxSource;
        private float toneProgress;
        private int currentStage;
        private bool ended;
        private ExamLevelEnding ending;
        private bool observeVisualActive;
        private float observeVisualGraceUntil;
        private Vector3 fallingSkyStart;
        private Vector3 cameraStart;

        public bool HasInteractableThoughts => !ended && FindFirstInteractableThought() != null;

        [Serializable]
        private sealed class FragmentSpec
        {
            [TextArea] public string text;
            public ExamFragmentKind kind;
            public ExamFragmentText fragmentComponent;
            public TextMesh textComponent;

            // Stores the text and correct destination for one draggable fragment.
            public FragmentSpec(string text, ExamFragmentKind kind)
            {
                this.text = text;
                this.kind = kind;
            }
        }

        [Serializable]
        private sealed class ThoughtSpec
        {
            [TextArea] public string thought;
            public ExamThoughtText thoughtComponent;
            public TextMesh textComponent;
            public FragmentSpec[] fragments;

            // Defines one catastrophic sentence, its text object, and the smaller sentences inside it.
            public ThoughtSpec(string thought, params FragmentSpec[] fragments)
            {
                this.thought = thought;
                this.fragments = fragments;
            }
        }

        [Serializable]
        private sealed class ThoughtStage
        {
            [TextArea] public string completeText;
            public ThoughtSpec[] thoughts;

            // Groups one or more simultaneous thoughts that must all be split and sorted.
            public ThoughtStage(string completeText, params ThoughtSpec[] thoughts)
            {
                this.completeText = completeText;
                this.thoughts = thoughts;
            }
        }

        private struct ExamStateSnapshot
        {
            public readonly int grounding;
            public readonly int trust;
            public readonly int stress;
            public readonly int usedMinutes;
            public readonly LittleCaveheartVisualState caveheartState;

            // Captures one before/after gameplay state so interaction logs stay consistent.
            public ExamStateSnapshot(
                int grounding,
                int trust,
                int stress,
                int usedMinutes,
                LittleCaveheartVisualState caveheartState)
            {
                this.grounding = grounding;
                this.trust = trust;
                this.stress = stress;
                this.usedMinutes = usedMinutes;
                this.caveheartState = caveheartState;
            }
        }

        private sealed class ActiveThought
        {
            public readonly ThoughtSpec spec;
            public readonly ExamThoughtText view;
            public readonly List<ExamFragmentText> fragments = new List<ExamFragmentText>();
            public readonly int thoughtIndex;
            public readonly int thoughtCount;
            public bool split;
            public bool resolved;
            public int eraseCount;

            // Tracks the runtime state owned by one currently visible thought sentence.
            public ActiveThought(ThoughtSpec spec, ExamThoughtText view, int thoughtIndex, int thoughtCount)
            {
                this.spec = spec;
                this.view = view;
                this.thoughtIndex = thoughtIndex;
                this.thoughtCount = thoughtCount;
            }
        }

        // Keeps the Inspector layout populated when the script reloads in the editor.
        private void OnValidate()
        {
            EnsureDefaultStageData();
        }

        // Builds default stage data and binds scene objects before play starts.
        private void Awake()
        {
            EnsureDefaultStageData();
            BindExistingScene();
            ApplyTypographyToSceneText();
            BindAuthoredSceneRenderers();
            LoadLittleCaveheartStateSprites();
            ApplyLittleCaveheartSprite(true);
            SetupLevelMusic();
            SetupInteractionSfx();
            CacheLevelToneStartValues();
        }

        // Resets the hidden state and starts the first exam thought.
        private void Start()
        {
            BeginLevel();
        }

        // Keeps the whitebox visual pressure and adaptive music synced to current hidden stats.
        private void Update()
        {
            ApplyVisualFeedback();
            UpdateLevelMusic(false);
            UpdateLevelTone(false);
        }

        // Restores the authored cold grade when the level controller is destroyed in Play Mode.
        private void OnDestroy()
        {
            RestoreLevelToneStartValues();
        }

        // Resolves dragging the eraser over a thought: it briefly helps, then makes the thought rebound.
        public bool TryEraseThought(ExamThoughtText targetThought)
        {
            var thought = FindActiveThought(targetThought, true);
            if (ended || thought == null)
            {
                return false;
            }

            var before = CaptureState();
            PlayWrongActionFeedback();
            AddTime(1);
            if (ended)
            {
                LogStateChange("erase thought", before);
                return true;
            }

            if (thought.eraseCount == 0)
            {
                stress = Mathf.Clamp(stress - 1, 0, 8);
                stress = Mathf.Clamp(stress + 2, 0, 8);
                feedback.Show("It goes quiet for a second.\nThen the thought comes back louder.");
            }
            else
            {
                stress = Mathf.Clamp(stress + 2, 0, 8);
                feedback.Show("The thought comes back larger.");
            }

            thought.eraseCount++;
            targetThought.FlickerAwayThenReturn();
            PublishHud();
            LogStateChange("erase thought", before);
            return true;
        }

        // Keeps legacy hold targets from breaking while still routing observation through active thoughts.
        public void ObserveActiveThought()
        {
            var thought = FindFirstInteractableThought();
            if (thought != null)
            {
                ObserveThought(thought);
            }
        }

        // Splits a specific catastrophe sentence into draggable body, reality, and future fragments.
        public void ObserveThought(ExamThoughtText targetThought)
        {
            var activeThought = FindActiveThought(targetThought, true);
            if (ended || activeThought == null)
            {
                return;
            }

            var before = CaptureState();
            activeThought.split = true;
            targetThought.MarkObserved();
            ShowThoughtCursorHint(false, 0f);
            cursorVisuals?.SetObserveHold(false, 0f);
            stress = Mathf.Clamp(stress - 1, 0, 8);
            AddTime(1);
            if (ended)
            {
                LogStateChange("observe thought", before);
                return;
            }

            feedback.Show("You stay with the thought.\nIt slows down enough to see what is inside.");
            observeVisualGraceUntil = Time.time + 0.65f;
            ApplyLittleCaveheartSprite(true);
            var sourcePosition = targetThought.transform.position;
            targetThought.gameObject.SetActive(false);
            SpawnFragments(activeThought, sourcePosition);
            PublishHud();
            LogStateChange("observe thought", before);
        }

        // Lets the pointer scanner show the observing state during the deliberate hold gesture.
        public void SetObserveVisualActive(bool active)
        {
            if (observeVisualActive == active)
            {
                return;
            }

            observeVisualActive = active;
            ApplyLittleCaveheartSprite(true);
        }

        // Evaluates a released fragment against the drop zone under it and applies mistake feedback.
        public void TryDropFragment(ExamFragmentText fragment)
        {
            if (ended || fragment == null || fragment.IsConnected)
            {
                return;
            }

            var before = CaptureState();
            var zone = FindDropZoneAt(fragment.transform.position);
            if (zone == null)
            {
                fragment.RegisterWrongDrop();
                LogStateChange($"drop {fragment.FragmentKind} outside", before);
                return;
            }

            if (zone.FragmentKind == fragment.FragmentKind)
            {
                zone.Flash(true);
                ConnectFragment(fragment, zone, before);
                return;
            }

            zone.Flash(false);
            PlayWrongActionFeedback();
            if (fragment.MistakeCount == 0)
            {
                feedback.Show("This does not stay there.\nMaybe it belongs somewhere else.");
            }
            else
            {
                stress = Mathf.Clamp(stress + 1, 0, 8);
                AddTime(1);
                feedback.Show("The thought starts spinning again.");
            }

            fragment.RegisterWrongDrop();
            PublishHud();
            LogStateChange($"drop {fragment.FragmentKind} into {zone.FragmentKind}", before);
        }

        // Resolves dropping the pencil on the paper, either as an early stress spike or level completion.
        public bool TryPlacePencil(ExamPencil pencil)
        {
            if (ended || pencil == null)
            {
                return false;
            }

            if (!IsOverPaper(pencil.transform.position))
            {
                return false;
            }

            var before = CaptureState();
            if (!CanPlacePencil())
            {
                PlayWrongActionFeedback();
                grounding = Mathf.Clamp(grounding + 1, 0, 8);
                stress = Mathf.Clamp(stress + 2, 0, 8);
                trust = Mathf.Clamp(trust - 1, 0, 8);
                AddTime(1);
                feedback.Show("He grips the pencil,\nbut the words still cover the page.");
                PublishHud();
                LogStateChange("place pencil too early", before);
                return false;
            }

            pencilPlaced = true;
            grounding = Mathf.Clamp(grounding + 2, 0, 8);
            trust = Mathf.Clamp(trust + 1, 0, 8);
            pencil.transform.position = new Vector3(-0.65f, -4.55f, -0.35f);
            CompleteLevel();
            LogStateChange("place pencil on paper", before);
            return true;
        }

        // Shows that clicking a thought only agitates it; staying with it is the useful input.
        public void RegisterThoughtClickAttempt(ExamThoughtText targetThought)
        {
            if (ended || FindActiveThought(targetThought, true) == null)
            {
                return;
            }

            var before = CaptureState();
            PlayWrongActionFeedback();
            feedback.Show("It shakes, but nothing changes.");
            targetThought.Jolt();
            LogStateChange("click thought", before);
        }

        // Keeps old callers working by applying click feedback to the first active thought.
        public void RegisterThoughtClickAttempt()
        {
            RegisterThoughtClickAttempt(FindFirstInteractableThought());
        }

        // Shows that pulling a thought around does not reveal what it contains.
        public void RegisterThoughtDragAttempt(ExamThoughtText targetThought)
        {
            if (ended || FindActiveThought(targetThought, true) == null)
            {
                return;
            }

            var before = CaptureState();
            PlayWrongActionFeedback();
            feedback.Show("It will not move.");
            targetThought.Jolt();
            LogStateChange("drag thought", before);
        }

        // Plays the soft breath cue when the player starts holding on an observable thought.
        public void PlayObserveHoldStart()
        {
            PlayInteractionSfx(observeHoldStartSfx);
        }

        // Plays the short click cue when the player picks up a draggable thought fragment.
        public void PlayFragmentPickUp()
        {
            PlayInteractionSfx(fragmentPickUpSfx);
        }

        // Plays the negative cue for actions that push against the current rule logic.
        public void PlayWrongActionFeedback()
        {
            PlayInteractionSfx(wrongActionSfx);
        }

        // Keeps old callers working by applying drag feedback to the first active thought.
        public void RegisterThoughtDragAttempt()
        {
            RegisterThoughtDragAttempt(FindFirstInteractableThought());
        }

        // Places a small hint beside the cursor while the player rests on a splittable thought.
        public void ShowThoughtCursorHint(bool visible, float progress01)
        {
            if (thoughtCursorHintText == null)
            {
                return;
            }

            thoughtCursorHintText.gameObject.SetActive(visible && HasInteractableThoughts && !ended);
            if (!thoughtCursorHintText.gameObject.activeSelf || Camera.main == null)
            {
                return;
            }

            var mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(Camera.main.transform.position.z);
            var world = Camera.main.ScreenToWorldPoint(mouse);
            thoughtCursorHintText.transform.position = new Vector3(world.x + 0.55f, world.y - 0.35f, -0.6f);
            thoughtCursorHintText.text = progress01 > 0.65f ? "stay with it..." : "stay with it";
        }

        // Finds the topmost active thought under a world-space cursor point.
        public ExamThoughtText FindThoughtAt(Vector3 pointerWorld)
        {
            if (ended)
            {
                return null;
            }

            var hits = Physics2D.OverlapPointAll(pointerWorld);
            for (var i = 0; i < hits.Length; i++)
            {
                var thought = hits[i].GetComponent<ExamThoughtText>();
                if (IsThoughtInteractable(thought))
                {
                    return thought;
                }
            }

            return null;
        }

        // Checks whether one thought can currently be clicked, dragged, erased, or held to split.
        public bool IsThoughtInteractable(ExamThoughtText targetThought)
        {
            return !ended && FindActiveThought(targetThought, true) != null;
        }

        // Finds the topmost unresolved fragment under a world-space cursor point.
        public ExamFragmentText FindFragmentAt(Vector3 pointerWorld)
        {
            if (ended)
            {
                return null;
            }

            var hits = Physics2D.OverlapPointAll(pointerWorld);
            for (var i = 0; i < hits.Length; i++)
            {
                var fragment = hits[i].GetComponent<ExamFragmentText>();
                if (fragment != null && activeFragments.Contains(fragment) && !fragment.IsConnected)
                {
                    return fragment;
                }
            }

            return null;
        }

        // Creates the default second-level staged thought groups only when the Inspector has none.
        private void EnsureDefaultStageData()
        {
            if (stageLayouts == null || stageLayouts.Length == 0)
            {
                stageLayouts = CreateDefaultStageLayouts();
            }

            BuildFutureReframes();
        }

        // Builds the editable default layout shown in the ExamLevelController Inspector.
        private static ThoughtStage[] CreateDefaultStageLayouts()
        {
            return new[]
            {
                new ThoughtStage(
                    "You can see two pieces now.\nA feeling, and a guess.",
                    new ThoughtSpec(
                        "I am going to fail.",
                        new FragmentSpec("I am scared.", ExamFragmentKind.Body),
                        new FragmentSpec("I might fail.", ExamFragmentKind.Future))),
                new ThoughtStage(
                    "Both fears are smaller when they are sorted.",
                    new ThoughtSpec(
                        "Everyone will be disappointed in me.",
                        new FragmentSpec("I am afraid of disappointing people.", ExamFragmentKind.Future),
                        new FragmentSpec("No one has seen my test yet.", ExamFragmentKind.Reality)),
                    new ThoughtSpec(
                        "I will not get into college.",
                        new FragmentSpec("I am imagining a far-away result.", ExamFragmentKind.Future),
                        new FragmentSpec("The result has not happened yet.", ExamFragmentKind.Reality))),
                new ThoughtStage(
                    "The sky is still loud.\nBut it is not the whole world.",
                    new ThoughtSpec(
                        "This exam will ruin everything.",
                        new FragmentSpec("My chest feels tight.", ExamFragmentKind.Body),
                        new FragmentSpec("This is one page on a desk.", ExamFragmentKind.Reality),
                        new FragmentSpec("I am afraid this will ruin everything.", ExamFragmentKind.Future))),
            };
        }

        // Rebuilds the future-thought rewrite table used after a future fragment is accepted.
        private void BuildFutureReframes()
        {
            futureReframes.Clear();
            futureReframes["I might fail."] = "I am afraid I might fail.";
            futureReframes["Everyone will be disappointed in me."] = "I am imagining people being disappointed.";
            futureReframes["I am afraid of disappointing people."] = "I am afraid of disappointing people.";
            futureReframes["I will not get into college."] = "I am imagining a far-away result.";
            futureReframes["I am imagining a far-away result."] = "This is a feared future, not a fact.";
            futureReframes["I am afraid this will ruin everything."] = "This is a fear, not a fact yet.";
            futureReframes["I know nothing."] = "Some things feel unclear right now.";
            futureReframes["Something bad is happening."] = "I am afraid something bad is happening.";
            futureReframes["My future is over."] = "I am imagining the future closing.";
        }

        // Finds authored scene objects by name so the scene stays editable without a builder script.
        private void BindExistingScene()
        {
            thoughtText = thoughtText == null
                ? FindObjectByName<ExamThoughtText>("Thought Position - Single Top") ?? FindObjectByName<ExamThoughtText>("FloatingThoughtText")
                : thoughtText;
            feedback = feedback == null ? FindObjectByName<ExamFeedbackPresenter>("ExamFeedback") : feedback;
            paperQuestionText = paperQuestionText == null ? FindObjectByName<TextMesh>("PaperQuestionText") : paperQuestionText;
            paperDropCollider = paperDropCollider == null ? FindSceneCollider("Paper") : paperDropCollider;
            thoughtCursorHintText = thoughtCursorHintText == null ? FindObjectByName<TextMesh>("ThoughtCursorHint") : thoughtCursorHintText;
            cursorVisuals = cursorVisuals == null ? GetOrAddComponent<ExamCursorVisuals>() : cursorVisuals;
            thoughtPointerInteraction = thoughtPointerInteraction == null ? GetOrAddComponent<ExamThoughtPointerInteraction>() : thoughtPointerInteraction;
            endScreen = endScreen == null ? FindObjectByName<ExamEndScreenPresenter>("ExamEndScreen") ?? GetOrAddComponent<ExamEndScreenPresenter>() : endScreen;
            fragmentRoot = fragmentRoot == null ? FindTransform("Fragments") : fragmentRoot;
            fallingSkyArea = fallingSkyArea == null ? FindTransform("FallingSkyArea") : fallingSkyArea;
            littleCaveheart = littleCaveheart == null ? FindTransform("LittleCaveheart") : littleCaveheart;

            littleCaveheartRenderer = ResolveLittleCaveheartRenderer();
            deskRenderer = deskRenderer == null ? FindSceneSpriteRenderer("Desk") : deskRenderer;
            paperRenderer = paperRenderer == null ? FindSceneSpriteRenderer("Paper") : paperRenderer;

            HideAllFragmentSlots();
            CollectThoughtViews();
            BackfillStageSceneReferences();
            for (var i = 0; i < thoughtViews.Count; i++)
            {
                thoughtViews[i].Bind(this);
                thoughtViews[i].gameObject.SetActive(false);
            }

            thoughtPointerInteraction?.Bind(this, cursorVisuals);
            if (thoughtCursorHintText != null)
            {
                CaveheartTypography.ApplyTo(thoughtCursorHintText);
                thoughtCursorHintText.gameObject.SetActive(false);
            }

            feedback?.BindExistingScene();
            endScreen?.HideImmediate();

            var erasers = FindObjectsOfType<ExamEraser>();
            for (var i = 0; i < erasers.Length; i++)
            {
                erasers[i].Bind(this);
            }

            var pencils = FindObjectsOfType<ExamPencil>();
            for (var i = 0; i < pencils.Length; i++)
            {
                pencils[i].Bind(this);
            }

            ConfigureDropZone("BodyDropArea", ExamFragmentKind.Body);
            ConfigureDropZone("RealityDropArea", ExamFragmentKind.Reality);
            ConfigureDropZone("FutureDropArea", ExamFragmentKind.Future);

            if (fallingSkyArea != null)
            {
                fallingSkyStart = fallingSkyArea.position;
            }

            if (Camera.main != null)
            {
                cameraStart = Camera.main.transform.position;
            }
        }

        // Collects the editor-authored thought slots and keeps their order stable for staged spawning.
        private void CollectThoughtViews()
        {
            thoughtViews.Clear();
            AddThoughtView(thoughtText);

            if (thoughtSlots != null)
            {
                for (var i = 0; i < thoughtSlots.Length; i++)
                {
                    AddThoughtView(thoughtSlots[i]);
                }
            }

            var found = FindObjectsOfType<ExamThoughtText>(true);
            Array.Sort(found, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            for (var i = 0; i < found.Length; i++)
            {
                AddThoughtView(found[i]);
            }
        }

        // Adds one thought view to the reusable pool without duplicating scene references.
        private void AddThoughtView(ExamThoughtText view)
        {
            if (view != null && !thoughtViews.Contains(view))
            {
                thoughtViews.Add(view);
            }
        }

        // Fills missing Inspector component references with the scene's default thought and fragment slots.
        private void BackfillStageSceneReferences()
        {
            if (stageLayouts == null)
            {
                return;
            }

            for (var stageIndex = 0; stageIndex < stageLayouts.Length; stageIndex++)
            {
                var stage = stageLayouts[stageIndex];
                if (stage?.thoughts == null)
                {
                    continue;
                }

                for (var thoughtIndex = 0; thoughtIndex < stage.thoughts.Length; thoughtIndex++)
                {
                    var thought = stage.thoughts[thoughtIndex];
                    if (thought == null)
                    {
                        continue;
                    }

                    var defaultThoughtView = GetOrCreateThoughtSlot(Mathf.Min(thoughtIndex, Mathf.Max(0, thoughtViews.Count - 1)));
                    thought.thoughtComponent = thought.thoughtComponent != null ? thought.thoughtComponent : defaultThoughtView;
                    thought.textComponent = thought.textComponent != null ? thought.textComponent : thought.thoughtComponent.TextComponent;
                    BackfillFragmentSceneReferences(thought);
                }
            }
        }

        // Fills missing fragment text/component references from the authored fragment pool in the scene.
        private void BackfillFragmentSceneReferences(ThoughtSpec thought)
        {
            if (thought.fragments == null)
            {
                return;
            }

            for (var fragmentIndex = 0; fragmentIndex < thought.fragments.Length; fragmentIndex++)
            {
                var fragment = thought.fragments[fragmentIndex];
                if (fragment == null)
                {
                    continue;
                }

                if (fragment.fragmentComponent != null && fragment.textComponent == null)
                {
                    fragment.textComponent = fragment.fragmentComponent.TextComponent;
                }
            }
        }

        // Gives every authored TextMesh the same font material refresh that the HUD text already receives.
        private void ApplyTypographyToSceneText()
        {
            var labels = FindObjectsOfType<TextMesh>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                CaveheartTypography.ApplyTo(labels[i]);
            }
        }

        // Binds renderers already authored in the scene without replacing their sprite, color, scale, or order.
        private void BindAuthoredSceneRenderers()
        {
            HideLegacyLittleCaveheartPlaceholder();
            littleCaveheartRenderer = ResolveLittleCaveheartRenderer();
            deskRenderer = deskRenderer == null ? FindSceneSpriteRenderer("Desk") : deskRenderer;
            paperRenderer = paperRenderer == null ? FindSceneSpriteRenderer("Paper") : paperRenderer;
        }

        // Initializes the material from the Inspector-authored anxious tone so the scene owns Level 3's start grade.
        private void CacheLevelToneStartValues()
        {
            if (examHandDrawnMaterial == null)
            {
                return;
            }

            toneProgress = 0f;
            UpdateLevelTone(true);
        }

        // Slowly restores warmth and saturation after the player reaches enough grounding.
        private void UpdateLevelTone(bool instant)
        {
            if (examHandDrawnMaterial == null)
            {
                return;
            }

            var target = grounding >= 5 ? 1f : 0f;
            toneProgress = instant
                ? target
                : Mathf.MoveTowards(toneProgress, target, Time.deltaTime / Mathf.Max(0.1f, toneTransitionSeconds));

            var tint = Color.Lerp(anxiousToneTint, groundedToneTint, toneProgress);
            var tintStrength = Mathf.Lerp(anxiousToneTintStrength, groundedToneTintStrength, toneProgress);
            var saturation = Mathf.Lerp(anxiousSaturation, groundedSaturation, toneProgress);
            examHandDrawnMaterial.SetColor(ToneTintId, tint);
            examHandDrawnMaterial.SetFloat(ToneTintStrengthId, tintStrength);
            examHandDrawnMaterial.SetFloat(SaturationId, saturation);
        }

        // Puts the Level 3 material back to the Inspector-authored anxious grade so runtime warming does not linger.
        private void RestoreLevelToneStartValues()
        {
            if (examHandDrawnMaterial == null)
            {
                return;
            }

            examHandDrawnMaterial.SetColor(ToneTintId, anxiousToneTint);
            examHandDrawnMaterial.SetFloat(ToneTintStrengthId, anxiousToneTintStrength);
            examHandDrawnMaterial.SetFloat(SaturationId, anxiousSaturation);
        }

        // Loads both Level 3 music clips, creates local loop sources, and silences any carried-over Level 1 music.
        private void SetupLevelMusic()
        {
            StopPersistentCaveheartMusic();
            LoadLevelMusicClips();
            pressureMusicSource = CreateMusicSource("Level 3 Pressure Music", pressureMusicClip);
            groundedMusicSource = CreateMusicSource("Level 3 Grounded Music", groundedMusicClip);
            UpdateLevelMusic(true);
        }

        // Stops the persistent Level 1 background source so the exam scene has one musical layer.
        private static void StopPersistentCaveheartMusic()
        {
            var legacyMusic = FindObjectOfType<CaveheartBackgroundMusic>();
            if (legacyMusic == null)
            {
                return;
            }

            var legacySource = legacyMusic.GetComponent<AudioSource>();
            if (legacySource != null)
            {
                legacySource.Stop();
            }
        }

        // Fills empty Inspector clip fields from Resources so the copied mp3 files work without manual binding.
        private void LoadLevelMusicClips()
        {
            pressureMusicClip = pressureMusicClip == null ? Resources.Load<AudioClip>(PressureMusicPath) : pressureMusicClip;
            groundedMusicClip = groundedMusicClip == null ? Resources.Load<AudioClip>(GroundedMusicPath) : groundedMusicClip;
        }

        // Creates one non-spatial looping source for a background music stem and starts it muted if needed.
        private AudioSource CreateMusicSource(string sourceName, AudioClip clip)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;

            if (clip != null)
            {
                source.Play();
            }

            return source;
        }

        // Crossfades from nervous exam music to grounded music once the player reaches enough grounding.
        private void UpdateLevelMusic(bool instant)
        {
            var groundedActive = grounding >= 5;
            var pressureTarget = groundedActive ? 0f : pressureMusicVolume;
            var groundedTarget = groundedActive ? groundedMusicVolume : 0f;
            ApplyMusicVolume(pressureMusicSource, pressureTarget, instant);
            ApplyMusicVolume(groundedMusicSource, groundedTarget, instant);
        }

        // Loads the Level 3 one-shot interaction sounds and creates a local non-spatial SFX source.
        private void SetupInteractionSfx()
        {
            LoadInteractionSfxClips();
            interactionSfxSource = CreateSfxSource("Level 3 Interaction SFX");
        }

        // Fills empty SFX fields from Resources so the chosen audio files work without scene rebinding.
        private void LoadInteractionSfxClips()
        {
            observeHoldStartSfx = observeHoldStartSfx == null ? Resources.Load<AudioClip>(ObserveHoldStartSfxPath) : observeHoldStartSfx;
            wrongActionSfx = wrongActionSfx == null ? Resources.Load<AudioClip>(WrongActionSfxPath) : wrongActionSfx;
            fragmentPickUpSfx = fragmentPickUpSfx == null ? Resources.Load<AudioClip>(FragmentPickUpSfxPath) : fragmentPickUpSfx;
            PrepareAudioClip(observeHoldStartSfx);
            PrepareAudioClip(wrongActionSfx);
            PrepareAudioClip(fragmentPickUpSfx);
        }

        // Creates one AudioSource for short UI-like sounds so they do not interfere with background music.
        private AudioSource CreateSfxSource(string sourceName)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = interactionSfxVolume;
            return source;
        }

        // Plays a selected Level 3 one-shot clip if it loaded successfully.
        private void PlayInteractionSfx(AudioClip clip)
        {
            if (interactionSfxSource == null || clip == null)
            {
                return;
            }

            interactionSfxSource.PlayOneShot(clip, interactionSfxVolume);
        }

        // Requests audio data up front to reduce the first-interaction delay for short feedback sounds.
        private static void PrepareAudioClip(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }
        }

        // Moves one AudioSource toward its target volume without assuming both music clips loaded.
        private void ApplyMusicVolume(AudioSource source, float targetVolume, bool instant)
        {
            if (source == null)
            {
                return;
            }

            if (instant)
            {
                source.volume = targetVolume;
                return;
            }

            var maxDelta = Time.deltaTime / Mathf.Max(0.1f, musicCrossfadeSeconds);
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, maxDelta);
        }

        // Initializes the hidden stats, clears outcome flags, and starts the first stage.
        private void BeginLevel()
        {
            grounding = 0;
            trust = 1;
            stress = 6;
            usedMinutes = 0;
            bodyConnected = false;
            realityConnected = false;
            futureConnected = false;
            pencilPlaced = false;
            currentStage = 0;
            ended = false;
            ending = ExamLevelEnding.None;
            observeVisualActive = false;
            observeVisualGraceUntil = 0f;
            cursorVisuals?.SetObserveHold(false, 0f);
            endScreen?.HideImmediate();

            if (paperQuestionText != null)
            {
                paperQuestionText.gameObject.SetActive(false);
            }

            HideAllFragmentSlots();
            feedback.SetTitle("");
            feedback.SetTitleVisible(false);
            feedback.Show("");
            ApplyLittleCaveheartSprite(true);
            UpdateLevelMusic(true);
            toneProgress = 0f;
            UpdateLevelTone(true);
            SpawnStage(0);
            PublishHud();
        }

        // Shows every thought in the requested stage and clears fragments from the previous one.
        private void SpawnStage(int stageIndex)
        {
            currentStage = stageIndex;
            ClearFragments();
            activeThoughts.Clear();
            HideThoughtViews();

            var stage = stageLayouts[stageIndex];
            for (var i = 0; i < stage.thoughts.Length; i++)
            {
                var thoughtView = ResolveThoughtView(stage.thoughts[i], i);
                thoughtView.gameObject.SetActive(true);
                thoughtView.transform.localScale = Vector3.one;
                thoughtView.UseTextMesh(stage.thoughts[i].textComponent);
                thoughtView.SetStandardPresentation();
                thoughtView.SetText(ResolveThoughtText(stage.thoughts[i]));
                thoughtView.SetPressure(stress);
                activeThoughts.Add(new ActiveThought(stage.thoughts[i], thoughtView, i, stage.thoughts.Length));
            }
        }

        // Turns off every reusable thought view before a new stage claims the slots it needs.
        private void HideThoughtViews()
        {
            for (var i = 0; i < thoughtViews.Count; i++)
            {
                if (thoughtViews[i] != null)
                {
                    thoughtViews[i].MarkObserved();
                    thoughtViews[i].gameObject.SetActive(false);
                }
            }
        }

        // Reuses an editor-authored thought slot, creating a fallback only if the scene is missing one.
        private ExamThoughtText GetOrCreateThoughtSlot(int index)
        {
            while (thoughtViews.Count <= index)
            {
                var thoughtObject = new GameObject($"Thought Position Runtime {thoughtViews.Count + 1}");
                thoughtObject.AddComponent<TextMesh>();
                thoughtObject.AddComponent<MeshRenderer>();
                thoughtViews.Add(thoughtObject.AddComponent<ExamThoughtText>());
            }

            return thoughtViews[index];
        }

        // Chooses the thought component assigned in the Inspector, falling back to the indexed scene pool.
        private ExamThoughtText ResolveThoughtView(ThoughtSpec thought, int fallbackIndex)
        {
            if (thought.thoughtComponent != null)
            {
                return thought.thoughtComponent;
            }

            var thoughtView = GetOrCreateThoughtSlot(fallbackIndex);
            thought.thoughtComponent = thoughtView;
            thought.textComponent = thought.textComponent != null ? thought.textComponent : thoughtView.TextComponent;
            return thoughtView;
        }

        // Chooses the split-fragment object assigned in the Inspector, falling back to the fragment pool.
        private GameObject ResolveFragmentSlot(FragmentSpec fragment, int fallbackIndex)
        {
            if (fragment.fragmentComponent != null)
            {
                return fragment.fragmentComponent.gameObject;
            }

            var slot = GetOrCreateFragmentSlot(fallbackIndex);
            fragment.fragmentComponent = slot.GetComponent<ExamFragmentText>();
            fragment.textComponent = fragment.textComponent != null ? fragment.textComponent : slot.GetComponent<TextMesh>();
            return slot;
        }

        // Uses the assigned TextMesh as the source of truth so editor line breaks survive Play mode.
        private static string ResolveThoughtText(ThoughtSpec thought)
        {
            if (thought.textComponent != null && !string.IsNullOrEmpty(thought.textComponent.text))
            {
                thought.thought = thought.textComponent.text;
                return thought.textComponent.text;
            }

            return thought.thought ?? string.Empty;
        }

        // Uses the assigned split TextMesh as the source of truth so per-fragment edits survive Play mode.
        private static string ResolveFragmentText(FragmentSpec fragment)
        {
            if (fragment.textComponent != null && !string.IsNullOrEmpty(fragment.textComponent.text))
            {
                fragment.text = fragment.textComponent.text;
                return fragment.textComponent.text;
            }

            return fragment.text ?? string.Empty;
        }

        // Creates the draggable fragments revealed by holding on a specific thought.
        private void SpawnFragments(ActiveThought thought, Vector3 sourcePosition)
        {
            for (var i = 0; i < thought.spec.fragments.Length; i++)
            {
                var slotIndex = activeFragments.Count;
                var fragmentSpec = thought.spec.fragments[i];
                var obj = ResolveFragmentSlot(fragmentSpec, slotIndex);
                var targetPosition = obj.transform.position;
                obj.SetActive(true);
                obj.transform.SetParent(fragmentRoot, false);
                obj.transform.position = sourcePosition;
                obj.transform.localScale = Vector3.one;

                var text = fragmentSpec.textComponent != null ? fragmentSpec.textComponent : obj.GetComponent<TextMesh>();
                if (text == null)
                {
                    text = obj.AddComponent<TextMesh>();
                }

                var fragmentText = ResolveFragmentText(fragmentSpec);
                text.text = fragmentText;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.characterSize = 0.115f;
                text.fontSize = 26;
                CaveheartTypography.ApplyTo(text);
                var textRenderer = obj.GetComponent<MeshRenderer>();
                if (textRenderer != null)
                {
                    textRenderer.sortingOrder = 80;
                }

                SetFragmentCardVisible(obj.transform, false);

                var collider = obj.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = obj.AddComponent<BoxCollider2D>();
                }

                collider.isTrigger = true;

                var fragment = fragmentSpec.fragmentComponent != null ? fragmentSpec.fragmentComponent : obj.GetComponent<ExamFragmentText>();
                if (fragment == null)
                {
                    fragment = obj.AddComponent<ExamFragmentText>();
                }

                fragmentSpec.fragmentComponent = fragment;
                fragmentSpec.textComponent = text;
                fragment.UseTextMesh(text);
                fragment.Configure(this, fragmentSpec.kind, fragmentText, sourcePosition, targetPosition, slotIndex);
                activeFragments.Add(fragment);
                thought.fragments.Add(fragment);
                fragmentOwners[fragment] = thought;
            }

            HideUnusedFragmentSlots(activeFragments.Count);
        }

        // Reuses an authored fragment slot when it exists, otherwise creates a fallback runtime slot.
        private GameObject GetOrCreateFragmentSlot(int index)
        {
            if (fragmentRoot != null && index < fragmentRoot.childCount)
            {
                var child = fragmentRoot.GetChild(index);
                if (IsFragmentPositionName(child.name))
                {
                    return child.gameObject;
                }
            }

            return new GameObject($"Split Position Runtime {index + 1}");
        }

        // Hides editor-authored slots that are not part of the current active fragment set.
        private void HideUnusedFragmentSlots(int _)
        {
            if (fragmentRoot == null)
            {
                return;
            }

            for (var i = 0; i < fragmentRoot.childCount; i++)
            {
                var child = fragmentRoot.GetChild(i);
                if (IsFragmentPositionName(child.name) && !IsActiveFragmentSlot(child.gameObject))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // Hides every editor-authored fragment slot so placeholder split text never appears before play.
        private void HideAllFragmentSlots()
        {
            HideUnusedFragmentSlots(0);
        }

        // Checks whether one editor slot is currently owned by a spawned split fragment.
        private bool IsActiveFragmentSlot(GameObject slotObject)
        {
            for (var i = 0; i < activeFragments.Count; i++)
            {
                if (activeFragments[i] != null && activeFragments[i].gameObject == slotObject)
                {
                    return true;
                }
            }

            return false;
        }

        // Hides the old fragment backing card so split text renders without a colored rectangle.
        private static void SetFragmentCardVisible(Transform parent, bool visible)
        {
            var card = parent.Find("Fragment Card")?.gameObject;
            if (card == null)
            {
                for (var i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child.name.StartsWith("Transparent Backing", StringComparison.Ordinal))
                    {
                        card = child.gameObject;
                        break;
                    }
                }
            }

            card?.SetActive(visible);
        }

        // Applies the stat effect for a correct drop and anchors the fragment in the accepted zone.
        private void ConnectFragment(ExamFragmentText fragment, ExamDropZone zone, ExamStateSnapshot before)
        {
            var connectedInZone = 0;
            for (var i = 0; i < activeFragments.Count; i++)
            {
                if (activeFragments[i].IsConnected && activeFragments[i].FragmentKind == fragment.FragmentKind)
                {
                    connectedInZone++;
                }
            }

            var targetPosition = zone.transform.position + new Vector3(-1.1f, 0.48f - connectedInZone * 0.38f, -0.45f);
            var transformed = fragment.FragmentKind == ExamFragmentKind.Future && futureReframes.TryGetValue(fragment.Text, out var reframe)
                ? reframe
                : fragment.Text;
            fragment.Connect(targetPosition, transformed);

            switch (fragment.FragmentKind)
            {
                case ExamFragmentKind.Body:
                    bodyConnected = true;
                    trust = Mathf.Clamp(trust + 1, 0, 8);
                    stress = Mathf.Clamp(stress - 2, 0, 8);
                    feedback.Show("Yes. This is happening in my body.");
                    break;
                case ExamFragmentKind.Reality:
                    realityConnected = true;
                    grounding = Mathf.Clamp(grounding + 2, 0, 8);
                    stress = Mathf.Clamp(stress - 1, 0, 8);
                    feedback.Show("This is here. This is happening now.");
                    break;
                case ExamFragmentKind.Future:
                    futureConnected = true;
                    grounding = Mathf.Clamp(grounding + 2, 0, 8);
                    trust = Mathf.Clamp(trust + 1, 0, 8);
                    feedback.Show("This is a fear, not a fact yet.");
                    break;
            }

            PublishHud();
            ResolveFragmentOwner(fragment);
            LogStateChange($"drop {fragment.FragmentKind} into {zone.FragmentKind}", before);
        }

        // Marks one thought as resolved when all fragments that came out of it have been sorted.
        private void ResolveFragmentOwner(ExamFragmentText fragment)
        {
            if (!fragmentOwners.TryGetValue(fragment, out var owner) || owner.resolved)
            {
                return;
            }

            for (var i = 0; i < owner.fragments.Count; i++)
            {
                if (!owner.fragments[i].IsConnected)
                {
                    return;
                }
            }

            owner.resolved = true;
            if (AllActiveThoughtsResolved())
            {
                StartCoroutine(AdvanceAfterFeedback());
            }
        }

        // Checks whether every visible thought in the current stage has been split and sorted.
        private bool AllActiveThoughtsResolved()
        {
            if (activeThoughts.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < activeThoughts.Count; i++)
            {
                if (!activeThoughts[i].resolved)
                {
                    return false;
                }
            }

            return true;
        }

        // Waits long enough for success feedback to read before advancing to the next stage.
        private IEnumerator AdvanceAfterFeedback()
        {
            var stage = stageLayouts[currentStage];
            yield return new WaitForSeconds(1.2f);
            if (ended)
            {
                yield break;
            }

            feedback.Show(stage.completeText);
            yield return new WaitForSeconds(1.3f);
            if (ended)
            {
                yield break;
            }

            if (currentStage + 1 < stageLayouts.Length)
            {
                SpawnStage(currentStage + 1);
            }
            else
            {
                RevealFirstQuestion();
            }
        }

        // Reveals the first exam question once the player has separated enough fear from facts.
        private void RevealFirstQuestion()
        {
            ClearFragments();
            HideThoughtViews();
            if (paperQuestionText != null)
            {
                paperQuestionText.gameObject.SetActive(true);
                paperQuestionText.text = "Name: ________\n1. Circle the familiar word:  cave";
            }

            feedback.Show("The first question is visible.\nYou can start here.");
        }

        // Encodes the readiness gate for turning anxiety into the first small action.
        private bool CanPlacePencil()
        {
            return bodyConnected
                && realityConnected
                && futureConnected
                && grounding >= 5
                && trust >= 4
                && stress <= 4
                && paperQuestionText != null
                && paperQuestionText.gameObject.activeSelf;
        }

        // Ends the level and chooses the final text based on remaining anxiety and support.
        private void CompleteLevel()
        {
            var finalText = grounding >= 7 && trust >= 5 && stress <= 3
                ? "The fear is still here.\nBut fear is no longer the same as fact."
                : "He is still nervous,\nbut the desk, the paper, and the first question are back.";
            EndLevel(ExamLevelEnding.Victory, finalText);
        }

        // Advances only interaction-confirmed time and closes the attempt at the exam bell.
        private void AddTime(int minutes)
        {
            usedMinutes = Mathf.Clamp(usedMinutes + minutes, 0, TimeoutMinutes);
            if (usedMinutes >= TimeoutMinutes && !pencilPlaced)
            {
                EndLevel(ExamLevelEnding.Failure, "The bell rings.\nHe is not ready to place the pencil yet.\nToday can stop here.");
            }

            PublishHud();
        }

        // Locks interaction, clears cursor-only hints, and shows the appropriate in-scene ending UI.
        private void EndLevel(ExamLevelEnding targetEnding, string finalText)
        {
            ended = true;
            ending = targetEnding;
            observeVisualActive = false;
            observeVisualGraceUntil = 0f;
            ShowThoughtCursorHint(false, 0f);
            cursorVisuals?.SetCursorMode(ExamCursorMode.Default);
            cursorVisuals?.SetObserveHold(false, 0f);
            feedback.Show(finalText);

            if (targetEnding == ExamLevelEnding.Victory)
            {
                endScreen?.ShowVictory(finalText);
            }
            else
            {
                endScreen?.ShowFailure(finalText);
            }

            PublishHud();
        }

        // Pushes hidden-state values and remaining exam time to the whitebox HUD.
        private void PublishHud()
        {
            feedback?.SetStats(grounding, trust, stress);
            feedback?.SetTime(usedMinutes, TimeoutMinutes);
            ApplyLittleCaveheartSprite(false);
            for (var i = 0; i < thoughtViews.Count; i++)
            {
                thoughtViews[i]?.SetPressure(stress);
            }
        }

        // Copies the current rule state before an interaction mutates any stat or visual mode.
        private ExamStateSnapshot CaptureState()
        {
            return new ExamStateSnapshot(
                grounding,
                trust,
                stress,
                usedMinutes,
                ChooseLittleCaveheartVisualState());
        }

        // Prints one concise Console line after an interaction so balancing changes are easy to trace.
        private void LogStateChange(string interactionName, ExamStateSnapshot before)
        {
            var after = CaptureState();
            Debug.Log(
                $"[Level3 Caveheart State] {interactionName}: "
                + $"grounding {before.grounding}->{after.grounding}, "
                + $"trust {before.trust}->{after.trust}, "
                + $"stress {before.stress}->{after.stress}, "
                + $"time {before.usedMinutes}->{after.usedMinutes}/{TimeoutMinutes}, "
                + $"state {before.caveheartState}->{after.caveheartState}",
                this);
        }

        // Maps hidden stats into simple visual feedback: sky pressure, camera shake, clarity, and openness.
        private void ApplyVisualFeedback()
        {
            var stress01 = Mathf.Clamp01(stress / 8f);
            var grounding01 = Mathf.Clamp01(grounding / 8f);
            var trust01 = Mathf.Clamp01(trust / 8f);
            if (ending == ExamLevelEnding.Victory)
            {
                stress01 = 0f;
                grounding01 = 1f;
                trust01 = 1f;
            }
            else if (ending == ExamLevelEnding.Failure)
            {
                stress01 = 1f;
                grounding01 = Mathf.Min(grounding01, 0.25f);
                trust01 = Mathf.Min(trust01, 0.35f);
            }

            if (fallingSkyArea != null)
            {
                fallingSkyArea.position = fallingSkyStart + new Vector3(0f, -0.42f * stress01, 0f);
            }

            if (Camera.main != null)
            {
                var shake = ending == ExamLevelEnding.Failure
                    ? Mathf.Sin(Time.time * 22f) * 0.018f
                    : ended ? 0f : Mathf.Sin(Time.time * 38f) * stress01 * 0.035f;
                Camera.main.transform.position = cameraStart + new Vector3(shake, 0f, 0f);
            }

            if (deskRenderer != null)
            {
                deskRenderer.color = Color.Lerp(new Color(0.32f, 0.25f, 0.2f, 0.75f), new Color(0.47f, 0.36f, 0.27f, 1f), grounding01);
            }

            if (paperRenderer != null)
            {
                paperRenderer.color = Color.Lerp(new Color(0.72f, 0.68f, 0.59f, 0.65f), new Color(0.98f, 0.94f, 0.82f, 1f), grounding01);
            }

            if (littleCaveheartRenderer != null)
            {
                if (HasLittleCaveheartStateSprites())
                {
                    ApplyLittleCaveheartSprite(false);
                }
                else
                {
                    littleCaveheartRenderer.color = Color.Lerp(new Color(0.72f, 0.48f, 0.42f, 1f), new Color(0.93f, 0.72f, 0.48f, 1f), trust01);
                }
            }
        }

        // Loads the four authored state sprites used by the Level 3 cave-school character layer.
        private void LoadLittleCaveheartStateSprites()
        {
            overwhelmedStateSprite = LoadExamStateSprite(OverwhelmedSpriteName);
            sittingStateSprite = LoadExamStateSprite(SittingSpriteName);
            observingStateSprite = LoadExamStateSprite(ObservingSpriteName);
            groundedStateSprite = LoadExamStateSprite(GroundedSpriteName);
        }

        // Picks and applies the current little-caveheart sprite from observe, grounding, and stress state.
        private void ApplyLittleCaveheartSprite(bool force)
        {
            if (littleCaveheartRenderer == null || !HasLittleCaveheartStateSprites())
            {
                return;
            }

            var nextState = ChooseLittleCaveheartVisualState();
            if (!force && nextState == currentLittleCaveheartVisualState)
            {
                return;
            }

            currentLittleCaveheartVisualState = nextState;
            littleCaveheartRenderer.sprite = SpriteForLittleCaveheartState(nextState);
            littleCaveheartRenderer.color = Color.white;
            littleCaveheartRenderer.sortingOrder = Mathf.Max(littleCaveheartRenderer.sortingOrder, 2);
        }

        // Gives the temporary observe sprite priority, then falls back to grounded, sitting, or overwhelmed.
        private LittleCaveheartVisualState ChooseLittleCaveheartVisualState()
        {
            if (ending == ExamLevelEnding.Victory)
            {
                return LittleCaveheartVisualState.Grounded;
            }

            if (ending == ExamLevelEnding.Failure)
            {
                return LittleCaveheartVisualState.Overwhelmed;
            }

            if (observeVisualActive || Time.time < observeVisualGraceUntil)
            {
                return LittleCaveheartVisualState.Observing;
            }

            if (grounding >= 8)
            {
                return LittleCaveheartVisualState.Grounded;
            }

            if (stress <= 4)
            {
                return LittleCaveheartVisualState.Sitting;
            }

            return LittleCaveheartVisualState.Overwhelmed;
        }

        // Selects the sprite for a visual state, falling back to overwhelmed if one asset is missing.
        private Sprite SpriteForLittleCaveheartState(LittleCaveheartVisualState state)
        {
            switch (state)
            {
                case LittleCaveheartVisualState.Sitting:
                    return sittingStateSprite != null ? sittingStateSprite : overwhelmedStateSprite;
                case LittleCaveheartVisualState.Observing:
                    return observingStateSprite != null ? observingStateSprite : overwhelmedStateSprite;
                case LittleCaveheartVisualState.Grounded:
                    return groundedStateSprite != null ? groundedStateSprite : overwhelmedStateSprite;
                default:
                    return overwhelmedStateSprite;
            }
        }

        // Checks whether any authored state sprite was loaded before replacing the whitebox fallback.
        private bool HasLittleCaveheartStateSprites()
        {
            return overwhelmedStateSprite != null
                || sittingStateSprite != null
                || observingStateSprite != null
                || groundedStateSprite != null;
        }

        // Loads one state sprite from Resources using the shared exam school-state folder.
        private static Sprite LoadExamStateSprite(string spriteName)
        {
            return Resources.Load<Sprite>(ExamStateSpriteRoot + spriteName);
        }

        // Finds the authored character-state layer before falling back to the old whitebox placeholder.
        private SpriteRenderer ResolveLittleCaveheartRenderer()
        {
            var stateRenderer = FindRenderer("LittleCaveheartStateSprite")
                ?? FindRenderer(GroundedSpriteName)
                ?? FindRenderer(OverwhelmedSpriteName)
                ?? FindRenderer(SittingSpriteName)
                ?? FindRenderer(ObservingSpriteName);

            if (stateRenderer != null)
            {
                return stateRenderer;
            }

            if (littleCaveheartRenderer != null)
            {
                return littleCaveheartRenderer;
            }

            return FindSceneSpriteRenderer("LittleCaveheart");
        }

        // Disables the old text-and-square character marker once the painted state layer is available.
        private void HideLegacyLittleCaveheartPlaceholder()
        {
            if (littleCaveheart == null)
            {
                return;
            }

            var textRenderer = littleCaveheart.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.enabled = false;
            }

            var text = littleCaveheart.GetComponent<TextMesh>();
            if (text != null)
            {
                text.text = string.Empty;
            }

            var block = littleCaveheart.Find("Whitebox Sprite");
            if (block != null)
            {
                block.gameObject.SetActive(false);
            }
        }

        // Finds the active runtime record for a thought view, optionally only if it has not split yet.
        private ActiveThought FindActiveThought(ExamThoughtText targetThought, bool requireUnsplit)
        {
            if (targetThought == null)
            {
                return null;
            }

            for (var i = 0; i < activeThoughts.Count; i++)
            {
                var thought = activeThoughts[i];
                if (thought.view == targetThought && (!requireUnsplit || !thought.split))
                {
                    return thought;
                }
            }

            return null;
        }

        // Returns the first unsplit thought in the current stage for legacy non-pointer calls.
        private ExamThoughtText FindFirstInteractableThought()
        {
            for (var i = 0; i < activeThoughts.Count; i++)
            {
                if (!activeThoughts[i].split && activeThoughts[i].view != null && activeThoughts[i].view.gameObject.activeInHierarchy)
                {
                    return activeThoughts[i].view;
                }
            }

            return null;
        }

        // Finds a drop zone under a world position, usually the fragment release point.
        private ExamDropZone FindDropZoneAt(Vector3 position)
        {
            var hits = Physics2D.OverlapPointAll(position);
            for (var i = 0; i < hits.Length; i++)
            {
                var zone = hits[i].GetComponent<ExamDropZone>();
                if (zone != null)
                {
                    return zone;
                }
            }

            return null;
        }

        // Checks whether the pencil was released over the editor-authored Paper collider.
        private bool IsOverPaper(Vector3 position)
        {
            paperDropCollider = paperDropCollider == null ? FindSceneCollider("Paper") : paperDropCollider;
            return paperDropCollider != null && paperDropCollider.OverlapPoint(position);
        }

        // Destroys or hides active runtime fragments before spawning a new stage.
        private void ClearFragments()
        {
            for (var i = activeFragments.Count - 1; i >= 0; i--)
            {
                if (activeFragments[i] != null)
                {
                    var fragmentObject = activeFragments[i].gameObject;
                    if (IsFragmentPositionName(fragmentObject.name) && fragmentObject.transform.parent == fragmentRoot)
                    {
                        fragmentObject.SetActive(false);
                    }
                    else
                    {
                        Destroy(fragmentObject);
                    }
                }
            }

            activeFragments.Clear();
            fragmentOwners.Clear();
        }

        // Finds a named scene object and returns a component on it.
        private static T FindObjectByName<T>(string objectName) where T : Component
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.GetComponent<T>();
        }

        // Reuses a controller-side helper component or adds it once when the scene has not serialized it yet.
        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        // Finds a named scene object's transform for controller-managed visual feedback.
        private static Transform FindTransform(string objectName)
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.transform;
        }

        // Finds a named object's SpriteRenderer when the whitebox object has one.
        private static SpriteRenderer FindRenderer(string objectName)
        {
            return FindObjectByName<SpriteRenderer>(objectName);
        }

        // Finds an authored SpriteRenderer on the named object or one of its children without modifying it.
        private static SpriteRenderer FindSceneSpriteRenderer(string objectName)
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.GetComponentInChildren<SpriteRenderer>(true);
        }

        // Finds an authored Collider2D on the named object or one of its children without creating one.
        private static Collider2D FindSceneCollider(string objectName)
        {
            var obj = GameObject.Find(objectName);
            return obj == null ? null : obj.GetComponentInChildren<Collider2D>(true);
        }

        // Recognizes both the old generic slot names and the new designer-facing position names.
        private static bool IsFragmentPositionName(string objectName)
        {
            return objectName.StartsWith("Fragment Slot", StringComparison.Ordinal)
                || objectName.StartsWith("Split Position", StringComparison.Ordinal);
        }

        // Assigns the expected fragment category to a named drop zone.
        private static void ConfigureDropZone(string objectName, ExamFragmentKind kind)
        {
            var zone = FindObjectByName<ExamDropZone>(objectName);
            zone?.Configure(kind);
        }

    }
}
