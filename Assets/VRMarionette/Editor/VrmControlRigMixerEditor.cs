using UnityEditor;
using UnityEngine;

namespace VRMarionette.Editor
{
    [CustomEditor(typeof(VrmControlRigMixer))]
    public class VrmControlRigMixerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 元のインスペクタを表示
            base.OnInspectorGUI();

            // 対象のコンポーネントを取得
            var mixer = (VrmControlRigMixer)target;

            if (GUILayout.Button("Reset"))
            {
                mixer.Reset();
            }
        }
    }
}