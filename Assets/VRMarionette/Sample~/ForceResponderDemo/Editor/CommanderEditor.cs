using UnityEditor;
using UnityEngine;
using VRMarionette_Sample.ForceResponderDemo.Runtime;

namespace VRMarionette_Sample.ForceResponderDemo.Editor
{
    [CustomEditor(typeof(Commander))]
    public class CommanderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 元のインスペクタを表示
            base.OnInspectorGUI();

            // 対象のコンポーネントを取得
            var commander = (Commander)target;

            if (GUILayout.Button("SetPosition"))
            {
                commander.SetPosition();
            }

            if (GUILayout.Button("Execute"))
            {
                commander.Execute();
            }

            if (GUILayout.Button("Reset"))
            {
                commander.Reset();
            }
        }
    }
}