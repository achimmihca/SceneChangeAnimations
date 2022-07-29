using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SceneChangeAnimations;
using Random = UnityEngine.Random;

public class DemoSceneChangingControl : MonoBehaviour
{
    public string targetSceneName;

    private SceneChangeAnimationControl sceneChangeAnimationControl;

    public void Awake()
    {
        sceneChangeAnimationControl = FindObjectOfType<SceneChangeAnimationControl>();
    }

    public void Start()
    {
        UIDocument uiDocument = FindObjectOfType<UIDocument>();
        Button button = uiDocument.rootVisualElement.Q<Button>();
        button.RegisterCallback<ClickEvent>(evt =>
        {
            sceneChangeAnimationControl.AnimateChangeToScene(
                () => SceneManager.LoadScene(targetSceneName),
                SelectNextAnimation());
        });
    }

    private Action<VisualElement> SelectNextAnimation()
    {
        int animationIndex = Random.Range(0, 2);
        if (animationIndex == 0)
        {
            return null;
        }
        else
        {
            return StartCustomAnimation;
        }
    }

    private void StartCustomAnimation(VisualElement visualElement)
    {
        // Must start animation on GameObject that uses DontDestroyOnLoad.
        // Otherwise the coroutine will not survive the scene change.
        sceneChangeAnimationControl.StartCoroutine(MoveToTop(visualElement));
    }

    private static IEnumerator MoveToTop(VisualElement visualElement)
    {
        return AnimationFunctions.TimeBasedAnimation(
            0.5f,
            animTimePercent => visualElement.style.bottom =
                new StyleLength(new Length(animTimePercent * 100, LengthUnit.Percent)),
            () => visualElement.RemoveFromHierarchy());
    }
}