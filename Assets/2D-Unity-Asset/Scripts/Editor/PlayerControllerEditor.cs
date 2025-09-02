using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NaughtyAttributes.Editor;
using NaughtyAttributes;
using UnityEngine.UIElements;

[CustomEditor(typeof(Player))]
public class PlayerControllerEditor : NaughtyInspector
{
    private bool showDoNotTouch = false; // Track foldout state
    private bool showPlayerAction = true;
    private Dictionary<string, bool> extensionToggles = new Dictionary<string, bool>();
    private List<Type> extensionTypes = new List<Type>();
    protected override void OnEnable()
    {
        base.OnEnable();
        extensionTypes.Clear();
        extensionToggles.Clear();

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(PlayerExtension)) && !type.IsAbstract)
                {
                    extensionTypes.Add(type);
                    extensionToggles[type.Name] = ((Player)target).GetComponent(type) != null;
                }
            }
        }
    }
    public override void OnInspectorGUI()
    {
        
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        SerializedObject serializedObject = new SerializedObject(target);
        Player player = (Player)target;

        #region Player Action
        GUIStyle boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
        boldFoldoutStyle.fontStyle = FontStyle.Bold;

        showPlayerAction = EditorGUILayout.Foldout(showPlayerAction, "Player's Action", true, boldFoldoutStyle);


        if (showPlayerAction)
        {
            foreach (Type extensionType in extensionTypes)
            {
                string effectName = extensionType.Name;
                bool currentToggle = extensionToggles[effectName];
                bool newToggle = EditorGUILayout.Toggle(effectName, currentToggle);

                if (newToggle != currentToggle)
                {
                    extensionToggles[effectName] = newToggle;
                    ToggleEffect(player, extensionType, newToggle);
                }
            }
            if (GUI.changed)
            {
                player.SetExtensions();
                EditorUtility.SetDirty(player);
            }
        }
        #endregion

        serializedObject.ApplyModifiedProperties();
    }

    private void ToggleEffect(Player interactable, Type effectType, bool enable)
    {
        if (enable)
        {
            if (interactable.GetComponent(effectType) == null)
            {
                interactable.gameObject.AddComponent(effectType);
            }
        }
        else
        {
            Component effect = interactable.GetComponent(effectType);
            if (effect != null)
            {
                
                DestroyImmediate(effect);
            }
        }
    }

}



