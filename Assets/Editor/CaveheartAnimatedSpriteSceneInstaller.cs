using MyLittleCaveheart;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLittleCaveheart.EditorTools
{
    public static class CaveheartAnimatedSpriteSceneInstaller
    {
        private const string ScenePath = "Assets/level1.unity";
        private const string SpriteObjectName = "Little Caveheart Animated Sprite";
        private const string InitialSpritePath = "Assets/Resources/Sprites/Caveheart/sleeping_0.png";

        private static readonly Vector3 DefaultPosition = new Vector3(0f, -0.35f, -0.55f);
        private static readonly Vector3 DefaultScale = new Vector3(2.2f, 2.2f, 1f);
        private const int DefaultSortingOrder = 80;

        [MenuItem("Tools/My Little Caveheart/Install Animated Character Sprite")]
        public static void InstallInCurrentScene()
        {
            InstallIntoScene(SceneManager.GetActiveScene(), true);
        }

        public static void InstallInLevelOneScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InstallIntoScene(scene, true);
            EditorSceneManager.SaveScene(scene);
        }

        private static void InstallIntoScene(Scene scene, bool selectSprite)
        {
            var controller = Object.FindFirstObjectByType<CaveheartGameController>();
            if (controller == null)
            {
                Debug.LogWarning("No CaveheartGameController found. Open or build the Caveheart scene first.");
                return;
            }

            var spriteObject = GameObject.Find(SpriteObjectName);
            var createdSpriteObject = false;
            if (spriteObject == null)
            {
                spriteObject = new GameObject(SpriteObjectName);
                Undo.RegisterCreatedObjectUndo(spriteObject, "Create Caveheart animated sprite");
                spriteObject.transform.position = DefaultPosition;
                spriteObject.transform.localScale = DefaultScale;
                createdSpriteObject = true;
            }

            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = Undo.AddComponent<SpriteRenderer>(spriteObject);
            }

            var initialSprite = AssetDatabase.LoadAssetAtPath<Sprite>(InitialSpritePath);
            if (renderer.sprite == null)
            {
                renderer.sprite = initialSprite;
            }

            renderer.color = Color.white;
            renderer.sortingOrder = DefaultSortingOrder;

            var animator = controller.GetComponent<CaveheartSpriteAnimator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<CaveheartSpriteAnimator>(controller.gameObject);
            }

            animator.Configure(renderer, order: DefaultSortingOrder);
            animator.LoadClips();
            animator.Play(CaveheartState.Sleeping);

            EditorUtility.SetDirty(spriteObject);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);

            if (selectSprite || createdSpriteObject)
            {
                Selection.activeGameObject = spriteObject;
            }

            Debug.Log($"Installed {SpriteObjectName} and bound it to {controller.name}.");
        }
    }
}
