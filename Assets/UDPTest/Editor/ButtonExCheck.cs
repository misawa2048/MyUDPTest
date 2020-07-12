using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CanEditMultipleObjects, CustomEditor(typeof(ButtonEx), true)]
public class ButtonExEditor : ButtonEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        this.serializedObject.Update();
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("buttonExDownEvent"), true);
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("buttonExUpEvent"), true);
        this.serializedObject.ApplyModifiedProperties();
    }
}