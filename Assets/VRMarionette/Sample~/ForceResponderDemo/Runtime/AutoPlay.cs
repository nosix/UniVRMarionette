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
            if (_responder is null) return;
            StartCoroutine(AutoPlayAsync());
        }

        private IEnumerator AutoPlayAsync()
        {
            Vector3 f;

            var direction = 1;
            while (true)
            {
                f.x = axis.x > 0 ? direction * force.x : Random.Range(-force.x, force.x);
                f.y = axis.y > 0 ? direction * force.y : Random.Range(-force.y, force.y);
                f.z = axis.z > 0 ? direction * force.z : Random.Range(-force.z, force.z);

                var forcePoint = _hips.position + offset;
                _responder.QueueForce(
                    _hips,
                    forcePoint,
                    f,
                    Quaternion.identity,
                    false,
                    true
                );

                direction *= -1;

                transform.position = forcePoint;

                yield return new WaitForSeconds(sleep);
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}