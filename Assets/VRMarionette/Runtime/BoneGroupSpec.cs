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
    /// 存在しない Bone の Ratio は Vector3.zero に設定される。
    /// </summary>
    public class BoneGroupSpec
    {
        public IReadOnlyDictionary<HumanBodyBones, Ratio> Ratios { get; }
        public IEnumerable<HumanBodyBones> Bones => Ratios.Keys;

        /// <summary>
        /// Key の Bone を基準(1)としたときの各 Bone の回転角度の割合
        /// 全てが必要になるわけではないので必要に応じて生成する
        /// </summary>
        private readonly
            Dictionary<HumanBodyBones, IReadOnlyDictionary<HumanBodyBones, Ratio>> _cachedRatios = new();

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
            var tmpRatios = new Dictionary<HumanBodyBones, Ratio>();
            foreach (var bone in bones)
            {
                var humanLimit = humanLimits.Get(bone);
                tmpRatios.Add(bone, new Ratio(
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
            return Ratios.ContainsKey(bone);
        }

        public IReadOnlyDictionary<HumanBodyBones, Ratio> GetRatiosBasedOn(HumanBodyBones bone)
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

        public readonly struct Ratio
        {
            private Vector3 Min { get; }
            private Vector3 Max { get; }

            public Ratio(Vector3 min, Vector3 max)
            {
                Min = min;
                Max = max;
            }

            public Vector3 Apply(Vector3 angle)
            {
                var x = 0f;
                var y = 0f;
                var z = 0f;

                if (angle.x < -Mathf.Epsilon) x = angle.x * Min.x;
                if (angle.x > Mathf.Epsilon) x = angle.x * Max.x;
                if (angle.y < -Mathf.Epsilon) y = angle.y * Min.y;
                if (angle.y > Mathf.Epsilon) y = angle.y * Max.y;
                if (angle.z < -Mathf.Epsilon) z = angle.z * Min.z;
                if (angle.z > Mathf.Epsilon) z = angle.z * Max.z;

                return new Vector3(x, y, z);
            }

            public static Ratio operator /(Ratio l, Ratio r)
            {
                return new Ratio(
                    new Vector3(
                        l.Min.x / r.Min.x,
                        l.Min.y / r.Min.y,
                        l.Min.z / r.Min.z
                    ),
                    new Vector3(
                        l.Max.x / r.Max.x,
                        l.Max.y / r.Max.y,
                        l.Max.z / r.Max.z
                    )
                );
            }
        }
    }
}