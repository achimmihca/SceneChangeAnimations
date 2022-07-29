using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneChangeAnimations
{
    public static class AnimationFunctions
    {
        public static IEnumerator FadeOut(
            VisualElement visualElement,
            float animTimeInSeconds,
            Action onCompleteAction)
        {
            return TimeBasedAnimation(
                animTimeInSeconds,
                animTimePercent => visualElement.style.opacity = 1 - animTimePercent,
                onCompleteAction);
        }

        public static IEnumerator TimeBasedAnimation(
            float animTimeInSeconds,
            Action<float> doApplyAnimTimeInPercent,
            Action onComplete)
        {
            float startTimeInSeconds = Time.time;
            float currentAnimTimeInSeconds;
            do
            {
                currentAnimTimeInSeconds = Time.time - startTimeInSeconds;
                float currentAnimTimeInPercent = currentAnimTimeInSeconds / animTimeInSeconds;
                doApplyAnimTimeInPercent(currentAnimTimeInPercent);
                yield return new WaitForEndOfFrame();
            } while (currentAnimTimeInSeconds < animTimeInSeconds);

            if (onComplete != null)
            {
                onComplete();
            }
        }
    }
}