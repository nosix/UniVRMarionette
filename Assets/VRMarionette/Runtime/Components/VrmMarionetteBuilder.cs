using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    public class VrmMarionetteBuilder : MonoBehaviour
    {
        [Space]
        public VRM10SpringBoneColliderGroup[] springBoneColliderGroups;

        [Space]
        public VrmForceSource[] forceSources;

        [Space]
        public HumanLimitContainer humanLimits;

        public ForceFieldContainer forceFields;

        public BodyWeightContainer bodyWeights;

        private void Start()
        {
            var instance = GetComponent<Vrm10Instance>();
            if (instance is not null) Build(instance);
        }

        protected void Build(Vrm10Instance instance)
        {
            // モデルと相互作用する Collider を設定する
            foreach (var spring in instance.SpringBone.Springs)
            {
                spring.ColliderGroups.AddRange(springBoneColliderGroups);
            }

            // SpringBone の設定を変更した後に再構築が必要
            instance.Runtime.ReconstructSpringBone();

            if (humanLimits)
            {
                // ControlRig を操作する機能を追加する
                var vrmControlRigManipulator =
                    instance.GetComponent<VrmControlRigManipulator>() ??
                    instance.gameObject.AddComponent<VrmControlRigManipulator>();
                vrmControlRigManipulator.Initialize(instance, humanLimits);
            }

            if (forceFields)
            {
                // Collider の衝突により力を働かせる機能を追加する
                var vrmForceGenerator =
                    instance.GetComponent<VrmForceGenerator>() ??
                    instance.gameObject.AddComponent<VrmForceGenerator>();
                vrmForceGenerator.Initialize(instance, forceFields);
                foreach (var forceSource in forceSources)
                {
                    forceSource.Initialize(vrmForceGenerator);
                }
            }

            if (bodyWeights)
            {
                // 重力の演算をする機能を追加する
                var vrmRigidbody =
                    instance.GetComponent<VrmRigidbody>() ??
                    instance.gameObject.AddComponent<VrmRigidbody>();
                vrmRigidbody.Initialize(instance, bodyWeights);
            }
        }
    }
}