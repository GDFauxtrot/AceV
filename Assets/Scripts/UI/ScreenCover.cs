using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;


namespace AceV
{
    public class ScreenCover : MonoBehaviour
    {
        public static ScreenCover Instance;

        public Image coverImage;
        private Coroutine fadeCoroutine;

        private Color currentColor = Color.black;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }


        private IEnumerator FadeCoroutine(float from, float to, float time, bool hideAtEnd)
        {
            float t = 0;

            while (t < time)
            {
                float desiredAlpha = Mathf.Lerp(from, to, t / time);

                SetImageColor(ColorAlpha(currentColor, desiredAlpha));

                yield return new WaitForSeconds(0f);
                t += Time.deltaTime;
            }

            if (hideAtEnd)
            {
                SetImageColor(ColorAlpha(currentColor, 0f));
            }
            else
            {
                SetImageColor(ColorAlpha(currentColor, to));
            }
            fadeCoroutine = null;
        }


        private void SetImageColor(Color color)
        {
            coverImage.color = color;
        }


        private Color ColorAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }


        private Coroutine FadeInternal(Color color, float fromAlpha, float toAlpha, float duration, bool hideAtEnd = false)
        {
            currentColor = color;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            fadeCoroutine = StartCoroutine(FadeCoroutine(fromAlpha, toAlpha, duration, hideAtEnd));
            return fadeCoroutine;
        }


        [YarnCommand]
        public static Coroutine ShowBlack(float duration)
        {
            return ScreenCover.Instance.FadeInternal(Color.black, 1f, 1f, duration, true);
        }


        [YarnCommand]
        public static Coroutine ShowWhite(float duration)
        {
            return ScreenCover.Instance.FadeInternal(Color.white, 1f, 1f, duration, true);
        }


        [YarnCommand]
        public static Coroutine FadeToBlack(float duration)
        {
            return ScreenCover.Instance.FadeInternal(Color.black, 0f, 1f, duration);
        }


        [YarnCommand]
        public static Coroutine FadeFromBlack(float duration)
        {
            return ScreenCover.Instance.FadeInternal(Color.black, 1f, 0f, duration);
        }


        [YarnCommand]
        public static Coroutine FadeToWhite(float duration)
        {
            return ScreenCover.Instance.FadeInternal(Color.white, 0f, 1f, duration);
        }


        [YarnCommand]
        public static Coroutine FadeFromWhite(float duration)
        {
            return ScreenCover.Instance.FadeInternal(Color.white, 1f, 0f, duration);
        }
    }
}
