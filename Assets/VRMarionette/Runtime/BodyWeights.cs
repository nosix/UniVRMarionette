using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    public class BodyWeights
    {
        private readonly IReadOnlyDictionary<HumanBodyBones, float> _entries;

        public BodyWeights(BodyWeightContainer container)
        {
            _entries = container.bodyWeights.ToDictionary(e => e.bone, e => e.weight);
        }

        public float Get(HumanBodyBones bone)
        {
            return _entries.GetValueOrDefault(bone, 0f);
        }
    }
}