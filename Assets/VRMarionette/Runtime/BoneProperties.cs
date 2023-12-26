using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    public class BoneProperties
    {
        private readonly IReadOnlyDictionary<Transform, BoneProperty> _properties;

        private BoneProperties(IReadOnlyDictionary<Transform, BoneProperty> properties)
        {
            _properties = properties.ToDictionary(
                e => e.Key,
                e => new BoneProperty(e.Value.Transform, e.Value.Bone, e.Value.Collider, this)
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

            public void Add(Vrm10ControlBone bone, CapsuleCollider collider)
            {
                _properties.Add(bone.ControlBone, new BoneProperty(bone.ControlBone, bone.BoneType, collider, null));
            }

            public BoneProperties Build()
            {
                return new BoneProperties(_properties);
            }
        }
    }
}