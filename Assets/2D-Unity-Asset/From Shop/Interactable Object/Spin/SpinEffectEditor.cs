using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpinEffect))]
public class SpinEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty axisProp = serializedObject.FindProperty("axis");
        SerializedProperty customAxisProp = serializedObject.FindProperty("customAxis");
        SerializedProperty rotationSpeedProp = serializedObject.FindProperty("rotationSpeed");

        EditorGUILayout.PropertyField(axisProp);

        if ((SpinEffect.AxisOption)axisProp.enumValueIndex == SpinEffect.AxisOption.Custom)
        {
            EditorGUILayout.PropertyField(customAxisProp);
        }

        EditorGUILayout.PropertyField(rotationSpeedProp);

        serializedObject.ApplyModifiedProperties();
    }
}
