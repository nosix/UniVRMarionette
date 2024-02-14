using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    public class VrmSpringBoneColliderMonitor : MonoBehaviour
    {
        /// <summary>
        /// SetMonitoringJoints で指定可能な Joint Name
        /// </summary>
        public IEnumerable<string> JointNames => _springJoints.Keys;

        /// <summary>
        /// SetMonitoringJoints で指定した Joint の回転角度 [0..180]
        /// 並び順は SetMonitoringJoints で指定した Joint の順番
        /// </summary>
        public IEnumerable<float> JointRotationAngles => _monitoringJointRotationAngles;

        private IReadOnlyDictionary<string, Transform> _springJoints;
        private readonly List<Transform> _monitoringJoints = new();
        private readonly List<float> _monitoringJointRotationAngles = new();

        private void Start()
        {
            var vrmInstance = GetComponent<Vrm10Instance>() ?? throw new InvalidOperationException(
                "The VrmSpringBoneColliderMonitor requires the Vrm10Instance component.");

            _springJoints = vrmInstance.SpringBone.Springs
                .SelectMany(s => s.Joints)
                .ToDictionary(
                    e => e.name,
                    e => e.transform
                );
        }

        public void SetMonitoringJoints(IEnumerable<string> jointNames)
        {
            _monitoringJoints.Clear();

            foreach (var targetName in jointNames)
            {
                if (!_springJoints.TryGetValue(targetName, out var targetTransform))
                    throw new ArgumentException($"'{targetName}' is not found.");
                _monitoringJoints.Add(targetTransform);
            }

            _monitoringJointRotationAngles.Capacity = _monitoringJoints.Count;
        }

        private void Update()
        {
            for (var i = 0; i < _monitoringJoints.Count; i++)
            {
                _monitoringJointRotationAngles[i] = Quaternion.Angle(
                    Quaternion.identity,
                    _monitoringJoints[i].localRotation
                );
            }
        }
    }
}