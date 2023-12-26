using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.VrmRigidbodyDemo.Runtime
{
    public class Setup : MonoBehaviour
    {
        public Transform centroid;
        public Transform ground;

        public void Initialize(Vrm10Instance instance)
        {
            var vrmRigidbody = instance.GetComponent<VrmRigidbody>();
            vrmRigidbody.centroid = centroid;
            vrmRigidbody.ground = ground;
        }
    }
}