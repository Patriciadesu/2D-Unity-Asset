using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[CustomEditor(typeof(InteractableObject))]
public class InteractableObjectEditor : Editor
{
    private Dictionary<string, bool> effectToggles = new Dictionary<string, bool>();
    private List<Type> effectTypes = new List<Type>();
    private string searchFilter = "";

    private bool showEffects = true;

    public override void OnInspectorGUI()
    {
        InteractableObject interactable = (InteractableObject)target;

        DrawDefaultInspector();
        EditorGUILayout.Space(10);

        showEffects = EditorGUILayout.BeginFoldoutHeaderGroup(showEffects, "Manage Object Effects");
        if (showEffects)
        {
            DrawEffectsUI(interactable);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (GUI.changed)
        {
            interactable.RefreshEffects();
            EditorUtility.SetDirty(interactable);
        }
    }

    private void DrawEffectsUI(InteractableObject interactable)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Search and Toggle Effects", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            searchFilter = EditorGUILayout.TextField("Search", searchFilter).Trim();

            var filteredEffects = effectTypes
                .Where(t => string.IsNullOrEmpty(searchFilter) || t.Name.ToLower().Contains(searchFilter.ToLower()))
                .OrderBy(t => t.Name)
                .ToList();

            if (filteredEffects.Count == 0)
            {
                EditorGUILayout.HelpBox("No effects match your search.", MessageType.Info);
                return;
            }

            foreach (Type effectType in filteredEffects)
            {
                string effectName = effectType.Name;

                if (!effectToggles.ContainsKey(effectName))
                    effectToggles[effectName] = interactable.GetComponent(effectType) != null;

                bool currentToggle = effectToggles[effectName];
                bool newToggle = EditorGUILayout.ToggleLeft(effectName, currentToggle);

                if (newToggle != currentToggle)
                {
                    effectToggles[effectName] = newToggle;
                    ToggleEffect(interactable, effectType, newToggle);
                }
            }
        }
    }

    private void ToggleEffect(InteractableObject interactable, Type effectType, bool enable)
    {
        if (enable)
        {
            if (interactable.GetComponent(effectType) == null)
                interactable.gameObject.AddComponent(effectType);
        }
        else
        {
            Component effect = interactable.GetComponent(effectType);
            if (effect != null)
                DestroyImmediate(effect);
        }
    }

    void OnEnable()
    {
        effectTypes.Clear();
        effectToggles.Clear();

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            Type[] types = null;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(ObjectEffect)) && !type.IsAbstract)
                {
                    effectTypes.Add(type);
                    effectToggles[type.Name] = ((InteractableObject)target).GetComponent(type) != null;
                }
            }
        }
    }
}
