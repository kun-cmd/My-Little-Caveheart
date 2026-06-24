using UnityEngine;
using System.Collections.Generic;

namespace MyLittleCaveheart
{
    [DefaultExecutionOrder(32000)]
    public sealed class CaveheartCursorVisibilityGuard : MonoBehaviour
    {
        private const string GuardObjectName = "Caveheart Cursor Visibility Guard";

        private static CaveheartCursorVisibilityGuard instance;
        private static readonly HashSet<int> activeOwners = new HashSet<int>();
        private static Texture2D transparentCursor;
        private static bool nativeCursorHidden;

        // Keeps the native cursor hidden while the owner is using a UI cursor.
        public static void SetOverrideActive(Object owner, bool active)
        {
            if (!Application.isPlaying)
            {
                activeOwners.Clear();
                ShowNativeCursor();
                return;
            }

            if (owner == null)
            {
                return;
            }

            var ownerId = owner.GetInstanceID();
            if (active)
            {
                EnsureInstance();
                activeOwners.Add(ownerId);
                ApplyCursorVisibility(true);
                return;
            }

            activeOwners.Remove(ownerId);
            if (instance != null || nativeCursorHidden)
            {
                ApplyCursorVisibility(true);
            }
        }

        // Creates one guard that switches native cursor state when the pointer crosses the game window boundary.
        private static void EnsureInstance()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (instance != null)
            {
                return;
            }

            var existing = GameObject.Find(GuardObjectName);
            var guardObject = existing == null ? new GameObject(GuardObjectName) : existing;
            instance = guardObject.GetComponent<CaveheartCursorVisibilityGuard>();
            if (instance == null)
            {
                instance = guardObject.AddComponent<CaveheartCursorVisibilityGuard>();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            activeOwners.Clear();
            ShowNativeCursor();

            if (transparentCursor != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(transparentCursor);
                }
                else
#endif
                {
                    Destroy(transparentCursor);
                }

                transparentCursor = null;
            }
        }

        // Checks only for pointer boundary changes, avoiding repeated native cursor writes every frame.
        private void LateUpdate()
        {
            ApplyCursorVisibility(false);
        }

        // Restores the native cursor when the game window loses focus.
        private void OnApplicationFocus(bool hasFocus)
        {
            ApplyCursorVisibility(true);
        }

        // Reports whether the requested owner should show its UI cursor inside the game window.
        public static bool ShouldShowUiCursor(Object owner)
        {
            return owner != null
                && activeOwners.Contains(owner.GetInstanceID())
                && IsPointerInsideGameWindow();
        }

        // Switches the native cursor only when the desired hidden/visible state changes.
        private static void ApplyCursorVisibility(bool force)
        {
            var shouldHideNativeCursor = activeOwners.Count > 0 && IsPointerInsideGameWindow();
            if (!force && shouldHideNativeCursor == nativeCursorHidden)
            {
                return;
            }

            if (shouldHideNativeCursor)
            {
                HideNativeCursor();
                return;
            }

            ShowNativeCursor();
        }

        // Checks whether the pointer is currently inside the focused game window.
        private static bool IsPointerInsideGameWindow()
        {
            if (!Application.isFocused)
            {
                return false;
            }

            var position = Input.mousePosition;
            return position.x >= 0f
                && position.y >= 0f
                && position.x < Screen.width
                && position.y < Screen.height;
        }

        // Replaces the native cursor with a transparent runtime texture so the UI cursor is the only visible cursor.
        private static void HideNativeCursor()
        {
            Cursor.visible = false;
            Cursor.SetCursor(GetTransparentCursor(), Vector2.zero, CursorMode.ForceSoftware);
            nativeCursorHidden = true;
        }

        // Restores Unity's native cursor when no UI cursor owner is active.
        private static void ShowNativeCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            nativeCursorHidden = false;
        }

        // Builds a runtime RGBA32 no-mipmap cursor texture, avoiding importer requirements on asset textures.
        private static Texture2D GetTransparentCursor()
        {
            if (transparentCursor != null)
            {
                return transparentCursor;
            }

            const int size = 16;
            var pixels = new Color32[size * size];
            transparentCursor = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Caveheart Transparent Native Cursor",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            transparentCursor.SetPixels32(pixels);
            transparentCursor.Apply(false, false);
            return transparentCursor;
        }
    }
}
