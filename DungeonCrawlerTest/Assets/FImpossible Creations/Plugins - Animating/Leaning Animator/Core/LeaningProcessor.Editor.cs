#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace
{
    public partial class LeaningProcessor
    {
        public static Texture2D _TexRootSway { get { if( _texRootSway != null ) return _texRootSway; _texRootSway = Resources.Load<Texture2D>( "Leaning/RootSway" ); return _texRootSway; } }
        private static Texture2D _texRootSway = null;
        public static Texture2D _TexSpineSway { get { if( _texSpineSway != null ) return _texSpineSway; _texSpineSway = Resources.Load<Texture2D>( "Leaning/SpineSway" ); return _texSpineSway; } }
        private static Texture2D _texSpineSway = null;
        public static Texture2D _TexAutoMotion { get { if( _texAutoMotion != null ) return _texAutoMotion; _texAutoMotion = Resources.Load<Texture2D>( "Leaning/AutoMotion" ); return _texAutoMotion; } }
        private static Texture2D _texAutoMotion = null;
        public static Texture2D _TexArmsMotion { get { if( _texArmsMotion != null ) return _texArmsMotion; _texArmsMotion = Resources.Load<Texture2D>( "Leaning/ArmSway" ); return _texArmsMotion; } }
        private static Texture2D _texArmsMotion = null;

        private static bool _editor_DrawAuto = true;
        private static bool _editor_DrawGroundAlign = false;
        private static bool _editor_DrawSpineGroundAlign = false;
        private static bool _editor_DrawSide = true;
        private static bool _editor_DrawArms = false;
        private static bool _editor_DrawSpineLean = true;

        public static void Editor_DrawTweakFullGUI( SerializedProperty sp_LeaningProcessor, ref LeaningAnimator.ELeaningEditorCat drawSetup, LeaningAnimator get = null )
        {
            Color bg = GUI.backgroundColor;

            GUILayout.Space( 4 );
            EditorGUILayout.BeginHorizontal();

            if( drawSetup == LeaningAnimator.ELeaningEditorCat.Setup ) GUI.backgroundColor = Color.yellow;

            int buttWdth = 74;
            string butttitle;
            if( drawSetup == LeaningAnimator.ELeaningEditorCat.Setup )
            {
                butttitle = " Parameters";
                buttWdth = 98;
            }
            else
            {
                butttitle = " Setup";
                buttWdth = 74;
            }

            if( GUILayout.Button( new GUIContent( butttitle, FGUI_Resources.Tex_GearSetup ), FGUI_Resources.ButtonStyle, new GUILayoutOption[] { GUILayout.Width( buttWdth ), GUILayout.Height( 24 ) } ) )
            {
                if( drawSetup == LeaningAnimator.ELeaningEditorCat.Setup ) drawSetup = LeaningAnimator.ELeaningEditorCat.Leaning;
                else
                    drawSetup = LeaningAnimator.ELeaningEditorCat.Setup;
            }
            GUI.backgroundColor = bg;

            EditorGUILayout.BeginVertical( FGUI_Resources.ViewBoxStyle );
            GUILayout.Space( 2 );

            string catText = "";
            if( drawSetup == LeaningAnimator.ELeaningEditorCat.Setup ) catText = "Setup Bone References";
            else if( drawSetup == LeaningAnimator.ELeaningEditorCat.Leaning ) catText = "Body Leaning Settings";
            else if( drawSetup == LeaningAnimator.ELeaningEditorCat.Advanced ) catText = "Advanced Features";

            if( GUILayout.Button( catText, FGUI_Resources.HeaderStyle ) )
            {
                if( drawSetup == LeaningAnimator.ELeaningEditorCat.Setup ) drawSetup = LeaningAnimator.ELeaningEditorCat.Leaning;
                else
                    drawSetup = LeaningAnimator.ELeaningEditorCat.Setup;
            }

            GUILayout.Space( 1 );

            EditorGUILayout.EndVertical();

            if( get )
            {
                if( get.Parameters.SpineStart == null )
                {
                    if( GUILayout.Button( FGUI_Resources.Tex_Refresh, GUILayout.Width( 24 ), GUILayout.Height( 22 ) ) ) get.Parameters.TryAutoFindReferences( get.transform );
                }
            }

            if( drawSetup == LeaningAnimator.ELeaningEditorCat.Advanced ) GUI.backgroundColor = Color.green;
            if( GUILayout.Button( new GUIContent( FGUI_Resources.Tex_MiniMotion ), FGUI_Resources.ButtonStyle, new GUILayoutOption[] { GUILayout.Width( 28 ), GUILayout.Height( 24 ) } ) )
            {
                drawSetup = LeaningAnimator.ELeaningEditorCat.Advanced;
            }
            GUI.backgroundColor = bg;

            EditorGUILayout.EndHorizontal();

            if( drawSetup == LeaningAnimator.ELeaningEditorCat.Setup )
            {
                GUILayout.Space( 4 );
                EditorGUILayout.BeginHorizontal();
                SerializedProperty sp_UpdateMode = sp_LeaningProcessor.FindPropertyRelative( "UpdateMode" );
                EditorGUILayout.PropertyField( sp_UpdateMode ); sp_UpdateMode.Next( false );
                EditorGUIUtility.labelWidth = 68;
                //GUILayout.FlexibleSpace();
                EditorGUILayout.PropertyField( sp_UpdateMode, GUILayout.Width( 88 ) );
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                GUILayout.Space( 4 );
                SerializedProperty sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "BaseTransform" );

                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_LeaningProcessor.FindPropertyRelative( "FootsOrigin" ) );
                GUILayout.Space( 8 );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                GUILayout.Space( 5 );
                EditorGUILayout.PropertyField( sp_BaseTransform, new GUIContent( sp_BaseTransform.displayName + " (Optional)", sp_BaseTransform.tooltip ) ); sp_BaseTransform.Next( false );
                GUILayout.Space( 3 );

                EditorGUIUtility.labelWidth = 154;
                EditorGUILayout.PropertyField( sp_BaseTransform, new GUIContent( sp_BaseTransform.displayName + " (Optional)" ) ); sp_BaseTransform.Next( false );
                if( get ) if( get.Parameters.LeftUpperArm )
                    {
                        EditorGUI.indentLevel++;
                        if( get.Parameters.LeftForearm == null ) if( get.Parameters.LeftUpperArm.childCount > 0 ) get.Parameters.LeftForearm = get.Parameters.LeftUpperArm.GetChild( 0 );
                        EditorGUILayout.PropertyField( sp_BaseTransform );
                        EditorGUI.indentLevel--;
                    }

                sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform, new GUIContent( sp_BaseTransform.displayName + " (Optional)" ) ); sp_BaseTransform.Next( false );
                if( get ) if( get.Parameters.RightUpperArm )
                    {
                        EditorGUI.indentLevel++;
                        if( get.Parameters.RightForearm == null ) if( get.Parameters.RightUpperArm.childCount > 0 ) get.Parameters.RightForearm = get.Parameters.RightUpperArm.GetChild( 0 );
                        EditorGUILayout.PropertyField( sp_BaseTransform );
                        EditorGUI.indentLevel--;
                    }
                GUILayout.Space( 3 );
                EditorGUIUtility.labelWidth = 0;

                GUILayout.Space( 4 );
                EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );

                if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawAuto, 10, "►" ) + "   Control Parameters", _TexAutoMotion ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ) } ) ) { _editor_DrawAuto = !_editor_DrawAuto; }

                if( _editor_DrawAuto )
                {
                    GUILayout.Space( 6 );
                    EditorGUIUtility.labelWidth = 170;
                    sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "ObjSpeedWhenBraking" );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); /*if (sp_BaseTransform.boolValue == false) GUI.color = Color.gray; */sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    bool drawWarning = sp_BaseTransform.intValue == 2;
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );

                    //if( Application.isPlaying ) GUI.enabled = false;
                    SerializedProperty sp_aParams = sp_LeaningProcessor.FindPropertyRelative( "IsMovingAnimatorParam" );
                    drawWarning = true;
                    EditorGUILayout.PropertyField( sp_aParams, new GUIContent( "Is Accelerating Parameter", sp_aParams.tooltip ) );

                    if( !string.IsNullOrEmpty( sp_aParams.stringValue ) )
                    {
                        drawWarning = false;
                        if( get.Parameters.AccelerationDetection != EMotionDetection.CustomDetection_AutoDetectionOFF )
                        {
                            EditorGUILayout.HelpBox( "Set 'Custom Detection' to use IsAccelerating parameter!", MessageType.None );
                        }
                    }

                    sp_aParams.NextVisible( false );

                    if( drawWarning || sp_BaseTransform.boolValue == false )
                    {
                        if( !sp_BaseTransform.boolValue )
                        {
                            EditorGUILayout.PropertyField( sp_aParams );
                            if( !string.IsNullOrEmpty( sp_aParams.stringValue ) ) drawWarning = false;
                        }

                        //if( Application.isPlaying ) GUI.enabled = true;

                        if( drawWarning )
                            EditorGUILayout.HelpBox( "Modify through code 'IsCharacterGrounded' and 'IsCharacterAccelerating' by outside script", MessageType.None );
                    }

                    //if( Application.isPlaying ) GUI.enabled = false;

                    EditorGUILayout.BeginHorizontal();
                    var spFadeOff = sp_LeaningProcessor.FindPropertyRelative( "FadeOffLeaningParam" );

                    if( string.IsNullOrWhiteSpace( spFadeOff.stringValue ) )
                        EditorGUILayout.PropertyField( spFadeOff );
                    else
                    {
                        if( EditorGUIUtility.currentViewWidth < 420 )
                            EditorGUILayout.PropertyField( spFadeOff, GUILayout.MaxWidth( 210 ) );
                        else EditorGUILayout.PropertyField( spFadeOff );
                    }


                    //if( Application.isPlaying ) GUI.enabled = true;
                    if( !string.IsNullOrWhiteSpace( spFadeOff.stringValue ) )
                    {
                        spFadeOff.NextVisible( false );
                        EditorGUIUtility.labelWidth = 8;
                        EditorGUIUtility.fieldWidth = 27;
                        EditorGUILayout.PropertyField( spFadeOff, new GUIContent( " ", spFadeOff.tooltip ) );
                        EditorGUIUtility.fieldWidth = 0;
                        EditorGUIUtility.labelWidth = 0;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField( sp_BaseTransform );

                    GUI.color = bg;
                }

                EditorGUILayout.EndVertical();

            }
            else if( drawSetup == LeaningAnimator.ELeaningEditorCat.Leaning )
            {
                GUILayout.Space( 2 );

                EditorGUILayout.BeginVertical( FGUI_Resources.ViewBoxStyle ); // ----------

                Editor_MainSettings( sp_LeaningProcessor.FindPropertyRelative( "SideSwayPower" ) );
                GUILayout.Space( 3 );
                Editor_RootSwaySettings( sp_LeaningProcessor.FindPropertyRelative( "RootBlend" ) );
                GUILayout.Space( 3 );

                bool drawSpine = true;
                if( get ) if( get.Parameters.SpineStart == null && get.Parameters.SpineMiddle == null ) drawSpine = false;

                if( drawSpine )
                    Editor_SpineLeanSettings( sp_LeaningProcessor.FindPropertyRelative( "SpineBlend" ) );
                GUILayout.Space( 4 );

                bool drawArms = true;
                if( get ) if( get.Parameters.LeftUpperArm == null && get.Parameters.RightUpperArm == null ) drawArms = false;

                if( drawArms )
                    Editor_ArmsSwaySettings( sp_LeaningProcessor.FindPropertyRelative( "ArmsBlend" ) );

                GUILayout.Space( 3 );
                SerializedProperty sp_lean = sp_LeaningProcessor.FindPropertyRelative( "StartRunPush" );
                EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean );
                EditorGUILayout.EndVertical();

                GUILayout.Space( -4 );
                EditorGUILayout.EndVertical();
            }
            else if( drawSetup == LeaningAnimator.ELeaningEditorCat.Advanced )
            {

                // Ground align
                GUILayout.Space( 4 );
                EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
                SerializedProperty sp_BaseTransform;

                if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawGroundAlign, 10, "►" ) + "   Optional Ground Aligning (Experimental)" ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ) } ) ) { _editor_DrawGroundAlign = !_editor_DrawGroundAlign; }

                if( _editor_DrawGroundAlign )
                {
                    GUILayout.Space( 6 );
                    sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "GroundAlignBlend" );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); /*if (sp_BaseTransform.boolValue == false) GUI.color = Color.gray; */sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );

                    if( get != null ) if( get.Parameters._UserUseCustomRaycast )
                            EditorGUILayout.HelpBox( "Using custom raycast, below parameters are not used now", MessageType.None );

                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );

                    if( get != null ) if( !get.Parameters._UserUseCustomRaycast )
                            EditorGUILayout.HelpBox( "Check scene gizmos on character to tweak raycasting", MessageType.None );

                    GUI.color = bg;
                }

                EditorGUILayout.EndVertical();


                // Spine Ground align


                GUILayout.Space( 4 );
                EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );

                if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawSpineGroundAlign, 10, "►" ) + "   Spine Ground Aligning (Experimental)" ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ) } ) ) { _editor_DrawSpineGroundAlign = !_editor_DrawSpineGroundAlign; }

                if( _editor_DrawSpineGroundAlign )
                {
                    if( get.Parameters.GroundAlignBlend <= 0f ) EditorGUILayout.HelpBox( "Spine Ground Align requires 'Ground Aligning' enabled!", MessageType.Warning );

                    GUILayout.Space( 6 );
                    sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "SpineGroundAlign" );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); /*if (sp_BaseTransform.boolValue == false) GUI.color = Color.gray; */sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );

                    GUI.color = bg;
                }

                EditorGUILayout.EndVertical();


                // Muscles
                GUILayout.Space( 6 );

                sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "UseMuscles" );

                if( Application.isPlaying == false ) EditorGUILayout.PropertyField( sp_BaseTransform ); // Muscles
                EditorGUIUtility.labelWidth = 0;

                if( sp_BaseTransform.boolValue )
                {
                    sp_BaseTransform.Next( false );
                    EditorGUILayout.PropertyField( sp_BaseTransform );
                }

                GUILayout.Space( 3 );
                EditorGUILayout.PropertyField( sp_LeaningProcessor.FindPropertyRelative( "ResetOnUngrounded" ) );
                GUILayout.Space( 3 );
            }
        }


        /// <param name="sp_lean"> SideSwayPower parameter </param>
        public static void Editor_MainSettings( SerializedProperty sp_lean )
        {
            // Main Params ---------------------
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
            GUILayout.Space( 2 );
            EditorGUILayout.LabelField( "Main Parameters", FGUI_Resources.HeaderStyle );
            GUILayout.Space( 2 );

            EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
            EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
            EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
            EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );

            EditorGUILayout.EndVertical();
        }

        /// <param name="sp_lean"> RootBlend parameter </param>
        public static void Editor_RootSwaySettings( SerializedProperty sp_lean )
        {
            // Root Params ---------------------
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
            GUILayout.Space( 2 );

            EditorGUILayout.BeginHorizontal();
            if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawSide, 10, "►" ) + "   Root Sway", _TexRootSway ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ), GUILayout.Width( 120 ) } ) ) { _editor_DrawSide = !_editor_DrawSide; }
            EditorGUIUtility.fieldWidth = 30;
            sp_lean.floatValue = Mathf.Round( EditorGUILayout.Slider( sp_lean.floatValue * 100f, 0f, 100f ) ) / 100f; EditorGUIUtility.fieldWidth = 0;
            EditorGUILayout.LabelField( "%", GUILayout.Width( 12 ) );
            EditorGUILayout.EndHorizontal();

            if( _editor_DrawSide )
            {
                sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                //EditorGUILayout.PropertyField(sp_lean); sp_lean.Next(false); 
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                //EditorGUILayout.PropertyField(sp_lean); sp_lean.Next(false);
            }

            EditorGUIUtility.fieldWidth = 0;
            EditorGUILayout.EndVertical();
        }

        /// <param name="sp_lean"> SpineSway parameter </param>
        public static void Editor_SpineLeanSettings( SerializedProperty sp_lean )
        {
            // Spine Params ---------------------
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );

            EditorGUILayout.BeginHorizontal();
            if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawSpineLean, 10, "►" ) + "   Spine Sway", _TexSpineSway ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ), GUILayout.Width( 120 ) } ) ) { _editor_DrawSpineLean = !_editor_DrawSpineLean; }
            EditorGUIUtility.fieldWidth = 30;
            sp_lean.floatValue = Mathf.Round( EditorGUILayout.Slider( sp_lean.floatValue * 100f, 0f, 100f ) ) / 100f; EditorGUIUtility.fieldWidth = 0;
            EditorGUILayout.LabelField( "%", GUILayout.Width( 12 ) );
            EditorGUILayout.EndHorizontal();

            if( _editor_DrawSpineLean )
            {
                sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean );
            }

            EditorGUIUtility.fieldWidth = 0;
            EditorGUILayout.EndVertical();
        }



        /// <param name="sp_lean"> ArmsBlend parameter </param>
        public static void Editor_ArmsSwaySettings( SerializedProperty sp_lean )
        {
            // Root Params ---------------------
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
            GUILayout.Space( 2 );

            EditorGUILayout.BeginHorizontal();
            if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawArms, 10, "►" ) + "   Arms Sway", _TexArmsMotion ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ), GUILayout.Width( 120 ) } ) ) { _editor_DrawArms = !_editor_DrawArms; }
            EditorGUIUtility.fieldWidth = 30;
            sp_lean.floatValue = Mathf.Round( EditorGUILayout.Slider( sp_lean.floatValue * 100f, 0f, 100f ) ) / 100f; EditorGUIUtility.fieldWidth = 0;
            EditorGUILayout.LabelField( "%", GUILayout.Width( 12 ) );
            EditorGUILayout.EndHorizontal();

            if( _editor_DrawArms )
            {
                sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
                EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
            }

            EditorGUIUtility.fieldWidth = 0;
            EditorGUILayout.EndVertical();
        }

        internal void DrawGizmos()
        {
            //if (SpineStart)
            //{
            //    float len = 1f;

            //    if (BaseTransform && SpineStart)
            //        len = Vector3.Distance(SpineStart.transform.position, BaseTransform.position);

            //    Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.5f);
            //    Gizmos.DrawRay(SpineStart.transform.position, (SpineStart.transform.rotation * Quaternion.Euler(SpineForwardSway)) * Vector3.forward * len);

            //    Gizmos.color = new Color(0.2f, .2f, 0.9f, 0.4f);
            //    Vector3 tip = SpineStart.transform.position + SpineStart.transform.rotation * Vector3.forward * len;
            //    Gizmos.DrawLine(SpineStart.transform.position, tip);
            //    Gizmos.DrawLine(tip, Vector3.LerpUnclamped(SpineStart.transform.position, tip, 0.82f) + SpineStart.transform.rotation * Vector3.left * len * 0.1f);
            //    Gizmos.DrawLine(tip, Vector3.LerpUnclamped(SpineStart.transform.position, tip, 0.82f) + SpineStart.transform.rotation * Vector3.right * len * 0.1f);
            //}

            if( GroundAlignBlend > 0f || TryAutoDetectGround )
            {

                Gizmos.color = new Color( 0.2f, 1f, 0.3f, 0.5f );

                Gizmos.DrawRay( BaseTransform.position + BaseTransform.up * GroundCastLength, -BaseTransform.up * 2f * GroundCastLength );

                if( BoxCastSize > 0f )
                {
                    Vector3 dir = BaseTransform.forward * BoxCastSize;
                    Gizmos.DrawRay( BaseTransform.position - BaseTransform.up * GroundCastLength - dir * 0.5f, dir );
                    dir = BaseTransform.right * BoxCastSize;
                    Gizmos.DrawRay( BaseTransform.position - BaseTransform.up * GroundCastLength - dir * 0.5f, dir );

                }

            }
        }



        public static void Editor_Advanced( SerializedProperty sp_LeaningProcessor, LeaningProcessor get )
        {
            Color bg = GUI.color;

            // Ground align
            GUILayout.Space( 4 );
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
            SerializedProperty sp_BaseTransform;

            if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawGroundAlign, 10, "►" ) + "   Optional Ground Aligning (Experimental)" ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ) } ) ) { _editor_DrawGroundAlign = !_editor_DrawGroundAlign; }

            if( _editor_DrawGroundAlign )
            {
                GUILayout.Space( 6 );
                sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "GroundAlignBlend" );
                EditorGUILayout.PropertyField( sp_BaseTransform ); /*if (sp_BaseTransform.boolValue == false) GUI.color = Color.gray; */sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );

                if( get != null ) if( get._UserUseCustomRaycast )
                        EditorGUILayout.HelpBox( "Using custom raycast, below parameters are not used now", MessageType.None );

                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );

                if( get != null ) if( !get._UserUseCustomRaycast )
                        EditorGUILayout.HelpBox( "Check scene gizmos on character to tweak raycasting", MessageType.None );

                GUI.color = bg;
            }

            EditorGUILayout.EndVertical();


            // Spine Ground align


            GUILayout.Space( 4 );
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );

            if( GUILayout.Button( new GUIContent( " " + FGUI_Resources.GetFoldSimbol( _editor_DrawSpineGroundAlign, 10, "►" ) + "   Spine Ground Aligning (Experimental)" ), FGUI_Resources.FoldStyle, new GUILayoutOption[] { GUILayout.Height( 22 ) } ) ) { _editor_DrawSpineGroundAlign = !_editor_DrawSpineGroundAlign; }

            if( _editor_DrawSpineGroundAlign )
            {
                if( get.GroundAlignBlend <= 0f ) EditorGUILayout.HelpBox( "Spine Ground Align requires 'Ground Aligning' enabled!", MessageType.Warning );

                GUILayout.Space( 6 );
                sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "SpineGroundAlign" );
                EditorGUILayout.PropertyField( sp_BaseTransform ); /*if (sp_BaseTransform.boolValue == false) GUI.color = Color.gray; */sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform ); sp_BaseTransform.Next( false );

                GUI.color = bg;
            }

            EditorGUILayout.EndVertical();


            // Muscles
            GUILayout.Space( 6 );

            sp_BaseTransform = sp_LeaningProcessor.FindPropertyRelative( "UseMuscles" );

            if( Application.isPlaying == false ) EditorGUILayout.PropertyField( sp_BaseTransform ); // Muscles
            EditorGUIUtility.labelWidth = 0;

            if( sp_BaseTransform.boolValue )
            {
                sp_BaseTransform.Next( false );
                EditorGUILayout.PropertyField( sp_BaseTransform );
            }

        }


        public static void Editor_AllSettings( SerializedProperty sp_LeaningProcessor, LeaningProcessor get, bool spineAlways = false, bool armsAlways = false )
        {
            GUILayout.Space( 2 );

            EditorGUILayout.BeginVertical( FGUI_Resources.ViewBoxStyle ); // ----------

            Editor_MainSettings( sp_LeaningProcessor.FindPropertyRelative( "SideSwayPower" ) );
            GUILayout.Space( 3 );
            Editor_RootSwaySettings( sp_LeaningProcessor.FindPropertyRelative( "RootBlend" ) );
            GUILayout.Space( 3 );

            bool drawSpine = true;
            if( get.SpineStart == null && get.SpineMiddle == null ) drawSpine = false;

            if( drawSpine )
                Editor_SpineLeanSettings( sp_LeaningProcessor.FindPropertyRelative( "SpineBlend" ) );
            GUILayout.Space( 4 );

            bool drawArms = true;
            if( get.LeftUpperArm == null && get.RightUpperArm == null ) drawArms = false;

            if( drawArms )
                Editor_ArmsSwaySettings( sp_LeaningProcessor.FindPropertyRelative( "ArmsBlend" ) );

            GUILayout.Space( 3 );
            SerializedProperty sp_lean = sp_LeaningProcessor.FindPropertyRelative( "StartRunPush" );
            EditorGUILayout.BeginVertical( FGUI_Resources.BGInBoxStyle );
            EditorGUILayout.PropertyField( sp_lean ); sp_lean.Next( false );
            EditorGUILayout.PropertyField( sp_lean );
            EditorGUILayout.EndVertical();

            GUILayout.Space( -4 );
            EditorGUILayout.EndVertical();
        }


        public void _EditorDebugGUI()
        {
            EditorGUILayout.LabelField( "Strafe: " + Mathf.Round( targetStrafeRot ) );
            //EditorGUILayout.LabelField("Side: " + Mathf.Round(targetSideRot));
            //EditorGUILayout.LabelField("Forward: " + Mathf.Round(targetForwardRot));
        }


    }
}
#endif
