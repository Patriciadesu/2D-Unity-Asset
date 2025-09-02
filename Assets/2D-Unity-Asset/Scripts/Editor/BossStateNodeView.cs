using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Timeline;

public class BossStateNodeView : Node
{
    public Port input;
    public Port output;
    public BossStateNode nodeData;

    private readonly Action<BossStateNodeView> onSelected;

    private SerializedObject soStateNode;
    private PopupField<string> bossStateTypePopup;
    private VisualElement bossStateFieldsRoot;

    // Custom-inspector area (for INodeInspectorContributor)
    private VisualElement _inspectorRoot;
    private Foldout _inspectorFoldout;

    // Timeline UI (shows only when the state has a TimelineAsset field)
    private VisualElement timelineDropZone;
    private ObjectField timelineObjectField;

    // Convenience property (null-safe)
    private BossState State => nodeData != null ? nodeData.state : null;

    public BossStateNodeView(BossStateNode nodeData,
                             Action<BossStateNodeView> onSelected = null,
                             IEdgeConnectorListener edgeListener = null)
    {
        this.nodeData = nodeData;
        this.onSelected = onSelected;

        title = nodeData.stateName;
        style.width = 260;

        // Ports
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

        // Name
        var nameField = new TextField("Name") { value = nodeData.stateName };
        nameField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == nodeData.stateName) return;
            Undo.RecordObject(nodeData, "Edit State Name");
            nodeData.stateName = evt.newValue;
            title = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(nameField);

        // Initial toggle
        var initToggle = new Toggle("Initial") { value = nodeData.isInitialState };
        initToggle.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == nodeData.isInitialState) return;
            Undo.RecordObject(nodeData, "Toggle Initial");
            nodeData.isInitialState = evt.newValue;
            EditorUtility.SetDirty(nodeData);
        });
        mainContainer.Add(initToggle);

        // BossState type + auto fields
        BuildBossStateSection();

        // Custom Inspector foldout (INodeInspectorContributor)
        _inspectorFoldout = new Foldout { text = "Inspector", value = true };
        _inspectorRoot = new VisualElement();
        _inspectorRoot.style.marginTop = 4;
        _inspectorRoot.style.marginBottom = 4;
        _inspectorFoldout.Add(_inspectorRoot);
        mainContainer.Add(_inspectorFoldout);



        RefreshNodeFromData();

        RefreshExpandedState();
        RefreshPorts();

        // Position
        SetPosition(new Rect(UnityEngine.Random.Range(50, 400), UnityEngine.Random.Range(50, 400), 260, 200));
        SetPosition(new Rect(nodeData.position, new Vector2(260, 200)));

        // Save position on move
        this.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            var rect = GetPosition();
            if (nodeData.position != rect.position)
            {
                Undo.RecordObject(nodeData, "Move State Node");
                nodeData.position = rect.position;
                EditorUtility.SetDirty(nodeData);
            }
        });

        // Context menu
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var gv = this.GetFirstAncestorOfType<GraphView>() as StateMachineGraphView;
            evt.menu.AppendAction("Disconnect", _ => gv?.DisconnectStateNode(this));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => gv?.DeleteSelection());
        }));
    }

    public override void OnSelected()
    {
        base.OnSelected();
        onSelected?.Invoke(this);
    }

    public void RefreshNodeFromData()
    {
        title = nodeData != null ? nodeData.stateName : "State";

        if (soStateNode == null) soStateNode = new SerializedObject(nodeData);
        else soStateNode.Update();

        // Rebuild custom inspector UI (interface-based)
        BuildInspector();

        RefreshExpandedState();
        RefreshPorts();
    }

    // ---------- BossState section ----------
    private static List<Type> _cachedBossStateTypes;
    private static List<Type> GetAllBossStateTypes()
    {
        if (_cachedBossStateTypes != null) return _cachedBossStateTypes;
        _cachedBossStateTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<BossState>()
            .Where(t => t.IsClass && !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToList();
        return _cachedBossStateTypes;
    }

    private void BuildBossStateSection()
    {
        soStateNode = new SerializedObject(nodeData);

        var stateTypes = GetAllBossStateTypes();
        var display = stateTypes.Select(t => t.Name).ToList();

        int currentIndex = -1;
        if (nodeData.state != null)
        {
            var ct = nodeData.state.GetType();
            currentIndex = stateTypes.FindIndex(t => t == ct);
        }
        if (currentIndex < 0 && stateTypes.Count > 0) currentIndex = 0;

        bossStateTypePopup = new PopupField<string>("Boss State Type", display,
            Mathf.Clamp(currentIndex, -1, display.Count - 1));
        bossStateTypePopup.RegisterValueChangedCallback(evt =>
        {
            int newIndex = display.IndexOf(evt.newValue);
            if (newIndex < 0 || newIndex >= stateTypes.Count) return;

            var newType = stateTypes[newIndex];
            var newInstance = (BossState)FormatterServices.GetUninitializedObject(newType);
            newInstance.stateName = newType.Name;

            Undo.RecordObject(nodeData, "Change BossState Type");
            nodeData.state = newInstance;
            EditorUtility.SetDirty(nodeData);

            soStateNode.Update();
            RefreshNodeFromData(); // update timeline UI etc.
            BuildInspector();      // refresh interface-driven UI
        });
        mainContainer.Add(bossStateTypePopup);

        bossStateFieldsRoot = new VisualElement();
        bossStateFieldsRoot.style.marginTop = 4;
        bossStateFieldsRoot.style.marginBottom = 4;
        mainContainer.Add(bossStateFieldsRoot);

        // Ensure we have an instance by default
        if (nodeData.state == null && stateTypes.Count > 0)
        {
            var t0 = stateTypes[currentIndex];
            var inst = (BossState)FormatterServices.GetUninitializedObject(t0);
            inst.stateName = t0.Name;

            Undo.RecordObject(nodeData, "Set Default BossState");
            nodeData.state = inst;
            EditorUtility.SetDirty(nodeData);
            soStateNode.Update();
        }

        //RebuildBossStateChildFields();
    }

    // private void RebuildBossStateChildFields()
    // {
    //     bossStateFieldsRoot.Clear();

    //     if (nodeData.state == null)
    //     {
    //         bossStateFieldsRoot.Add(new Label("No BossState selected."));
    //         return;
    //     }

    //     if (soStateNode == null) soStateNode = new SerializedObject(nodeData);
    //     soStateNode.Update();

    //     // This expects BossStateNode to have a [SerializeReference] BossState state;
    //     var stateProp = soStateNode.FindProperty("state");
    //     if (stateProp == null)
    //     {
    //         bossStateFieldsRoot.Add(new Label("No 'state' property found."));
    //         return;
    //     }

    //     var stateType = nodeData.state.GetType();
    //     var childNames = GetDerivedSerializedFieldNames(stateType, typeof(BossState));

    //     for (int i = 0; i < childNames.Count; i++)
    //     {
    //         var name = childNames[i];
    //         var p = stateProp.FindPropertyRelative(name);
    //         if (p != null)
    //         {
    //             var pf = new PropertyField(p);
    //             pf.Bind(soStateNode);
    //             bossStateFieldsRoot.Add(pf);
    //         }
    //     }

    //     soStateNode.ApplyModifiedProperties();
    // }

    private static List<string> GetDerivedSerializedFieldNames(Type type, Type baseType)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var fields = type.GetFields(flags)
            .Where(f =>
                !f.IsStatic &&
                (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
            .Select(f => f.Name)
            .ToList();

        // exclude base fields
        var baseFields = baseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(f => f.Name).ToHashSet();
        fields.RemoveAll(baseFields.Contains);
        return fields;
    }

    // ---------- Custom inspector via interface ----------
    private void BuildInspector()
    {
        _inspectorRoot.Clear();

        if (State is INodeInspectorContributor contributor)
        {
            contributor.BuildInspectorUI(_inspectorRoot);
        }
        else
        {
            var label = new Label("No custom inspector for this state.");
            _inspectorRoot.Add(label);
        }
    }

    // Call this when something changes and you want the UI to update
    public void RefreshInspector()
    {
        if (State is INodeInspectorContributor contributor)
            contributor.RefreshInspectorUI();
    }
}
