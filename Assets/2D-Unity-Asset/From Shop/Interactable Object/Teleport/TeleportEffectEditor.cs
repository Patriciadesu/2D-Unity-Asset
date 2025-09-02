using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TeleportEffect))]
public class TeleportEffectEditor : Editor
{
    SerializedProperty teleportTypeProp;
    SerializedProperty teleportDestinationProp;
    SerializedProperty targetObjectProp;
    SerializedProperty offsetProp;

    void OnEnable()
    {
        teleportTypeProp = serializedObject.FindProperty("teleportType");
        teleportDestinationProp = serializedObject.FindProperty("teleportDestination");
        targetObjectProp = serializedObject.FindProperty("targetObject");
        offsetProp = serializedObject.FindProperty("offset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(teleportTypeProp);

        var type = (TeleportEffect.TeleportType)teleportTypeProp.enumValueIndex;

        switch (type)
        {
            case TeleportEffect.TeleportType.ToPosition:
                EditorGUILayout.PropertyField(teleportDestinationProp);
                break;
            case TeleportEffect.TeleportType.ToObject:
                EditorGUILayout.PropertyField(targetObjectProp);
                break;
            case TeleportEffect.TeleportType.OffsetFromCurrentPosition:
                EditorGUILayout.PropertyField(offsetProp);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
