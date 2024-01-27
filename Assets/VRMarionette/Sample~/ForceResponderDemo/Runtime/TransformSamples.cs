using System;
using System.Collections;
using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.ForceResponderDemo.Runtime
{
    public class TransformSamples : MonoBehaviour
    {
        public float movement = 0.05f;
        public HumanoidMixer mixer;
        public Transform editTarget;
        public TransformSample[] samples;

        public void SetInstance(Vrm10Instance instance)
        {
            mixer = instance.GetComponent<HumanoidMixer>() ?? throw new InvalidOperationException(
                "HumanoidMixer is required.");
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
            StartCoroutine(Next());
        }

        private IEnumerator Next()
        {
            yield return null;
            var forward = editTarget.rotation * Vector3.forward;
            editTarget.position += forward * movement;
        }

        [Serializable]
        public struct TransformSample
        {
            public Vector3 position;
            public Vector3 rotation;
        }
    }
}