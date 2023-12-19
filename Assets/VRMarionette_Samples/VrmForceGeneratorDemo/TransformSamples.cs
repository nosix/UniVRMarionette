using System;
using UnityEngine;

namespace VRMarionette_Samples.VrmForceGeneratorDemo
{
    public class TransformSamples : MonoBehaviour
    {
        public Transform editTarget;
        public TransformSample[] samples;

        public void Reset()
        {
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