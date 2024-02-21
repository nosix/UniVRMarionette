using UnityEngine;
using VRMarionette.MetaXR;

namespace MetaXR_Sample.MetaXRDemoApp
{
    public class GrabSupport : MonoBehaviour
    {
        public VrmMarionetteHand leftHand;
        public VrmMarionetteHand rightHand;

        public void Update()
        {
            leftHand.Pinch(OVRInput.Get(OVRInput.Button.PrimaryHandTrigger));
            rightHand.Pinch(OVRInput.Get(OVRInput.Button.SecondaryHandTrigger));
        }
    }
}