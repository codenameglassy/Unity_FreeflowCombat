using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HoaxGames
{
    // Copyright © Kreshnik Halili

    public class FootIK : MonoBehaviour
    {
        [System.Serializable] public class AnimatorStateEntry { public int layer; public string animatorState; }

        [Header("INFO: Please activate IK pass in the Animator Controller (base layer recommended)")]
        [Header("NOTE 1: Please make sure you don't use BoxColliders with Size near ~0 in any axis because it breaks BoxCasts.")]
        [Header("NOTE 2: It is recommended to set \"Compute Ik Hint Position Strategy\" to \"BONE_BASED\" when used with Animation-Layer-Blending and Avatar Masks)")]

        [Header("General")]
        [SerializeField, Tooltip("Please activate the animator IK pass on a specific layer - typically it is most likely best to activate it for the base layer. This is an requirement for getting OnAnimatorIK called on this script (otherwise the script is not going to work). This variable defines the index of the specific layer where the IK pass is activated and will be used by the script.")]
        protected int m_animatorIkPassLayerIndex = 0; public int animatorIkPassLayerIndex { get { return m_animatorIkPassLayerIndex; } }
        [SerializeField, Tooltip("The Collision-Layer for foot and body placement detection.")]
        protected LayerMask m_collisionLayerMask = 1 << 0; public LayerMask collisionLayerMask { get { return m_collisionLayerMask; } set { m_collisionLayerMask = value; } }
        [SerializeField, Tooltip("The collision-setting for trigger-interactions for foot and body placement detection. Default setting is 'Ignore'.")]
        protected QueryTriggerInteraction m_triggerCollisionInteraction = QueryTriggerInteraction.Ignore; public QueryTriggerInteraction triggerCollisionInteraction { get { return m_triggerCollisionInteraction; } set { m_triggerCollisionInteraction = value; } }
        [SerializeField, Range(0.1f, 10.0f), Tooltip("The speed at which the positions and rotations interpolate to the respective default values inbetween valid / grounded and invalid / not grounded state changes.")]
        protected float m_resetToDefaultValuesSpeed = 1.0f;
        [SerializeField, Tooltip("This is a special behaviour that takes into consideration that the character controller is crouching. This behaviour adapts both feet and body placement accordingly to this condition.")]
        protected bool m_activateCrouchSpecificBehaviour = false;
        [SerializeField, Range(0.0f, 1.0f), Tooltip("A value of 0 means that your character has no correction tolerance for feet IKs when he crouches therefore he balances the computed IK-corrections using his body position. A value of 1 means 100% tolerance and basically disables the crouching specific behaviour.")]
        protected float m_crouchCorrectionTolerance = 0.0f;
        [SerializeField, Tooltip("A list of all animator states (in the animator controller), that will force activate the crouching behaviour when they are active. This is a very handy feature to activate the crouching specific behaviour when your character starts crouching without writing a line of code. Layer == animator layer. Animator state == animator state name.")]
        protected List<AnimatorStateEntry> m_forceActivateCrouchingBehaviour = new List<AnimatorStateEntry>();
        [SerializeField, Tooltip("A list of all animator states (in the animator controller), that will disable this effect when they are active. Layer == animator layer. Animator state == animator state name.")]
        protected List<AnimatorStateEntry> m_forceInvalidOnAnimatorStates = new List<AnimatorStateEntry>();
        [SerializeField, Tooltip("This parameter shifts the characters whole body and IK positions along the characters local Up-axis. Use this to correct root position offsets between feets and root position \"globally\". The same can be done per animation file under the corresponding Animation import-settings tab by the Offset variable of Root Transform Position (Y).")]
        protected float m_globalCharacterYOffset = 0.0f; public float globalCharacterYOffset { get { return m_globalCharacterYOffset; } set { m_globalCharacterYOffset = value; } }
        [SerializeField, Tooltip("If false, Global Character Offset won't affect Hand IKs and Hand Hint IKs. If true, it will apply the Global Character Y Offset to those IKs as well, however it won't affect their weight. If you use custom HandIKs for your character this setting may be relevant to the overall behaviour.")]
        protected bool m_applyGlobalCharacterYOffsetToOtherIKsToo = true;

        [Header("Foot IK placement")]
        [SerializeField, Range(0, 1), Tooltip("The strength that the effects will be applyed to.")]
        protected float m_ikPlacementWeight = 1;
        [SerializeField, Range(1, 50), Tooltip("This value is used to smoothly lerp IK positions. The higher the value, the stiffer the transition. The smaller the value, the smoother the transition.")]
        protected float m_ikPlacementStiffness = 8.0f;
        [SerializeField, Range(1, 50), Tooltip("This value is used to smoothly slerp IK rotations. The higher the value, the stiffer the transition. The smaller the value, the smoother the transition.")]
        protected float m_ikRotationStiffness = 8.0f;
        [SerializeField, Tooltip("With this parameter you can choose between different interpolation methods for IK foot placement.")]
        protected InterpolationMethod m_ikInterpolationMethod = InterpolationMethod.LERP;
        [SerializeField, Range(0, 5), Tooltip("The IK upwards extrapolation defines how much the feet overshoots when trying to achive the IK target position. Upwards in this context means that the player moves upwards. A value > 0 will lead to a more robust and solid transition. A too high value may lead to weird looking transitions.")]
        protected float m_ikUpwardsExtrapolation = 1.0f;
        [SerializeField, Range(0, 10), Tooltip("With this parameter you can improve the behaviour when going up on slopes. A higher value will pull the feet faster to the ground when they are in the air. A too high value may cause too much stretching on the legs when going upwards.")]
        protected float m_ikUpwardsPullBackDownExtrapolation = 0.0f; public float ikUpwardsPullBackDownExtrapolation { get { return m_ikUpwardsPullBackDownExtrapolation; } set { m_ikUpwardsPullBackDownExtrapolation = value; } }
        [SerializeField, Range(0, 10), Tooltip("The IK downwards extrapolation defines how much the feet overshoots when trying to achive the IK target position. Downwards in this context means that the player moves downwards. A value > 0 will lead to a more robust and solid transition. A too high value may cause minor vibrations.")]
        protected float m_ikDownwardsExtrapolation = 2.5f;
        [SerializeField, Range(0, 1), Tooltip("A value of 1 will use the foots length and height parameters and adapt them accordingly depending on the internal rotation of the foot.")]
        protected float m_automaticFootRotationBasedHeightAdaption = 0.7f;
        [SerializeField, Tooltip("The maximum IK correction for placing the feet.")]
        protected float m_ikMaxCorrection = 0.5f; public float ikMaxCorrection { get { return m_ikMaxCorrection; } set { m_ikMaxCorrection = value; } }
        [SerializeField, Tooltip("An additional raycast margin to find the correct IK position.")]
        protected float m_ikCorrectionRaycastMargin = 0.05f; public float ikCorrectionRaycastMargin { get { return m_ikCorrectionRaycastMargin; } set { m_ikCorrectionRaycastMargin = value; } }
        [SerializeField, Tooltip("This value defines the distance between IK position and the foot ground-point. It's like the height of the foot. This value is not allowed to be zero or smaller zero.")]
        protected float m_ikFootHeight = 0.1f;
        [SerializeField, Tooltip("The IK foot size defines the length of the foot used for the simulation model. This value is not allowed to be zero or smaller zero.")]
        protected float m_ikFootLength = 0.24f;
        [SerializeField, Tooltip("The IK foot width defines the width of the foot used for the simulation model. This value is not allowed to be zero or smaller zero.")]
        protected float m_ikFootWidth = 0.085f;
        [SerializeField, Tooltip("The IK foot forward bias defines the offset for the foots raycast origin in relation to the given IK position")]
        protected float m_ikFootForwardBias = 0.065f;
        [SerializeField, Range(0, 90), Tooltip("With this parameter you can clamp the maximum allowed ankle adaption angle.")]
        protected float m_maxAnkleRotationAdaptionAngle = 90.0f;
        [SerializeField, Range(0, 250), Tooltip("This is an advanced setting with which intersections, when the feet move downwards, are stopped. A low value will let the feet bump inside the ground for a short period of time.")]
        protected float m_antiDownwardsIntersectionStiffness = 50.0f;
        [SerializeField, Range(0, 90), Tooltip("This is an advanced setting where if this value is exceeded, the detected ground won't be handled as detected anymore.")]
        protected float m_maxGroundAngleAtWhichTheGroundIsDetectedAsGround = 81.0f; public float maxGroundAngleAtWhichTheGroundIsDetectedAsGround { get { return m_maxGroundAngleAtWhichTheGroundIsDetectedAsGround; } set { m_maxGroundAngleAtWhichTheGroundIsDetectedAsGround = value; } }
        [SerializeField, Range(0, 90), Tooltip("This is an advanced setting with which you can setup the maximum allowed angle the feet will adapt to.")]
        protected float m_maxGroundAngleTheFeetsCanAdaptTo = 79.0f; public float maxGroundAngleTheFeetsCanAdaptTo { get { return m_maxGroundAngleTheFeetsCanAdaptTo; } set { m_maxGroundAngleTheFeetsCanAdaptTo = value; } }
        [SerializeField, Range(0, 0.15f), Tooltip("This is an advanced setting with which you can set the minimum threshold required of the IK to move before a new IK position will be calculated.")]
        protected float m_minimumMovementThreshold = 0.003f;
        [SerializeField, Tooltip("This is an advanced setting. When enabled, iStep will check if the underneath ground moved. If the underneath ground moved, iStep will ignore minimum movement threshold and force calculate a new IK position.")]
        protected bool m_accountMinimumMovementThresholdForMovingGroundObjects = true;
        [SerializeField, Range(0.0f, 1.0f), Tooltip("This value defines when to start gluing the feets (applying ground-slope rotation to the feet) to the ground in relation to the distance to the current and target feet positions.")]
        protected float m_isGluedToGroundNormalizedThreshold = 0.7f;
        [SerializeField, Tooltip("This value defines when to start gluing the feets (applying ground-slope rotation to the feet) to the ground based on the distance of the feet from the ground.")]
        protected float m_isGluedToGroundDistanceThreshold = 0.055f;
        [SerializeField, Tooltip("If this value is true, rotations will be \"unlocked\" to their original rotation when not glued to the ground. If this value is false the foot rotation will try to adapt to the ground whenever grounded. The recommended value for having better transitions is typically true. [Default: true]")]
        protected bool m_onlyAdaptRotationWhenGluedToGround = true;
        [SerializeField, Tooltip("IK_HINT_POSITION_BASED takes the given IK Hint Positions through animator.GetIKHintPosition(..) and calculates the new adapted hint positions based on the given value. BONE_BASED uses the given bone-structure and direct bone values instead to calculate the correct hint positions. When using multiple layers + Avatar Masks, BONE_BASED could be the preferred value to use because Unity has difficulties giving correctly blended IK hint position values when using multiple layers + masks.")]
        protected IkHintComputationStrategy m_computeIkHintPositionStrategy = IkHintComputationStrategy.IK_HINT_POSITION_BASED; public IkHintComputationStrategy computeIkHintPositionStrategy { get { return m_computeIkHintPositionStrategy; } set { m_computeIkHintPositionStrategy = value; } }

        [Header("Body placement")]
        [SerializeField, Range(0, 1), Tooltip("The strength that the effects will be applyed to.")]
        protected float m_bodyPlacementWeight = 1; public float bodyPlacementWeight { get { return m_bodyPlacementWeight; } set { m_bodyPlacementWeight = value; } }
        [SerializeField, Range(1, 50), Tooltip("This value is used to smoothly lerp body position transitions. The higher the value, the stiffer the transition. The smaller the value, the smoother the transition.")]
        protected float m_bodyStiffness = 8.0f;
        [SerializeField, Tooltip("With this parameter you can choose between different interpolation methods for body position placement.")]
        protected InterpolationMethod m_bodyPositionInterpolationMethod = InterpolationMethod.LERP;
        [SerializeField, Tooltip("The maximum possible body position correction.")]
        protected float m_bodyPositionMaxCorrection = 0.7f; public float bodyPositionMaxCorrection { get { return m_bodyPositionMaxCorrection; } set { m_bodyPositionMaxCorrection = value; } }
        [SerializeField, Range(0, 0.3f), Tooltip("The tolerance is used to avoid stuttering on edge-cases where your characters position-height is at about the same distance away from the adjacent ground as defined by BodyPositionMaxCorrection. A tolerance value of 0 would in the explained edge-case lead to trying to correctly place and adapt the body position in one frame and avoid placing the body position in the next frame - causing stuttering. By increasing the tolerance value you increase the buffer used to avoid stuttering. However the higher the value the less likely it is that your character will start adapting its body position in regard to BodyPositionMaxCorrection since the tolerance is uses subtractive.")]
        protected float m_bodyPositionMaxCorrectionTolerance = 0.05f;
        [SerializeField, Range(0, 1), Tooltip("The strength at which the maximum possible body position that can be corrected will increase with the planar forwrad distance of the left and right feet.")]
        protected float m_increaseBodyPositionMaxCorrectionWithForwardFootDistance = 0.0f;
        [SerializeField, Tooltip("If enabled and the collision layers for FootIK for example differs from your character controllers collision layers, iStep will adapt the body position upwards as well if necessary to prevent bending over. [Default: true]")]
        protected bool m_canBodyPositionMoveHigherThanGroundedPosition = true;
        [SerializeField, Tooltip("This parameter applys an offset in the Up-axis to the bodys center of mass position. This is similar to GlobalCharacterYOffset however this value doesn't shift the whole body by affecting the IKs in addition like GlobalCharacterYOffset does. Therefore use this value for cool, dynamic special effects, like crouching or simple shifts, however use GlobalCharacterYOffset to fix feet-zero-position offsets with your animations and character. If this parameter is used for crouching or dynamic crouch-like behaviours you may think about activating ActivateCrouchSpecificBehaviour additionally.")]
        protected float m_bodyPositionOffset = 0.0f; public float bodyPositionOffset { get { return m_bodyPositionOffset; } set { m_bodyPositionOffset = value; } }

        [Header("Grounded-Check")]
        [SerializeField, Tooltip("The maximum raydistance to check for being grounded.")]
        protected float m_checkGroundedDistance = 0.7f; public float checkGroundedDistance { get { return m_checkGroundedDistance; } set { m_checkGroundedDistance = value; } }
        [SerializeField, Tooltip("The radius used to check if grounded.")]
        protected float m_checkGroundedRadius = 0.20f; public float checkGroundedRadius { get { return m_checkGroundedRadius; } set { m_checkGroundedRadius = value; } }
        [SerializeField, Tooltip("Automatically unground when the upwards-velocity exceeds the given value. If the value is < 0 (for example -1), this effect is disabled. Be cautios when using this effect because moving up-stairs and up-hill may trigger this effect even if you don't want or don't expect it.")]
        protected float m_autoUngroundUpwardsVelocity = -1; public float autoUngroundUpwardsVelocity { get { return m_autoUngroundUpwardsVelocity; } set { m_autoUngroundUpwardsVelocity = value; } }

        [Header("Slope-based spine bending/leaning")]
        [SerializeField, Range(-100, 100), Tooltip("Using leaning is not fully supported when used with Animation Rigging! This parameter defines how much the characters spine should bend forward when running up on slopes.")]
        protected float m_slopeBendingUpwardsStrength = 0.0f;
        [SerializeField, Range(-100, 100), Tooltip("Using leaning is not fully supported when used with Animation Rigging! This parameter defines how much the characters spine should bend backward when running down on slopes.")]
        protected float m_slopeBendingDownwardsStrength = 0.0f;
        [SerializeField, Range(0.2f, 20.0f), Tooltip("This value is used to smoothly lerp slope-bending transitions. The higher the value, the stiffer the transition. The smaller the value, the smoother the transition.")]
        protected float m_slopeBendingTransitionStiffness = 10.0f;
        [SerializeField, Range(1, 50), Tooltip("This value defines the number of slope-bending values to calculate a running average. It is synced with the FixedUpdate. If your FixedUpdate-Rate is set to the default 50, a queue count value of 20 would lead to using the running average of the last 20/50 secs --> 0.4 seconds. This value is used to smooth spiky terrain-surfaces or staircase-like steps your character has to take. A too low value leads to spiky transitions. A too high value leads to unresponsive transitions.")]
        protected int m_averageSlopeLeaningFixedQueueCount = 20;
        [SerializeField, Range(0, 90), Tooltip("This parameter defines the maximum absolute value (in degree) slope-based spine bending can reach. It basically clamps the bending values.")]
        protected float m_maxAbsoluteSlopeSpineBending = 90.0f;
        [SerializeField, Tooltip("Select your prefered slope-based bending behaviour with this setting.")]
        protected SlopeLeaningType m_slopeLeaningType = SlopeLeaningType.SURFACE_ANGLE_DETECTED_BY_GROUNDED_CHECK;
        [SerializeField, Tooltip("If enabled the characters shoulders will counteract the spine-bending. If you use custom HandIKs for your character this setting may be relevant to the overall behaviour.")]
        protected bool m_shouldersCounteractSpineBending = true;
        [SerializeField, Tooltip("If enabled velocity will be forced to 5 independent from the real characters velocity making the effect being played whenever on slopes indipendent from movement.")]
        protected bool m_ignoreVelocityDependency = false;
        [SerializeField]
        protected Axis m_relativeSpineRotationAxisAffectedBySlopeLeaning = Axis.X_AXIS;

        [Header("Full-body exaggerated leaning")]
        [SerializeField, Range(0, 20), Tooltip("Using leaning is not fully supported when used with Animation Rigging! This parameter defines how much the characters body should lean towards the moving direction when moving. This affects the whole body from the bottom of the feet to the top of the head and is always applied when the character moves.")]
        protected float m_moveLeaningStrength = 0.0f;
        [SerializeField, Range(0, 2), Tooltip("Using leaning is not fully supported when used with Animation Rigging! This parameter defines how much the characters body should kneel when leaning towards the moving direction when moving. This effect is only applied if MoveLeaningStrength is bigger 0.01 and the character simultaneously moves.")]
        protected float m_moveLeaningKneelFactor = 0.0f;
        [SerializeField, Range(0, 40), Tooltip("Using leaning is not fully supported when used with Animation Rigging! This parameter defines how much the characters spine should lean towards the moving direction when moving and is always applied when the character moves.")]
        protected float m_moveSpineLeaningStrength = 0.0f;
        [SerializeField, Range(1, 100), Tooltip("This value is used to smoothly lerp move-leaning transitions. The higher the value, the stiffer the transition. The smaller the value, the smoother the transition.")]
        protected float m_moveLeaningStiffness = 12.0f;
        [SerializeField, Range(0, 1), Tooltip("With this value you can change how much the leaning is applied as start-stop behaviour instead of a continued leaning.")]
        protected float m_moveLeaningIsStartStopPercentage = 0.0f;
        [SerializeField, Range(1, 100), Tooltip("The higher the value the longer the Start leaning will be.")]
        protected float m_moveLeaningIsStartStopStartDurationStrength = 20.0f;
        [SerializeField, Range(1, 100), Tooltip("The higher the value the longer the Stop leaning transition will take until it finishs.")]
        protected float m_moveLeaningIsStartStopStopDurationStrength = 15f;
        [SerializeField, Tooltip("Leans when the actor is grounded - like walking and running. [Default: true]")]
        protected bool m_moveLeanWhenGrounded = true;
        [SerializeField, Tooltip("Leans when the actor is not grounded - like jumping or flying. [Default: true]")]
        protected bool m_moveLeanWhenNotGrounded = true;
        [SerializeField, Tooltip("Flips the left/right leaning direction. [Default: false]")]
        protected bool m_flipRightMoveLean = false;
        [SerializeField, Tooltip("Flips the forward/backward leaning direction. [Default: false]")]
        protected bool m_flipForwardMoveLean = false;

        [Header("Footstep Events")]
        [SerializeField, Tooltip("The distance from foot to ground used to try to trigger a footstep event in regard to the characters up-axis.")]
        protected float m_eventCheckResetDistance = 0.05f; public float eventCheckResetDistance { get { return m_eventCheckResetDistance; } set { m_eventCheckResetDistance = value; } }
        [SerializeField, Tooltip("The step length used to fire step events.")]
        protected float m_stepLength = 0.7f; public float stepLength { get { return m_stepLength; } set { m_stepLength = value; } }
        [SerializeField, Tooltip("A delay that can be applied for when a footstep event is being recognised and when it is being triggered.")]
        protected float m_footstepEventDelay = 0.0f; public float footstepEventDelay { get { return m_footstepEventDelay; } set { m_footstepEventDelay = value; } }
        [Space]
        [SerializeField, Tooltip("Whenever the left foot steps on the ground")]
        protected IKResultEvent m_onFootstepLeftStart; public IKResultEvent onFootstepLeftStart { get { return m_onFootstepLeftStart; } }
        [SerializeField, Tooltip("Whenever the right foot steps on the ground")]
        protected IKResultEvent m_onFootstepRightStart; public IKResultEvent onFootstepRightStart { get { return m_onFootstepRightStart; } }
        [SerializeField, Tooltip("Whenever the left foot stops stepping on the ground")]
        protected UnityEvent m_onFootstepLeftStop; public UnityEvent onFootstepLeftStop { get { return m_onFootstepLeftStop; } }
        [SerializeField, Tooltip("Whenever the right foot stops stepping on the ground")]
        protected UnityEvent m_onFootstepRightStop; public UnityEvent onFootstepRightStop { get { return m_onFootstepRightStop; } }
        [SerializeField, Tooltip("Fires whenever the grounded state is entered. Depends on if grounded check is enabled.")]
        protected GroundedResultEvent m_onGroundedEntered; public GroundedResultEvent onGroundedEntered { get { return m_onGroundedEntered; } }
        [SerializeField, Tooltip("Fires whenever the grounded state is leaved. Depends on if grounded check is enabled.")]
        protected UnityEvent m_onGroundedExited; public UnityEvent onGroundedExited { get { return m_onGroundedExited; } }

        [Header("Debugging")]
        [SerializeField] protected bool m_isGrounded = false;
        [SerializeField] protected float m_currBodyPositionOffset;
        [SerializeField] protected float m_maxBodyPositionOffset;
        [SerializeField] protected float m_currIkCorrectionOffset;
        [SerializeField] protected float m_maxIkCorrectionOffset;
        [SerializeField] protected float m_upwardsVelocity;
        [SerializeField] protected float m_maxUpwardsVelocity;

        protected bool m_isWaitingForPlayingLeftFootEvent = true;
        protected bool m_isWaitingForPlayingRightFootEvent = true;
        protected Vector3 m_lastRightIKStepEventPos;
        protected Vector3 m_lastLeftIKStepEventPos;

        protected Animator m_animator;
        protected Transform m_transform; public Transform targetTransform { get { return m_transform; } set { m_transform = value; } }

        protected Vector3 m_prevTransformPosition;
        protected Vector3 m_prevBodyPosition;
        protected Vector3 m_bodyOffset; public Vector3 bodyOffset { get { return m_bodyOffset; } } public Vector3 fullBodyOffset { get { return m_globalCharacterYOffset * m_transform.up + (m_bodyOffset + m_bodyPositionOffset * m_transform.up) * m_bodyPlacementWeight * m_bodyResetLerp + m_crouchOffset; } }
        protected Vector3 m_rawbodyOffsetVec;
        protected Vector3 m_ikLeftOffset;
        protected Vector3 m_ikRightOffset;
        protected Quaternion m_ikLeftDeltaRotation;
        protected Quaternion m_ikRightDeltaRotation;

        protected Vector3 m_ikLeftOffsetSmoothDampVelocity;
        protected Vector3 m_ikRightOffsetSmoothDampVelocity;
        protected Vector3 m_ikBodyPositionSmoothDampVelocity;

        protected float m_feetResetLerp = 0;
        protected float m_bodyResetLerp = 0;

        protected Vector3 m_rawVelocityPlanarMoveLean = Vector3.zero;
        protected Vector3 m_velocityPlanarMoveLean = Vector3.zero;
        protected Vector3 m_velocityPlanarSlopeLean = Vector3.zero;

        protected Vector3 m_extrapolationLeft = Vector3.zero;
        protected Vector3 m_extrapolationRight = Vector3.zero;

        protected float m_ikFootHeightToUseLeft;
        protected float m_ikFootHeightToUseRight;
        protected float m_ikFootLengthToUseLeft;
        protected float m_ikFootLengthToUseRight;

        protected float m_currSlopeLeaning = 0.0f;
        protected Queue<float> m_averageSlopeLeaningValuesQueue = new Queue<float>();
        protected bool m_updateSlopeLeaning = true;

        protected IKResult m_resultLeft;
        protected IKResult m_resultRight;

        protected Vector3 m_resultLeftLastTransformPosition;
        protected Quaternion m_resultLeftLastTransformRotation;
        protected Vector3 m_resultRightLastTransformPosition;
        protected Quaternion m_resultRightLastTransformRotation;

        protected Transform m_spineTransform;
        protected Transform m_leftShoulderTransform;
        protected Transform m_rightShoulderTransform;
        protected Transform m_leftUpperLeg;
        protected Transform m_rightUpperLeg;
        protected Transform m_leftLowerLeg;
        protected Transform m_rightLowerLeg;
        protected Transform m_leftFoot;
        protected Transform m_rightFoot;

        protected Vector3 m_crouchOffset; public Vector3 crouchOffset { get { return m_crouchOffset; } }

        protected Vector3 m_prevIkLeft;
        protected Vector3 m_prevIkRight;

        protected Vector3 m_lastFootstepPosLeft;
        protected Vector3 m_lastFootstepPosRight;
        protected float m_elapsedFootstepLeftDistance;
        protected float m_elapsedFootstepRightDistance;
        protected bool m_lastEventFiredWasLeft = true;
        protected bool m_ignoreLastEventFiredWasLeft = true;
        protected bool m_canIgnoreLastEventFiredWasLeft = true;

        protected float m_prevIkFootHeight;
        protected float m_prevIkFootForwardBias;
        protected float m_prevIkMaxCorrection;
        protected float m_prevIkFootLength;
        protected float m_prevIkFootWidth;
        protected float m_prevBodyPositionMaxCorrection;
        protected float m_prevCheckGroundedDistance;
        protected LayerMask m_prevCollisionLayerMask;

        protected GroundedResult m_groundedResult = new GroundedResult(false, false, null, Vector3.zero, Vector3.zero, Vector3.zero); public GroundedResult getGroundedResult() { return m_groundedResult; }

        protected Transform m_groundedResultLastTransform;
        protected Vector3 m_groundedResultLastTransformPosition;
        protected Quaternion m_groundedResultLastTransformRotation;

        protected ValidationType m_validationType = ValidationType.CHECK_IS_GROUNDED;

        protected Axis m_leftAxisHint;
        protected bool m_isLeftAxisHintNegative;
        protected Axis m_rightAxisHint;
        protected bool m_isRightAxisHintNegative;

        protected List<KeyValuePair<int, int>> m_deactiveOnAnimatorState = new List<KeyValuePair<int, int>>();
        protected List<KeyValuePair<int, int>> m_activateCrouchingOnAnimatorState = new List<KeyValuePair<int, int>>();

        protected Vector3 m_characterExternalMovingPlatformOffsetVec = Vector3.zero; public Vector3 characterExternalMovingPlatformOffsetVec { get { return m_characterExternalMovingPlatformOffsetVec; } set { m_characterExternalMovingPlatformOffsetVec = value; } }

        protected float m_prevFootstepEventDelay = 0;
        protected WaitForSeconds m_footstepEventWaitForSeconds;


        public enum IkHintComputationStrategy
        {
            IK_HINT_POSITION_BASED,
            BONE_BASED,
            BONE_BASED_WITH_FEET_DIRECTION_INFLUENCE
        }

        public enum InterpolationMethod
        {
            LERP,
            SMOOTH_DAMP
        }

        public enum SlopeLeaningType
        {
            DISABLED,
            SURFACE_ANGLE_DETECTED_BY_GROUNDED_CHECK,
            AVERAGE_SURFACE_ANGLE_DETECTED_BY_FEETS,
            HEIGHT_OFFSET_DETECTED_BY_FEETS
        }

        public enum Axis
        {
            X_AXIS,
            Y_AXIS,
            Z_AXIS
        }

        public enum ValidationType
        {
            CHECK_IS_GROUNDED,
            FORCE_VALID,
            FORCE_INVALID
        }

        public virtual InterpolationMethod getIkInterpolationMethod() { return m_ikInterpolationMethod; }
        public virtual void setIkInterpolationMethod(InterpolationMethod method) { m_ikInterpolationMethod = method; }
        public virtual InterpolationMethod getBodyPositionInterpolationMethod() { return m_bodyPositionInterpolationMethod; }
        public virtual void setBodyPositionInterpolationMethod(InterpolationMethod method) { m_bodyPositionInterpolationMethod = method; }

        public virtual void setInterpolationMethod(int method)
        {
            m_ikInterpolationMethod = (InterpolationMethod)method;
            m_bodyPositionInterpolationMethod = (InterpolationMethod)method;
        }

        public virtual float getMinimumMovementThreshold() { return m_minimumMovementThreshold; }

        public virtual void setMinimumMovementThreshold(float value)
        {
            m_minimumMovementThreshold = value;
        }

        public virtual void setBodyAndIkStiffnessAlltogether(float value)
        {
            m_ikPlacementStiffness = value;
            m_ikRotationStiffness = value;
            m_bodyStiffness = value;
        }

        public virtual void setIsGluedToGroundDistanceThreshold(float value)
        {
            m_isGluedToGroundDistanceThreshold = value;
        }

        public virtual float getIsGluedToGroundDistanceThreshold() { return m_isGluedToGroundDistanceThreshold; }

        public virtual void setAntiDownwardsIntersectionStiffness(float value)
        {
            m_antiDownwardsIntersectionStiffness = value;
        }

        public virtual float getAntiDownwardsIntersectionStiffness() { return m_antiDownwardsIntersectionStiffness; }

        public virtual void setIkRotationStiffness(float value)
        {
            m_ikRotationStiffness = value;
        }

        public virtual float getIkRotationStiffness() { return m_ikRotationStiffness; }


        public virtual void setIkPlacementStiffness(float value)
        {
            m_ikPlacementStiffness = value;
        }

        public virtual float getIkPlacementStiffness() { return m_ikPlacementStiffness; }

        public virtual void setBodyStiffness(float value)
        {
            m_bodyStiffness = value;
        }

        public virtual float getBodyStiffness() { return m_bodyStiffness; }

        public virtual void setActivateCrouchingBehaviour(bool isCrouching)
        {
            m_activateCrouchSpecificBehaviour = isCrouching;
        }

        public virtual void setIsValidAndShouldCheckForGrounded(ValidationType validation)
        {
            m_validationType = validation;
        }

        public virtual void setMoveLeaningStrength(float value)
        {
            m_moveLeaningStrength = value;
        }

        public virtual void setMoveLeaningKneelFactor(float value)
        {
            m_moveLeaningKneelFactor = value;
        }

        public virtual void setMoveLeaningKneelFactorWithMultiplier005(float value)
        {
            m_moveLeaningKneelFactor = value * 0.05f;
        }

        public virtual void setSlopeBendingUpwardsStrength(float value)
        {
            m_slopeBendingUpwardsStrength = value;
        }

        public virtual void setSlopeBendingDownwardsStrength(float value)
        {
            m_slopeBendingDownwardsStrength = value;
        }

        public virtual void setShouldersCounteractSpineBending(bool value)
        {
            m_shouldersCounteractSpineBending = value;
        }

        public virtual void setBodyPlacementWeight(float weight)
        {
            m_bodyPlacementWeight = weight;
        }

        public virtual void setFootPlacementWeight(float weight)
        {
            m_ikPlacementWeight = weight;
        }

        public virtual void setBodyPositionOffset(float offset)
        {
            m_bodyPositionOffset = offset;
        }

        public virtual void setCanBodyPositionMoveHigherThanGroundedPosition(bool value)
        {
            m_canBodyPositionMoveHigherThanGroundedPosition = value;
        }

        public virtual void setIKExtrapolation(float extrapolation)
        {
            m_ikUpwardsExtrapolation = extrapolation;
            m_ikDownwardsExtrapolation = extrapolation * 2.5f;
        }

        public virtual void setIKUpwardsExtrapolation(float extrapolation)
        {
            m_ikUpwardsExtrapolation = extrapolation;
        }

        public virtual void setIKDownwardsExtrapolation(float extrapolation)
        {
            m_ikDownwardsExtrapolation = extrapolation;
        }

        public virtual void tryAddToForceInvalidOnAnimatorState(AnimatorStateEntry newEntry)
        {
            foreach (var entry in m_forceInvalidOnAnimatorStates) if (entry.animatorState == newEntry.animatorState) return;
            m_forceInvalidOnAnimatorStates.Add(newEntry);

            int hash = Animator.StringToHash(newEntry.animatorState);
            foreach (var entry in m_deactiveOnAnimatorState) if (entry.Value == hash) return;
            m_deactiveOnAnimatorState.Add(new KeyValuePair<int, int>(newEntry.layer, hash));
        }

        protected virtual void Awake()
        {
            m_transform = this.transform;
            m_animator = this.GetComponent<Animator>();

            if (m_animator != null)
            {
                m_spineTransform = m_animator.GetBoneTransform(HumanBodyBones.Spine);
                m_leftShoulderTransform = m_animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                m_rightShoulderTransform = m_animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                m_prevBodyPosition = m_animator.GetBoneTransform(HumanBodyBones.Hips).position;
                m_leftUpperLeg = m_animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                m_rightUpperLeg = m_animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                m_leftLowerLeg = m_animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                m_rightLowerLeg = m_animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                m_leftFoot = m_animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                m_rightFoot = m_animator.GetBoneTransform(HumanBodyBones.RightFoot);

                findLegsForwardAxes(out m_leftAxisHint, out m_isLeftAxisHintNegative, out m_rightAxisHint, out m_isRightAxisHintNegative);
            }

            m_prevTransformPosition = m_transform.position;
            m_resultLeft = new IKResult(Vector3.zero, Vector3.zero, false, Vector3.zero, Vector3.zero, Vector3.up, null, null);
            m_resultRight = new IKResult(Vector3.zero, Vector3.zero, false, Vector3.zero, Vector3.zero, Vector3.up, null, null);

            foreach (var entry in m_forceInvalidOnAnimatorStates) m_deactiveOnAnimatorState.Add(new KeyValuePair<int, int>(entry.layer, Animator.StringToHash(entry.animatorState)));
            foreach (var entry in m_forceActivateCrouchingBehaviour) m_activateCrouchingOnAnimatorState.Add(new KeyValuePair<int, int>(entry.layer, Animator.StringToHash(entry.animatorState)));

            for (int i = 0; i < m_averageSlopeLeaningFixedQueueCount; i++) m_averageSlopeLeaningValuesQueue.Enqueue(0);

            m_footstepEventWaitForSeconds = new WaitForSeconds(m_footstepEventDelay);
            m_prevFootstepEventDelay = m_footstepEventDelay;
        }

        protected virtual void Start()
        {
            m_prevTransformPosition = m_transform.position;
            m_ikFootHeightToUseLeft = m_ikFootHeight;
            m_ikFootHeightToUseRight = m_ikFootHeight;
            m_ikFootLengthToUseLeft = m_ikFootLength;
            m_ikFootLengthToUseRight = m_ikFootLength;
        }

        public virtual void reinitialize()
        {
            Awake();
            Start();
        }

        protected virtual void FixedUpdate()
        {
            m_updateSlopeLeaning = true;
        }

        //a callback for calculating IK
        protected virtual void OnAnimatorIK(int layerIdx)
        {
            if (layerIdx != m_animatorIkPassLayerIndex) return;

            // clamping values that are not allowed to be zero or smaller zero
            if (m_ikFootHeight < 0.001f) m_ikFootHeight = 0.001f;
            if (m_ikFootLength < 0.001f) m_ikFootLength = 0.001f;
            if (m_ikFootWidth < 0.001f) m_ikFootWidth = 0.001f;

            Vector3 feetZeroPos = m_transform.position;
            isValidAndGrounded(m_groundedResult, feetZeroPos, m_transform.up, m_collisionLayerMask);

            if (m_groundedResult.isGrounded && m_groundedResult.isValid)
            {
                if (m_isGrounded == false)
                {
                    if (m_onGroundedEntered != null) m_onGroundedEntered.Invoke(m_groundedResult);
                    m_isGrounded = true;

                    m_ikLeftOffset = Vector3.Lerp(Vector3.zero, m_ikLeftOffset, m_feetResetLerp);
                    m_ikRightOffset = Vector3.Lerp(Vector3.zero, m_ikRightOffset, m_feetResetLerp);
                    m_extrapolationLeft = Vector3.Lerp(Vector3.zero, m_extrapolationLeft, m_feetResetLerp);
                    m_extrapolationRight = Vector3.Lerp(Vector3.zero, m_extrapolationRight, m_feetResetLerp);
                }

                updateIK(feetZeroPos);

                Vector3 calculatedPlanarTargetVelocity = calculatePlanarVelocity(feetZeroPos);

                if (m_ignoreVelocityDependency) m_velocityPlanarSlopeLean = m_transform.forward * 5;
                else m_velocityPlanarSlopeLean = calculatedPlanarTargetVelocity;

                if (m_moveLeanWhenGrounded)
                {
                    m_rawVelocityPlanarMoveLean = calculatedPlanarTargetVelocity;
                }
                else
                {
                    m_rawVelocityPlanarMoveLean = Vector3.zero;
                }
            }
            else
            {
                if (m_isGrounded)
                {
                    if (m_onGroundedExited != null) m_onGroundedExited.Invoke();
                    m_isGrounded = false;
                }

                revertUpdateIK();

                if (m_moveLeanWhenNotGrounded)
                {
                    Vector3 calculatedPlanarTargetVelocity = calculatePlanarVelocity(feetZeroPos);
                    m_rawVelocityPlanarMoveLean = calculatedPlanarTargetVelocity;
                }
                else
                {
                    m_rawVelocityPlanarMoveLean = Vector3.zero;
                }
            }

            updateSlopeLeaning(m_groundedResult);
            updateLeaning(feetZeroPos);

            m_prevTransformPosition = feetZeroPos;
            m_characterExternalMovingPlatformOffsetVec = Vector3.zero;

            if(m_groundedResult.groundedTransform != null)
            {
                m_groundedResultLastTransform = m_groundedResult.groundedTransform;
                m_groundedResultLastTransformPosition = m_groundedResultLastTransform.position;
                m_groundedResultLastTransformRotation = m_groundedResultLastTransform.rotation;
            }
        }

        protected virtual void updateSlopeLeaning(GroundedResult groundedResult)
        {
            if (m_slopeLeaningType == SlopeLeaningType.DISABLED) return;

            if (m_updateSlopeLeaning)
            {
                float slopeLeaning = 0;

                if (groundedResult.isGrounded && groundedResult.isValid)
                {
                    if (m_slopeLeaningType == SlopeLeaningType.SURFACE_ANGLE_DETECTED_BY_GROUNDED_CHECK)
                    {
                        float dot = -Vector3.Dot(groundedResult.groundedNormal, m_transform.forward);

                        if (dot > 0)
                        {
                            slopeLeaning = Mathf.Clamp(dot * m_slopeBendingUpwardsStrength * m_velocityPlanarSlopeLean.magnitude, -m_maxAbsoluteSlopeSpineBending, m_maxAbsoluteSlopeSpineBending);
                        }
                        else
                        {
                            slopeLeaning = Mathf.Clamp(dot * m_slopeBendingDownwardsStrength * m_velocityPlanarSlopeLean.magnitude, -m_maxAbsoluteSlopeSpineBending, m_maxAbsoluteSlopeSpineBending);
                        }
                    }
                    else if (m_slopeLeaningType == SlopeLeaningType.HEIGHT_OFFSET_DETECTED_BY_FEETS)
                    {
                        float dot = -Vector3.Dot(groundedResult.groundedNormal, m_transform.forward);
                        float feetsHeightDiff = (m_ikLeftOffset - m_ikRightOffset).magnitude;

                        if (dot > 0)
                        {
                            slopeLeaning = Mathf.Clamp(feetsHeightDiff * m_slopeBendingUpwardsStrength * m_velocityPlanarSlopeLean.magnitude, -m_maxAbsoluteSlopeSpineBending, m_maxAbsoluteSlopeSpineBending);
                        }
                        else
                        {
                            slopeLeaning = Mathf.Clamp(-feetsHeightDiff * m_slopeBendingDownwardsStrength * m_velocityPlanarSlopeLean.magnitude, -m_maxAbsoluteSlopeSpineBending, m_maxAbsoluteSlopeSpineBending);
                        }
                    }
                    else if (m_slopeLeaningType == SlopeLeaningType.AVERAGE_SURFACE_ANGLE_DETECTED_BY_FEETS)
                    {
                        float dot = -Vector3.Dot((m_resultLeft.normal + m_resultRight.normal) * 0.5f, m_transform.forward);

                        if (dot > 0)
                        {
                            slopeLeaning = Mathf.Clamp(dot * m_slopeBendingUpwardsStrength * m_velocityPlanarSlopeLean.magnitude, -m_maxAbsoluteSlopeSpineBending, m_maxAbsoluteSlopeSpineBending);
                        }
                        else
                        {
                            slopeLeaning = Mathf.Clamp(dot * m_slopeBendingDownwardsStrength * m_velocityPlanarSlopeLean.magnitude, -m_maxAbsoluteSlopeSpineBending, m_maxAbsoluteSlopeSpineBending);
                        }
                    }
                    //else // do nothing
                }
                //else // do nothing

                m_averageSlopeLeaningValuesQueue.Dequeue();
                m_averageSlopeLeaningValuesQueue.Enqueue(slopeLeaning);

                m_updateSlopeLeaning = false;
            }

            float avgSlopeLeaning = 0.0f;

            foreach (var value in m_averageSlopeLeaningValuesQueue) avgSlopeLeaning += value;
            if (m_averageSlopeLeaningValuesQueue.Count > 0) avgSlopeLeaning /= m_averageSlopeLeaningValuesQueue.Count;

            m_currSlopeLeaning = Mathf.Lerp(m_currSlopeLeaning, avgSlopeLeaning, Time.deltaTime * m_slopeBendingTransitionStiffness);

            if (m_relativeSpineRotationAxisAffectedBySlopeLeaning == Axis.X_AXIS)
            {
                m_spineTransform.localRotation = Quaternion.Euler(m_currSlopeLeaning, 0, 0) * m_spineTransform.localRotation;

                if (m_shouldersCounteractSpineBending)
                {
                    m_animator.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, Quaternion.Euler(-m_currSlopeLeaning, 0, 0) * m_leftShoulderTransform.localRotation);
                    m_animator.SetBoneLocalRotation(HumanBodyBones.RightShoulder, Quaternion.Euler(-m_currSlopeLeaning, 0, 0) * m_rightShoulderTransform.localRotation);
                }
            }
            else if (m_relativeSpineRotationAxisAffectedBySlopeLeaning == Axis.Y_AXIS)
            {
                m_spineTransform.localRotation = Quaternion.Euler(0, m_currSlopeLeaning, 0) * m_spineTransform.localRotation;

                if (m_shouldersCounteractSpineBending)
                {
                    m_animator.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, Quaternion.Euler(0, -m_currSlopeLeaning, 0) * m_leftShoulderTransform.localRotation);
                    m_animator.SetBoneLocalRotation(HumanBodyBones.RightShoulder, Quaternion.Euler(0, -m_currSlopeLeaning, 0) * m_rightShoulderTransform.localRotation);
                }
            }
            else
            {
                m_spineTransform.localRotation = Quaternion.Euler(0, 0, m_currSlopeLeaning) * m_spineTransform.localRotation;

                if (m_shouldersCounteractSpineBending)
                {
                    m_animator.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, Quaternion.Euler(0, 0, -m_currSlopeLeaning) * m_leftShoulderTransform.localRotation);
                    m_animator.SetBoneLocalRotation(HumanBodyBones.RightShoulder, Quaternion.Euler(0, 0, -m_currSlopeLeaning) * m_rightShoulderTransform.localRotation);
                }
            }

            m_animator.SetBoneLocalRotation(HumanBodyBones.Spine, m_spineTransform.localRotation);
        }

        protected virtual Vector3 calculatePlanarVelocity(Vector3 feetZeroPos)
        {
            if (m_moveLeaningStrength < 0.001f && m_moveSpineLeaningStrength < 0.001f && m_slopeLeaningType == SlopeLeaningType.DISABLED) return Vector3.zero;

            Vector3 velocity = Vector3.zero;
            if(Time.deltaTime > 0) velocity = (feetZeroPos - (m_prevTransformPosition + m_characterExternalMovingPlatformOffsetVec)) / Time.deltaTime;
            Vector3 velocityUp = Vector3.Project(velocity, m_transform.up);
            return velocity - velocityUp;
        }

        protected Vector3 m_moveLeanChangeVec;
        protected Vector3 m_velocityPlanarMoveLeanChange;

        protected virtual void updateLeaning(Vector3 feetZeroPos)
        {
            if (m_moveLeaningStrength < 0.001f && m_moveSpineLeaningStrength < 0.001f) return;

            Vector3 rotationVec = m_animator.bodyPosition - feetZeroPos;

            m_velocityPlanarMoveLean = Vector3.Lerp(m_velocityPlanarMoveLean, m_rawVelocityPlanarMoveLean, Time.deltaTime * m_moveLeaningStiffness);

            Vector3 velMoveLeanChange = m_rawVelocityPlanarMoveLean - m_velocityPlanarMoveLeanChange;

            if(Vector3.Dot(velMoveLeanChange.normalized, m_rawVelocityPlanarMoveLean.normalized) < 0)
            {
                m_velocityPlanarMoveLeanChange = Vector3.Lerp(m_velocityPlanarMoveLeanChange, m_rawVelocityPlanarMoveLean, Time.deltaTime * 100.0f / Mathf.Max(m_moveLeaningIsStartStopStopDurationStrength, 1));
            }
            else
            {
                m_velocityPlanarMoveLeanChange = Vector3.Lerp(m_velocityPlanarMoveLeanChange, m_rawVelocityPlanarMoveLean, Time.deltaTime * 100.0f / Mathf.Max(m_moveLeaningIsStartStopStartDurationStrength, 1));
            }

            velMoveLeanChange = m_rawVelocityPlanarMoveLean - m_velocityPlanarMoveLeanChange;
            m_moveLeanChangeVec = Vector3.Lerp(m_moveLeanChangeVec, velMoveLeanChange, Time.deltaTime * 10.0f);

            Vector3 leaningVec = Vector3.Lerp(m_velocityPlanarMoveLean, m_moveLeanChangeVec, m_moveLeaningIsStartStopPercentage);


            Vector3 rightLeanVec = Vector3.Project(leaningVec, m_transform.right);
            Vector3 forwardLeanVec = Vector3.Project(leaningVec, m_transform.forward);

            float signRight = 1;
            float signForward = 1;

            if (Vector3.Dot(rightLeanVec, m_transform.right) > 0) signRight = -1;
            if (Vector3.Dot(forwardLeanVec, m_transform.forward) < 0) signForward = -1;

            if (m_flipForwardMoveLean) signForward *= -1;
            if (m_flipRightMoveLean) signRight *= -1;


            // rotation spine
            if(m_moveSpineLeaningStrength >= 0.001f)
            {
                Quaternion rotRightAxisSpine = Quaternion.AngleAxis(forwardLeanVec.magnitude * signForward * m_moveSpineLeaningStrength, m_transform.right);
                Quaternion rotForwardAxisSpine = Quaternion.AngleAxis(rightLeanVec.magnitude * signRight * m_moveSpineLeaningStrength, m_transform.forward);
                Quaternion worldDeltaRotSpine = rotForwardAxisSpine * rotRightAxisSpine;

                m_spineTransform.rotation = worldDeltaRotSpine * m_spineTransform.rotation;
                m_animator.SetBoneLocalRotation(HumanBodyBones.Spine, m_spineTransform.localRotation);
            }


            // rotating full body
            if (m_moveLeaningStrength < 0.001f) return;

            Quaternion rotRightAxis = Quaternion.AngleAxis(forwardLeanVec.magnitude * signForward * m_moveLeaningStrength, m_transform.right);
            Quaternion rotForwardAxis = Quaternion.AngleAxis(rightLeanVec.magnitude * signRight * m_moveLeaningStrength, m_transform.forward);
            Quaternion fullRot = rotForwardAxis * rotRightAxis;

            Vector3 rotationVecRotated = fullRot * rotationVec;
            Vector3 kneelOffset = Mathf.Min(leaningVec.magnitude * m_moveLeaningKneelFactor * 0.25f, 1) * rotationVecRotated;
            m_animator.bodyPosition = feetZeroPos + rotationVecRotated - kneelOffset;
            m_animator.bodyRotation = fullRot * m_animator.bodyRotation;

            // fix hints
            if (m_moveLeaningKneelFactor < 0.001f)
            {
                // fix simple
                Vector3 diffVecLeft = m_animator.GetIKHintPosition(AvatarIKHint.LeftKnee) - feetZeroPos;
                Vector3 diffVecRight = m_animator.GetIKHintPosition(AvatarIKHint.RightKnee) - feetZeroPos;

                m_animator.SetIKHintPosition(AvatarIKHint.LeftKnee, feetZeroPos + fullRot * diffVecLeft);
                m_animator.SetIKHintPosition(AvatarIKHint.RightKnee, feetZeroPos + fullRot * diffVecRight);
            }
            else
            {
                // fix advanced
                Vector3 ikLeft = m_animator.GetIKPosition(AvatarIKGoal.LeftFoot);
                Vector3 ikRight = m_animator.GetIKPosition(AvatarIKGoal.RightFoot);
                Vector3 leftHintPos = m_animator.GetIKHintPosition(AvatarIKHint.LeftKnee);
                Vector3 rightHintPos = m_animator.GetIKHintPosition(AvatarIKHint.RightKnee);
                Vector3 leftUpperLegPos = m_leftUpperLeg.position;
                Vector3 rightUpperLegPos = m_rightUpperLeg.position;

                Vector3 ikLeftToBodyPosOriginalVec = leftUpperLegPos - ikLeft;
                Vector3 ikRightToBodyPosOriginalVec = rightUpperLegPos - ikRight;
                Vector3 ikLeftIkHintOriginalVec = leftHintPos - ikLeft;
                Vector3 ikRightIkHintOriginalVec = rightHintPos - ikRight;

                float lowerLeft = ikLeftIkHintOriginalVec.magnitude;
                float upperLeft = (leftHintPos - leftUpperLegPos).magnitude;
                float lowerRight = ikRightIkHintOriginalVec.magnitude;
                float upperRight = (rightHintPos - rightUpperLegPos).magnitude;

                leftUpperLegPos = ikLeft + fullRot * ikLeftToBodyPosOriginalVec;
                rightUpperLegPos = ikRight + fullRot * ikRightToBodyPosOriginalVec;
                leftHintPos = ikLeft + fullRot * ikLeftIkHintOriginalVec;
                rightHintPos = ikRight + fullRot * ikRightIkHintOriginalVec;

                ikLeftToBodyPosOriginalVec = leftUpperLegPos - ikLeft;
                ikRightToBodyPosOriginalVec = rightUpperLegPos - ikRight;
                ikLeftIkHintOriginalVec = leftHintPos - ikLeft;
                ikRightIkHintOriginalVec = rightHintPos - ikRight;

                Vector3 leftHintDirVec = ikLeftIkHintOriginalVec - Vector3.Project(ikLeftIkHintOriginalVec, ikLeftToBodyPosOriginalVec.normalized);
                Vector3 rightHintDirVec = ikRightIkHintOriginalVec - Vector3.Project(ikRightIkHintOriginalVec, ikRightToBodyPosOriginalVec.normalized);

                Vector3 leftHintFinalPos = calculateHintPos(leftUpperLegPos - kneelOffset, ikLeft, lowerLeft, upperLeft, ikLeftToBodyPosOriginalVec, leftHintDirVec);
                Vector3 rightHintFinalPos = calculateHintPos(rightUpperLegPos - kneelOffset, ikRight, lowerRight, upperRight, ikRightToBodyPosOriginalVec, rightHintDirVec);

                m_animator.SetIKHintPosition(AvatarIKHint.LeftKnee, leftHintFinalPos);
                m_animator.SetIKHintPosition(AvatarIKHint.RightKnee, rightHintFinalPos);
            }
        }

        protected virtual bool wasGroundMovingForGrounded()
        {
            if (m_groundedResult.groundedTransform != m_groundedResultLastTransform) return true;
            if (m_groundedResult.groundedTransform == null) return false;
            float deltaPos = (m_groundedResult.groundedTransform.position - m_groundedResultLastTransformPosition).magnitude;
            if (deltaPos > Mathf.Epsilon) return true;
            float deltaAngle = Quaternion.Angle(m_groundedResultLastTransformRotation, m_groundedResult.groundedTransform.rotation);
            if (deltaAngle > Mathf.Epsilon) return true;
            return false;
        }

        protected virtual bool wasGroundMovingForIkLeft()
        {
            if (m_resultLeft.primaryHitTransform == null) return false;
            float deltaPos = (m_resultLeft.primaryHitTransform.position - m_resultLeftLastTransformPosition).magnitude;
            if (deltaPos > Mathf.Epsilon) return true;
            float deltaAngle = Quaternion.Angle(m_resultLeftLastTransformRotation, m_resultLeft.primaryHitTransform.rotation);
            if (deltaAngle > Mathf.Epsilon) return true;
            return false;
        }

        protected virtual bool wasGroundMovingForIkRight()
        {
            if (m_resultRight.primaryHitTransform == null) return false;
            float deltaPos = (m_resultRight.primaryHitTransform.position - m_resultRightLastTransformPosition).magnitude;
            if (deltaPos > Mathf.Epsilon) return true;
            float deltaAngle = Quaternion.Angle(m_resultRightLastTransformRotation, m_resultRight.primaryHitTransform.rotation);
            if (deltaAngle > Mathf.Epsilon) return true;
            return false;
        }

        protected void findLegsForwardAxes(out Axis leftLegAxis, out bool isNegativeLeftLegAxis, out Axis rightLegAxis, out bool isNegativeRightLegAxis)
        {
            Transform rightUpperLeg = m_animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform leftUpperLeg = m_animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);

            Vector3 upVec = m_animator.GetBoneTransform(HumanBodyBones.Head).position - m_animator.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 rightVec = rightUpperLeg.position - leftUpperLeg.position;
            upVec.Normalize();
            rightVec.Normalize();
            Vector3 forwardVec = Vector3.Cross(rightVec, upVec).normalized;

            // left
            float dotX = Vector3.Dot(forwardVec, leftUpperLeg.right);
            float dotY = Vector3.Dot(forwardVec, leftUpperLeg.up);
            float dotZ = Vector3.Dot(forwardVec, leftUpperLeg.forward);

            leftLegAxis = Axis.Z_AXIS;
            isNegativeLeftLegAxis = false;

            if (Mathf.Abs(dotX) > Mathf.Abs(dotY) && Mathf.Abs(dotX) > Mathf.Abs(dotZ))
            {
                leftLegAxis = Axis.X_AXIS;
                if (dotX < 0) isNegativeLeftLegAxis = true;
            }
            else if (Mathf.Abs(dotY) > Mathf.Abs(dotX) && Mathf.Abs(dotY) > Mathf.Abs(dotZ))
            {
                leftLegAxis = Axis.Y_AXIS;
                if (dotY < 0) isNegativeLeftLegAxis = true;
            }
            else if (dotZ < 0)
            {
                isNegativeLeftLegAxis = true;
            }

            // right
            dotX = Vector3.Dot(forwardVec, rightUpperLeg.right);
            dotY = Vector3.Dot(forwardVec, rightUpperLeg.up);
            dotZ = Vector3.Dot(forwardVec, rightUpperLeg.forward);

            rightLegAxis = Axis.Z_AXIS;
            isNegativeRightLegAxis = false;

            if (Mathf.Abs(dotX) > Mathf.Abs(dotY) && Mathf.Abs(dotX) > Mathf.Abs(dotZ))
            {
                rightLegAxis = Axis.X_AXIS;
                if (dotX < 0) isNegativeRightLegAxis = true;
            }
            else if (Mathf.Abs(dotY) > Mathf.Abs(dotX) && Mathf.Abs(dotY) > Mathf.Abs(dotZ))
            {
                rightLegAxis = Axis.Y_AXIS;
                if (dotY < 0) isNegativeRightLegAxis = true;
            }
            else if (dotZ < 0)
            {
                isNegativeRightLegAxis = true;
            }
        }

        protected virtual void updateIK(Vector3 feetZeroPos)
        {
            m_feetResetLerp = Mathf.Lerp(m_feetResetLerp, 1, Time.deltaTime * m_ikPlacementStiffness * m_resetToDefaultValuesSpeed);
            m_bodyResetLerp = Mathf.Lerp(m_bodyResetLerp, 1, Time.deltaTime * m_bodyStiffness * m_resetToDefaultValuesSpeed);

            Vector3 bodyPosition = m_animator.bodyPosition;

            Vector3 ikLeft = m_animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 ikRight = m_animator.GetIKPosition(AvatarIKGoal.RightFoot);

            // calculate params for hint pos
            Vector3 leftHintPos = m_leftLowerLeg.position;
            Vector3 rightHintPos = m_rightLowerLeg.position;
            if(m_computeIkHintPositionStrategy == IkHintComputationStrategy.IK_HINT_POSITION_BASED)
            {
                leftHintPos = m_animator.GetIKHintPosition(AvatarIKHint.LeftKnee);
                rightHintPos = m_animator.GetIKHintPosition(AvatarIKHint.RightKnee);
            }

            Vector3 leftUpperLegPos = m_leftUpperLeg.position;
            Vector3 rightUpperLegPos = m_rightUpperLeg.position;

            Vector3 ikLeftToBodyPosOriginalVec = leftUpperLegPos - ikLeft;
            Vector3 ikRightToBodyPosOriginalVec = rightUpperLegPos - ikRight;
            Vector3 ikLeftIkHintOriginalVec = leftHintPos - ikLeft;
            Vector3 ikRightIkHintOriginalVec = rightHintPos - ikRight;
            Vector3 leftHintDirVec = ikLeftIkHintOriginalVec - Vector3.Project(ikLeftIkHintOriginalVec, ikLeftToBodyPosOriginalVec.normalized);
            Vector3 rightHintDirVec = ikRightIkHintOriginalVec - Vector3.Project(ikRightIkHintOriginalVec, ikRightToBodyPosOriginalVec.normalized);

            float lowerLeft = ikLeftIkHintOriginalVec.magnitude;
            float upperLeft = (leftHintPos - leftUpperLegPos).magnitude;
            float lowerRight = ikRightIkHintOriginalVec.magnitude;
            float upperRight = (rightHintPos - rightUpperLegPos).magnitude;

            if(m_computeIkHintPositionStrategy == IkHintComputationStrategy.BONE_BASED)
            {
                if (m_leftAxisHint == Axis.X_AXIS) leftHintDirVec = m_leftLowerLeg.right * lowerLeft + m_leftUpperLeg.right * upperLeft;
                else if (m_leftAxisHint == Axis.Y_AXIS) leftHintDirVec = m_leftLowerLeg.up * lowerLeft + m_leftUpperLeg.up * upperLeft;
                else leftHintDirVec = m_leftLowerLeg.forward * lowerLeft + m_leftUpperLeg.forward * upperLeft;
                if (m_isLeftAxisHintNegative) leftHintDirVec *= -1;

                if (m_rightAxisHint == Axis.X_AXIS) rightHintDirVec = m_rightLowerLeg.right * lowerRight + m_rightUpperLeg.right * upperRight;
                else if (m_rightAxisHint == Axis.Y_AXIS) rightHintDirVec = m_rightLowerLeg.up * lowerRight + m_rightUpperLeg.up * upperRight;
                else rightHintDirVec = m_rightLowerLeg.forward * lowerRight + m_rightUpperLeg.forward * upperRight;
                if (m_isRightAxisHintNegative) rightHintDirVec *= -1;
            }
            else if (m_computeIkHintPositionStrategy == IkHintComputationStrategy.BONE_BASED_WITH_FEET_DIRECTION_INFLUENCE)
            {
                if (m_leftAxisHint == Axis.X_AXIS) leftHintDirVec = m_leftLowerLeg.right * lowerLeft + m_leftUpperLeg.right * upperLeft + m_leftFoot.right * lowerLeft;
                else if (m_leftAxisHint == Axis.Y_AXIS) leftHintDirVec = m_leftLowerLeg.up * lowerLeft + m_leftUpperLeg.up * upperLeft + m_leftFoot.up * lowerLeft;
                else leftHintDirVec = m_leftLowerLeg.forward * lowerLeft + m_leftUpperLeg.forward * upperLeft + m_leftFoot.forward * lowerLeft;
                if (m_isLeftAxisHintNegative) leftHintDirVec *= -1;

                if (m_rightAxisHint == Axis.X_AXIS) rightHintDirVec = m_rightLowerLeg.right * lowerRight + m_rightUpperLeg.right * upperRight + m_rightFoot.right * lowerRight;
                else if (m_rightAxisHint == Axis.Y_AXIS) rightHintDirVec = m_rightLowerLeg.up * lowerRight + m_rightUpperLeg.up * upperRight + m_rightFoot.up * lowerRight;
                else rightHintDirVec = m_rightLowerLeg.forward * lowerRight + m_rightUpperLeg.forward * upperRight + m_rightFoot.forward * lowerRight;
                if (m_isRightAxisHintNegative) rightHintDirVec *= -1;
            }

            // apply global offset
            Vector3 globalCharOffsetVec = m_globalCharacterYOffset * m_transform.up;
            if (m_globalCharacterYOffset > 0 || m_globalCharacterYOffset < 0)
            {
                bodyPosition += globalCharOffsetVec;
                ikLeft += globalCharOffsetVec;
                ikRight += globalCharOffsetVec;
                leftUpperLegPos += globalCharOffsetVec;
                rightUpperLegPos += globalCharOffsetVec;

                if(m_applyGlobalCharacterYOffsetToOtherIKsToo)
                {
                    m_animator.SetIKPosition(AvatarIKGoal.LeftHand, m_animator.GetIKPosition(AvatarIKGoal.LeftHand) + globalCharOffsetVec);
                    m_animator.SetIKPosition(AvatarIKGoal.RightHand, m_animator.GetIKPosition(AvatarIKGoal.RightHand) + globalCharOffsetVec);
                    m_animator.SetIKHintPosition(AvatarIKHint.LeftElbow, m_animator.GetIKHintPosition(AvatarIKHint.LeftElbow) + globalCharOffsetVec);
                    m_animator.SetIKHintPosition(AvatarIKHint.RightElbow, m_animator.GetIKHintPosition(AvatarIKHint.RightElbow) + globalCharOffsetVec);
                }
            }

            Vector3 ikLeftMovementDirection = ikLeft - (m_prevIkLeft + m_characterExternalMovingPlatformOffsetVec);
            Vector3 ikRightMovementDirection = ikRight - (m_prevIkRight + m_characterExternalMovingPlatformOffsetVec);
            float ikLeftMovementDistance = ikLeftMovementDirection.magnitude;
            float ikRightMovementDistance = ikRightMovementDirection.magnitude;

            Quaternion currRotLeft = m_animator.GetIKRotation(AvatarIKGoal.LeftFoot);
            Quaternion currRotRight = m_animator.GetIKRotation(AvatarIKGoal.RightFoot);

            // foot height / length automatic improvements
            Vector3 transformMovementVec = m_transform.position - (m_prevTransformPosition + m_characterExternalMovingPlatformOffsetVec);
            Vector3 transformMovementVerticalVec = Vector3.Project(transformMovementVec, m_transform.up);

            Vector3 leftFootForwardVec = currRotLeft * Vector3.forward;
            Vector3 rightFootForwardVec = currRotRight * Vector3.forward;

            float dotIsMovingUpwards = Vector3.Dot(transformMovementVerticalVec.normalized, m_transform.up);
            bool isMovingUpwards = dotIsMovingUpwards >= 0;
            if (isMovingUpwards == false)
            {
                float footForwardOffset = (m_ikFootForwardBias + m_ikFootLength * 0.5f) * m_automaticFootRotationBasedHeightAdaption;
                Vector3 lv = Vector3.Project(leftFootForwardVec * footForwardOffset, m_transform.up);
                Vector3 rv = Vector3.Project(rightFootForwardVec * footForwardOffset, m_transform.up);
                float l = 1.0f;
                float r = 1.0f;
                if (Vector3.Dot(lv.normalized, m_transform.up) >= 0) l = 0.0f;
                if (Vector3.Dot(rv.normalized, m_transform.up) >= 0) r = 0.0f;
                m_ikFootHeightToUseLeft = m_ikFootHeight + l * lv.magnitude;
                m_ikFootHeightToUseRight = m_ikFootHeight + r * rv.magnitude;

                Vector3 lf = Vector3.Project(leftFootForwardVec * m_ikFootLength, m_transform.forward);
                Vector3 rf = Vector3.Project(rightFootForwardVec * m_ikFootLength, m_transform.forward);
                m_ikFootLengthToUseLeft = Mathf.Max(lf.magnitude, m_ikFootHeight);
                m_ikFootLengthToUseRight = Mathf.Max(rf.magnitude, m_ikFootHeight);
            }
            else
            {
                m_ikFootHeightToUseLeft = m_ikFootHeight;
                m_ikFootHeightToUseRight = m_ikFootHeight;
                m_ikFootLengthToUseLeft = m_ikFootLength;
                m_ikFootLengthToUseRight = m_ikFootLength;
            }

            Vector3 ikPosFromOriginLeft = ikLeft - feetZeroPos;
            Vector3 projOnRayDirVecLeft = Vector3.Project(ikPosFromOriginLeft, -m_transform.up);
            Vector3 ikBottomPointLeft = ikLeft - projOnRayDirVecLeft;

            Vector3 ikPosFromOriginRight = ikRight - feetZeroPos;
            Vector3 projOnRayDirVecRight = Vector3.Project(ikPosFromOriginRight, -m_transform.up);
            Vector3 ikBottomPointRight = ikRight - projOnRayDirVecRight;

            float legsPlanarForwardDist = (Vector3.Project(ikBottomPointRight, m_transform.forward) - Vector3.Project(ikBottomPointLeft, m_transform.forward)).magnitude * m_increaseBodyPositionMaxCorrectionWithForwardFootDistance * 0.5f;
            float maxBodyPositionCorrectionToUse = m_bodyPositionMaxCorrection + legsPlanarForwardDist;
            float raycastFeetDistance = Mathf.Max(maxBodyPositionCorrectionToUse, m_checkGroundedDistance + legsPlanarForwardDist);

            // fixing stuttering on edge-case where body position is about the same distance away from the ground as defined by m_bodyPositionMaxCorrection
            float rawbodyoffsetmagnitude = m_rawbodyOffsetVec.magnitude;
            if (rawbodyoffsetmagnitude < 0.01f)
            {
                float toleranceToSubtract = m_bodyPositionMaxCorrectionTolerance * m_bodyPositionMaxCorrection;
                maxBodyPositionCorrectionToUse -= toleranceToSubtract;
                raycastFeetDistance -= toleranceToSubtract;
            }

            bool forceUpdateLeft = false;
            bool forceUpdateRight = false;

            if(m_accountMinimumMovementThresholdForMovingGroundObjects)
            {
                float diffToRawOffset = (m_rawbodyOffsetVec - m_bodyOffset).magnitude;
                bool forceupdateGeneral = wasGroundMovingForGrounded() || (diffToRawOffset > 0.001f && rawbodyoffsetmagnitude > 0.001f);
                forceUpdateLeft = forceupdateGeneral || wasGroundMovingForIkLeft();
                forceUpdateRight = forceupdateGeneral || wasGroundMovingForIkRight();
            }

            if( Mathf.Abs(m_ikFootHeight - m_prevIkFootHeight) > Mathf.Epsilon ||
                Mathf.Abs(m_ikMaxCorrection - m_prevIkMaxCorrection) > Mathf.Epsilon ||
                Mathf.Abs(m_ikFootForwardBias - m_prevIkFootForwardBias) > Mathf.Epsilon ||
                Mathf.Abs(m_ikFootLength - m_prevIkFootLength) > Mathf.Epsilon ||
                Mathf.Abs(m_ikFootWidth - m_prevIkFootWidth) > Mathf.Epsilon ||
                Mathf.Abs(m_bodyPositionMaxCorrection - m_prevBodyPositionMaxCorrection) > Mathf.Epsilon ||
                Mathf.Abs(m_checkGroundedDistance - m_prevCheckGroundedDistance) > Mathf.Epsilon ||
                m_collisionLayerMask.value != m_prevCollisionLayerMask.value)
            {
                forceUpdateLeft = true;
                forceUpdateRight = true;

                m_prevIkFootHeight = m_ikFootHeight;
                m_prevIkMaxCorrection = m_ikMaxCorrection;
                m_prevIkFootForwardBias = m_ikFootForwardBias;
                m_prevIkFootLength = m_ikFootLength;
                m_prevIkFootWidth = m_ikFootWidth;
                m_prevBodyPositionMaxCorrection = m_bodyPositionMaxCorrection;
                m_prevCheckGroundedDistance = m_checkGroundedDistance;
                m_prevCollisionLayerMask.value = m_collisionLayerMask.value;
            }

            if (ikLeftMovementDistance > m_minimumMovementThreshold || forceUpdateLeft)
            {
                m_prevIkLeft = ikLeft;
                findNewIKPos(m_resultLeft, ikLeft, ikBottomPointLeft, m_ikFootHeightToUseLeft, m_ikFootForwardBias, m_transform.forward, m_transform.rotation, feetZeroPos, -m_transform.up, m_ikMaxCorrection, m_ikCorrectionRaycastMargin, maxBodyPositionCorrectionToUse, raycastFeetDistance, m_ikFootLengthToUseLeft, m_ikFootWidth, m_collisionLayerMask);
                
                if (m_resultLeft.primaryHitTransform != null)
                {
                    m_resultLeftLastTransformPosition = m_resultLeft.primaryHitTransform.position;
                    m_resultLeftLastTransformRotation = m_resultLeft.primaryHitTransform.rotation;
                }
            }

            if (ikRightMovementDistance > m_minimumMovementThreshold || forceUpdateRight)
            {
                m_prevIkRight = ikRight;
                findNewIKPos(m_resultRight, ikRight, ikBottomPointRight, m_ikFootHeightToUseRight, m_ikFootForwardBias, m_transform.forward, m_transform.rotation, feetZeroPos, -m_transform.up, m_ikMaxCorrection, m_ikCorrectionRaycastMargin, maxBodyPositionCorrectionToUse, raycastFeetDistance, m_ikFootLengthToUseRight, m_ikFootWidth, m_collisionLayerMask);

                if (m_resultRight.primaryHitTransform != null)
                {
                    m_resultRightLastTransformPosition = m_resultRight.primaryHitTransform.position;
                    m_resultRightLastTransformRotation = m_resultRight.primaryHitTransform.rotation;
                }
            }

            // handling edge-case when one leg has no detection found and the other has
            if (m_resultLeft.primaryHitTransform == null && m_resultRight.primaryHitTransform != null)
            {
                Vector3 rightOffsetVec = Vector3.Project(m_resultRight.surfacePoint - feetZeroPos, m_transform.up);
                if(Vector3.Dot(rightOffsetVec.normalized, m_transform.up) < 0) m_resultLeft.init(ikLeft, ikLeft + rightOffsetVec, false, ikBottomPointLeft + rightOffsetVec, ikBottomPointLeft + rightOffsetVec, m_transform.up, null, null);
            }
            else if (m_resultLeft.primaryHitTransform != null && m_resultRight.primaryHitTransform == null)
            {
                Vector3 leftOffsetVec = Vector3.Project(m_resultLeft.surfacePoint - feetZeroPos, m_transform.up);
                if(Vector3.Dot(leftOffsetVec.normalized, m_transform.up) < 0) m_resultRight.init(ikRight, ikRight + leftOffsetVec, false, ikBottomPointRight + leftOffsetVec, ikBottomPointRight + leftOffsetVec, m_transform.up, null, null);
            }

            // calculate body offset vector
            Vector3 leftRightBottomDiffVec = m_resultRight.ikPos - m_resultLeft.ikPos;
            Vector3 leftRightBottomDiffProjVec = Vector3.Project(leftRightBottomDiffVec, m_transform.up);

            Vector3 leftBottomVec = m_resultLeft.surfacePoint - feetZeroPos;
            Vector3 leftBottomProjVec = Vector3.Project(leftBottomVec, m_transform.up);

            Vector3 rightBottomVec = m_resultRight.surfacePoint - feetZeroPos;
            Vector3 rightBottomProjVec = Vector3.Project(rightBottomVec, m_transform.up);

            if (leftRightBottomDiffProjVec.magnitude > maxBodyPositionCorrectionToUse)
            {
                if (Vector3.Dot(rightBottomProjVec.normalized, m_transform.up) < 0)
                {
                    m_resultRight.init(ikRight, ikRight, false, ikBottomPointRight, ikBottomPointRight, m_transform.up, null, null);
                    rightBottomProjVec = Vector3.zero;
                }
                else
                {
                    m_resultLeft.init(ikLeft, ikLeft, false, ikBottomPointLeft, ikBottomPointLeft, m_transform.up, null, null);
                    leftBottomProjVec = Vector3.zero;
                }
            }

            m_rawbodyOffsetVec = Vector3.zero;
            Vector3 ikLeftOffsetVec = Vector3.zero;
            Vector3 ikRightOffsetVec = Vector3.zero;

            if (Vector3.Dot(leftBottomProjVec.normalized, m_transform.up) < 0)
            {
                ikLeftOffsetVec = leftBottomProjVec;

                if (Vector3.Dot(rightBottomProjVec.normalized, m_transform.up) < 0)
                {
                    ikRightOffsetVec = rightBottomProjVec;

                    if (rightBottomProjVec.magnitude > leftBottomProjVec.magnitude)
                    {
                        m_rawbodyOffsetVec = rightBottomProjVec;
                    }
                    else
                    {
                        m_rawbodyOffsetVec = leftBottomProjVec;
                    }
                }
                else
                {
                    m_rawbodyOffsetVec = leftBottomProjVec;
                }
            }
            else if (Vector3.Dot(rightBottomProjVec.normalized, m_transform.up) < 0)
            {
                ikRightOffsetVec = rightBottomProjVec;
                m_rawbodyOffsetVec = rightBottomProjVec;
            }
            else if (m_canBodyPositionMoveHigherThanGroundedPosition)
            {
                if (rightBottomProjVec.magnitude < leftBottomProjVec.magnitude)
                {
                    m_rawbodyOffsetVec = rightBottomProjVec;
                }
                else
                {
                    m_rawbodyOffsetVec = leftBottomProjVec;
                }

                // the ik point is not allowed to be lower than the original ik
                Vector3 correctedIkPos = ikLeft + m_rawbodyOffsetVec;
                Vector3 distVec = m_resultLeft.gluedIKPos - correctedIkPos;
                if (Vector3.Dot(distVec.normalized, -m_transform.up) > 0)
                {
                    m_resultLeft.ikPos = correctedIkPos;
                    m_resultLeft.isGlued = false;
                }

                correctedIkPos = ikRight + m_rawbodyOffsetVec;
                distVec = m_resultRight.gluedIKPos - correctedIkPos;
                if (Vector3.Dot(distVec.normalized, -m_transform.up) > 0)
                {
                    m_resultRight.ikPos = correctedIkPos;
                    m_resultRight.isGlued = false;
                }
            }

            if(m_bodyPositionInterpolationMethod == InterpolationMethod.LERP) m_bodyOffset = Vector3.Lerp(m_bodyOffset, m_rawbodyOffsetVec, Time.deltaTime * m_bodyStiffness);
            else m_bodyOffset = Vector3.SmoothDamp(m_bodyOffset, m_rawbodyOffsetVec, ref m_ikBodyPositionSmoothDampVelocity, 1.0f / m_bodyStiffness);
            m_animator.bodyPosition = bodyPosition + (m_bodyOffset + m_bodyPositionOffset * m_transform.up) * m_bodyPlacementWeight * m_bodyResetLerp;

            // set debug params
            m_currBodyPositionOffset = m_bodyOffset.magnitude;
            m_maxBodyPositionOffset = Mathf.Max(m_maxBodyPositionOffset, m_currBodyPositionOffset);

            // calculate ik positions
            // since the findNewIKPos function clamps the positions to not be lower than it's original IK, in the case that the ik algorithm
            // moves the bodyPosition downwards (can't happen on flat surfaces) we want to set the IK position downwards with the exact same value.
            // this makes the animations not being glued to the ground when the feet basically moves up + moves the feet downwards when the body position
            // is moved downwards as well. However, this is not moved by the same body offset vector, but by the respective feet downward vectors that may
            // or may not affect the body position, depending on the above case handling.
            Vector3 ikLeftTargetPos = m_resultLeft.ikPos + ikLeftOffsetVec * m_bodyPlacementWeight;
            Vector3 ikRightTargetPos = m_resultRight.ikPos + ikRightOffsetVec * m_bodyPlacementWeight;

            // fix so that the new ikTargetPos doesn't get lower than the lowest allowed (glued) position.
            Vector3 distVecLeft = ikLeftTargetPos - m_resultLeft.gluedIKPos;
            Vector3 distVecRight = ikRightTargetPos - m_resultRight.gluedIKPos;

            if (Vector3.Dot(distVecLeft.normalized, -m_transform.up) > 0)
            {
                ikLeftTargetPos = m_resultLeft.gluedIKPos;
                m_resultLeft.isGlued = true;
                m_resultLeft.ikPos = ikLeftTargetPos;
            }

            if (Vector3.Dot(distVecRight.normalized, -m_transform.up) > 0)
            {
                ikRightTargetPos = m_resultRight.gluedIKPos;
                m_resultRight.isGlued = true;
                m_resultRight.ikPos = ikRightTargetPos;
            }

            debugDrawPoint(ikLeftTargetPos, Color.magenta);
            debugDrawPoint(ikRightTargetPos, Color.magenta);

            Vector3 ikLeftOffset = ikLeftTargetPos - ikLeft;
            Vector3 ikRightOffset = ikRightTargetPos - ikRight;

            // extrapolation
            Vector3 leftOffsetDiff = ikLeftOffset - m_ikLeftOffset;
            Vector3 rightOffsetDiff = ikRightOffset - m_ikRightOffset;

            if (m_resultLeft.primaryHitTransform != null && leftOffsetDiff.magnitude > 0.01f)
            {
                if (Vector3.Dot(leftOffsetDiff.normalized, m_transform.up) > 0)
                {
                    if (isMovingUpwards) m_extrapolationLeft = leftOffsetDiff * m_ikUpwardsExtrapolation;
                    else m_extrapolationLeft = leftOffsetDiff * m_ikDownwardsExtrapolation;
                }
                else
                {
                    if(dotIsMovingUpwards > 0.01f) m_extrapolationLeft = m_bodyOffset.magnitude * leftOffsetDiff * m_ikUpwardsPullBackDownExtrapolation;
                    else m_extrapolationLeft = Vector3.zero;
                }
            }
            else m_extrapolationLeft = Vector3.zero;

            if (m_resultRight.primaryHitTransform != null && rightOffsetDiff.magnitude > 0.01f)
            {
                if (Vector3.Dot(rightOffsetDiff.normalized, m_transform.up) > 0)
                {
                    if (isMovingUpwards) m_extrapolationRight = rightOffsetDiff * m_ikUpwardsExtrapolation;
                    else m_extrapolationRight = rightOffsetDiff * m_ikDownwardsExtrapolation;
                }
                else
                {
                    if(dotIsMovingUpwards > 0.01f) m_extrapolationRight = m_bodyOffset.magnitude * rightOffsetDiff * m_ikUpwardsPullBackDownExtrapolation;
                    else m_extrapolationRight = Vector3.zero;
                }
            }
            else m_extrapolationRight = Vector3.zero;

            // apply positional offset
            Vector3 targetOffsetLeft = ikLeftOffset + m_extrapolationLeft;
            Vector3 targetOffsetRight = ikRightOffset + m_extrapolationRight;

            if (m_ikInterpolationMethod == InterpolationMethod.LERP)
            {
                float stiffnessLerpValue = Time.deltaTime * m_ikPlacementStiffness;
                m_ikLeftOffset = Vector3.Lerp(m_ikLeftOffset, targetOffsetLeft, stiffnessLerpValue);
                m_ikRightOffset = Vector3.Lerp(m_ikRightOffset, targetOffsetRight, stiffnessLerpValue);
            }
            else
            {
                m_ikLeftOffset = Vector3.SmoothDamp(m_ikLeftOffset, targetOffsetLeft, ref m_ikLeftOffsetSmoothDampVelocity, 1.0f / m_ikPlacementStiffness);
                m_ikRightOffset = Vector3.SmoothDamp(m_ikRightOffset, targetOffsetRight, ref m_ikRightOffsetSmoothDampVelocity, 1.0f / m_ikPlacementStiffness);
            }

            // this is a special case catch (which clamps the offsets), when the final offset length gets bigger than the downwards correction offset length (overshooting when moving feet downwards)
            if (m_antiDownwardsIntersectionStiffness > 0.01f)
            {
                Vector3 changeInBodyPosition = m_animator.bodyPosition - (m_prevBodyPosition + m_characterExternalMovingPlatformOffsetVec);
                if (changeInBodyPosition.magnitude > 0)
                {
                    Vector3 projChangeInBodyPos = Vector3.Project(changeInBodyPosition, -m_transform.up);
                    if (Vector3.Dot(projChangeInBodyPos.normalized, -m_transform.up) > 0)
                    {
                        float clampLerpValue = Mathf.Min(Time.deltaTime * m_antiDownwardsIntersectionStiffness, 1);
                        if (ikLeftOffset.magnitude > 0 && m_ikLeftOffset.magnitude > ikLeftOffset.magnitude && Vector3.Dot(ikLeftOffset.normalized, -m_transform.up) > 0 && Vector3.Dot(m_ikLeftOffset.normalized, -m_transform.up) > 0)
                        {
                            m_ikLeftOffset = Vector3.Lerp(m_ikLeftOffset, ikLeftOffset, clampLerpValue);
                            m_extrapolationLeft = Vector3.Lerp(m_extrapolationLeft, Vector3.zero, clampLerpValue);
                        }

                        if (ikRightOffset.magnitude > 0 && m_ikRightOffset.magnitude > ikRightOffset.magnitude && Vector3.Dot(ikRightOffset.normalized, -m_transform.up) > 0 && Vector3.Dot(m_ikRightOffset.normalized, -m_transform.up) > 0)
                        {
                            m_ikRightOffset = Vector3.Lerp(m_ikRightOffset, ikRightOffset, clampLerpValue);
                            m_extrapolationRight = Vector3.Lerp(m_extrapolationRight, Vector3.zero, clampLerpValue);
                        }
                    }
                }
            }
            m_prevBodyPosition = m_animator.bodyPosition;

            Vector3 nextIkLeft = ikLeft + m_ikLeftOffset;
            Vector3 nextIkRight = ikRight + m_ikRightOffset;

            m_animator.SetIKPosition(AvatarIKGoal.LeftFoot, nextIkLeft);
            m_animator.SetIKPosition(AvatarIKGoal.RightFoot, nextIkRight);

            m_animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, m_ikPlacementWeight * m_feetResetLerp);
            m_animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, m_ikPlacementWeight * m_feetResetLerp);

            // set debug params
            m_currIkCorrectionOffset = m_ikLeftOffset.magnitude;
            m_maxIkCorrectionOffset = Mathf.Max(m_maxIkCorrectionOffset, m_currIkCorrectionOffset);

            // crouching behaviour
            if (isCrouching())
            {
                float crouchOffset = 0.0f;
                if (Vector3.Dot(m_ikLeftOffset, m_transform.up) > 0) crouchOffset = Mathf.Max(crouchOffset, m_ikLeftOffset.magnitude);
                if (Vector2.Dot(m_ikRightOffset, m_transform.up) > 0) crouchOffset = Mathf.Max(crouchOffset, m_ikRightOffset.magnitude);
                m_crouchOffset = Vector3.Lerp(m_crouchOffset, (crouchOffset * m_transform.up - m_bodyOffset) * (1 - m_crouchCorrectionTolerance) * m_bodyPlacementWeight, Time.deltaTime * m_bodyStiffness);
            }
            else m_crouchOffset = Vector3.Lerp(m_crouchOffset, Vector3.zero, Time.deltaTime * m_bodyStiffness);
            m_animator.bodyPosition += m_crouchOffset;

            // calculate hint positions
            leftUpperLegPos += (m_animator.bodyPosition - bodyPosition);
            rightUpperLegPos += (m_animator.bodyPosition - bodyPosition);

            Vector3 leftHintFinalPos = calculateHintPos(leftUpperLegPos, nextIkLeft, lowerLeft, upperLeft, ikLeftToBodyPosOriginalVec, leftHintDirVec);
            Vector3 rightHintFinalPos = calculateHintPos(rightUpperLegPos, nextIkRight, lowerRight, upperRight, ikRightToBodyPosOriginalVec, rightHintDirVec);

            m_animator.SetIKHintPosition(AvatarIKHint.LeftKnee, leftHintFinalPos);
            m_animator.SetIKHintPosition(AvatarIKHint.RightKnee, rightHintFinalPos);

            m_animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, m_ikPlacementWeight * m_feetResetLerp);
            m_animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, m_ikPlacementWeight * m_feetResetLerp);

            // glue fix
            if ((m_ikLeftOffset.magnitude / targetOffsetLeft.magnitude) > m_isGluedToGroundNormalizedThreshold || m_ikLeftOffset.magnitude < m_isGluedToGroundDistanceThreshold) m_resultLeft.isGlued = true;
            if ((m_ikRightOffset.magnitude / targetOffsetRight.magnitude) > m_isGluedToGroundNormalizedThreshold || m_ikRightOffset.magnitude < m_isGluedToGroundDistanceThreshold) m_resultRight.isGlued = true;

            // calculate ik rotations
            float rotationStiffnessSlerpValue = Time.deltaTime * m_ikRotationStiffness;
            if (m_resultLeft.isGlued || m_onlyAdaptRotationWhenGluedToGround == false)
            {
                Vector3 normalLeftFoot = m_resultLeft.normal;
                Vector3 axis = Vector3.Cross(m_transform.up, normalLeftFoot);
                float angle = Vector3.SignedAngle(m_transform.up, normalLeftFoot, axis);
                angle = Mathf.Clamp(angle, -m_maxAnkleRotationAdaptionAngle, m_maxAnkleRotationAdaptionAngle);
                Quaternion deltaLeft = Quaternion.AngleAxis(angle, axis);
                //Quaternion deltaLeft = Quaternion.FromToRotation(m_transform.up, normalLeftFoot);
                m_ikLeftDeltaRotation = Quaternion.Slerp(m_ikLeftDeltaRotation, deltaLeft, rotationStiffnessSlerpValue);
            }
            else
            {
                m_ikLeftDeltaRotation = Quaternion.Slerp(m_ikLeftDeltaRotation, Quaternion.identity, rotationStiffnessSlerpValue);
            }

            if (m_resultRight.isGlued || m_onlyAdaptRotationWhenGluedToGround == false)
            {
                Vector3 normalRightFoot = m_resultRight.normal;
                Vector3 axis = Vector3.Cross(m_transform.up, normalRightFoot);
                float angle = Vector3.SignedAngle(m_transform.up, normalRightFoot, axis);
                angle = Mathf.Clamp(angle, -m_maxAnkleRotationAdaptionAngle, m_maxAnkleRotationAdaptionAngle);
                Quaternion deltaRight = Quaternion.AngleAxis(angle, axis);
                //Quaternion deltaRight = Quaternion.FromToRotation(m_transform.up, normalRightFoot);
                m_ikRightDeltaRotation = Quaternion.Slerp(m_ikRightDeltaRotation, deltaRight, rotationStiffnessSlerpValue);
            }
            else
            {
                m_ikRightDeltaRotation = Quaternion.Slerp(m_ikRightDeltaRotation, Quaternion.identity, rotationStiffnessSlerpValue);
            }

            Quaternion nextRotLeft = m_ikLeftDeltaRotation * currRotLeft;
            Quaternion nextRotRight = m_ikRightDeltaRotation * currRotRight;
            m_animator.SetIKRotation(AvatarIKGoal.LeftFoot, nextRotLeft);
            m_animator.SetIKRotation(AvatarIKGoal.RightFoot, nextRotRight);

            m_animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, m_ikPlacementWeight * m_feetResetLerp);
            m_animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, m_ikPlacementWeight * m_feetResetLerp);

            updateEvents(ikLeftTargetPos, ikRightTargetPos, transformMovementVec - transformMovementVerticalVec);
        }

        protected Vector3 calculateHintPos(Vector3 bodyPos, Vector3 nextIkPos, float lowerLength, float upperLength, Vector3 ikPosToBodyPosOriginalVec, Vector3 hintDirectionVec)
        {
            Vector3 nextIkPosToBodyPosVec = bodyPos - nextIkPos;
            Vector3 nextIkPosToBodyPosVecNormalized = nextIkPosToBodyPosVec.normalized;

            float a = upperLength;
            float b = lowerLength;
            float c = nextIkPosToBodyPosVec.magnitude;

            // to avoid isNan
            if (b > a && b > c) b = Mathf.Min(b, (c + a) * 0.995f);
            else if (a > b && a > c) a = Mathf.Min(a, (c + b) * 0.995f);
            else c = Mathf.Min(c, (a + b) * 0.995f);

            float alphaRad = Mathf.Acos((b * b + c * c - a * a) / (2 * b * c));

            float xFactor = 0.5f;
            float yFactor = 0.5f;

            if (float.IsNaN(alphaRad) == false)
            {
                xFactor = Mathf.Cos(alphaRad) * b;
                yFactor = Mathf.Sin(alphaRad) * b;
            }

            Quaternion rot = Quaternion.FromToRotation(ikPosToBodyPosOriginalVec.normalized, nextIkPosToBodyPosVecNormalized);
            hintDirectionVec = rot * hintDirectionVec;
            Vector3 upwardsDirVec = hintDirectionVec - Vector3.Project(hintDirectionVec, nextIkPosToBodyPosVecNormalized);

            return nextIkPos + xFactor * nextIkPosToBodyPosVecNormalized + yFactor * upwardsDirVec.normalized;

            ////ALTERNATE COMPUTATION - delivers basically the same results
            //Vector3 AC_vec = bodyPos - nextIkPos;
            //float a = upperLength;
            //float b = AC_vec.magnitude;
            //float c = lowerLength;

            //if (b > a && b > c) b = Mathf.Min(b, (c + a) * 0.995f);
            //else if (a > b && a > c) a = Mathf.Min(a, (c + b) * 0.995f);
            //else c = Mathf.Min(c, (a + b) * 0.995f);

            //Vector3 nextIkPosToBodyPosVec = bodyPos - nextIkPos;
            //Vector3 nextIkPosToBodyPosVecNormalized = nextIkPosToBodyPosVec.normalized;
            //Quaternion rot = Quaternion.FromToRotation(ikPosToBodyPosOriginalVec.normalized, nextIkPosToBodyPosVecNormalized);
            //hintDirectionVec = rot * hintDirectionVec;

            //Vector3 rotationAxis = Vector3.Cross(AC_vec.normalized, hintDirectionVec.normalized).normalized;
            //float acosInternal = (b * b + c * c - a * a) / (2 * b * c);
            //float alpha = 45;
            //if (Mathf.Abs(acosInternal) < 1.0f) alpha = Mathf.Acos(acosInternal) * Mathf.Rad2Deg;
            //Vector3 directionVec = Quaternion.AngleAxis(alpha, rotationAxis) * AC_vec.normalized;

            //return nextIkPos + directionVec * c;
        }

        protected virtual bool isCrouching()
        {
            if (m_activateCrouchSpecificBehaviour) return true;

            foreach (var entry in m_activateCrouchingOnAnimatorState)
            {
                if (m_animator.layerCount > entry.Key)
                {
                    var nextAnimState = m_animator.GetNextAnimatorStateInfo(entry.Key);
                    if (nextAnimState.shortNameHash == entry.Value && nextAnimState.normalizedTime > 0) return true;

                    bool hasStartedTransitionToAnotherState = nextAnimState.shortNameHash != entry.Value && nextAnimState.normalizedTime > 0;
                    if (hasStartedTransitionToAnotherState == false && m_animator.GetCurrentAnimatorStateInfo(entry.Key).shortNameHash == entry.Value) return true;
                }
            }

            return false;
        }

        protected virtual void updateEvents(Vector3 ikLeftTargetPos,Vector3 ikRightTargetPos, Vector3 transformMovementPlanarVec)
        {
            float distToGroundLeft = Vector3.Project(m_resultLeft.surfacePoint - ikLeftTargetPos, m_transform.up).magnitude;
            float distToGroundRight = Vector3.Project(m_resultRight.surfacePoint - ikRightTargetPos, m_transform.up).magnitude;

            Vector3 movedLeftVec = m_resultLeft.surfacePoint - (m_lastFootstepPosLeft + m_characterExternalMovingPlatformOffsetVec);
            Vector3 movedRightVec = m_resultRight.surfacePoint - (m_lastFootstepPosRight + m_characterExternalMovingPlatformOffsetVec);
            Vector3 upwardsLeftDirVec = Vector3.Project(movedLeftVec, m_transform.up);
            Vector3 upwardsLRightDirVec = Vector3.Project(movedRightVec, m_transform.up);
            movedLeftVec = movedLeftVec - upwardsLeftDirVec;
            movedRightVec = movedRightVec - upwardsLRightDirVec;
            m_elapsedFootstepLeftDistance += movedLeftVec.magnitude;
            m_elapsedFootstepRightDistance += movedRightVec.magnitude;

            bool isStanding = true;
            if(Time.deltaTime > 0) isStanding = (transformMovementPlanarVec.magnitude / Time.deltaTime) < 0.01f;
            if (m_canIgnoreLastEventFiredWasLeft && m_ignoreLastEventFiredWasLeft == false && isStanding)
            {
                m_ignoreLastEventFiredWasLeft = true;
                m_canIgnoreLastEventFiredWasLeft = false;
                m_elapsedFootstepLeftDistance = 0.0f;
                m_elapsedFootstepRightDistance = 0.0f;
            }
            else if (isStanding == false)
            {
                m_canIgnoreLastEventFiredWasLeft = true;
            }

            // fire for left foot
            if ((m_ignoreLastEventFiredWasLeft || !m_lastEventFiredWasLeft) && distToGroundLeft < (m_ikFootHeight + m_eventCheckResetDistance) && m_elapsedFootstepLeftDistance > m_stepLength)
            {
                if(m_footstepEventDelay > 0.001f)
                {
                    StartCoroutine(playFootstepLeftEventDelayed(m_footstepEventDelay));
                }
                else
                {
                    if (m_onFootstepLeftStart != null) m_onFootstepLeftStart.Invoke(m_resultLeft);
                    if (m_onFootstepRightStop != null) m_onFootstepRightStop.Invoke();
                }

                m_elapsedFootstepLeftDistance = 0.0f;
                m_elapsedFootstepRightDistance = 0.0f;
                m_lastEventFiredWasLeft = true;
                m_ignoreLastEventFiredWasLeft = false;
            }
            // fire for right foot
            else if ((m_ignoreLastEventFiredWasLeft || m_lastEventFiredWasLeft) && distToGroundRight < (m_ikFootHeight + m_eventCheckResetDistance) && m_elapsedFootstepRightDistance > m_stepLength)
            {
                if (m_footstepEventDelay > 0.001f)
                {
                    StartCoroutine(playFootstepRightEventDelayed(m_footstepEventDelay));
                }
                else
                {
                    if (m_onFootstepRightStart != null) m_onFootstepRightStart.Invoke(m_resultRight);
                    if (m_onFootstepLeftStop != null) m_onFootstepLeftStop.Invoke();
                }

                m_elapsedFootstepLeftDistance = 0.0f;
                m_elapsedFootstepRightDistance = 0.0f;
                m_lastEventFiredWasLeft = false;
                m_ignoreLastEventFiredWasLeft = false;
            }

            m_lastFootstepPosLeft = m_resultLeft.surfacePoint;
            m_lastFootstepPosRight = m_resultRight.surfacePoint;
        }

        IEnumerator playFootstepLeftEventDelayed(float delay)
        {
            if(Mathf.Approximately(delay, m_prevFootstepEventDelay) == false)
            {
                m_prevFootstepEventDelay = delay;
                m_footstepEventWaitForSeconds = new WaitForSeconds(delay);
            }

            yield return m_footstepEventWaitForSeconds;

            if (m_onFootstepLeftStart != null) m_onFootstepLeftStart.Invoke(m_resultLeft);
            if (m_onFootstepRightStop != null) m_onFootstepRightStop.Invoke();
        }

        IEnumerator playFootstepRightEventDelayed(float delay)
        {
            if (Mathf.Approximately(delay, m_prevFootstepEventDelay) == false)
            {
                m_prevFootstepEventDelay = delay;
                m_footstepEventWaitForSeconds = new WaitForSeconds(delay);
            }

            yield return m_footstepEventWaitForSeconds;

            if (m_onFootstepRightStart != null) m_onFootstepRightStart.Invoke(m_resultRight);
            if (m_onFootstepLeftStop != null) m_onFootstepLeftStop.Invoke();
        }

        protected virtual void revertUpdateIK()
        {
            // reset to default body center of mass position
            // reset to default IKs and weights
            m_feetResetLerp = Mathf.Lerp(m_feetResetLerp, 0, Time.deltaTime * m_ikPlacementStiffness * m_resetToDefaultValuesSpeed);
            m_bodyResetLerp = Mathf.Lerp(m_bodyResetLerp, 0, Time.deltaTime * m_bodyStiffness * m_resetToDefaultValuesSpeed);

            // body position
            m_animator.bodyPosition = m_animator.bodyPosition + (m_bodyOffset + m_bodyPositionOffset * m_transform.up) * m_bodyPlacementWeight * m_bodyResetLerp;

            // additional crouching body pos
            m_crouchOffset = Vector3.Lerp(m_crouchOffset, Vector3.zero, Time.deltaTime * m_bodyStiffness);
            m_animator.bodyPosition += m_crouchOffset;

            // ik positions
            Vector3 ikLeft = m_animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 ikRight = m_animator.GetIKPosition(AvatarIKGoal.RightFoot);

            Vector3 ikLeftOffset = Vector3.Lerp(Vector3.zero, m_ikLeftOffset, m_feetResetLerp);
            Vector3 ikRightOffset = Vector3.Lerp(Vector3.zero, m_ikRightOffset, m_feetResetLerp);
            Vector3 extrapolationLeft = Vector3.Lerp(Vector3.zero, m_extrapolationLeft, m_feetResetLerp);
            Vector3 extrapolationRight = Vector3.Lerp(Vector3.zero, m_extrapolationRight, m_feetResetLerp);

            if (m_globalCharacterYOffset > 0 || m_globalCharacterYOffset < 0)
            {
                Vector3 globalCharOffsetVec = m_globalCharacterYOffset * m_transform.up;
                ikLeft += globalCharOffsetVec;
                ikRight += globalCharOffsetVec;
                m_animator.bodyPosition += globalCharOffsetVec;

                if (m_applyGlobalCharacterYOffsetToOtherIKsToo)
                {
                    m_animator.SetIKPosition(AvatarIKGoal.LeftHand, m_animator.GetIKPosition(AvatarIKGoal.LeftHand) + globalCharOffsetVec);
                    m_animator.SetIKPosition(AvatarIKGoal.RightHand, m_animator.GetIKPosition(AvatarIKGoal.RightHand) + globalCharOffsetVec);
                    m_animator.SetIKHintPosition(AvatarIKHint.LeftElbow, m_animator.GetIKHintPosition(AvatarIKHint.LeftElbow) + globalCharOffsetVec);
                    m_animator.SetIKHintPosition(AvatarIKHint.RightElbow, m_animator.GetIKHintPosition(AvatarIKHint.RightElbow) + globalCharOffsetVec);
                }

                m_animator.SetIKHintPosition(AvatarIKHint.LeftKnee, m_animator.GetIKHintPosition(AvatarIKHint.LeftKnee) + globalCharOffsetVec);
                m_animator.SetIKHintPosition(AvatarIKHint.RightKnee, m_animator.GetIKHintPosition(AvatarIKHint.RightKnee) + globalCharOffsetVec);
            }

            Vector3 nextIkLeft = ikLeft + ikLeftOffset + extrapolationLeft;
            Vector3 nextIkRight = ikRight + ikRightOffset + extrapolationRight;
            m_animator.SetIKPosition(AvatarIKGoal.LeftFoot, nextIkLeft);
            m_animator.SetIKPosition(AvatarIKGoal.RightFoot, nextIkRight);

            m_animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, m_feetResetLerp * m_ikPlacementWeight);
            m_animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, m_feetResetLerp * m_ikPlacementWeight);

            // hint positions
            m_animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, m_ikPlacementWeight * m_feetResetLerp);
            m_animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, m_ikPlacementWeight * m_feetResetLerp);

            // ik rotations
            Quaternion currRotLeft = m_animator.GetIKRotation(AvatarIKGoal.LeftFoot);
            Quaternion currRotRight = m_animator.GetIKRotation(AvatarIKGoal.RightFoot);

            m_ikLeftDeltaRotation = Quaternion.Slerp(Quaternion.identity, m_ikLeftDeltaRotation, m_feetResetLerp);
            m_ikRightDeltaRotation = Quaternion.Slerp(Quaternion.identity, m_ikRightDeltaRotation, m_feetResetLerp);

            m_animator.SetIKRotation(AvatarIKGoal.LeftFoot, m_ikLeftDeltaRotation * currRotLeft);
            m_animator.SetIKRotation(AvatarIKGoal.RightFoot, m_ikRightDeltaRotation * currRotRight);

            m_animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, m_feetResetLerp * m_ikPlacementWeight);
            m_animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, m_feetResetLerp * m_ikPlacementWeight);

            // events
            if (m_isWaitingForPlayingLeftFootEvent == false && m_onFootstepLeftStop != null) m_onFootstepLeftStop.Invoke();
            if (m_isWaitingForPlayingRightFootEvent == false && m_onFootstepRightStop != null) m_onFootstepRightStop.Invoke();
            m_isWaitingForPlayingLeftFootEvent = true;
            m_isWaitingForPlayingRightFootEvent = true;
            m_lastLeftIKStepEventPos = nextIkLeft;
            m_lastRightIKStepEventPos = nextIkRight;
        }

#if UNITY_EDITOR
        private HashSet<Collider> m_errorLogged = new HashSet<Collider>();
#endif

        protected virtual void findNewIKPos(IKResult ikResult, Vector3 ikPos, Vector3 ikBottomPoint, float heightOffset, float forwardBias, Vector3 forwardDir, Quaternion rotation, Vector3 origin, Vector3 rayDirection, float maxIKCorrection, float ikCorrectionRaycastMargin, float maxFootCorrection, float maxCenterOfMassCorrection, float raycastLength, float raycastWidth, int raycastLayer)
        {
            float boxCastHeight = Mathf.Max(heightOffset * 0.01f, 0.01f);

            Vector3 rayOriginPoint = ikBottomPoint - rayDirection * (maxIKCorrection + boxCastHeight + ikCorrectionRaycastMargin);
            Vector3 rayOrigin = rayOriginPoint + forwardBias * forwardDir;

            debugDrawPoint(rayOrigin, Color.blue);
            debugDrawPoint(ikPos, Color.yellow);

            Ray ray = new Ray(rayOrigin, rayDirection);
            RaycastHit hit;
            float raycastDistance = maxIKCorrection + maxCenterOfMassCorrection + ikCorrectionRaycastMargin;

            if (Physics.BoxCast(ray.origin, new Vector3(raycastWidth * 0.5f, boxCastHeight, raycastLength * 0.5f), ray.direction, out hit, rotation, raycastDistance, raycastLayer, m_triggerCollisionInteraction))
            {
                Vector3 hitPoint = rayOrigin + hit.distance * rayDirection + boxCastHeight * rayDirection;
                Vector3 foundBottomPoint = hitPoint - forwardDir * forwardBias;
                Vector3 originalFoundIKPoint = foundBottomPoint - rayDirection * heightOffset;

                // normal fix when IsNaN (this breaks BoxCast independently of the fix, however it stops destroying the whole visuals when this happens)
                if (float.IsNaN(hit.normal.x) || float.IsNaN(hit.normal.y) || float.IsNaN(hit.normal.z))
                {
                    hit.normal = -rayDirection;

#if UNITY_EDITOR
                    if(m_errorLogged.Contains(hit.collider) == false)
                    {
                        Debug.LogError("iStep detected a collider (most likely a BoxCollider) with size in an axis near to ~0 which breaks Unity Physics (BoxCast). Please correct the broken collider [GameObject: " + hit.collider.name + "].");
                        m_errorLogged.Add(hit.collider);
                    }
#endif
                }

                // boxcast slope offset fix
                Vector3 normalToUse = hit.normal;
                float alpha = Vector3.Angle(-rayDirection, normalToUse);

                RaycastHit fixHit;
                float radius = heightOffset;
                Vector3 fixOrigin = rayOriginPoint - radius * rayDirection;
                Vector3? ikPointFixHit = null;
                if (Physics.SphereCast(fixOrigin, radius, rayDirection, out fixHit, raycastDistance, raycastLayer, m_triggerCollisionInteraction))
                {
                    ikPointFixHit = fixOrigin + fixHit.distance * rayDirection;

                    float beta = Vector3.Angle(-rayDirection, fixHit.normal);

                    if (Mathf.Abs(beta - alpha) > 1.0f)
                    {
                        Vector3 hitpointsVec = fixHit.point - hit.point;
                        if (hitpointsVec.magnitude > 0.001f)
                        {
                            hitpointsVec.Normalize();
                            Vector3 r = Vector3.Cross(hitpointsVec, -rayDirection).normalized;
                            Vector3 fixNormal = Vector3.Cross(r, hitpointsVec).normalized;
                            float fixAlpha = Vector3.Angle(-rayDirection, fixNormal);

                            if (Mathf.Abs(fixAlpha) < Mathf.Abs(alpha))
                            {
                                normalToUse = fixNormal;
                                alpha = fixAlpha;

                                if (Mathf.Abs(beta) > Mathf.Abs(alpha))
                                {
                                    alpha = beta;
                                    normalToUse = fixHit.normal;
                                }
                            }
                        }
                    }
                }

                if (Mathf.Abs(alpha) > m_maxGroundAngleAtWhichTheGroundIsDetectedAsGround)
                {
                    ikResult.init(ikPos, ikPos, false, ikBottomPoint, ikBottomPoint, -rayDirection, null, null);
                    return;
                }

                alpha = Mathf.Clamp(alpha, -m_maxGroundAngleTheFeetsCanAdaptTo, m_maxGroundAngleTheFeetsCanAdaptTo);

                Vector3 pointsDistVec = foundBottomPoint - hit.point;
                float l = pointsDistVec.magnitude;
                Vector3 c1 = Vector3.Cross(pointsDistVec.normalized, normalToUse).normalized;
                Vector3 c2 = Vector3.Cross(normalToUse, c1).normalized;
                float cosAlpha = Mathf.Cos(alpha * Mathf.Deg2Rad);

                if (cosAlpha > 0 && cosAlpha < 0.01f) cosAlpha = 0.01f;
                else if (cosAlpha < 0 && cosAlpha > -0.01f) cosAlpha = -0.01f;

                float ll = l / cosAlpha;
                Vector3 angleOffset = Vector3.Project(c2 * ll, -rayDirection);
                float realHeightOffset = heightOffset / cosAlpha;

                foundBottomPoint = foundBottomPoint + angleOffset;
                Vector3 foundIKPos = foundBottomPoint - rayDirection * realHeightOffset;
                Vector3 surfacePoint = foundIKPos + rayDirection * heightOffset;

                Vector3 movementDirVec = foundIKPos - originalFoundIKPoint;
                if (ikPointFixHit.HasValue && Vector3.Dot(movementDirVec.normalized, -rayDirection) > 0)
                {
                    Vector3 ikPointsDistVec = ikPointFixHit.Value - foundIKPos;

                    if (Vector3.Dot(ikPointsDistVec.normalized, rayDirection) > 0)
                    {
                        foundIKPos += ikPointsDistVec;
                        surfacePoint += ikPointsDistVec;
                        foundBottomPoint += ikPointsDistVec;
                    }
                }

                // the ik point is not allowed to be lower than the original ik
                bool isGlued = true;
                Vector3 gluedIkPos = foundIKPos;
                Vector3 distVec = foundIKPos - ikPos;
                if (Vector3.Dot(distVec.normalized, rayDirection) > 0)
                {
                    foundIKPos = ikPos;
                    isGlued = false;

                    // surface and bottom point are not allowed to be lower than max allowed lowest
                    if (distVec.magnitude > maxFootCorrection)
                    {
                        Vector3 surfaceOffsetVec = -rayDirection * (distVec.magnitude - maxFootCorrection);
                        surfacePoint = surfacePoint + surfaceOffsetVec;
                        foundBottomPoint = foundBottomPoint + surfaceOffsetVec;
                    }
                }
                else
                {
                    // the ik point is not allowed to be higher than the max correction
                    distVec = foundIKPos - ikBottomPoint;
                    if (distVec.magnitude > (maxIKCorrection + heightOffset))
                    {
                        foundIKPos = ikBottomPoint + distVec.normalized * (maxIKCorrection + heightOffset);
                        gluedIkPos = foundIKPos;
                    }
                }

                debugDrawPoint(hitPoint, Color.blue);
                debugDrawPoint(surfacePoint, Color.black);
                debugDrawPoint(foundBottomPoint, Color.red);
                debugDrawPoint(foundIKPos, Color.cyan);
                debugDrawArrow(rayOrigin, hitPoint, Color.blue);

                ikResult.init(foundIKPos, gluedIkPos, isGlued, foundBottomPoint, surfacePoint, normalToUse, hit.transform, fixHit.transform);
                return;
            }

            ikResult.init(ikPos, ikPos, false, ikBottomPoint, ikBottomPoint, -rayDirection, null, null);
        }

        private void debugDrawPoint(Vector3 point, Color color)
        {
#if UNITY_EDITOR
            Debug.DrawLine(point - Vector3.right * 0.025f, point + Vector3.right * 0.025f, color);
            Debug.DrawLine(point - Vector3.forward * 0.025f, point + Vector3.forward * 0.025f, color);
            Debug.DrawLine(point - Vector3.up * 0.025f, point + Vector3.up * 0.025f, color);
#endif
        }

        private void debugDrawArrow(Vector3 start, Vector3 end, Color color)
        {
#if UNITY_EDITOR
            Debug.DrawLine(start, end, color);
            Debug.DrawLine(end, end + Vector3.up * 0.025f + Vector3.right * 0.025f, color);
            Debug.DrawLine(end, end + Vector3.up * 0.025f - Vector3.right * 0.025f, color);
#endif
        }

        protected virtual void isValidAndGrounded(GroundedResult groundedResult, Vector3 feetZeroPos, Vector3 upVec, int collisionLayer)
        {
            // general false validation
            if (m_animator == null)
            {
                groundedResult.init(false, false, null, Vector3.zero, Vector3.zero, Vector3.zero);
                return;
            }
            if (m_animator.enabled == false)
            {
                groundedResult.init(false, false, null, Vector3.zero, Vector3.zero, Vector3.zero);
                return;
            }

            bool isValid = true;

            if (m_validationType == ValidationType.FORCE_INVALID)
            {
                isValid = false;
            }
            else if (m_validationType == ValidationType.FORCE_VALID)
            {
                groundedResult.init(true, true, m_transform, feetZeroPos, feetZeroPos, upVec);
                return;
            }
            // else -> Check_Is_Grounded

            foreach (var entry in m_deactiveOnAnimatorState)
            {
                if (m_animator.layerCount > entry.Key)
                {
                    var nextAnimState = m_animator.GetNextAnimatorStateInfo(entry.Key);
                    if (nextAnimState.shortNameHash == entry.Value && nextAnimState.normalizedTime > 0) isValid = false;

                    bool hasStartedTransitionToAnotherState = nextAnimState.shortNameHash != entry.Value && nextAnimState.normalizedTime > 0;
                    if (hasStartedTransitionToAnotherState == false && m_animator.GetCurrentAnimatorStateInfo(entry.Key).shortNameHash == entry.Value) isValid = false;
                }
            }

            // if we moved up, validation can be false
            if (m_autoUngroundUpwardsVelocity > 0)
            {
                Vector3 distVec = Vector3.Project(feetZeroPos - (m_prevTransformPosition + m_characterExternalMovingPlatformOffsetVec), upVec);

                float dot = Vector3.Dot(distVec.normalized, upVec);
                if (dot > 0.975f)
                {
                    m_upwardsVelocity = 0;
                    if(Time.deltaTime > 0) m_upwardsVelocity = distVec.magnitude / Time.deltaTime;
                    m_maxUpwardsVelocity = Mathf.Max(m_upwardsVelocity, m_maxUpwardsVelocity);

                    if (m_upwardsVelocity > m_autoUngroundUpwardsVelocity)
                    {
                        groundedResult.init(isValid, false, null, Vector3.zero, Vector3.zero, Vector3.zero);
                        return;
                    }
                }
            }

            // if we are not grounded validation is false
            float raycastUpwardsOffset = m_checkGroundedRadius + m_checkGroundedDistance;
            float raycastDistance = raycastUpwardsOffset + m_checkGroundedDistance - m_checkGroundedRadius;

            RaycastHit hit;
            Vector3 origin = feetZeroPos + upVec * raycastUpwardsOffset;
            if (Physics.SphereCast(origin, m_checkGroundedRadius, -upVec, out hit, raycastDistance, collisionLayer, m_triggerCollisionInteraction))
            {
                groundedResult.init(isValid, true, hit.transform, hit.point, origin - upVec * (hit.distance + m_checkGroundedRadius), hit.normal);
                return;
            }

            groundedResult.init(isValid, false, null, Vector3.zero, Vector3.zero, Vector3.zero);
        }
    }
}