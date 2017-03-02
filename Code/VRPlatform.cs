using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace gs
{
    /// <summary>
    ///  Abstraction of VR platforms. Since HMDs are basically the same,
    ///  this is really more about the spatial controllers (Touch vs Vive Wand).
    ///  The various properties try to provide a uniform interface to these devices.
    /// </summary>
    public static class VRPlatform
    {
        // TODO:
        //   - make it possible to hide/show Vive controller geometry



        // By default the "direction" of the vive controller is up, like its a sword.
        // We generally want it to point forward, like the Rift controllers. This angle
        // seams reasonable but could be made configurable...
        public static float RiftControllerRotationAngle = 0.0f;
        public static float ViveControllerRotationAngle = 60.0f;


        // this is really more about the controllers...
        public enum Device
        {
            NoVRDevice, GenericVRDevice, OculusRift, HTCVive
        }



        // platform interface to detect if VR is enabled/disabled
        public static bool VREnabled
        {
            get { return VRSettings.enabled; }
            set { VRSettings.enabled = value; }
        }




        static GameObject spatialCameraRig = null;
        static Device currentVRDevice = Device.NoVRDevice;
        public static bool Initialize(GameObject SpatialCameraRig)
        {
            spatialCameraRig = SpatialCameraRig;

            if (VRSettings.isDeviceActive) {

                string sModel = VRDevice.model;
                UnityEngine.Debug.Log(string.Format("VRPlatform.Initialize: VRDevice Model is \"{0}\"", sModel));

                if (spatialCameraRig == null) {
                    currentVRDevice = Device.GenericVRDevice;
                    UnityEngine.Debug.Log("VRPlatform.Initialize: no spatial camera rig provided, using generic VR device");
                } else if (OVRManager.isHmdPresent) { 
                    currentVRDevice = Device.OculusRift;
                } else if (SteamVR.active) {
                    currentVRDevice = Device.HTCVive;
                } else {
                    currentVRDevice = Device.GenericVRDevice;
                    UnityEngine.Debug.Log("VRPlatform.Initialize: could not detect Oculus Rift or HTC Vive, using generic VR device");
                }
                return true;

            } else
                return false;
        }



        public static Device CurrentVRDevice {
            get {
                return currentVRDevice;
            }
        }



        /// <summary>
        /// Check if a 3D Spatial controller is activated
        /// </summary>
        public static bool HaveActiveSpatialInput
        {
            get {
                if ( CurrentVRDevice == Device.OculusRift ) {
                    return OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) ||
                            OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
                } else if ( CurrentVRDevice == Device.HTCVive ) {
                    return LeftViveControllerDeviceValid || RightViveControllerDeviceValid;
                } else {
                    return false;
                }
            }
        }



        /*
         * Check if Left / Right controller is being tracked
         */
        public static bool IsLeftControllerTracked {
            get { return IsSpatialDeviceTracked(0);  }
        }
        public static bool IsRightControllerTracked {
            get { return IsSpatialDeviceTracked(1);  }
        }
        public static bool IsSpatialDeviceTracked(int i)
        {
            if ( CurrentVRDevice == Device.OculusRift ) {
                OVRInput.Controller controller = (i == 0) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                return OVRInput.GetControllerPositionTracked(controller);

            } else if ( CurrentVRDevice == Device.HTCVive ) {
                var device = (i == 0) ? LeftViveControllerDevice : RightViveControllerDevice;
                return device.hasTracking;

            } else {
                return false;
            }
        }


        public static Vector3 LeftControllerPosition {
            get { return GetLocalControllerPosition(0); }
        }
        public static Vector3 RightControllerPosition {
            get { return GetLocalControllerPosition(1); }
        }
        public static Vector3 GetLocalControllerPosition(int i)
        {
            if ( CurrentVRDevice == Device.OculusRift ) {
                OVRInput.Controller controller = (i == 0) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                return OVRInput.GetLocalControllerPosition(controller);

            } else if ( CurrentVRDevice == Device.HTCVive ) {
                GameObject controller = (i == 0) ? LeftViveControllerGO : RightViveControllerGO;
                if (controller == null)
                    return Vector3.zero;
                SteamVR_TrackedObject tracker = controller.GetComponent<SteamVR_TrackedObject>();
                return tracker.transform.localPosition;

            } else {
                return Vector3.zero;
            }
        }


        public static Quaternion LeftControllerRotation {
            get { return GetLocalControllerRotation(0); }
        }
        public static Quaternion RightControllerRotation {
            get { return GetLocalControllerRotation(1); }
        }
        public static Quaternion GetLocalControllerRotation(int i)
        {
            if ( CurrentVRDevice == Device.OculusRift ) {
                OVRInput.Controller controller = (i == 0) ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                Quaternion rotation = OVRInput.GetLocalControllerRotation(controller);
                if (RiftControllerRotationAngle != 0) {
                    Vector3 axisX = rotation * Vector3.right;
                    rotation = Quaternion.AngleAxis(RiftControllerRotationAngle, axisX) * rotation;
                }
                return rotation;

            } else if ( CurrentVRDevice == Device.HTCVive ) {
                GameObject controller = (i == 0) ? LeftViveControllerGO : RightViveControllerGO;
                if (controller == null)
                    return Quaternion.identity;
                SteamVR_TrackedObject tracker = controller.GetComponent<SteamVR_TrackedObject>();
                Quaternion rotation = tracker.transform.localRotation;
                if (ViveControllerRotationAngle != 0) {
                    Vector3 axisX = rotation * Vector3.right;
                    rotation = Quaternion.AngleAxis(ViveControllerRotationAngle, axisX) * rotation;
                }
                return rotation;

            } else {
                return Quaternion.identity;
            }
        }




        /*
         * Primary and Secondary Triggers and Grip Buttons
         *    - on Oculus Touch, Primary = Front/Index-Finger Trigger and Secondary = Grip/Middle-Finger Trigger
         *    - on Vive Wand, Primary = Trigger and Secondary = Grip Button
         *    - Triggers provide float value in [0..1]
         *    - Vive Grip Buttons are not actually triggers! So they only have values 0 and 1.
         *    - [Vive-Only] Also have LeftGripButton and RightGripButton which return true/false
         *      
         */

        public static float LeftTrigger {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    Vector2 v = LeftViveControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
                    return v.x;
                } else {
                    return 0.0f;
                }
            }
        }



        public static float RightTrigger {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    Vector2 v = RightViveControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
                    return v.x;
                } else {
                    return 0.0f;
                }
            }
        }



        public static float LeftSecondaryTrigger {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    bool bDown = LeftViveControllerDevice.GetPress(SteamVR_Controller.ButtonMask.Grip);
                    return (bDown) ? 1.0f : 0.0f;
                } else {
                    return 0.0f;
                }
            }
        }


        public static float RightSecondaryTrigger {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    bool bDown = RightViveControllerDevice.GetPress(SteamVR_Controller.ButtonMask.Grip);
                    return (bDown) ? 1.0f : 0.0f;
                } else {
                    return 0.0f;
                }
            }
        }





        public static bool LeftGripButton {
            get {
                if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetPress(SteamVR_Controller.ButtonMask.Grip);
                } else {
                    return false;
                }
            }
        }

        public static bool RightGripButton {
            get {
                if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetPress(SteamVR_Controller.ButtonMask.Grip);
                } else {
                    return false;
                }
            }
        }





        /*
         * Left and Right "Sticks"
         *     - on Oculus Touch these are the joysticks
         *     - On Vive Wand these are the trackpad areas
         *     - (Left|Right)StickPosition    : 2D point in range [-1,1]x[-1,1] 
         *     - (Left|Right)StickTouching    : is joystick/touchpad being touched
         *     - (Left|Right)Pressed          : was joystick/touchpad clicked down this frame   
         *     - (Left|Right)Down             : is joystick/touchpad clicked down
         *     - (Left|Right)Released         : was joystick/touchpad click-down released this frame
         */




        public static Vector2 LeftStickPosition
        {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return Vector2.zero;
            }
        }


        public static Vector2 RightStickPosition
        {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return Vector2.zero;
            }
        }




        public static bool LeftStickTouching {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, OVRInput.Controller.LTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }
        public static bool LeftStickPressed {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }
        public static bool LeftStickDown {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }
        public static bool LeftStickReleased {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }



        public static bool RightStickTouching {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, OVRInput.Controller.RTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }
        public static bool RightStickPressed {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }
        public static bool RightStickDown {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }
        public static bool RightStickReleased {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                } else
                    return false;
            }
        }

        



        /*
         *  Menu Buttons
         *     - Oculus only has one menu butotn, on left controller
         *     - Vive has Left and Right menu buttons
         *     - Each button has Pressed / Down / Released state properties
         */

        public static bool LeftMenuButtonPressed {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetDown(OVRInput.Button.Start);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
                } else
                    return false;
            }
        }
        public static bool LeftMenuButtonDown {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Button.Start);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
                } else
                    return false;
            }
        }
        public static bool LeftMenuButtonReleased {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetUp(OVRInput.Button.Start);
                } else if (CurrentVRDevice == Device.HTCVive) {
                    return LeftViveControllerDevice.GetPressUp(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
                } else
                    return false;
            }
        }



        // Oculus Touch does not have right menu button
        public static bool RightMenuButtonPressed {
            get {
                if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
                } else
                    return false;
            }
        }
        public static bool RightMenuButtonDown {
            get {
                if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
                } else
                    return false;
            }
        }
        public static bool RightMenuButtonReleased {
            get {
                if (CurrentVRDevice == Device.HTCVive) {
                    return RightViveControllerDevice.GetPressUp(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
                } else
                    return false;
            }
        }





        /*
         * A/B/X/Y Buttons
         *    - only supported on Rift
         */

        public static bool AButtonPressed {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
                } else
                    return false;
            }
        }
        public static bool AButtonDown {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
                } else
                    return false;
            }
        }
        public static bool AButtonReleased {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch);
                } else
                    return false;
            }
        }



        public static bool BButtonPressed {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);
                } else
                    return false;
            }
        }
        public static bool BButtonDown {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
                } else
                    return false;
            }
        }
        public static bool BButtonReleased {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch);
                } else
                    return false;
            }
        }




        public static bool XButtonPressed {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
                } else
                    return false;
            }
        }
        public static bool XButtonDown {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
                } else
                    return false;
            }
        }
        public static bool XButtonReleased {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch);
                } else
                    return false;
            }
        }




        public static bool YButtonPressed {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch);
                } else
                    return false;
            }
        }
        public static bool YButtonDown {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
                } else
                    return false;
            }
        }
        public static bool YButtonReleased {
            get {
                if (CurrentVRDevice == Device.OculusRift) {
                    return OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch);
                } else
                    return false;
            }
        }









        /*
         * Automatically replace dummy Main Camera with VR Platform-Specific camera rig
         *    - finds main cam via MainCamera tag
         *    - this main camera is destroyed
         *    - for Vive, instantiates SteamVR camera rig prefabs (must be in /SteamVR/ path in a /Resources/ folder)
         *    - For Rift (...)
         *  
         *  NOTE: this code is called before any other F3 setup. So, many F3 things
         *  are not initialized (eg like DebugUtil.Log)
         */
        public static GameObject AutoConfigureVR()
        {
            if (VREnabled == false)
                return null;

            string sModel = VRDevice.model;
            UnityEngine.Debug.Log(string.Format(
                "VRPlatform.AutoConfigureVR: VRDevice Model is \"{0}\", SteamVR.enabled={1}", sModel, SteamVR.enabled));

            GameObject camRig = null;

            if (OVRManager.isHmdPresent)
                camRig = AutoConfigure_OVR();
            //else if ( SteamVR.active )          // [RMS] SteamVR.active is false until we create a SteamVR prefab!
            else if ( sModel.Contains("Vive") )   //   This works but I don't like it...no other way in the SDK to tell this??
                camRig = AutoConfigure_SteamVR();

            UnityEngine.Debug.Log(string.Format(
                "VRPlatform.AutoConfigureVR: {0}", (camRig == null) ? "failed" : "success!"));

            return camRig;
        }
        static GameObject AutoConfigure_OVR()
        {
            GameObject cameraRig = null;
            GameObject mainCamGO = null;
            Camera mainCam = null;

            // create Oculus OVR prefabs and keep track of camera rig
            try {
                mainCamGO = find_main_camera();
                mainCam = mainCamGO.GetComponent<Camera>();

                cameraRig = GameObject.Instantiate(Resources.Load("OVR/OVRCameraRig")) as GameObject;
                cameraRig.name = "OVRCameraRig";

                // prefab has all three of these tagged MainCamera?
                List<GameObject> children = new List<GameObject>();
                collect_all_children(cameraRig, children);
                foreach ( var child in children ) {
                    if ( child.tag == "MainCamera" ) {
                        if (child.name != "CenterEyeAnchor")
                            child.tag = "Untagged";
                    }
                }

            } catch ( Exception e ) {
                UnityEngine.Debug.Log(string.Format(
                    "VRPlatform.AutoConfigureVR: Error instantiating Oculus VR Prefabs: " + e.Message));
                return null;
            }

            // set to same position as mainCam (should be same position as in Editor)
            cameraRig.transform.position = mainCamGO.transform.position;

            // disconnect the existing camera
            mainCam.tag = "Untagged";
            mainCamGO.tag = "Untagged";
            GameObject.Destroy(mainCamGO);
            Component.Destroy(mainCam);

            return cameraRig;
        }
        static GameObject AutoConfigure_SteamVR()
        {
            GameObject cameraRig = null;
            GameObject mainCamGO = null;
            Camera mainCam = null;

            // create vive VR prefabs and keep track of camera rig
            try {
                mainCamGO = find_main_camera();
                mainCam = mainCamGO.GetComponent<Camera>();

                GameObject steamVR = GameObject.Instantiate(Resources.Load("SteamVR/[SteamVR]")) as GameObject;
                steamVR.name = "[SteamVR]";
                // [RMS] have not tested this
                //GameObject status = GameObject.Instantiate(Resources.Load("SteamVR/[Status]")) as GameObject;
                //status.name = "[Status]";
                cameraRig = GameObject.Instantiate(Resources.Load("SteamVR/[CameraRig]")) as GameObject;
                cameraRig.name = "[CameraRig]";
            } catch ( Exception e ) {
                UnityEngine.Debug.Log(string.Format(
                    "VRPlatform.AutoConfigureVR: Error instantiating Vive VR Prefabs: " + e.Message));
                return null;
            }

            // set to same position as mainCam (should be same position as in Editor)
            cameraRig.transform.position = mainCamGO.transform.position;

            // disconnect the existing camera
            mainCam.tag = "Untagged";
            mainCamGO.tag = "Untagged";
            GameObject.Destroy(mainCamGO);
            Component.Destroy(mainCam);

            return cameraRig;
        }
        static GameObject find_main_camera()
        {
            GameObject[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
            if (mainCameras.Length != 1)
                throw new Exception("found multiple objects tagged MainCamera, aborting");
            return mainCameras[0];
        }









        /*
         * Internals below here
         */



        // Vive-specific stuff. Unlike OVR, SteamVR does not provide very straightforward
        // code-level access to controllers. We need to find a MonoBehavior attached to
        // each controller, which is set up by the [CameraRig] prefab. 
        // 
        // Also, in SteamVR the left/right relationship of controllers will change if the
        // user switches hands. This messes up all sorts of stuff if we are looking it up
        // dynamically. So, cache it first time and stick with it.
        static GameObject leftViveController, rightViveController;
        static int iLeftViveDeviceIdx = -1, iRightViveDeviceIdx = -1;
        static void lookup_vive_controllers()
        {
            if (spatialCameraRig == null)
                return;
            if (leftViveController != null && rightViveController != null)
                return;

            iLeftViveDeviceIdx = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
            iRightViveDeviceIdx = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);

            int n = spatialCameraRig.transform.childCount;
            for (int i = 0; i < n; ++i) {
                GameObject child = spatialCameraRig.transform.GetChild(i).gameObject;
                SteamVR_TrackedObject tracked = child.GetComponent<SteamVR_TrackedObject>();
                if (tracked == null)
                    continue;
                int idx = (int)tracked.index;
                if (idx == iLeftViveDeviceIdx)
                    leftViveController = child;
                else if (idx == iRightViveDeviceIdx)
                    rightViveController = child;
            }
        }

        static GameObject LeftViveControllerGO {
            get {
                lookup_vive_controllers();
                return leftViveController;
            }
        }
        static GameObject RightViveControllerGO {
            get {
                lookup_vive_controllers();
                return rightViveController;
            }
        }

        static SteamVR_Controller.Device LeftViveControllerDevice {
            get {
                lookup_vive_controllers();
                if (iLeftViveDeviceIdx == -1)
                    return SteamVR_Controller.Input(0);
                return SteamVR_Controller.Input(iLeftViveDeviceIdx);
            }
        }
        static bool LeftViveControllerDeviceValid {
            get {
                return iLeftViveDeviceIdx != -1;
            }
        }

        static SteamVR_Controller.Device RightViveControllerDevice {
            get {
                lookup_vive_controllers();
                if (iRightViveDeviceIdx == -1)
                    return SteamVR_Controller.Input(0);
                return SteamVR_Controller.Input(iRightViveDeviceIdx);
            }
        }
        static bool RightViveControllerDeviceValid {
            get {
                return iRightViveDeviceIdx != -1;
            }
        }





        // this is just an internal utility method...
        private static void collect_all_children(GameObject root, List<GameObject> vChildren)
        {
            foreach ( Transform t in root.transform ) {
                GameObject go = t.gameObject;
                vChildren.Add(go);
                collect_all_children(go, vChildren);
            }
        }
    }
}
