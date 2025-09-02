using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

class VerticalEdgeConnectorListener : IEdgeConnectorListener
{
    private readonly GraphView _gv;
    public VerticalEdgeConnectorListener(GraphView gv) { _gv = gv; }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        // No-op (you could spawn a node here on empty drop if desired)
    }

    public void OnDrop(GraphView graphView, Edge originalEdge)
    {
        // Remove Unity's provisional edge
        _gv.RemoveElement(originalEdge);

        // Build our custom VerticalEdge
        var e = new VerticalEdge
        {
            output = originalEdge.output,
            input = originalEdge.input
        };
        e.output.Connect(e);
        e.input.Connect(e);
        _gv.AddElement(e);

        // === Update the data model immediately (so you don't rely on edgesToCreate) ===
        var fromState = e.output?.node as BossStateNodeView;
        var toTrans = e.input?.node as StateTransitionNodeView;

        var fromTrans = e.output?.node as StateTransitionNodeView;
        var toState = e.input?.node as BossStateNodeView;

        // A: State -> Transition
        if (fromState != null && toTrans != null)
        {
            var state = fromState.nodeData;
            var trans = toTrans.transitionData;

            Undo.RecordObject(state, "Connect State → Transition");
            var list = new List<StateTransition>(state.transitions ?? new StateTransition[0]);
            if (!list.Contains(trans)) list.Add(trans);
            state.transitions = list.ToArray();
            EditorUtility.SetDirty(state);
        }

        // B: Transition -> State
        if (fromTrans != null && toState != null)
        {
            var trans = fromTrans.transitionData;
            var state = toState.nodeData;

            Undo.RecordObject(trans, "Connect Transition → State");
            var list = new List<BossStateNode>(trans.nextStates ?? new BossStateNode[0]);
            if (!list.Contains(state)) list.Add(state);
            trans.nextStates = list.ToArray();
            EditorUtility.SetDirty(trans);
        }

        AssetDatabase.SaveAssets();
    }
}
