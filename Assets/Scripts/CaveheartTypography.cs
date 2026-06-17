using UnityEngine;
using UnityEngine.UI;

namespace MyLittleCaveheart
{
    public static class CaveheartTypography
    {
        private const string StorybookFontResourcePath = "Fonts/ShortStack-Regular";

        private static Font storybookFont;

        // Loads the active storybook font once and falls back to Unity's legacy font if the asset is missing.
        public static Font StorybookFont
        {
            // Returns the cached font asset, loading it from Resources on first use.
            get
            {
                if (storybookFont == null)
                {
                    storybookFont = Resources.Load<Font>(StorybookFontResourcePath);
                }

                return storybookFont != null
                    ? storybookFont
                    : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        // Applies the shared storybook font to screen-space UI text.
        public static void ApplyTo(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.font = StorybookFont;
        }

        // Applies the shared storybook font and material to world-space TextMesh labels.
        public static void ApplyTo(TextMesh textMesh)
        {
            if (textMesh == null)
            {
                return;
            }

            var font = StorybookFont;
            textMesh.font = font;

            var renderer = textMesh.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
            {
                renderer.sharedMaterial = font.material;
            }
        }
    }
}
