Shader "Hidden/Caveheart/Screen Space Hand Drawn"
{
    Properties
    {
        _InkColor ("Ink Color", Color) = (0.14, 0.085, 0.055, 1)
        _LineWidth ("Line Width", Range(0.25, 5)) = 2.2
        _EdgeThreshold ("Edge Threshold", Range(0.01, 0.5)) = 0.08
        _EdgeStrength ("Edge Strength", Range(0, 4)) = 2
        _FineDetailSuppression ("Fine Detail Suppression", Range(0, 1)) = 0.65
        _FineDetailSupportWidth ("Fine Detail Support Width", Range(1, 5)) = 2.6
        _FineDetailSupportThreshold ("Fine Detail Support Threshold", Range(0.001, 0.5)) = 0.055
        _FineDetailSupportSoftness ("Fine Detail Support Softness", Range(0.001, 0.5)) = 0.16
        _ShadowEdgeSuppression ("Shadow Edge Suppression", Range(0, 1)) = 0.78
        _ShadowChromaThreshold ("Shadow Chroma Threshold", Range(0.001, 0.5)) = 0.06
        _ShadowChromaSoftness ("Shadow Chroma Softness", Range(0.001, 0.5)) = 0.12
        _JitterStrength ("Jitter Strength", Range(0, 4)) = 1.55
        _JitterFrequency ("Jitter Frequency", Range(1, 12)) = 5
        _NoiseScale ("Noise Scale", Range(16, 180)) = 86
        _ColorEdgeStrength ("Color Edge Strength", Range(0, 2)) = 0.65
        _XDogBlend ("XDoG Blend", Range(0, 1)) = 0.42
        _XDogRadius ("XDoG Radius", Range(0.5, 4)) = 2.05
        _XDogThreshold ("XDoG Threshold", Range(0.001, 0.4)) = 0.075
        _XDogStrength ("XDoG Strength", Range(0, 8)) = 2.05
        _XDogPhi ("XDoG Phi", Range(1, 30)) = 12
        _XDogInteriorSuppression ("XDoG Interior Suppression", Range(0, 1)) = 0.9
        _XDogSupportThreshold ("XDoG Support Threshold", Range(0.001, 0.5)) = 0.035
        _XDogSupportSoftness ("XDoG Support Softness", Range(0.001, 0.5)) = 0.12
        _SecondaryStrokeStrength ("Secondary Stroke", Range(0, 1)) = 0
        _LineSoftness ("Line Softness", Range(0.02, 0.5)) = 0.2
        _BrokenLineStrength ("Broken Line", Range(0, 1)) = 0
        _PaperOverlayTex ("Paper Overlay", 2D) = "white" {}
        _PaperOverlayTint ("Paper Overlay Tint", Color) = (1, 1, 1, 1)
        _PaperOverlayStrength ("Paper Overlay Strength", Range(0, 1)) = 0.18
        _PaperGrain ("Paper Grain", Range(0, 1)) = 0
        _Posterize ("Posterize", Range(0, 1)) = 0
        _ToneTint ("Tone Tint", Color) = (1, 1, 1, 1)
        _ToneTintStrength ("Tone Tint Strength", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend One Zero

        Pass
        {
            Name "Caveheart Screen Space Hand Drawn"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float4 _InkColor;
            float _LineWidth;
            float _EdgeThreshold;
            float _EdgeStrength;
            float _FineDetailSuppression;
            float _FineDetailSupportWidth;
            float _FineDetailSupportThreshold;
            float _FineDetailSupportSoftness;
            float _ShadowEdgeSuppression;
            float _ShadowChromaThreshold;
            float _ShadowChromaSoftness;
            float _JitterStrength;
            float _JitterFrequency;
            float _NoiseScale;
            float _ColorEdgeStrength;
            float _XDogBlend;
            float _XDogRadius;
            float _XDogThreshold;
            float _XDogStrength;
            float _XDogPhi;
            float _XDogInteriorSuppression;
            float _XDogSupportThreshold;
            float _XDogSupportSoftness;
            float _SecondaryStrokeStrength;
            float _LineSoftness;
            float _BrokenLineStrength;
            TEXTURE2D(_PaperOverlayTex);
            SAMPLER(sampler_PaperOverlayTex);
            float4 _PaperOverlayTex_ST;
            float4 _PaperOverlayTint;
            float _PaperOverlayStrength;
            float _PaperGrain;
            float _Posterize;
            float4 _ToneTint;
            float _ToneTintStrength;
            float _Saturation;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float HeldTime()
            {
                return floor(_Time.y * max(1.0, _JitterFrequency));
            }

            float2 JitterUv(float2 uv)
            {
                float held = HeldTime();
                float2 cell = floor(uv * _NoiseScale);
                float n0 = Hash21(cell + held);
                float n1 = Hash21(cell.yx + held * 1.73 + 9.17);
                float2 pixel = 1.0 / max(_ScreenParams.xy, float2(1.0, 1.0));
                return uv + (float2(n0, n1) - 0.5) * pixel * _JitterStrength;
            }

            float3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            float Luma(float3 color)
            {
                return Luminance(color);
            }

            float3 Chroma(float3 color)
            {
                float brightness = max(max(color.r, color.g), max(color.b, 0.08));
                return color / brightness;
            }

            float Sobel(float2 uv, float widthMultiplier)
            {
                float2 texel = (1.0 / max(_ScreenParams.xy, float2(1.0, 1.0))) * _LineWidth * widthMultiplier;
                float3 tl = SampleColor(uv + texel * float2(-1.0,  1.0));
                float3  t = SampleColor(uv + texel * float2( 0.0,  1.0));
                float3 tr = SampleColor(uv + texel * float2( 1.0,  1.0));
                float3  l = SampleColor(uv + texel * float2(-1.0,  0.0));
                float3  r = SampleColor(uv + texel * float2( 1.0,  0.0));
                float3 bl = SampleColor(uv + texel * float2(-1.0, -1.0));
                float3  b = SampleColor(uv + texel * float2( 0.0, -1.0));
                float3 br = SampleColor(uv + texel * float2( 1.0, -1.0));

                float lumGx = -Luma(tl) - 2.0 * Luma(l) - Luma(bl) + Luma(tr) + 2.0 * Luma(r) + Luma(br);
                float lumGy = -Luma(bl) - 2.0 * Luma(b) - Luma(br) + Luma(tl) + 2.0 * Luma(t) + Luma(tr);
                float lumaEdge = sqrt(lumGx * lumGx + lumGy * lumGy);

                float3 ctl = Chroma(tl);
                float3 ct = Chroma(t);
                float3 ctr = Chroma(tr);
                float3 cl = Chroma(l);
                float3 cr = Chroma(r);
                float3 cbl = Chroma(bl);
                float3 cb = Chroma(b);
                float3 cbr = Chroma(br);
                float3 chromaGx = -ctl - 2.0 * cl - cbl + ctr + 2.0 * cr + cbr;
                float3 chromaGy = -cbl - 2.0 * cb - cbr + ctl + 2.0 * ct + ctr;
                float chromaEdge = sqrt(dot(chromaGx, chromaGx) + dot(chromaGy, chromaGy));
                float shadowMask = smoothstep(
                    _ShadowChromaThreshold,
                    _ShadowChromaThreshold + _ShadowChromaSoftness,
                    chromaEdge);
                float shadowReducedLuma = lerp(lumaEdge, lumaEdge * shadowMask, _ShadowEdgeSuppression);
                float colorEdge = chromaEdge * _ColorEdgeStrength;
                return max(shadowReducedLuma, colorEdge);
            }

            float StrokeLine(float2 uv, float widthMultiplier, float thresholdOffset)
            {
                float edge = Sobel(uv, widthMultiplier);
                float threshold = max(0.001, _EdgeThreshold + thresholdOffset);
                return smoothstep(threshold, threshold + _LineSoftness, edge * _EdgeStrength);
            }

            float SupportedStrokeLine(float2 uv, float widthMultiplier, float thresholdOffset)
            {
                float inkStroke = StrokeLine(uv, widthMultiplier, thresholdOffset);
                float supportEdge = Sobel(uv, _FineDetailSupportWidth);
                float supportMask = smoothstep(
                    _FineDetailSupportThreshold,
                    _FineDetailSupportThreshold + _FineDetailSupportSoftness,
                    supportEdge);
                return saturate(lerp(inkStroke, inkStroke * supportMask, _FineDetailSuppression));
            }

            float3 GaussianColor(float2 uv, float radius)
            {
                float2 texel = (1.0 / max(_ScreenParams.xy, float2(1.0, 1.0))) * radius * _LineWidth;
                float3 center = SampleColor(uv) * 0.28;
                float3 axial =
                    SampleColor(uv + texel * float2( 1.0,  0.0)) +
                    SampleColor(uv + texel * float2(-1.0,  0.0)) +
                    SampleColor(uv + texel * float2( 0.0,  1.0)) +
                    SampleColor(uv + texel * float2( 0.0, -1.0));
                float3 diagonal =
                    SampleColor(uv + texel * float2( 1.0,  1.0)) +
                    SampleColor(uv + texel * float2(-1.0,  1.0)) +
                    SampleColor(uv + texel * float2( 1.0, -1.0)) +
                    SampleColor(uv + texel * float2(-1.0, -1.0));
                return center + axial * 0.12 + diagonal * 0.06;
            }

            float XDogLine(float2 uv)
            {
                float3 narrow = GaussianColor(uv, _XDogRadius);
                float3 wide = GaussianColor(uv, _XDogRadius * 2.15);
                float lumaBand = abs(Luma(narrow) - Luma(wide));
                float chromaBand = length(Chroma(narrow) - Chroma(wide));
                float shadowMask = smoothstep(
                    _ShadowChromaThreshold,
                    _ShadowChromaThreshold + _ShadowChromaSoftness,
                    chromaBand);
                float shadowReducedLuma = lerp(lumaBand, lumaBand * shadowMask, _ShadowEdgeSuppression);
                float colorBand = chromaBand * _ColorEdgeStrength;
                float band = max(shadowReducedLuma, colorBand) * _XDogStrength;
                float shaped = smoothstep(_XDogThreshold, _XDogThreshold + 1.0 / max(1.0, _XDogPhi), band);
                float supportEdge = Sobel(uv, max(1.0, _XDogRadius * 0.75));
                float supportMask = smoothstep(
                    _XDogSupportThreshold,
                    _XDogSupportThreshold + _XDogSupportSoftness,
                    supportEdge);
                return saturate(lerp(shaped, shaped * supportMask, _XDogInteriorSuppression));
            }

            float3 PosterizeColor(float3 color)
            {
                float levels = 5.0;
                float3 stepped = floor(color * levels) / levels;
                return lerp(color, stepped, _Posterize);
            }

            float3 ApplyToneGrade(float3 color)
            {
                float luma = Luma(color);
                float3 saturated = lerp(float3(luma, luma, luma), color, _Saturation);
                float3 tinted = saturated * _ToneTint.rgb;
                return lerp(saturated, tinted, _ToneTintStrength);
            }

            float4 SamplePaperOverlay(float2 uv)
            {
                float2 paperUv = TRANSFORM_TEX(uv, _PaperOverlayTex);
                return SAMPLE_TEXTURE2D(_PaperOverlayTex, sampler_PaperOverlayTex, paperUv);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord.xy;
                float2 edgeUv = JitterUv(uv);
                float3 color = SampleColor(uv);

                float2 secondaryUv = JitterUv(uv + (1.0 / max(_ScreenParams.xy, float2(1.0, 1.0))) * float2(0.85, -0.55));
                float sobelLine = SupportedStrokeLine(edgeUv, 1.0, 0.0);
                float xdogLine = XDogLine(edgeUv);
                float secondaryLine = StrokeLine(secondaryUv, 0.55, 0.035) * _SecondaryStrokeStrength;
                float inkLine = saturate(lerp(sobelLine, max(sobelLine, xdogLine), _XDogBlend) + secondaryLine);

                float held = HeldTime();
                float grain = Hash21(floor(uv * _NoiseScale * 1.7) + held * 0.37);
                float breakMask = lerp(1.0, smoothstep(0.08, 0.95, grain), _BrokenLineStrength);
                inkLine *= breakMask;

                float paper = Hash21(floor(uv * _NoiseScale * 2.35) + 17.0);
                float3 toned = PosterizeColor(color);
                toned += (paper - 0.5) * _PaperGrain;

                float4 paperOverlay = SamplePaperOverlay(uv);
                float paperOverlayMask = saturate(paperOverlay.a * _PaperOverlayStrength);
                float3 paperOverlayColor = 1.0 - pow(saturate(1.0 - paperOverlay.rgb), 0.5);
                float3 paperOverlayTinted = lerp(float3(1.0, 1.0, 1.0), paperOverlayColor * _PaperOverlayTint.rgb, paperOverlayMask);
                toned *= paperOverlayTinted;

                float3 inked = lerp(toned, _InkColor.rgb, saturate(inkLine * _InkColor.a));
                float3 graded = ApplyToneGrade(inked);
                return half4(saturate(graded), 1.0);
            }
            ENDHLSL
        }
    }

    CustomEditor "CaveheartScreenSpaceHandDrawnShaderGUI"
}
