using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    public class BoneProperties : IEnumerable<BoneProperty>
    {
        private readonly IReadOnlyDictionary<Transform, BoneProperty> _propertiesByTransform;
        private readonly IReadOnlyDictionary<HumanBodyBones, BoneProperty> _propertiesByBone;

        private BoneProperties(IReadOnlyDictionary<Transform, BoneProperty> propertiesByTransform)
        {
            _propertiesByTransform = propertiesByTransform;
            _propertiesByBone = propertiesByTransform.Values.ToDictionary(
                p => p.Bone,
                p => p
            );
        }

        public BoneProperty Get(Transform transform)
        {
            if (!_propertiesByTransform.TryGetValue(transform, out var property))
            {
                throw new InvalidOperationException("The Transform that is not a Bone has been specified.");
            }

            return property;
        }

        public BoneProperty Get(HumanBodyBones bone)
        {
            if (!_propertiesByBone.TryGetValue(bone, out var property))
            {
                throw new InvalidOperationException($"Not compatible with {bone}.");
            }

            return property;
        }

        public bool TryGetValue(Transform transform, out BoneProperty property)
        {
            return _propertiesByTransform.TryGetValue(transform, out property);
        }

        public class Builder
        {
            private readonly BoneGroups _groups;
            private readonly Dictionary<Transform, BoneProperty> _properties = new();

            public Builder(BoneGroups groups)
            {
                _groups = groups;
            }

            public void Add(HumanBodyBones bone, Transform boneTransform, HumanLimit limit, CapsuleCollider collider)
            {
                _properties.Add(boneTransform, new BoneProperty(
                    boneTransform,
                    bone,
                    limit,
                    collider,
                    _groups.GetSpec(bone)
                ));
            }

            public BoneProperties Build()
            {
                return new BoneProperties(_properties);
            }
        }

        public IEnumerator<BoneProperty> GetEnumerator()
        {
            return _propertiesByTransform.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}