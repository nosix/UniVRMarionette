using UnityEngine;
using UniVRM10;

namespace VRMarionette_Sample.ForceResponderDemo.Runtime
{
    public class TransformSamplesInitializer : MonoBehaviour
    {
        public TransformSamples[] samples;

        public void Initialize(Vrm10Instance instance)
        {
            foreach (var s in samples) s.SetInstance(instance);
        }
    }
}