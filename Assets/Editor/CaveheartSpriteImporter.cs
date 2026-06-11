using UnityEditor;
using UnityEngine;

namespace MyLittleCaveheart.EditorTools
{
    public sealed class CaveheartSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith("Assets/Resources/Sprites/Caveheart/") || !normalizedPath.EndsWith(".png"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = UsesHighResolutionStateSprite(normalizedPath) ? 1254f : 64f;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
        }

        private static bool UsesHighResolutionStateSprite(string normalizedPath)
        {
            if (normalizedPath.StartsWith("Assets/Resources/Sprites/Caveheart/RulesStates1254/")
                || normalizedPath.StartsWith("Assets/Resources/Sprites/Caveheart/Generated/"))
            {
                return true;
            }

            var fileName = System.IO.Path.GetFileNameWithoutExtension(normalizedPath);
            return fileName == "sleeping_0"
                || fileName == "sleeping_1"
                || fileName == "sleeping_2"
                || fileName == "sleeping_3"
                || fileName == "startled_0"
                || fileName == "startled_1"
                || fileName == "startled_2"
                || fileName == "startled_3"
                || fileName == "resisting_0"
                || fileName == "resisting_1"
                || fileName == "resisting_2"
                || fileName == "resisting_3"
                || fileName == "settled_0"
                || fileName == "settled_1"
                || fileName == "settled_2"
                || fileName == "settled_3"
                || fileName == "sitting_0"
                || fileName == "sitting_1"
                || fileName == "sitting_2"
                || fileName == "sitting_3";
        }
    }
}
