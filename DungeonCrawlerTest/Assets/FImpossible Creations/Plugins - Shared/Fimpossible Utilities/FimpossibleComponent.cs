using UnityEngine;

namespace FIMSpace
{
    /// <summary>
    /// Base class for fimpossible scripts, inheriting smaller inspector window and RMB context, displaying items for store page
    /// </summary>
    public abstract class FimpossibleComponent : MonoBehaviour
    {
        public virtual void OnValidate()
        {
#if UNITY_EDITOR
            // Hide gizmo icon if used
            if( _wasIconEnabled ) return;
            FSceneIcons.SetGizmoIconEnabled( this, false );
            _wasIconEnabled = false;
#endif
        }

        #region Editor Class

        public virtual string HeaderInfo
        { get { return ""; } }

#if UNITY_EDITOR

        private bool _wasIconEnabled = false;

        public virtual UnityEditor.MessageType HeaderInfoType => UnityEditor.MessageType.None;

        [UnityEditor.MenuItem( "CONTEXT/FimpossibleComponent/Go To Fimpossible Creations Store Page", false, 10000 )]
        private static void OpenFimpossibleStorePage( UnityEditor.MenuCommand menuCommand )
        {
            Application.OpenURL( "https://assetstore.unity.com/publishers/37262" );
        }

        [UnityEditor.MenuItem( "CONTEXT/FimpossibleComponent/Go To Fimpossible Creations Discord Server", false, 10001 )]
        private static void OpenFimpossibleDiscord( UnityEditor.MenuCommand menuCommand )
        {
            Application.OpenURL( "https://discord.gg/Y3WrzQp" );
        }


        [UnityEditor.MenuItem( "CONTEXT/FimpossibleComponent/Tutorials on the Youtube Channel", false, 10002 )]
        private static void OpenFimpossibleYoutubeChannel( UnityEditor.MenuCommand menuCommand )
        {
            Application.OpenURL( "https://www.youtube.com/@FImpossibleCreations" );
        }


        [UnityEditor.CanEditMultipleObjects]
        [UnityEditor.CustomEditor( typeof( FimpossibleComponent ), true )]
        public class FimpossibleComponentEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                FimpossibleComponent get = serializedObject.targetObject as FimpossibleComponent;
                if( get.HeaderInfo != "" )
                {
                    UnityEditor.EditorGUILayout.HelpBox( get.HeaderInfo, get.HeaderInfoType );
                }

                serializedObject.Update();

                GUILayout.Space( 4f );
                DrawPropertiesExcluding( serializedObject, "m_Script" );

                serializedObject.ApplyModifiedProperties();
            }
        }

#endif

        #endregion Editor Class
    }
}