using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    /// <summary>
    /// Vrm10Instance が設定されている GameObject に設定して SpringJoint の揺れを検出する
    /// </summary>
    /// <remarks>
    /// 使用方法
    /// <list type="number">
    /// <item>Vrm10Instance が設定されている GameObject に VrmSpringJointMonitor を追加する</item>
    /// <item>JointNames プロパティで SpringJoint の名前一覧を取得する</item>
    /// <item>SetMonitoringJoints メソッドで監視対象の SpringJoint の名前を指定する</item>
    /// <item>JointRotationAngles プロパティで SprintJoint の回転角度を取得して揺れ度合いを把握する</item>
    /// </list>
    /// </remarks>
    public class VrmSpringJointMonitor : MonoBehaviour
    {
        /// <summary>
        /// SetMonitoringJoints で指定可能な JointName
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

        public void Setup(Vrm10Instance instance)
        {
            _springJoints = instance.SpringBone.Springs
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