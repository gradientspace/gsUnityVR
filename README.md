# gsUnityVR
C# library for using VR and Spatial Controllers (HTC Vive Wands, Oculus Touch) in Unity

Includes Oculus VR for Unity (OVR) and Steam VR (SteamVR). Not necessarily the most recent versions! But we try.

The \Code\ Subdirectory is under the MIT License. The other Subdirectories are under the respective OVR and SteamVR licenses, see their folders for details.

Questions? Contact Ryan Schmidt [@rms80](http://www.twitter.com/rms80) / [gradientspace](http://www.gradientspace.com)


# What is this??

If you are building something for VR, the standard way to do it is to set up a Scene with a Prefab that contains the HMD Camera and Motion Controllers. The OVR library for Rift comes with **OVRCameraRig** and SteamVR comes with **[CameraRig]**. The problem comes when you want to support both Vive and Rift. Either you maintain two separate Scenes, with a loader that can start either (ugh!) or you forego OVR and just use SteamVR (no way!).

And even if you go the separate-scenes route, the APIs to access the motion controller positions / buttons / etc is completely different. So then you will have branching everywhere.

The point of this library is to deal with this mess. gsUnityVR does two things:

1. It provides a (mostly) platform-agnostic interface to the motion controllers
2. It can load and configure the Camera Rig Prefab appropriate for the active VR platform at scene start-up

The second one is a bit tricky because the Touch controllers and Vive wands have quite different capabilities. In places where there is really no similarity I have provided an accessor that returns false or 0 on the alternative devices. 

This all happens through a single static class, **VRPlatform**. Documentation is below.

Hopefully, someday, this package will not be necessary. But that day might be a long time coming...


# Usage

If you are starting from scratch, you can set up an emptyUunity project to use gsUnityVR in about 30 seconds. Here are the steps:

1. create a new scene
2. In **Player Settings**, under **Other Settings**, check **Virtual Reality Supported**, and then in the *Virtual Reality SDKs* section, add the **Oculus** and **OpenVR** SDKs (make sure Oculus is first)
3. Assign the script `/gsUnityVR/Code/ConvertToVRSpatialRig.cs` to the **Main Camera** object in your scene.

When you hit Play, the *Main Camera* GameObject should be replaced by the appropriate VR Camera Rig, and VRPlatform will be initialized.


If you want to use gsUnityVR but don't want to use the auto-configure functionality, you can still do so. You have two options. The first is to still use the *ConvertToVRSpatialRig* script, but to intialize the *VR Camera Rig* variable with your existing OVR/SteamVR camera rig. Then the Auto-Setup will be skipped.

Alternately you can skip the script altogether and just do it in code. You just need to call *vs.VRPlatform.Initialize* with your camera rig:
~~~~
GameObject vr_camera_rig = (find somehow)
gs.VRPlatform.Initialize(vr_camera_rig);
~~~~
in some Awake() function, before you try to use VRPlatform for anything else



# Notes

SteamVR by default will auto-enable VR when you hit the Unity Play button in the Editor.
Since this library includes SteamVR, this will start happening as soon as you include this project.
To prevent this behavior, un-check the box in the SteamVR section of the Unity Preferences panel (under Edit)



# VRPlatform API

These are all members of the **VRPlatform** class

### Setup

<dl> 
  <dt>bool VREnabled</dt>
  <dd>Check if VR is Enabled, and also Enable/Disable VR (with setter)</dd>

  <dt>GameObject AutoConfigureVR()</dt>
  <dd>finds existing object tagged *MainCamera* in the scene, and replaces it with appropriate OVR or SteamVR camera rig Prefab, if possible. Returns root GameObject in the Prefab.</dd>

  <dt>Initialize(GameObject SpatialCameraRig)</dt>
  <dd>Initialize the VRPlatform static class. Must be called before using all following VRPlatform functions.</dd>

  <dt>enum Device { NoVRDevice, GenericVRDevice, OculusRift, HTCVive }</dt>
  <dd>Enum of possible VR devices</dd>

  <dt>Device CurrentVRDevice</dt>
  <dd>Property that returns which device is active</dd>

  <dt>bool HaveActiveSpatialInput</dt>
  <dd>check if a 3D spatial input controller is actively being tracked</dd>

  <dt>bool IsSpatialDeviceTracked(int i)</dt>
  <dd>check if a specific controller is being tracked, with 0 == left and 1 == right</dd>

  <dt>IsLeftControllerTracked / IsRightControllerTracked</dt>
  <dd>shortcuts for above method</dd>

</dl>

### Controller Pose

<dl>

  <dt>Vector3 GetLocalControllerPosition(int i)</dt>
  <dd>local position of spatial controller <strong>relative to Camera Rig</strong></dd>

  <dt>Vector3 LeftControllerPosition/RightControllerPosition</dt>
  <dd>shortcuts for above method</dd>

  <dt>Quaternion GetLocalControllerRotation(int i)</dt>
  <dd>local orientation of spatial controller <strong>relative to Camera Rig</strong></dd>

  <dt>Quaternion LeftControllerRotation/RightControllerRotation</dt>
  <dd>shortcuts for above method</dd>

</dl>


### Triggers

<dl>

  <dt>float LeftTrigger/RightTrigger</dt>
  <dd>position of left and right controller triggers, in range 0 to 1.</dd>

  <dt>float LeftSecondaryTrigger/RightSecondaryTrigger</dt>
  <dd>on Touch, position of grip triggers, in rage 0 to 1. On Vive, position of grip buttons, but since these are actually click-buttons, only ever returns 0 or 1</dd>

  <dt>bool LeftGripButton/RightGripButton</dt>
  <dd>convenience functions for Vive controller grip buttons, returns true and false. On Touch returns false.</dd>


</dl>
  

### Stick / Touchpad

<dl>

<dt>Vector2 LeftStickPosition / RightStickPosition</dt>
<dd>(todo)</dd>

<dt>bool LeftStickTouching / RightStickTouching</dt>
<dd></dd>

<dt>bool LeftStickPressed / LeftStickDown / LeftStickReleased</dt>
<dd></dd>

<dt>bool RightStickPressed / RightStickDown / RightStickReleased</dt>
<dd></dd>


</dl>


### Menu Buttons

<dl>

<dt>LeftMenuButtonPressed / LeftMenuButtonDown / LeftMenuButtonReleased</dt>
<dd></dd>

<dt>RightMenuButtonPressed / RightMenuButtonDown / RightMenuButtonReleased </dt>
<dd>(only on Vive)</dd>

</dl>


### Touch ABXY Buttons

Only on Touch.

<dl>

<dt>AButtonPressed / AButtonDown / AButtonReleased</dt>
<dd></dd>

<dt>AButtonPressed / BButtonDown / BButtonReleased</dt>
<dd></dd>

<dt>XButtonPressed / XButtonDown / XButtonReleased</dt>
<dd></dd>

<dt>YButtonPressed / YButtonDown / YButtonReleased</dt>
<dd></dd>

</dl>


(todo: finish this!)





