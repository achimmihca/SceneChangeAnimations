using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SceneChangeAnimations
{
    public class SceneChangeAnimationControl : MonoBehaviour
    {
        public PanelSettings panelSettings;
        public bool setMainCameraClearFlags = true;

        // By default, it is possible to click through the "screenshot" VisualElement.
        public PickingMode visualElementPickingMode = PickingMode.Ignore;

        // The method used to find a UIDocument in the scene.
        public Func<UIDocument> findUIDocumentFunction = DefaultFindUIDocumentFunction;

        private RenderTexture renderTexture;
        private Texture2D texture2D;

        private bool hasRenderedTexture;

        private Action<VisualElement> animateVisualElementAction;

        public virtual void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public virtual void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public virtual void Start()
        {
            // Init single instance
            var sceneChangeAnimationControls = FindObjectsOfType<SceneChangeAnimationControl>();
            if (sceneChangeAnimationControls.Length > 1)
            {
                // Another instance already exists. Destroy this one
                Destroy(gameObject);
                return;
            }

            // Start is not called again for this DontDestroyOnLoad-object
            DontDestroyOnLoad(gameObject);

            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            panelSettings.targetTexture = renderTexture;
        }

        public void AnimateChangeToScene(
            Action doLoadSceneAction,
            Action<VisualElement> doAnimateVisualElementAction = null)
        {
            animateVisualElementAction = doAnimateVisualElementAction;

            // Take "screenshot" of "old" scene.
            // Therefor, use the PanelSettings that render to texture.
            UIDocument uiDocument = findUIDocumentFunction();
            uiDocument.panelSettings = panelSettings;

            if (setMainCameraClearFlags
                && Camera.main != null)
            {
                // Do not clear the background or the user will see a blank screen for once frame,
                // when the UIDocument is rendered to the RenderTexture.
                Camera.main.clearFlags = CameraClearFlags.Nothing;
            }

            // Unity needs one additional frame to render to the texture
            // See https://forum.unity.com/threads/force-render-ui-or-getting-event-for-rendered-ui.1263884/
            StartCoroutine(WaitForFramesThenDo(2, doLoadSceneAction));

            hasRenderedTexture = true;
        }

        private VisualElement CreateRenderTextureVisualElement()
        {
            if (texture2D != null)
            {
                // Free dynamically created texture.
                Destroy(texture2D);
            }

            // Add screenshot to "new" scene and animate it away
            VisualElement visualElement = new VisualElement();
            texture2D = RenderTextureToTexture2D(renderTexture);
            visualElement.style.backgroundImage = new StyleBackground(texture2D);
            visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
            visualElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            visualElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            visualElement.pickingMode = visualElementPickingMode;
            return visualElement;
        }

        private VisualElement AppendRenderTextureVisualElement(UIDocument uiDocument)
        {
            VisualElement visualElement = CreateRenderTextureVisualElement();
            uiDocument.rootVisualElement.Add(visualElement);
            return visualElement;
        }

        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!hasRenderedTexture)
            {
                return;
            }

            UIDocument newSceneUIDocument = findUIDocumentFunction();
            VisualElement visualElement = AppendRenderTextureVisualElement(newSceneUIDocument);
            if (animateVisualElementAction != null)
            {
                animateVisualElementAction(visualElement);
            }
            else
            {
                StartFadeOutAnimation(visualElement);
            }
        }

        protected virtual void OnDestroy()
        {
            // Free dynamically created texture.
            Destroy(renderTexture);
        }

        private void StartFadeOutAnimation(VisualElement visualElement)
        {
            StartCoroutine(AnimationFunctions.FadeOut(
                visualElement,
                0.5f,
                () => visualElement.RemoveFromHierarchy()));
        }

        private static IEnumerator WaitForFramesThenDo(int frameCount, Action action)
        {
            for (int i = 1; i <= frameCount; i++)
            {
                yield return new WaitForEndOfFrame();
            }
            action();
        }

        private static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
        {
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture2D.Apply(false);
            Graphics.CopyTexture(renderTexture, texture2D);
            return texture2D;
        }

        private static UIDocument DefaultFindUIDocumentFunction()
        {
            return FindObjectOfType<UIDocument>();
        }
    }
}