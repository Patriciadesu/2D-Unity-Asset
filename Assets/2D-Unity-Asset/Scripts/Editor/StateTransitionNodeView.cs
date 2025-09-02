// At top of the file (if not already present)
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;            // Undo, EditorUtility, SerializedObject
using UnityEditor.UIElements; // PopupField, PropertyField
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class StateTransitionNodeView : Node
{
    public Port input;
    public Port output;
    public StateTransition transitionData;

    private SerializedObject soTransition;
    private Label conditionInfoLabel;
    private VisualElement conditionFieldsRoot;
    private PopupField<string> conditionTypePopup;

    private static List<Type> _cachedConditionTypes;
    private static List<Type> GetAllConditionTypes()
    {
        if (_cachedConditionTypes != null) return _cachedConditionTypes;
        _cachedConditionTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<Condition>()
            .Where(t => !t.IsAbstract && t.IsClass && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.Name)
            .ToList();
        return _cachedConditionTypes;
    }

    public StateTransitionNodeView(StateTransition transition, IEdgeConnectorListener edgeListener = null)
    {
        this.transitionData = transition;
        title = "Transition";
        style.width = 260;
        style.backgroundColor = new Color(0.18f, 0.2f, 0.3f);

        input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Multi, typeof(bool));
        input.portName = "";
        titleContainer.Add(input);

        output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
        output.portName = "";
        extensionContainer.Add(output);

        if (edgeListener != null)
        {
            input.AddManipulator(new EdgeConnector<Edge>(edgeListener));
            output.AddManipulator(new EdgeConnector<Edge>(edgeListener));
        }

        soTransition = new SerializedObject(transitionData);

        // Dropdown
        var types = GetAllConditionTypes();
        var names = types.Select(t => t.Name).ToList();
        int currentIndex = -1;
        if (transitionData.condition != null)
        {
            var ct = transitionData.condition.GetType();
            currentIndex = types.FindIndex(t => t == ct);
        }
        if (currentIndex < 0 && types.Count > 0) currentIndex = 0;

        conditionTypePopup = new PopupField<string>("Condition Type", names,
            Mathf.Clamp(currentIndex, -1, names.Count - 1));
        conditionTypePopup.RegisterValueChangedCallback(evt =>
        {
            int newIndex = names.IndexOf(evt.newValue);
            if (newIndex < 0 || newIndex >= types.Count) return;

            var newType = types[newIndex];
            var newInstance = Activator.CreateInstance(newType) as Condition;
            if (newInstance == null) return;

            Undo.RecordObject(transitionData, "Change Condition Type");
            transitionData.condition = newInstance;
            EditorUtility.SetDirty(transitionData);

            soTransition.Update();
            RebuildConditionFields();
        });
        mainContainer.Add(conditionTypePopup);

        // Info
        conditionInfoLabel = new Label();
        conditionInfoLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        mainContainer.Add(conditionInfoLabel);

        // Fields root
        conditionFieldsRoot = new VisualElement();
        conditionFieldsRoot.style.marginTop = 4;
        conditionFieldsRoot.style.marginBottom = 4;
        mainContainer.Add(conditionFieldsRoot);

        // Default instance if none
        if (transitionData.condition == null && types.Count > 0)
        {
            var t0 = types[currentIndex];
            var inst = Activator.CreateInstance(t0) as Condition;
            if (inst != null)
            {
                Undo.RecordObject(transitionData, "Set Default Condition");
                transitionData.condition = inst;
                EditorUtility.SetDirty(transitionData);
                soTransition.Update();
            }
        }

        RebuildConditionFields();

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(UnityEngine.Random.Range(200, 650), UnityEngine.Random.Range(80, 450), 260, 160));
        SetPosition(new Rect(transitionData.position, new Vector2(260, 160)));

        this.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            var rect = GetPosition();
            if (transitionData.position != rect.position)
            {
                Undo.RecordObject(transitionData, "Move Transition Node");
                transitionData.position = rect.position;
                EditorUtility.SetDirty(transitionData);
            }
        });
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var gv = this.GetFirstAncestorOfType<GraphView>() as StateMachineGraphView;
            evt.menu.AppendAction("Disconnect", _ => gv?.DisconnectTransitionNode(this));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => gv?.DeleteSelection());
        }));
    }

    public void RefreshNodeFromData()
    {
        if (soTransition == null) soTransition = new SerializedObject(transitionData);
        else soTransition.Update();

        RebuildConditionFields();

        RefreshExpandedState();
        RefreshPorts();
    }



    private void RebuildConditionFields()
    {
        conditionFieldsRoot.Clear();

        if (transitionData.condition == null)
        {
            conditionFieldsRoot.Add(new Label("No condition selected."));
            return;
        }

        var condProp = soTransition.FindProperty("condition");
        if (condProp == null)
        {
            conditionFieldsRoot.Add(new Label("No 'condition' property found."));
            return;
        }

        // Draw all serialized fields of the current condition instance
        var iterator = condProp.Copy();
        var end = iterator.GetEndProperty();
        bool enterChildren = true;
        int startDepth = -1;

        while (iterator.NextVisible(enterChildren))
        {
            if (SerializedProperty.EqualContents(iterator, end))
                break;

            if (startDepth < 0) startDepth = iterator.depth;
            if (iterator.depth < startDepth) break;
            if (!iterator.propertyPath.StartsWith(condProp.propertyPath))
                break;

            if (iterator.name.StartsWith("managedReference")) { enterChildren = true; continue; }

            var childCopy = iterator.Copy();
            var field = new PropertyField(childCopy);
            field.Bind(soTransition);
            conditionFieldsRoot.Add(field);

            enterChildren = false;
        }

        soTransition.ApplyModifiedProperties();
    }
}