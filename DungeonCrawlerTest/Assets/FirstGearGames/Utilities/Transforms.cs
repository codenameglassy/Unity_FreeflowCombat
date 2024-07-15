using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Utilities.Objects
{

    public static class Transforms
    {
        /// <summary>
        /// Returns the topmost parent for a transform.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Transform TopmostParent(this Transform t)
        {
            if (t.parent == null)
            {
                return t;
            }
            else
            {
                Transform result = t.parent;
                while (result.parent != null)
                    result = result.parent;

                return result;
            }
        }


        /// <summary>
        /// Destroys all children under the specified transform.
        /// </summary>
        /// <param name="t"></param>
        public static void DestroyChildren(this Transform t, bool destroyImmediately = false)
        {
            foreach (Transform child in t)
            {
                if (destroyImmediately)
                    MonoBehaviour.DestroyImmediate(child.gameObject);
                else
                    MonoBehaviour.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Gets components in children and optionally parent.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="parent"></param>
        /// <param name="includeParent"></param>
        /// <param name="includeInactive"></param>
        public static void GetComponentsInChildren<T>(Transform parent, List<T> results, bool includeParent = true, bool includeInactive = false) where T : Component
        {
            if (!includeParent)
            {
                List<T> current = new List<T>();
                for (int i = 0; i < parent.childCount; i++)
                {
                    parent.GetChild(i).GetComponentsInChildren(includeInactive, current);
                    results.AddRange(current);
                }
            }
            else
            {
                parent.GetComponentsInChildren(includeInactive, results);
            }
        }

    }



}
