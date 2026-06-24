using UnityEditor;
using UnityEngine;

namespace MyLittleCaveheart.EditorTools
{
    public sealed class CaveheartSpriteImporter : AssetPostprocessor
    {
        private const string Level2SceneArtRoot = "Assets/Resources/Sprites/Caveheart/Level2Fire/SceneArt/";
        private const string Level2StateSpriteRoot = "Assets/Resources/Sprites/Caveheart/Level2Fire/States1254/";

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
            importer.spritePixelsPerUnit = SpritePixelsPerUnitFor(normalizedPath);
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
        }

        private static float SpritePixelsPerUnitFor(string normalizedPath)
        {
            if (normalizedPath.StartsWith(Level2StateSpriteRoot))
            {
                return 512f;
            }

            if (TryGetLevel2SceneArtPixelsPerUnit(normalizedPath, out var pixelsPerUnit))
            {
                return pixelsPerUnit;
            }

            return UsesHighResolutionStateSprite(normalizedPath) ? 1254f : 64f;
        }

        private static bool TryGetLevel2SceneArtPixelsPerUnit(string normalizedPath, out float pixelsPerUnit)
        {
            pixelsPerUnit = 64f;
            if (!normalizedPath.StartsWith(Level2SceneArtRoot))
            {
                return false;
            }

            switch (System.IO.Path.GetFileNameWithoutExtension(normalizedPath))
            {
                case "CaveBackground_WarmInterior":
                case "CaveBackground_NoFire":
                    pixelsPerUnit = 115.2f;
                    return true;
                case "Campfire_Glow":
                case "DryingSpots_TwoWarmStones":
                    pixelsPerUnit = 341f;
                    return true;
                case "Firewood_MixedDryWetPile":
                    pixelsPerUnit = 394f;
                    return true;
                case "Firewood_Dry_Log":
                case "Firewood_Wet_Log":
                    pixelsPerUnit = 512f;
                    return true;
                case "LionShadow_WallProjection":
                    pixelsPerUnit = 410f;
                    return true;
                case "Campfire_StoneRing":
                    pixelsPerUnit = 445f;
                    return true;
                case "CaveWind_Left":
                case "CaveWind_Right":
                    pixelsPerUnit = 600f;
                    return true;
                case "AshPoker":
                    pixelsPerUnit = 512f;
                    return true;
                case "Campfire_AshLayer":
                    pixelsPerUnit = 585f;
                    return true;
                case "HunterSilhouette_Event":
                    pixelsPerUnit = 661f;
                    return true;
                case "Campfire_Embers":
                    pixelsPerUnit = 721f;
                    return true;
                case "Campfire_FlameOuter":
                    pixelsPerUnit = 883f;
                    return true;
                case "Campfire_FlameInner":
                case "WindBlockStone":
                    pixelsPerUnit = 1024f;
                    return true;
                default:
                    return false;
            }
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
