using UnityEngine;
using VRMarionette.MetaXR;

namespace MetaXR_Samples.MetaXRDemoApp
{
    public class GrabSupport : MonoBehaviour
    {
        public VrmMarionetteHand leftHand;
        public VrmMarionetteHand rightHand;

        public void Update()
        {
            leftHand.Grab(OVRInput.Get(OVRInput.Button.PrimaryHandTrigger));
            rightHand.Grab(OVRInput.Get(OVRInput.Button.SecondaryHandTrigger));
        }
    }
}