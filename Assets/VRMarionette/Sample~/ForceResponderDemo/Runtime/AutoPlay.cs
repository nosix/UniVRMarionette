using System.Collections;
using UnityEngine;
using UniVRM10;
using VRMarionette;
using Random = UnityEngine.Random;

namespace VRMarionette_Sample.ForceResponderDemo.Runtime
{
    public class AutoPlay : MonoBehaviour
    {
        public float sleep = 1f;
        public Vector3 offset;
        public Vector3 force;
        public Vector3 axis;

        private ForceResponder _responder;
        private Transform _hips;

        public void Initialize(Vrm10Instance instance)
        {
            _responder = instance.GetComponent<ForceResponder>();

            var animator = instance.GetComponent<Animator>();
            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        private void OnEnable()
        {
            StartCoroutine(AutoPlayAsync());
        }

        private IEnumerator AutoPlayAsync()
        {
            var f = new Vector3(force.x, force.y, force.z);

            while (true)
            {
                f.x = axis.x > 0 ? f.x : Random.Range(-force.x, force.x);
                f.y = axis.y > 0 ? f.y : Random.Range(-force.y, force.y);
                f.z = axis.z > 0 ? f.z : Random.Range(-force.z, force.z);

                var forcePoint = _hips.position + offset;
                _responder.QueueForce(
                    _hips,
                    forcePoint,
                    f,
                    Quaternion.identity,
                    false,
                    true
                );

                f.x *= axis.x > 0 ? -1 : 1;
                f.y *= axis.y > 0 ? -1 : 1;
                f.z *= axis.z > 0 ? -1 : 1;

                transform.position = forcePoint;

                yield return new WaitForSeconds(sleep);
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}