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
            importer.spritePixelsPerUnit = 64f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
        }
    }
}
