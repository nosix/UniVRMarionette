using UnityEditor;
using UnityEngine;

namespace VRMarionette.Editor
{
    [CustomEditor(typeof(HumanoidMixer))]
    public class HumanoidMixerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 元のインスペクタを表示
            base.OnInspectorGUI();

            // 対象のコンポーネントを取得
            var mixer = (HumanoidMixer)target;

            if (GUILayout.Button("Reset"))
            {
                mixer.Reset();
            }
        }
    }
}