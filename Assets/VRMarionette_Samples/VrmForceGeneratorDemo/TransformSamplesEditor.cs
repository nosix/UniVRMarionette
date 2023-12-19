using UnityEditor;
using UnityEngine;

namespace VRMarionette_Samples.VrmForceGeneratorDemo
{
    [CustomEditor(typeof(TransformSamples))]
    public class TransformSamplesEditor : Editor
    {
        private int _selectedIndex;

        public override void OnInspectorGUI()
        {
            // 元のインスペクタを表示
            base.OnInspectorGUI();

            // 対象のコンポーネントを取得
            var component = (TransformSamples)target;

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