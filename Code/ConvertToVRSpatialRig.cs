using System;
using UnityEngine;

namespace gs
{
    public class ConvertToVRSpatialRig : MonoBehaviour 
    {
        public GameObject VRCameraRig;

        void Awake()
        {
            if ( VRCameraRig == null )
                VRCameraRig = gs.VRPlatform.AutoConfigureVR();
            gs.VRPlatform.Initialize(VRCameraRig);
        }
        
    }
}

