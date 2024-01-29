using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    public class BoneProperties : IEnumerable<BoneProperty>
    {
        private readonly IReadOnlyDictionary<Transform, BoneProperty> _properties;

        private BoneProperties(IReadOnlyDictionary<Transform, BoneProperty> properties)
        {
            _properties = properties.ToDictionary(
                e => e.Key,
                e => new BoneProperty(e.Value.Transform, e.Value.Bone, e.Value.Limit, e.Value.Collider, this)
            );
        }

        public BoneProperty Get(Transform transform)
        {
            if (!_properties.TryGetValue(transform, out var property))
            {
                throw new InvalidOperationException("The Transform that is not a Bone has been specified.");
            }

            return property;
        }

        public bool TryGetValue(Transform transform, out BoneProperty property)
        {
            return _properties.TryGetValue(transform, out property);
        }

        public class Builder
        {
            private readonly Dictionary<Transform, BoneProperty> _properties = new();

            public void Add(HumanBodyBones bone, Transform boneTransform, HumanLimit limit, CapsuleCollider collider)
            {
                _properties.Add(boneTransform, new BoneProperty(boneTransform, bone, limit, collider, null));
            }

            public BoneProperties Build()
            {
                return new BoneProperties(_properties);
            }
        }

        public IEnumerator<BoneProperty> GetEnumerator()
        {
            return _properties.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}