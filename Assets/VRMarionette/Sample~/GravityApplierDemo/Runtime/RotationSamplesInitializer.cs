using UnityEngine;
using UniVRM10;

namespace VRMarionette_Sample.GravityApplierDemo.Runtime
{
    public class RotationSamplesInitializer : MonoBehaviour
    {
        public RotationSamples[] samples;

        public void Initialize(Vrm10Instance instance)
        {
            foreach (var s in samples) s.SetInstance(instance);
        }
    }
}