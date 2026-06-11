using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyLittleCaveheart.Editor
{
    [InitializeOnLoad]
    public static class CaveheartHandDrawnRendererInstaller
    {
        private const string RendererPath = "Assets/Rendering/Caveheart2DRenderer.asset";
        private const string MaterialPath = "Assets/Rendering/CaveheartScreenSpaceHandDrawn.mat";
        private const string FeatureName = "Caveheart Screen Space Hand Drawn";

        static CaveheartHandDrawnRendererInstaller()
        {
            EditorApplication.delayCall += Install;
        }

        private static void Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Install;
                return;
            }

            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (rendererData == null || material == null)
            {
                return;
            }

            for (var i = 0; i < rendererData.rendererFeatures.Count; i++)
            {
                if (rendererData.rendererFeatures[i] is FullScreenPassRendererFeature existing && existing.name == FeatureName)
                {
                    Configure(existing, material);
                    EditorUtility.SetDirty(existing);
                    EditorUtility.SetDirty(rendererData);
                    AssetDatabase.SaveAssets();
                    return;
                }
            }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;
            Configure(feature, material);
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var serializedRenderer = new SerializedObject(rendererData);
            var features = serializedRenderer.FindProperty("m_RendererFeatures");
            var featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");

            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;

            featureMap.arraySize++;
            featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;

            serializedRenderer.ApplyModifiedProperties();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
        }

        private static void Configure(FullScreenPassRendererFeature feature, Material material)
        {
            feature.SetActive(true);
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.None;
            feature.passMaterial = material;
            feature.passIndex = 0;
            feature.bindDepthStencilAttachment = false;
        }
    }
}
