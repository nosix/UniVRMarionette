using UnityEditor;
using UnityEngine;

namespace Capsule
{
    [CustomEditor(typeof(Capsule))]
    public class CapsuleEditor : Editor
    {
        private CapsuleCollider _capsuleCollider;

        public override void OnInspectorGUI()
        {
            // 元のインスペクタを表示
            base.OnInspectorGUI();

            // 対象のコンポーネントを取得
            var capsule = (Capsule)target;

            if (GUILayout.Button("Sync Capsule Collider"))
            {
                Sync(_capsuleCollider, capsule);
            }

            var prevCapsuleCollider = _capsuleCollider;
            _capsuleCollider = (CapsuleCollider)EditorGUILayout.ObjectField(
                "Collider", _capsuleCollider, typeof(CapsuleCollider), true);
            if (prevCapsuleCollider != _capsuleCollider) Sync(_capsuleCollider, capsule);
        }

        private static void Sync(CapsuleCollider capsuleCollider, Capsule capsule)
        {
            capsule.SetCapsule(capsuleCollider);

            var srcTransform = capsuleCollider.transform;
            var dstTransform = capsule.transform;
            dstTransform.position = srcTransform.position;
            dstTransform.rotation = srcTransform.rotation;
            dstTransform.localScale = srcTransform.localScale;

            capsule.Color = Color.yellow;
        }
    }
}