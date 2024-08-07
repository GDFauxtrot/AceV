using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AceV
{
    public static class Utils
    {
        public static void RunFunctionDelayed(float time, Action funcToRun)
        {
            GameManager.Instance.StartCoroutine(RunFunctionDelayedCoroutine(time, funcToRun));
        }

        private static IEnumerator RunFunctionDelayedCoroutine(float time, Action funcToRun)
        {
            yield return new WaitForSeconds(time);
            funcToRun.Invoke();
        }
    }
}