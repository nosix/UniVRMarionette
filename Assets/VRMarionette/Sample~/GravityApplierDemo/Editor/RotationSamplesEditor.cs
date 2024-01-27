using UnityEditor;
using UnityEngine;
using VRMarionette_Sample.GravityApplierDemo.Runtime;

namespace VRMarionette_Sample.GravityApplierDemo.Editor
{
    [CustomEditor(typeof(RotationSamples))]
    public class RotationSamplesEditor : UnityEditor.Editor
    {
        private int _selectedIndex;

        public override void OnInspectorGUI()
        {
            // 元のインスペクタを表示
            base.OnInspectorGUI();

            // 対象のコンポーネントを取得
            var component = (RotationSamples)target;

            if (component.samples is not { Length: > 0 }) return;

            _selectedIndex = EditorGUILayout.IntSlider(_selectedIndex, 0, component.samples.Length - 1);
            if (GUILayout.Button("Apply"))
            {
                component.Apply(_selectedIndex);
            }

            if (GUILayout.Button("Reset"))
            {
                component.Reset();
            }
        }
    }
}