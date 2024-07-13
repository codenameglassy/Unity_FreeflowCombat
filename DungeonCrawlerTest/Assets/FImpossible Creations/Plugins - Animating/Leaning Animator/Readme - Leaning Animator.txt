__________________________________________________________________________________________

Package "Leaning Animator"
Version 1.0.2.7

Made by FImpossible Creations - Filip Moeglich
https://www.FilipMoeglich.pl
FImpossibleCreations@Gmail.com or FImpossibleGames@Gmail.com or Filip.Moeglich@Gmail.com

__________________________________________________________________________________________

Youtube: https://www.youtube.com/channel/UCDvDWSr6MAu1Qy9vX4w8jkw
Facebook: https://www.facebook.com/FImpossibleCreations
Twitter (@FimpossibleC): https://twitter.com/FImpossibleC

___________________________________________________

User Manual:

To use Leaning Animator simply add it to your game movement controller game object.
Assign bone transforms you want to animate.
"Foots Origin" is root bone of your skeleton with center position of foots/ground level of your character model.
Define "Object Speed When Braking" and "Object Speed When Running" to make component handle animations accordingly to your object movement speeds.
You can check this values during playmode, on the bottom of inspector window of this component there will be displayed speed of your controller which you can assign to "Object Speed When Running".
I recommend disabling "Try Auto Detect Acceleration" and "Try Auto Detect Ground" and instead add some lines in your custom character controller script.
Lines like leaningAnimator.SetIsAccelerating = myMovementController.IsAccelerating; and leaningAnimator.SetIsGrounded = myMovementController.Grounded; something similar to that.

Now you can hit gear icon on the top left and switch to tweaking section.

In tweaking section during edit mode, enter on fields to display tooltips.
After tweaking enter playmode and tweak everything during it, before closing playmode
hit right mouse button on the component, hit "Copy Component" then close playmode,
now during edit mode click again right mouse button on the component and hit
"Paste Component Values" then all changed done in playmode are saved.

__________________________________________________________________________________________
Description:

Boost feeling of your biped characters movement animations with procedural
leaning algorithms for whole object, spine bones and arms!

Instant setup with few clicks and then tweak it to your needs!
Component is dedicated to biped/humanoidal characters, works on
any animation setups, no matter if it's humanoid, generic or legacy type.

MAIN FEATURES:
• Fast setup
• Good performance
• Clean and compact inspector window
• Works on all types of skeleton

__________________________________________________________________________________________
Changelog:

Version 1.0.2.7:
- Support for runtime generated Leaning Animator (call newLeaningAnimator.Reset() on AddComponent)

Version 1.0.2.6:
- Added User_AfterTeleport() method to avoid leaning animator motion after teleporting character to the new placement

Version 1.0.2.5:
- Ground align wasn't working with 'Try Detect Ground' disabled
- Added message if using animator property for `Is Moving` to switch to the custom detection mode
- Updated Demo Scene

Version 1.0.2.4:
- Animate physics update mode fix

Version 1.0.2.3:
- Added Reset On Ungrounded switch under Advanced Features settings

Version 1.0.2.2:
- Update for leaning effects blending

Version 1.0.2.1:
- Parameter to fade in/out leaning animator effect seamlessly (Fade Off Leaning Param)

Version 1.0.2:
- Fixed lean strafe factor when running towards left side
- Added re-triggering lean when running left-right, forward-back in a single run without stopping character
- Added control parameter for backward lean when running back facing forward
- Added possibility to read "Is Accelerating" and "Is Grounded" through unity's Animator variables when enabling custom detection mode
- When foots origin is not placed in model's foot then correct transform will be automatically generated during component start
- Few small improvements

Version 1.0.1:
-Added parameters "Update Mode" and "Calibrate" to support animators with "Animate Physics"
-Added parameter "Acceleration Detection" replacing "TryAutoDetectAcceleration" to suppoert non-rigidbody controllers when using auto detection
-Added parameter "Clamp Spine Sway" to limit spine maximum sway rotation angle
-Now side leaning should be supported better in different frames-per-second domains