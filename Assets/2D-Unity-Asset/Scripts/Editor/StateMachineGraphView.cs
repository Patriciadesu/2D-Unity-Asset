using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class StateMachineGraphView : GraphView
{
    private readonly Func<string, BossStateNode> requestNewState;
    private readonly Func<string, BossStateNode, StateTransition> requestNewTransition;

    private readonly Dictionary<BossStateNode, BossStateNodeView> stateNodeViews = new();
    private readonly Dictionary<StateTransition, StateTransitionNodeView> transitionNodeViews = new();

    internal readonly VerticalEdgeConnectorListener edgeListener;

    private BossStateGraph currentGraph;
    private BossStateNodeView lastSelectedStateView;

    private bool _isApplyingGraphChange; // guard re-entrancy

    public BossStateNode GetCurrentSelectedStateNode()
        => lastSelectedStateView != null ? lastSelectedStateView.nodeData : null;

    internal void NotifyStateSelected(BossStateNodeView view) => lastSelectedStateView = view;

    public StateMachineGraphView(
        Func<string, BossStateNode> requestNewState,
        Func<string, BossStateNode, StateTransition> requestNewTransition)
    {
        this.requestNewState = requestNewState;
        this.requestNewTransition = requestNewTransition;

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        viewTransformChanged += (gv) =>
        {
            foreach (var e in graphElements.ToList())
                if (e is Edge ed) ed.MarkDirtyRepaint();
        };

        graphViewChanged = OnGraphViewChanged;
        edgeListener = new VerticalEdgeConnectorListener(this);
    }

    public void PopulateView(BossStateGraph graph)
    {
        ClearGraph();
        currentGraph = graph;
        stateNodeViews.Clear();
        transitionNodeViews.Clear();

        if (graph == null) return;

        // 1) States
        var states = graph.stateNodes;
        if (states != null)
        {
            for (int i = 0; i < states.Length; i++)
            {
                var n = states[i];
                if (n != null) AddOrUpdateStateNodeView(n);
            }
        }

        // 2) Transitions (render ALL from graph.transitionNodes)
        var trans = graph.transitionNodes;
        if (trans != null)
        {
            for (int i = 0; i < trans.Length; i++)
            {
                var t = trans[i];
                if (t != null) AddOrUpdateTransitionNodeView(t);
            }
        }

        // 3) Edges State->Transition from state's transitions[]
        if (states != null)
        {
            for (int i = 0; i < states.Length; i++)
            {
                var s = states[i];
                if (s == null || s.transitions == null) continue;

                for (int j = 0; j < s.transitions.Length; j++)
                {
                    var t = s.transitions[j];
                    if (t == null) continue;

                    AddOrUpdateTransitionNodeView(t);

                    var e1 = new VerticalEdge
                    {
                        output = stateNodeViews[s].output,
                        input  = transitionNodeViews[t].input
                    };
                    e1.output.Connect(e1);
                    e1.input.Connect(e1);
                    AddElement(e1);
                }
            }
        }

        // 4) Edges Transition->State from transition.nextStates[]
        if (trans != null)
        {
            for (int i = 0; i < trans.Length; i++)
            {
                var t = trans[i];
                if (t == null || t.nextStates == null) continue;

                for (int j = 0; j < t.nextStates.Length; j++)
                {
                    var next = t.nextStates[j];
                    if (next == null) continue;

                    AddOrUpdateStateNodeView(next);

                    var e2 = new VerticalEdge
                    {
                        output = transitionNodeViews[t].output,
                        input  = stateNodeViews[next].input
                    };
                    e2.output.Connect(e2);
                    e2.input.Connect(e2);
                    AddElement(e2);
                }
            }
        }
    }

    public void ClearGraph()
    {
        var toRemove = graphElements.ToList();
        for (int i = 0; i < toRemove.Count; i++) RemoveElement(toRemove[i]);
    }

    public void AddOrUpdateStateNodeView(BossStateNode node)
    {
        if (!stateNodeViews.TryGetValue(node, out var view))
        {
            view = new BossStateNodeView(node, NotifyStateSelected, edgeListener);
            AddElement(view);
            stateNodeViews[node] = view;
        }
        else
        {
            view.RefreshNodeFromData();
        }
    }

    public void AddOrUpdateTransitionNodeView(StateTransition t)
    {
        if (!transitionNodeViews.TryGetValue(t, out var view))
        {
            view = new StateTransitionNodeView(t, edgeListener);
            AddElement(view);
            transitionNodeViews[t] = view;
        }
        else
        {
            view.RefreshNodeFromData();
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter _)
    {
        var result = new List<Port>();
        foreach (var candidate in ports.ToList())
        {
            if (candidate == startPort) continue;
            if (candidate.direction == startPort.direction) continue;
            if (candidate.node == startPort.node) continue;
            if (candidate.orientation != startPort.orientation) continue;

            bool startIsState      = startPort.node is BossStateNodeView;
            bool startIsTransition = startPort.node is StateTransitionNodeView;
            bool candIsState       = candidate.node is BossStateNodeView;
            bool candIsTransition  = candidate.node is StateTransitionNodeView;

            bool valid =
                (startIsState && startPort.direction == Direction.Output && candIsTransition && candidate.direction == Direction.Input) ||
                (startIsTransition && startPort.direction == Direction.Output && candIsState && candidate.direction == Direction.Input);

            if (valid) result.Add(candidate);
        }
        return result;
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (_isApplyingGraphChange) return change; // guard

        try
        {
            _isApplyingGraphChange = true;

            // Edge creates (update arrays)
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var fromState = edge.output?.node as BossStateNodeView;
                    var toTrans   = edge.input?.node  as StateTransitionNodeView;

                    var fromTrans = edge.output?.node as StateTransitionNodeView;
                    var toState   = edge.input?.node  as BossStateNodeView;

                    // State -> Transition
                    if (fromState != null && toTrans != null)
                    {
                        var state = fromState.nodeData;
                        var trans = toTrans.transitionData;

                        Undo.RecordObject(state, "Connect State → Transition");
                        var list = new List<StateTransition>(state.transitions ?? Array.Empty<StateTransition>());
                        if (!list.Contains(trans)) list.Add(trans);
                        state.transitions = list.ToArray();
                        EditorUtility.SetDirty(state);
                    }

                    // Transition -> State
                    if (fromTrans != null && toState != null)
                    {
                        var trans = fromTrans.transitionData;
                        var state = toState.nodeData;

                        Undo.RecordObject(trans, "Connect Transition → State");
                        var list = new List<BossStateNode>(trans.nextStates ?? Array.Empty<BossStateNode>());
                        if (!list.Contains(state)) list.Add(state);
                        trans.nextStates = list.ToArray();
                        EditorUtility.SetDirty(trans);
                    }
                }
            }

            // Deletions (edges & nodes)
            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove)
                {
                    if (el is Edge edge)
                    {
                        var fromState = edge.output?.node as BossStateNodeView;
                        var toTrans   = edge.input?.node  as StateTransitionNodeView;

                        var fromTrans = edge.output?.node as StateTransitionNodeView;
                        var toState   = edge.input?.node  as BossStateNodeView;

                        // Removing State -> Transition
                        if (fromState != null && toTrans != null)
                        {
                            var state = fromState.nodeData;
                            var trans = toTrans.transitionData;

                            Undo.RecordObject(state, "Disconnect State → Transition");
                            var list = new List<StateTransition>(state.transitions ?? Array.Empty<StateTransition>());
                            list.Remove(trans);
                            state.transitions = list.ToArray();
                            EditorUtility.SetDirty(state);
                        }

                        // Removing Transition -> State
                        if (fromTrans != null && toState != null)
                        {
                            var trans = fromTrans.transitionData;
                            var state = toState.nodeData;

                            Undo.RecordObject(trans, "Disconnect Transition → State");
                            var list = new List<BossStateNode>(trans.nextStates ?? Array.Empty<BossStateNode>());
                            list.Remove(state);
                            trans.nextStates = list.ToArray();
                            EditorUtility.SetDirty(trans);
                        }
                    }
                    else if (el is BossStateNodeView stateView)
                    {
                        var state = stateView.nodeData;
                        if (currentGraph != null && state != null)
                        {
                            Undo.RecordObject(currentGraph, "Delete State Node");
                            var states = new List<BossStateNode>(currentGraph.stateNodes ?? Array.Empty<BossStateNode>());
                            states.Remove(state);
                            currentGraph.stateNodes = states.ToArray();
                            EditorUtility.SetDirty(currentGraph);

                            // Remove from all transitions' nextStates
                            var tn = currentGraph.transitionNodes;
                            if (tn != null)
                            {
                                for (int i = 0; i < tn.Length; i++)
                                {
                                    var t = tn[i];
                                    if (t == null || t.nextStates == null) continue;
                                    var list = new List<BossStateNode>(t.nextStates);
                                    if (list.Remove(state))
                                    {
                                        Undo.RecordObject(t, "Update Transition nextStates");
                                        t.nextStates = list.ToArray();
                                        EditorUtility.SetDirty(t);
                                    }
                                }
                            }

                            // Defer asset delete to avoid re-entrancy
                            EditorApplication.delayCall += () =>
                            {
                                var path = AssetDatabase.GetAssetPath(state);
                                if (!string.IsNullOrEmpty(path))
                                    AssetDatabase.DeleteAsset(path);
                            };
                        }
                    }
                    else if (el is StateTransitionNodeView transView)
                    {
                        var trans = transView.transitionData;
                        if (trans != null)
                        {
                            // Remove from all states' transitions
                            var sn = currentGraph?.stateNodes;
                            if (sn != null)
                            {
                                for (int i = 0; i < sn.Length; i++)
                                {
                                    var s = sn[i];
                                    if (s == null || s.transitions == null) continue;
                                    var list = new List<StateTransition>(s.transitions);
                                    if (list.Remove(trans))
                                    {
                                        Undo.RecordObject(s, "Delete Transition");
                                        s.transitions = list.ToArray();
                                        EditorUtility.SetDirty(s);
                                    }
                                }
                            }

                            // Remove from graph list
                            if (currentGraph != null)
                            {
                                Undo.RecordObject(currentGraph, "Delete Transition Node");
                                var list = new List<StateTransition>(currentGraph.transitionNodes ?? Array.Empty<StateTransition>());
                                list.Remove(trans);
                                currentGraph.transitionNodes = list.ToArray();
                                EditorUtility.SetDirty(currentGraph);
                            }

                            // Defer asset delete
                            EditorApplication.delayCall += () =>
                            {
                                var path = AssetDatabase.GetAssetPath(trans);
                                if (!string.IsNullOrEmpty(path))
                                    AssetDatabase.DeleteAsset(path);
                            };
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            _isApplyingGraphChange = false;
        }

        return change;
    }
}