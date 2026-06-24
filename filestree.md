# My Little Caveman 项目结构

> 说明：这是一个 Unity/C# 项目结构速览。为保持可读性，树中省略了大多数 Unity `.meta` 文件，以及 `Library/`、`Temp/`、`obj/`、`Logs/`、`UserSettings/` 等本地缓存或生成目录。

## 项目概览

- 项目类型：Unity 2D 游戏原型 / Caveheart 关卡项目
- 核心语言：C#
- 渲染管线：Universal Render Pipeline，项目中含 2D Renderer 与手绘屏幕空间 Shader
- 主要内容：
  - `level1.unity`：Caveheart Level 1 / 早晨照顾小穴居人的核心循环
  - `level2.unity`：The Fire I Keep / 火堆维持与可编辑白盒场景
  - `level3.unity`：考试压力场景与拖拽、擦除、观察等交互
  - `Assets/Scripts/`：运行时代码主干
  - `Assets/Editor/`：编辑器工具、导入器、场景安装脚本
  - `Assets/Resources/`：运行时加载的字体、音频、精灵资源

## 树状结构

```text
My Little Caveman/
├── .agents/                         # 本地 agent 配置
├── .codex/                          # Codex 工作区配置
├── .codegraph/                      # CodeGraph 索引数据
├── .git/                            # Git 仓库数据
├── .history/                        # 本地历史记录，通常不纳入版本控制
├── Assets/
│   ├── level1.unity                 # Level 1 场景
│   ├── level2.unity                 # Level 2 火堆关卡场景
│   ├── level3.unity                 # Level 3 考试压力场景
│   ├── ClickFeedback.prefab         # 点击反馈预制体
│   ├── UniversalRenderPipelineGlobalSettings.asset
│   ├── Audio/
│   │   └── Caveheart/
│   │       ├── *.mp3                # 原始/导入音频素材
│   │       ├── Extracted/           # 解压后的音效包内容
│   │       └── SourcePacks/         # 外部 CC0 音效包归档
│   ├── Documentation/
│   │   ├── CaveheartGameplayRules.md
│   │   ├── CaveheartPlaytestForm.md
│   │   ├── CaveheartWechatQuestionnaire.txt
│   │   ├── WHITEBOX_DEMO_README.md
│   │   └── ConceptArt/
│   │       └── LittleCaveman/       # 小穴居人、光标、Level 3 等概念图与草稿
│   ├── Editor/
│   │   ├── CaveheartAnimatedSpriteSceneInstaller.cs
│   │   ├── CaveheartHandDrawnRendererInstaller.cs
│   │   ├── CaveheartScreenSpaceHandDrawnShaderGUI.cs
│   │   ├── CaveheartSpriteImporter.cs
│   │   └── CaveheartWhiteboxSceneBuilder.cs
│   ├── Rendering/
│   │   ├── Caveheart2DRenderer.asset
│   │   ├── Caveheart2DRenderer_Level3Exam.asset
│   │   ├── CaveheartPaperOverlay.png
│   │   ├── CaveheartScreenSpaceHandDrawn.mat
│   │   ├── CaveheartScreenSpaceHandDrawn_Level3Exam.mat
│   │   └── CaveheartURP2D.asset
│   ├── Resources/
│   │   ├── Audio/
│   │   │   └── Caveheart/
│   │   │       ├── action_click.mp3
│   │   │       ├── action_observe_breath.mp3
│   │   │       ├── action_urge.mp3
│   │   │       ├── action_water.mp3
│   │   │       ├── action_window.mp3
│   │   │       ├── bgm_pale_sunrise.mp3
│   │   │       ├── feedback_negative_3.mp3
│   │   │       ├── feedback_positive_2.mp3
│   │   │       ├── feedback_positive_3.mp3
│   │   │       ├── feedback_reject_2.mp3
│   │   │       ├── feedback_reject_3.mp3
│   │   │       ├── level3_cave_exam_pressure.mp3
│   │   │       ├── level3_stone_marimba_grounded.mp3
│   │   │       └── README_LICENSES.md
│   │   ├── Fonts/
│   │   │   ├── ShortStack-Regular.ttf
│   │   │   ├── ShortStack-OFL.txt
│   │   │   ├── ZCOOLKuaiLe-Regular.ttf
│   │   │   ├── ZCOOLKuaiLe-OFL.txt
│   │   │   └── README_LICENSES.md
│   │   └── Sprites/
│   │       └── Caveheart/
│   │           ├── Backgrounds/
│   │           ├── CursorIcons/
│   │           │   └── Size64/      # 游戏内自定义光标图标
│   │           ├── Generated/       # 生成或处理后的角色图
│   │           ├── Level2Fire/
│   │           │   ├── SceneArt/    # 火堆、风、木柴、挡风石等场景资源
│   │           │   └── States1254/  # Level 2 小穴居人状态图
│   │           ├── Level3School/
│   │           │   ├── Characters/  # Level 3 角色图
│   │           │   ├── Props/       # 铅笔、橡皮等道具
│   │           │   └── States1254/  # Level 3 状态图
│   │           ├── RulesStates1254/ # Level 1 规则状态图
│   │           ├── UI/
│   │           └── Whitebox/
│   ├── Scenes/
│   │   ├── SampleScene.unity
│   │   └── WhiteboxMorningDemo.unity
│   ├── Scripts/
│   │   ├── MyLittleCaveheart.asmdef
│   │   ├── CaveheartActionButtonFeedback.cs
│   │   ├── CaveheartAudioFeedback.cs
│   │   ├── CaveheartBackgroundMusic.cs
│   │   ├── CaveheartCharacterView.cs
│   │   ├── CaveheartClickable.cs
│   │   ├── CaveheartClickFeedback.cs
│   │   ├── CaveheartCursorVisibilityGuard.cs
│   │   ├── CaveheartEnvironmentFeedback.cs
│   │   ├── CaveheartGameController.cs
│   │   ├── CaveheartInteractionContext.cs
│   │   ├── CaveheartInteractionResult.cs
│   │   ├── CaveheartInteractionType.cs
│   │   ├── CaveheartRules.cs
│   │   ├── CaveheartSceneBootstrapper.cs
│   │   ├── CaveheartSpriteAnimator.cs
│   │   ├── CaveheartState.cs
│   │   ├── CaveheartStats.cs
│   │   ├── CaveheartTypography.cs
│   │   ├── CaveheartWhiteboxPresenter.cs
│   │   ├── FireLevelController.cs
│   │   ├── FireLevelInteractable.cs
│   │   ├── ExamCursorVisuals.cs
│   │   ├── ExamDropZone.cs
│   │   ├── ExamEndScreenPresenter.cs
│   │   ├── ExamEraser.cs
│   │   ├── ExamFeedbackPresenter.cs
│   │   ├── ExamFragmentText.cs
│   │   ├── ExamHoldObserve.cs
│   │   ├── ExamLevelController.cs
│   │   ├── ExamPencil.cs
│   │   ├── ExamThoughtPointerInteraction.cs
│   │   └── ExamThoughtText.cs
│   ├── Shaders/
│   │   ├── CaveheartScreenSpaceHandDrawn.shader
│   │   └── WhiteboxFlash.shader
│   ├── Tests/
│   │   └── EditMode/
│   │       ├── MyLittleCaveheart.EditModeTests.asmdef
│   │       └── CaveheartRulesTests.cs
│   └── WhiteboxGenerated/
│       └── whitebox_square.png
├── Packages/
│   ├── manifest.json                # Unity 包依赖声明
│   └── packages-lock.json           # Unity 包锁定文件
├── ProjectSettings/
│   ├── ProjectSettings.asset
│   ├── ProjectVersion.txt
│   ├── EditorBuildSettings.asset
│   ├── GraphicsSettings.asset
│   ├── QualitySettings.asset
│   ├── TagManager.asset
│   └── ...                          # 其他 Unity 项目设置
├── Tools/
│   └── generate_caveheart_sprites.py # 生成 Caveheart 精灵图的工具脚本
├── .gitignore
├── .vsconfig
├── Assembly-CSharp-Editor.csproj     # Unity 生成的编辑器 C# 项目
├── My Little Caveman.sln             # Unity/IDE 解决方案
├── MyLittleCaveheart.csproj          # Unity 生成的运行时代码项目
└── MyLittleCaveheart.EditModeTests.csproj
```

## 核心代码分区

```text
Assets/Scripts/
├── Caveheart*                        # Level 1 与通用 Caveheart 系统
│   ├── GameController                # 主交互循环、状态推进、HUD/输入
│   ├── Rules / Stats / State         # 规则、数值、状态枚举
│   ├── Clickable / ClickFeedback     # 场景点击交互与反馈
│   ├── CharacterView / SpriteAnimator
│   ├── EnvironmentFeedback / AudioFeedback / BackgroundMusic
│   ├── CursorVisibilityGuard         # 自定义光标与原生光标可见性守卫
│   └── Typography / WhiteboxPresenter / SceneBootstrapper
├── FireLevel*                        # Level 2 火堆关卡逻辑
│   ├── FireLevelController
│   └── FireLevelInteractable
└── Exam*                             # Level 3 考试压力关卡逻辑
    ├── ExamLevelController
    ├── ExamCursorVisuals
    ├── ExamThoughtText / ExamFragmentText
    ├── ExamPencil / ExamEraser
    ├── ExamDropZone
    ├── ExamHoldObserve
    └── ExamEndScreenPresenter / ExamFeedbackPresenter
```

## 依赖与生成目录

```text
Packages/manifest.json
├── com.unity.render-pipelines.universal 14.0.12
├── com.unity.test-framework 1.1.33
├── com.unity.textmeshpro 3.0.7
├── com.unity.ugui 1.0.0
├── com.unity.visualscripting 1.9.4
└── Unity 内置 modules

本地生成/缓存目录（通常不提交）：
├── Library/
├── Temp/
├── obj/
├── Logs/
└── UserSettings/
```
