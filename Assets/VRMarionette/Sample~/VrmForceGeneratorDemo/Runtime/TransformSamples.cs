using System;
using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.VrmForceGeneratorDemo.Runtime
{
    public class TransformSamples : MonoBehaviour
    {
        public VrmControlRigMixer mixer;
        public Transform editTarget;
        public TransformSample[] samples;

        public void SetInstance(Vrm10Instance instance)
        {
            mixer = instance.GetComponent<VrmControlRigMixer>()
                    ?? throw new InvalidOperationException("VrmControlRigMixer is needed.");
        }

        public void Reset()
        {
            mixer.Reset();

            editTarget.localPosition = Vector3.zero;
            editTarget.localEulerAngles = Vector3.zero;
        }

        public void Apply(int index)
        {
            var sample = samples[index];
            editTarget.localPosition = sample.position;
            editTarget.localEulerAngles = sample.rotation;
        }

        [Serializable]
        public struct TransformSample
        {
            public Vector3 position;
            public Vector3 rotation;
        }
    }
}