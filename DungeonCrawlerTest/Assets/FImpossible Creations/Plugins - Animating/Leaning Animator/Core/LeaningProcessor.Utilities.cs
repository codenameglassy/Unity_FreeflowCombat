using System.Collections;
using UnityEngine;

namespace FIMSpace
{
    public partial class LeaningProcessor
    {
        // Supporting solution for fixed animate physics mode
        //private bool lateFixedIsRunning = false;
        private bool fixedAllow = true;
        //private IEnumerator LateFixed()
        //{
        //    WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();
        //    lateFixedIsRunning = true;

        //    while (true)
        //    {
        //        yield return fixedWait;
        //        CalibrateBones();
        //        fixedAllow = true;
        //    }
        //}

        public static bool CheckIfIsNull(object o)
        {
#if UNITY_2018_1_OR_NEWER
            if (o is null) return true;
#else
            if (o == null) return true;
            if (object.ReferenceEquals(o, null)) return true;
#endif

            return false;
        }

    }
}