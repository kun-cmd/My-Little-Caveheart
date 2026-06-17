using UnityEditor;
using UnityEngine;

public sealed class CaveheartScreenSpaceHandDrawnShaderGUI : ShaderGUI
{
    private MaterialProperty inkColor;
    private MaterialProperty lineWidth;
    private MaterialProperty lineSoftness;
    private MaterialProperty edgeThreshold;
    private MaterialProperty edgeStrength;
    private MaterialProperty fineDetailSuppression;
    private MaterialProperty fineDetailSupportWidth;
    private MaterialProperty fineDetailSupportThreshold;
    private MaterialProperty fineDetailSupportSoftness;
    private MaterialProperty shadowEdgeSuppression;
    private MaterialProperty shadowChromaThreshold;
    private MaterialProperty shadowChromaSoftness;
    private MaterialProperty colorEdgeStrength;
    private MaterialProperty xdogBlend;
    private MaterialProperty xdogRadius;
    private MaterialProperty xdogThreshold;
    private MaterialProperty xdogStrength;
    private MaterialProperty xdogPhi;
    private MaterialProperty xdogInteriorSuppression;
    private MaterialProperty xdogSupportThreshold;
    private MaterialProperty xdogSupportSoftness;
    private MaterialProperty secondaryStrokeStrength;
    private MaterialProperty jitterStrength;
    private MaterialProperty jitterFrequency;
    private MaterialProperty noiseScale;
    private MaterialProperty brokenLineStrength;
    private MaterialProperty paperOverlayTex;
    private MaterialProperty paperOverlayTint;
    private MaterialProperty paperOverlayStrength;
    private MaterialProperty paperGrain;
    private MaterialProperty posterize;
    private MaterialProperty toneTint;
    private MaterialProperty toneTintStrength;
    private MaterialProperty saturation;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Cache(properties);

        EditorGUILayout.HelpBox(
            "Screen-space ink pass. Use the first two groups for the main silhouette feel, then add jitter and texture only as needed.",
            MessageType.None);

        DrawPresetButtons(materialEditor);
        EditorGUILayout.Space();

        DrawGroupHeader("Ink Shape", "Controls the thickness, softness, and color of the drawn line.");
        materialEditor.ShaderProperty(inkColor, MakeLabel("Ink Color", "Final line tint blended over the screen image."));
        materialEditor.ShaderProperty(lineWidth, MakeLabel("Line Width", "Sampling radius for the edge pass. Higher values catch broader shapes."));
        materialEditor.ShaderProperty(lineSoftness, MakeLabel("Line Softness", "Softens the edge threshold so lines feel less digital."));

        EditorGUILayout.Space();
        DrawGroupHeader("Edge Reading", "Controls what counts as a drawable edge and how strongly it gets inked.");
        materialEditor.ShaderProperty(edgeThreshold, MakeLabel("Edge Threshold", "Raises or lowers how much contrast is needed before a line appears."));
        materialEditor.ShaderProperty(edgeStrength, MakeLabel("Edge Strength", "Boosts the detected edge before thresholding."));
        materialEditor.ShaderProperty(fineDetailSuppression, MakeLabel("Fine Detail Suppression", "Reduces small interior texture lines while keeping stronger, broader edges."));
        materialEditor.ShaderProperty(fineDetailSupportWidth, MakeLabel("Fine Detail Support Width", "Broad edge radius used to decide whether a line is important enough to keep."));
        materialEditor.ShaderProperty(fineDetailSupportThreshold, MakeLabel("Fine Detail Support Threshold", "How much broad edge support a fine line needs before it survives."));
        materialEditor.ShaderProperty(fineDetailSupportSoftness, MakeLabel("Fine Detail Support Softness", "Softens fine detail suppression so hair and soft contours are not cut too abruptly."));
        materialEditor.ShaderProperty(shadowEdgeSuppression, MakeLabel("Shadow Edge Suppression", "Suppresses same-color brightness-only edges so soft shadow borders do not become dirty ink lines."));
        materialEditor.ShaderProperty(shadowChromaThreshold, MakeLabel("Shadow Chroma Threshold", "How much hue/chroma change is needed before an edge is treated as a real drawable color edge."));
        materialEditor.ShaderProperty(shadowChromaSoftness, MakeLabel("Shadow Chroma Softness", "Softens shadow suppression so subtle real color edges can survive."));
        materialEditor.ShaderProperty(colorEdgeStrength, MakeLabel("Color Edge Strength", "Adds more outline response when color changes more than brightness."));
        materialEditor.ShaderProperty(xdogBlend, MakeLabel("XDoG Blend", "Adds a softer multi-scale edge pass that can catch hair-like interior and low-contrast lines."));
        materialEditor.ShaderProperty(xdogRadius, MakeLabel("XDoG Radius", "Sampling scale for the XDoG pass. Higher values make broader, sketchier bands."));
        materialEditor.ShaderProperty(xdogThreshold, MakeLabel("XDoG Threshold", "Contrast needed before XDoG contributes ink. Lower values reveal more subtle hair edges."));
        materialEditor.ShaderProperty(xdogStrength, MakeLabel("XDoG Strength", "Boosts the XDoG response before shaping."));
        materialEditor.ShaderProperty(xdogPhi, MakeLabel("XDoG Phi", "Sharpness of the XDoG transition. Higher values make ink appear more decisively."));
        materialEditor.ShaderProperty(xdogInteriorSuppression, MakeLabel("Interior Suppression", "Suppresses XDoG lines unless a broader edge also supports them."));
        materialEditor.ShaderProperty(xdogSupportThreshold, MakeLabel("Support Threshold", "How much broader Sobel edge support XDoG needs before drawing."));
        materialEditor.ShaderProperty(xdogSupportSoftness, MakeLabel("Support Softness", "Softens the support gate so weak hair edges can still survive."));
        materialEditor.ShaderProperty(secondaryStrokeStrength, MakeLabel("Secondary Stroke", "Adds a second, offset pass to fake doubled hand-drawn strokes."));

        EditorGUILayout.Space();
        DrawGroupHeader("Line Motion", "Makes the outline feel held and redrawn instead of perfectly locked to the screen.");
        materialEditor.ShaderProperty(jitterStrength, MakeLabel("Jitter Strength", "How far the line UV is nudged each held frame."));
        materialEditor.ShaderProperty(jitterFrequency, MakeLabel("Jitter Frequency", "How often the held jitter pattern updates over time."));
        materialEditor.ShaderProperty(noiseScale, MakeLabel("Noise Scale", "Size of the jitter and grain cells. Lower is chunkier, higher is finer."));

        EditorGUILayout.Space();
        DrawGroupHeader("Paper Overlay", "A real paper texture multiplied over the full screen. Keep the strength subtle and let the texture do the work.");
        materialEditor.TexturePropertySingleLine(MakeLabel("Paper Texture", "Paper image laid over the screen after posterization and before inking."), paperOverlayTex, paperOverlayStrength);
        materialEditor.TextureScaleOffsetProperty(paperOverlayTex);
        materialEditor.ShaderProperty(paperOverlayTint, MakeLabel("Paper Tint", "Extra tint applied to the paper image before blending."));

        EditorGUILayout.Space();
        DrawGroupHeader("Surface Texture", "Optional extra breakup on top of the paper image.");
        materialEditor.ShaderProperty(brokenLineStrength, MakeLabel("Broken Line", "Randomly knocks out parts of the stroke to simulate dry ink or pencil skips."));
        materialEditor.ShaderProperty(paperGrain, MakeLabel("Micro Grain", "Tiny procedural grain under the paper image. Leave very low if the paper texture already reads well."));
        materialEditor.ShaderProperty(posterize, MakeLabel("Posterize", "Reduces color steps for a flatter illustrated feel."));

        EditorGUILayout.Space();
        DrawGroupHeader("Runtime Tone", "Full-screen color grade knobs. Level scripts can animate these while leaving the ink and paper parameters hand-tuned.");
        materialEditor.ShaderProperty(toneTint, MakeLabel("Tone Tint", "Color multiplied into the final screen image when Tone Tint Strength is above zero."));
        materialEditor.ShaderProperty(toneTintStrength, MakeLabel("Tone Tint Strength", "How much the final image is pulled toward the tint color."));
        materialEditor.ShaderProperty(saturation, MakeLabel("Saturation", "Overall color saturation after ink and paper have been applied."));
    }

    private void Cache(MaterialProperty[] properties)
    {
        inkColor = FindProperty("_InkColor", properties);
        lineWidth = FindProperty("_LineWidth", properties);
        lineSoftness = FindProperty("_LineSoftness", properties);
        edgeThreshold = FindProperty("_EdgeThreshold", properties);
        edgeStrength = FindProperty("_EdgeStrength", properties);
        fineDetailSuppression = FindProperty("_FineDetailSuppression", properties);
        fineDetailSupportWidth = FindProperty("_FineDetailSupportWidth", properties);
        fineDetailSupportThreshold = FindProperty("_FineDetailSupportThreshold", properties);
        fineDetailSupportSoftness = FindProperty("_FineDetailSupportSoftness", properties);
        shadowEdgeSuppression = FindProperty("_ShadowEdgeSuppression", properties);
        shadowChromaThreshold = FindProperty("_ShadowChromaThreshold", properties);
        shadowChromaSoftness = FindProperty("_ShadowChromaSoftness", properties);
        colorEdgeStrength = FindProperty("_ColorEdgeStrength", properties);
        xdogBlend = FindProperty("_XDogBlend", properties);
        xdogRadius = FindProperty("_XDogRadius", properties);
        xdogThreshold = FindProperty("_XDogThreshold", properties);
        xdogStrength = FindProperty("_XDogStrength", properties);
        xdogPhi = FindProperty("_XDogPhi", properties);
        xdogInteriorSuppression = FindProperty("_XDogInteriorSuppression", properties);
        xdogSupportThreshold = FindProperty("_XDogSupportThreshold", properties);
        xdogSupportSoftness = FindProperty("_XDogSupportSoftness", properties);
        secondaryStrokeStrength = FindProperty("_SecondaryStrokeStrength", properties);
        jitterStrength = FindProperty("_JitterStrength", properties);
        jitterFrequency = FindProperty("_JitterFrequency", properties);
        noiseScale = FindProperty("_NoiseScale", properties);
        brokenLineStrength = FindProperty("_BrokenLineStrength", properties);
        paperOverlayTex = FindProperty("_PaperOverlayTex", properties);
        paperOverlayTint = FindProperty("_PaperOverlayTint", properties);
        paperOverlayStrength = FindProperty("_PaperOverlayStrength", properties);
        paperGrain = FindProperty("_PaperGrain", properties);
        posterize = FindProperty("_Posterize", properties);
        toneTint = FindProperty("_ToneTint", properties);
        toneTintStrength = FindProperty("_ToneTintStrength", properties);
        saturation = FindProperty("_Saturation", properties);
    }

    private static void DrawGroupHeader(string title, string description)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(description, MessageType.None);
    }

    private static GUIContent MakeLabel(string title, string tooltip)
    {
        return new GUIContent(title, tooltip);
    }

    private void DrawPresetButtons(MaterialEditor materialEditor)
    {
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clean Ink"))
            {
                ApplyPreset(materialEditor, new HandDrawnPreset(
                    new Color(0.14f, 0.085f, 0.055f, 1f),
                    2.1f,
                    0.18f,
                    0.08f,
                    1.9f,
                    0.68f,
                    2.6f,
                    0.055f,
                    0.16f,
                    0.78f,
                    0.06f,
                    0.12f,
                    0.55f,
                    0.34f,
                    1.95f,
                    0.08f,
                    1.9f,
                    10f,
                    0.9f,
                    0.035f,
                    0.12f,
                    0.0f,
                    1.15f,
                    4.5f,
                    92f,
                    0.14f,
                    Color.white,
                    0.0f,
                    0.0f,
                    0.0f));
            }

            if (GUILayout.Button("Storybook Rough"))
            {
                ApplyPreset(materialEditor, new HandDrawnPreset(
                    new Color(0.16f, 0.095f, 0.06f, 1f),
                    2.35f,
                    0.24f,
                    0.075f,
                    2.15f,
                    0.6f,
                    2.45f,
                    0.05f,
                    0.18f,
                    0.72f,
                    0.055f,
                    0.14f,
                    0.72f,
                    0.48f,
                    2.1f,
                    0.07f,
                    2.25f,
                    12f,
                    0.86f,
                    0.032f,
                    0.13f,
                    0.0f,
                    1.65f,
                    5.2f,
                    82f,
                    0.22f,
                    new Color(1f, 0.985f, 0.965f, 1f),
                    0.14f,
                    0.0f,
                    0.08f));
            }

            if (GUILayout.Button("Dry Pencil"))
            {
                ApplyPreset(materialEditor, new HandDrawnPreset(
                    new Color(0.1f, 0.1f, 0.11f, 0.88f),
                    1.55f,
                    0.28f,
                    0.09f,
                    1.55f,
                    0.72f,
                    2.8f,
                    0.06f,
                    0.14f,
                    0.86f,
                    0.07f,
                    0.1f,
                    0.5f,
                    0.5f,
                    1.85f,
                    0.085f,
                    2.0f,
                    9f,
                    0.92f,
                    0.04f,
                    0.1f,
                    0.0f,
                    1.9f,
                    6.1f,
                    74f,
                    0.3f,
                    new Color(0.98f, 0.975f, 0.965f, 1f),
                    0.38f,
                    0.02f,
                    0.16f));
            }
        }
    }

    private void ApplyPreset(MaterialEditor materialEditor, HandDrawnPreset preset)
    {
        foreach (var target in materialEditor.targets)
        {
            var material = target as Material;
            if (material == null)
            {
                continue;
            }

            Undo.RecordObject(material, "Apply Hand Drawn Preset");
            material.SetColor("_InkColor", preset.InkColor);
            material.SetFloat("_LineWidth", preset.LineWidth);
            material.SetFloat("_LineSoftness", preset.LineSoftness);
            material.SetFloat("_EdgeThreshold", preset.EdgeThreshold);
            material.SetFloat("_EdgeStrength", preset.EdgeStrength);
            material.SetFloat("_FineDetailSuppression", preset.FineDetailSuppression);
            material.SetFloat("_FineDetailSupportWidth", preset.FineDetailSupportWidth);
            material.SetFloat("_FineDetailSupportThreshold", preset.FineDetailSupportThreshold);
            material.SetFloat("_FineDetailSupportSoftness", preset.FineDetailSupportSoftness);
            material.SetFloat("_ShadowEdgeSuppression", preset.ShadowEdgeSuppression);
            material.SetFloat("_ShadowChromaThreshold", preset.ShadowChromaThreshold);
            material.SetFloat("_ShadowChromaSoftness", preset.ShadowChromaSoftness);
            material.SetFloat("_ColorEdgeStrength", preset.ColorEdgeStrength);
            material.SetFloat("_XDogBlend", preset.XDogBlend);
            material.SetFloat("_XDogRadius", preset.XDogRadius);
            material.SetFloat("_XDogThreshold", preset.XDogThreshold);
            material.SetFloat("_XDogStrength", preset.XDogStrength);
            material.SetFloat("_XDogPhi", preset.XDogPhi);
            material.SetFloat("_XDogInteriorSuppression", preset.XDogInteriorSuppression);
            material.SetFloat("_XDogSupportThreshold", preset.XDogSupportThreshold);
            material.SetFloat("_XDogSupportSoftness", preset.XDogSupportSoftness);
            material.SetFloat("_SecondaryStrokeStrength", preset.SecondaryStrokeStrength);
            material.SetFloat("_JitterStrength", preset.JitterStrength);
            material.SetFloat("_JitterFrequency", preset.JitterFrequency);
            material.SetFloat("_NoiseScale", preset.NoiseScale);
            material.SetFloat("_PaperOverlayStrength", preset.PaperOverlayStrength);
            material.SetColor("_PaperOverlayTint", preset.PaperOverlayTint);
            material.SetFloat("_BrokenLineStrength", preset.BrokenLineStrength);
            material.SetFloat("_PaperGrain", preset.PaperGrain);
            material.SetFloat("_Posterize", preset.Posterize);
            EditorUtility.SetDirty(material);
        }
    }

    private readonly struct HandDrawnPreset
    {
        public readonly Color InkColor;
        public readonly float LineWidth;
        public readonly float LineSoftness;
        public readonly float EdgeThreshold;
        public readonly float EdgeStrength;
        public readonly float FineDetailSuppression;
        public readonly float FineDetailSupportWidth;
        public readonly float FineDetailSupportThreshold;
        public readonly float FineDetailSupportSoftness;
        public readonly float ShadowEdgeSuppression;
        public readonly float ShadowChromaThreshold;
        public readonly float ShadowChromaSoftness;
        public readonly float ColorEdgeStrength;
        public readonly float XDogBlend;
        public readonly float XDogRadius;
        public readonly float XDogThreshold;
        public readonly float XDogStrength;
        public readonly float XDogPhi;
        public readonly float XDogInteriorSuppression;
        public readonly float XDogSupportThreshold;
        public readonly float XDogSupportSoftness;
        public readonly float SecondaryStrokeStrength;
        public readonly float JitterStrength;
        public readonly float JitterFrequency;
        public readonly float NoiseScale;
        public readonly float PaperOverlayStrength;
        public readonly Color PaperOverlayTint;
        public readonly float BrokenLineStrength;
        public readonly float PaperGrain;
        public readonly float Posterize;

        public HandDrawnPreset(
            Color inkColor,
            float lineWidth,
            float lineSoftness,
            float edgeThreshold,
            float edgeStrength,
            float fineDetailSuppression,
            float fineDetailSupportWidth,
            float fineDetailSupportThreshold,
            float fineDetailSupportSoftness,
            float shadowEdgeSuppression,
            float shadowChromaThreshold,
            float shadowChromaSoftness,
            float colorEdgeStrength,
            float xdogBlend,
            float xdogRadius,
            float xdogThreshold,
            float xdogStrength,
            float xdogPhi,
            float xdogInteriorSuppression,
            float xdogSupportThreshold,
            float xdogSupportSoftness,
            float secondaryStrokeStrength,
            float jitterStrength,
            float jitterFrequency,
            float noiseScale,
            float paperOverlayStrength,
            Color paperOverlayTint,
            float brokenLineStrength,
            float paperGrain,
            float posterize)
        {
            InkColor = inkColor;
            LineWidth = lineWidth;
            LineSoftness = lineSoftness;
            EdgeThreshold = edgeThreshold;
            EdgeStrength = edgeStrength;
            FineDetailSuppression = fineDetailSuppression;
            FineDetailSupportWidth = fineDetailSupportWidth;
            FineDetailSupportThreshold = fineDetailSupportThreshold;
            FineDetailSupportSoftness = fineDetailSupportSoftness;
            ShadowEdgeSuppression = shadowEdgeSuppression;
            ShadowChromaThreshold = shadowChromaThreshold;
            ShadowChromaSoftness = shadowChromaSoftness;
            ColorEdgeStrength = colorEdgeStrength;
            XDogBlend = xdogBlend;
            XDogRadius = xdogRadius;
            XDogThreshold = xdogThreshold;
            XDogStrength = xdogStrength;
            XDogPhi = xdogPhi;
            XDogInteriorSuppression = xdogInteriorSuppression;
            XDogSupportThreshold = xdogSupportThreshold;
            XDogSupportSoftness = xdogSupportSoftness;
            SecondaryStrokeStrength = secondaryStrokeStrength;
            JitterStrength = jitterStrength;
            JitterFrequency = jitterFrequency;
            NoiseScale = noiseScale;
            PaperOverlayStrength = paperOverlayStrength;
            PaperOverlayTint = paperOverlayTint;
            BrokenLineStrength = brokenLineStrength;
            PaperGrain = paperGrain;
            Posterize = posterize;
        }
    }
}
