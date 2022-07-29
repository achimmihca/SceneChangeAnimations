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
        private RenderTexture renderTexture;
        private Texture2D texture2D;

        private bool hasRenderedTexture;

        private Action<VisualElement> animateVisualElementAction;

        // By default click through the "screenshot" VisualElement.
        public PickingMode VisualElementPickingMode { get; set; } = PickingMode.Ignore;

        public Func<UIDocument> FindUIDocumentFunction { get; set; }= DefaultFindUIDocumentFunction;

        public void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void Start()
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
            this.animateVisualElementAction = doAnimateVisualElementAction;

            // Take "screenshot" of "old" scene.
            // Therefor, copy the UIDocument but with different PanelSettings that render to texture.
            UIDocument uiDocumentOriginal = FindUIDocumentFunction();
            UIDocument uiDocumentCopy = Instantiate(uiDocumentOriginal);
            uiDocumentCopy.panelSettings = panelSettings;
            hasRenderedTexture = true;

            // Unity needs one additional frame to render to the texture
            // See https://forum.unity.com/threads/force-render-ui-or-getting-event-for-rendered-ui.1263884/
            StartCoroutine(WaitForFramesThenDo(2, doLoadSceneAction));
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
            visualElement.pickingMode = VisualElementPickingMode;
            return visualElement;
        }

        private VisualElement AppendRenderTextureVisualElement()
        {
            VisualElement visualElement = CreateRenderTextureVisualElement();
            UIDocument uiDocument = FindUIDocumentFunction();
            uiDocument.rootVisualElement.Add(visualElement);
            return visualElement;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!hasRenderedTexture)
            {
                return;
            }

            VisualElement visualElement = AppendRenderTextureVisualElement();
            if (animateVisualElementAction != null)
            {
                animateVisualElementAction(visualElement);
            }
            else
            {
                StartFadeOutAnimation(visualElement);
            }
        }

        private void OnDestroy()
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