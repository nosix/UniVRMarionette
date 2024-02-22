using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    /// <summary>
    /// 連動して動作する骨グループの仕様を表す。
    /// 各骨に可動範囲の角度(minは負方向の角度,maxは正方向の角度)が設定されている。
    /// グループとしての可動範囲は各骨の可動範囲の合計になる。
    /// 各骨の可動範囲÷グループの可動範囲が Ratio (分配比率)として保持されている。
    /// </summary>
    public class BoneGroupSpec
    {
        public IReadOnlyDictionary<HumanBodyBones, BoneGroups.Ratio> Ratios { get; }
        public IEnumerable<HumanBodyBones> Bones => _cachedRatios.Keys;

        private readonly
            Dictionary<HumanBodyBones, IReadOnlyDictionary<HumanBodyBones, BoneGroups.Ratio>> _cachedRatios = new();

        public BoneGroupSpec(HumanLimits humanLimits, params HumanBodyBones[] bones)
        {
            // Group に含まれる全骨の合計角度(可動範囲)を求める
            var totalMinAngle = new Vector3();
            var totalMaxAngle = new Vector3();
            foreach (var bone in bones)
            {
                var humanLimit = humanLimits.Get(bone);
                totalMinAngle += humanLimit.min; // min は 0 以下
                totalMaxAngle += humanLimit.max; // max は 0 以上
            }

            // Group 内での各部位の角度の分配比率を求める
            var tmpRatios = new Dictionary<HumanBodyBones, BoneGroups.Ratio>();
            foreach (var bone in bones)
            {
                var humanLimit = humanLimits.Get(bone);
                tmpRatios.Add(bone, new BoneGroups.Ratio(
                    new Vector3(
                        ToRatio(humanLimit.min.x, totalMinAngle.x),
                        ToRatio(humanLimit.min.y, totalMinAngle.y),
                        ToRatio(humanLimit.min.z, totalMinAngle.z)
                    ),
                    new Vector3(
                        ToRatio(humanLimit.max.x, totalMaxAngle.x),
                        ToRatio(humanLimit.max.y, totalMaxAngle.y),
                        ToRatio(humanLimit.max.z, totalMaxAngle.z)
                    )
                ));
            }

            Ratios = tmpRatios;
        }

        public bool Contains(HumanBodyBones bone)
        {
            return _cachedRatios.ContainsKey(bone);
        }

        public IReadOnlyDictionary<HumanBodyBones, BoneGroups.Ratio> GetRatiosBasedOn(HumanBodyBones bone)
        {
            if (_cachedRatios.TryGetValue(bone, out var cache)) return cache;

            var baseRatio = Ratios[bone];
            var ratios = Ratios.ToDictionary(
                pair => pair.Key,
                pair => pair.Value / baseRatio
            );
            _cachedRatios.Add(bone, ratios);
            return ratios;
        }

        private static float ToRatio(float value, float total)
        {
            return Mathf.Approximately(total, 0f) ? 0f : value / total;
        }
    }
}