using FIMSpace.FTools;
using UnityEngine;

namespace FIMSpace
{
    public partial class LeaningProcessor
    {
        // Transforms ----------------------

        [Tooltip( "Main object of your whole character controller" )]
        public Transform BaseTransform;
        [Tooltip( "First spine bone of your character which will bend" )]
        public Transform SpineStart;
        [Tooltip( "Optional another spine bone to bend" )]
        public Transform SpineMiddle;
        [Tooltip( "Optional another bone to bend" )]
        public Transform Chest;
        [Tooltip( "Optional another bone to bend" )]
        public Transform Neck;

        [Space( 5 )]
        [Tooltip( "Optional upper arm bone which will be rotated additionally + it's child bone which should be forearm (first child)" )]
        public Transform LeftUpperArm;
        public Transform LeftForearm;
        [Tooltip( "Optional upper arm bone which will be rotated additionally + it's child bone which should be forearm (first child)" )]
        public Transform RightUpperArm;
        public Transform RightForearm;

        public void TryAutoFindReferences( Transform root )
        {
            BaseTransform = root;
            Animator a = root.GetComponentInChildren<Animator>();
            if( !a ) a = root.GetComponentInParent<Animator>();

            if( a )
                if( a.isHuman )
                {
                    SpineStart = a.GetBoneTransform( HumanBodyBones.Spine );
                    if( SpineMiddle == null ) SpineMiddle = a.GetBoneTransform( HumanBodyBones.Chest );
                    Chest = a.GetBoneTransform( HumanBodyBones.UpperChest );
                    Neck = a.GetBoneTransform( HumanBodyBones.Neck );
                    if( Neck == null ) Neck = a.GetBoneTransform( HumanBodyBones.Head );
                }

            if( Neck == null ) Neck = FTransformMethods.FindChildByNameInDepth( "neck", root );
            if( Neck == null ) Neck = FTransformMethods.FindChildByNameInDepth( "head", root );

            if( Chest == null ) if( Neck != null ) { if( Neck.parent && Neck.parent.parent ) Chest = Neck.parent.parent; else Chest = FTransformMethods.FindChildByNameInDepth( "chest", root ); } else Chest = FTransformMethods.FindChildByNameInDepth( "chest", root );
            if( SpineStart == null ) SpineStart = FTransformMethods.FindChildByNameInDepth( "spine", root );
            if( SpineMiddle == null ) { if( Chest ) SpineMiddle = Chest.transform.parent; else if( SpineStart ) if( SpineStart.childCount > 0 ) SpineMiddle = SpineStart.GetChild( 0 ); }
        }

        // Bones ----------------------

        private UniRotateBone footOriginBone;
        private UniRotateBone spineStartBone;
        private UniRotateBone spineMiddleBone;
        private UniRotateBone chestBone;
        private UniRotateBone neckBone;

        private UniRotateBone lUpperarmBone;
        private UniRotateBone rUpperarmBone;
        private UniRotateBone lForearmBone;
        private UniRotateBone rForearmBone;

        private FMuscle_Vector3 spineTargetMuscle = null;
        private FMuscle_Vector3 spineAdditionalMuscle = null;
        private FMuscle_Float rootMuscle = null;
        private FMuscle_Float lArmMuscle = null;
        private FMuscle_Float rArmMuscle = null;

        public void PrepareBones()
        {
            if( BaseTransform == null )
            {
                Debug.LogError( "[Leaning Animator] Could not initialize leaning animator because there is no BaseTransform assigned!" );
                return;
            }

            if( FootsOrigin )
            {
                footOriginBone = new UniRotateBone( FootsOrigin, BaseTransform );
            }

            if( SpineStart ) spineStartBone = new UniRotateBone( SpineStart, BaseTransform );
            if( SpineMiddle ) spineMiddleBone = new UniRotateBone( SpineMiddle, BaseTransform );
            if( Chest ) chestBone = new UniRotateBone( Chest, BaseTransform );
            if( Neck ) neckBone = new UniRotateBone( Neck, BaseTransform );

            if( LeftUpperArm )
            {
                lUpperarmBone = new UniRotateBone( LeftUpperArm, BaseTransform );
                if( LeftForearm ) lForearmBone = new UniRotateBone( LeftForearm, BaseTransform );
            }

            if( RightUpperArm )
            {
                rUpperarmBone = new UniRotateBone( RightUpperArm, BaseTransform );
                if( RightForearm ) rForearmBone = new UniRotateBone( RightForearm, BaseTransform );
            }

            PrepareMuscles();
        }

        void PrepareMuscles()
        {
            if( !UseMuscles ) return;

            if( FootsOrigin )
            {
                rootMuscle = new FMuscle_Float();
                rootMuscle.Initialize( 0f );
            }

            if( LeftUpperArm )
            {
                lArmMuscle = new FMuscle_Float();
                lArmMuscle.Initialize( 0f );
            }

            if( RightUpperArm )
            {
                rArmMuscle = new FMuscle_Float();
                rArmMuscle.Initialize( 0f );
            }

            spineTargetMuscle = new FMuscle_Vector3();
            spineTargetMuscle.Initialize( Vector3.zero );

            spineAdditionalMuscle = new FMuscle_Vector3();
            spineAdditionalMuscle.Initialize( Vector3.zero );
        }
    }
}