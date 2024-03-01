using UnityEngine;

namespace VRMarionette.ForceTask
{
    public class SingleForceTask : IForceTask
    {
        public BoneProperty Target { get; }
        public Vector3 ForcePoint { get; }
        public Vector3 Force { get; }
        public Quaternion? Rotation { get; }
        public bool IsPushing { get; }
        public bool AllowBodyMovement { get; }

        public SingleForceTask(
            BoneProperty target,
            Vector3 forcePoint,
            Vector3 force,
            Quaternion? rotation,
            bool isPushing,
            bool allowBodyMovement
        )
        {
            Target = target;
            ForcePoint = forcePoint;
            Force = force;
            Rotation = rotation;
            IsPushing = isPushing;
            AllowBodyMovement = allowBodyMovement;
        }

        public bool IsZero()
        {
            return Mathf.Approximately(Force.magnitude, 0f) &&
                   Mathf.Approximately(Rotation.GetValueOrDefault(Quaternion.identity).eulerAngles.magnitude, 0f);
        }

        public string ToLogString()
        {
            return $"force: {Force}\n" +
                   $"rotation: {Rotation.GetValueOrDefault(Quaternion.identity).eulerAngles}\n";
        }
    }
}