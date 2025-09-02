// Editor/ItemDataEditor.cs
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    private SerializedProperty _itemImageProp;
    private SerializedProperty _typeProp;

    private string[] _labels;
    private Type[] _types;
    private int _selectedIndex;

    void OnEnable()
    {
        _itemImageProp = serializedObject.FindProperty("itemimage");
        _typeProp      = serializedObject.FindProperty("type");

        BuildTypeList();
        _selectedIndex = GetCurrentTypeIndex();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_itemImageProp);

        DrawTypeDropdown();

        // Draw the fields of the current ItemType instance (if any)
        if (_typeProp.managedReferenceValue != null)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("ItemType Fields", EditorStyles.boldLabel);
                DrawManagedReferenceChildren(_typeProp);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTypeDropdown()
    {
        EditorGUI.BeginChangeCheck();
        _selectedIndex = EditorGUILayout.Popup(new GUIContent("Item Type"), _selectedIndex, _labels);
        if (EditorGUI.EndChangeCheck())
        {
            if (_selectedIndex == 0)
            {
                // (None)
                _typeProp.managedReferenceValue = null;
            }
            else
            {
                var chosenType = _types[_selectedIndex - 1];
                var current    = GetCurrentManagedType();

                if (current != chosenType)
                {
                    // Create a fresh instance of the selected subclass
                    _typeProp.managedReferenceValue = Activator.CreateInstance(chosenType);
                }
            }
        }
    }

    private void BuildTypeList()
{
    var found = TypeCache.GetTypesDerivedFrom<ItemType>()
        .Where(t => t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
        .OrderBy(t => t.Namespace)
        .ThenBy(t => t.Name)
        .ToArray();

    // Build readable labels without assembly, but detect duplicates
    var nameKeys = new Dictionary<string, int>(); // key = FullName, count
    foreach (var t in found)
    {
        var key = string.IsNullOrEmpty(t.Namespace) ? t.Name : $"{t.Namespace}.{t.Name}";
        nameKeys.TryGetValue(key, out var c);
        nameKeys[key] = c + 1;
    }

    var labels = new List<string> { "(None)" };
    var types  = new List<Type>();

    foreach (var t in found)
    {
        var fullName = string.IsNullOrEmpty(t.Namespace) ? t.Name : $"{t.Namespace}.{t.Name}";
        string label;

        // If duplicate full names exist across assemblies, append a short assembly tag
        if (nameKeys[fullName] > 1)
        {
            var asm = t.Assembly.GetName().Name;
            label = $"{fullName}  [{asm}]"; // only in the rare conflict case
        }
        else
        {
            label = fullName; // clean: no [Assembly-CSharp]
        }

        labels.Add(label);
        types.Add(t);
    }

    _labels = labels.ToArray();
    _types  = types.ToArray();
}

    private int GetCurrentTypeIndex()
    {
        var current = GetCurrentManagedType();
        if (current == null) return 0;
        for (int i = 0; i < _types.Length; i++)
        {
            if (_types[i] == current) return i + 1; // +1 because 0 = (None)
        }
        return 0;
    }

    private Type GetCurrentManagedType()
    {
        // Unity stores managedReferenceFullTypename as "AssemblyName TypeFullName"
        var full = _typeProp.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(full)) return null;

        int space = full.IndexOf(' ');
        if (space <= 0 || space >= full.Length - 1) return null;

        string asm  = full.Substring(0, space);
        string type = full.Substring(space + 1);

        // Compose "TypeFullName, AssemblyName" for Type.GetType
        return Type.GetType($"{type}, {asm}");
    }

    private static void DrawManagedReferenceChildren(SerializedProperty managedRefProp)
    {
        var copy = managedRefProp.Copy();
        var end  = managedRefProp.GetEndProperty();

        // Move to first child of the managed reference
        bool enterChildren = true;
        while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
        {
            // Skip Unity's hidden type metadata properties
            if (copy.name == "m_Script" || copy.name == "managedReferenceFullTypename" || copy.name == "managedReferenceId")
            {
                enterChildren = false;
                continue;
            }

            EditorGUILayout.PropertyField(copy, true);
            enterChildren = false;
        }
    }
}
#endif
