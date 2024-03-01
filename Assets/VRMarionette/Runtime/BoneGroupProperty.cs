using System;
using UnityEngine;

namespace VRMarionette
{
    public class BoneGroupProperty
    {
        public Transform TargetTransform { get; }
        public Transform OriginTransform { get; }
        public float Length { get; }

        public BoneGroupProperty(BoneProperty targetProperty, BoneProperties properties)
        {
            if (!targetProperty.HasCollider) throw new InvalidOperationException();

            TargetTransform = targetProperty.Transform;

            if (targetProperty.GroupSpec is null)
            {
                OriginTransform = targetProperty.Transform;
                Length = targetProperty.Length;
                return;
            }

            var length = targetProperty.Length;
            var originTransform = targetProperty.Transform;
            var parentTransform = originTransform.parent;
            while (true)
            {
                var parentProperty = properties.Get(parentTransform);
                if (!targetProperty.GroupSpec.Contains(parentProperty.Bone)) break;
                length += parentProperty.Length;
                originTransform = parentTransform;
                parentTransform = originTransform.parent;
            }

            OriginTransform = originTransform;
            Length = length;
        }

        public Vector3 ToLocalPosition(Vector3 worldPosition)
        {
            var t = TargetTransform;
            var localPosition = t.InverseTransformPoint(worldPosition);

            while (t != OriginTransform)
            {
                var parentTransform = t.parent;
                localPosition += parentTransform.InverseTransformPoint(t.position);
                t = parentTransform;
            }

            return localPosition;
        }
    }
}