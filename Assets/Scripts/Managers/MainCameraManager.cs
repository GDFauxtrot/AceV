using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace AceV
{
    public class MainCameraManager : MonoBehaviour
    {
        public static MainCameraManager Instance;

        private Coroutine shakeCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        [YarnCommand]
        public static void ShakeScreen(float intensity, float duration, float falloff)
        {
            MainCameraManager.Instance.ShakeScreenInternal(intensity, duration, falloff);
        }


        private void ShakeScreenInternal(float intensity, float duration, float falloff)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration, falloff));
        }


        private IEnumerator ShakeCoroutine(float intensity, float duration, float falloff)
        {
            float timer = 0f;
            while (timer < duration)
            {
                Vector3 offset = GetShakeOffset(intensity);
                transform.position = new Vector3(offset.x, offset.y, transform.position.z);
                timer += Time.deltaTime;
                yield return new WaitForSeconds(0f);
            }
            timer = 0f;
            while (timer < falloff)
            {
                float falloffScale = Mathf.Lerp(1f, 0f, timer / falloff);
                Vector3 offset = GetShakeOffset(intensity * falloffScale);
                transform.position = new Vector3(offset.x, offset.y, transform.position.z);
                timer += Time.deltaTime;
                yield return new WaitForSeconds(0f);
            }
            transform.position = new Vector3(0f, 0f, transform.position.z);
        }


        private Vector3 GetShakeOffset(float intensity)
        {
            float damp = 0.1f;
            return new Vector3(
                Random.Range(-intensity, intensity) * damp,
                Random.Range(-intensity, intensity) * damp,
                0f);
        }
    }
}
