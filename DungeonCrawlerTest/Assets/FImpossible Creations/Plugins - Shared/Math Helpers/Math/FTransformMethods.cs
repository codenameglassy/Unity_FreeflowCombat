using UnityEngine;
using System.Collections.Generic;


namespace FIMSpace
{
    /// <summary>
    /// FM: Class with methods which can be helpful when using unity Transforms
    /// </summary>
    public static class FTransformMethods
    {
        /// <summary>
        /// Method which is searching in depth of choosed transform for other transform with choosed name
        /// </summary>
        public static Transform FindChildByNameInDepth(string name, Transform transform, bool findInDeactivated = true, string[] additionalContains = null)
        {
            /* If choosed transform is already one we are searching for */
            if (transform.name == name)
            {
                return transform;
            }

            /* Searching every transform component inside choosed transform */
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(findInDeactivated))
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                {
                    bool allow = false;

                    if (additionalContains == null || additionalContains.Length == 0) allow = true;
                    else
                        for (int i = 0; i < additionalContains.Length; i++)
                            if (child.name.ToLower().Contains(additionalContains[i].ToLower()))
                            {
                                allow = true;
                                break;
                            }

                    if (allow) return child;
                }
            }

            return null;
        }


        /// <summary>
        /// Method which finds all components of given type in all children in choosed transform
        /// </summary>
        public static List<T> FindComponentsInAllChildren<T>(Transform transformToSearchIn, bool includeInactive = false, bool tryGetMultipleOutOfSingleObject = false) where T : Component
        {
            List<T> components = new List<T>();

            foreach (T child in transformToSearchIn.GetComponents<T>())
            {
                if (child) components.Add(child);
            }

            foreach (Transform child in transformToSearchIn.GetComponentsInChildren<Transform>(includeInactive))
            {
                if (tryGetMultipleOutOfSingleObject == false)
                {
                    T component = child.GetComponent<T>();
                    if (component) if (components.Contains(component) == false) components.Add(component);
                }
                else
                {
                    foreach (T component in child.GetComponents<T>())
                    {
                        if (component) if (components.Contains(component) == false) components.Add(component);
                    }
                }
            }

            return components;
        }

        /// <summary>
        /// Method which finds component of given type in all children in choosed transform
        /// </summary>
        public static T FindComponentInAllChildren<T>(Transform transformToSearchIn) where T : Component
        {
            foreach (Transform childInDepth in transformToSearchIn.GetComponentsInChildren<Transform>())
            {
                T component = childInDepth.GetComponent<T>();
                if (component) return component;
            }

            return null;
        }

        /// <summary>
        /// Method which finds component of given type in all parents in choosed transform
        /// </summary>
        public static T FindComponentInAllParents<T>(Transform transformToSearchIn) where T : Component
        {
            Transform p = transformToSearchIn.parent;

            for (int i = 0; i < 100 /* safe limit */; i++)
            {
                T component = p.GetComponent<T>();
                if (component) return component;

                p = p.parent;
                if (p == null) return null;
            }

            return null;
        }

        /// <summary>
        /// Changing activation for all children in give transform
        /// </summary>
        public static void ChangeActiveChildrenInside(Transform parentOfThem, bool active)
        {
            for (int i = 0; i < parentOfThem.childCount; i++)
            {
                parentOfThem.GetChild(i).gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Making parents active from one transform until reach choosen transform or null
        /// </summary>
        public static void ChangeActiveThroughParentTo(Transform start, Transform end, bool active, bool changeParentsChildrenActivation = false)
        {
            start.gameObject.SetActive(active);
            Transform p = start.parent;

            for (int i = 0; i < 100 /* safe limit */; i++)
            {
                if (p == end) return;
                if (p == null) return;

                if (changeParentsChildrenActivation) ChangeActiveChildrenInside(p, active);

                p = p.parent;
            }
        }


        public static Transform GetObjectByPath( Transform root, string path )
        {
            if( root == null ) return null;

            var pathSteps = path.Split( '/' );

            Transform current = root;
            for( int i = 0; i < pathSteps.Length; i++ )
            {
                Transform target = current.Find( pathSteps[i] );
                if( target == null ) return null;
                current = target;
            }

            return current;
        }


        public static string CalculateTransformPath(Transform child, Transform root)
        {
            if( child.parent == null ) return "";
            
            string path = "";
            bool first = true;

            while(child != root)
            {
                if( child == null ) return "";

                if( first == true ) path = child.name; else path = child.name + "/" + path;
                first = false;
                
                child = child.parent;
            }

            return path;
        }

    }
}