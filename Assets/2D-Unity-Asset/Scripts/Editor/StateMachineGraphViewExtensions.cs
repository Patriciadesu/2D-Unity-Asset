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

public static class StateMachineGraphViewExtensions
{
    public static void DisconnectStateNode(this StateMachineGraphView gv, BossStateNodeView stateView)
    {
        if (gv == null || stateView == null || stateView.nodeData == null) return;

        // remove visual edges first
        foreach (var e in gv.edges.ToList())
            if (e.input?.node == stateView || e.output?.node == stateView)
                gv.RemoveElement(e);

        var state = stateView.nodeData;

        // clear state's transitions
        if (state.transitions != null && state.transitions.Length > 0)
        {
            Undo.RecordObject(state, "Disconnect State");
            state.transitions = Array.Empty<StateTransition>();
            EditorUtility.SetDirty(state);
        }

        // remove this state from all transition.nextStates
        var graph = GetCurrentGraph(gv);
        if (graph != null && graph.transitionNodes != null)
        {
            for (int i = 0; i < graph.transitionNodes.Length; i++)
            {
                var t = graph.transitionNodes[i];
                if (t == null || t.nextStates == null) continue;
                var list = new List<BossStateNode>(t.nextStates);
                if (list.Remove(state))
                {
                    Undo.RecordObject(t, "Disconnect State from Transition");
                    t.nextStates = list.ToArray();
                    EditorUtility.SetDirty(t);
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    public static void DisconnectTransitionNode(this StateMachineGraphView gv, StateTransitionNodeView transView)
    {
        if (gv == null || transView == null || transView.transitionData == null) return;

        foreach (var e in gv.edges.ToList())
            if (e.input?.node == transView || e.output?.node == transView)
                gv.RemoveElement(e);

        var trans = transView.transitionData;

        // remove from every state's transitions
        var graph = GetCurrentGraph(gv);
        if (graph != null && graph.stateNodes != null)
        {
            for (int i = 0; i < graph.stateNodes.Length; i++)
            {
                var s = graph.stateNodes[i];
                if (s == null || s.transitions == null) continue;
                var list = new List<StateTransition>(s.transitions);
                if (list.Remove(trans))
                {
                    Undo.RecordObject(s, "Disconnect Transition");
                    s.transitions = list.ToArray();
                    EditorUtility.SetDirty(s);
                }
            }
        }

        // keep the transition asset (user wanted them preserved)
        // optionally clear its nextStates
        if (trans.nextStates != null && trans.nextStates.Length > 0)
        {
            Undo.RecordObject(trans, "Clear Transition NextStates");
            trans.nextStates = Array.Empty<BossStateNode>();
            EditorUtility.SetDirty(trans);
        }

        AssetDatabase.SaveAssets();
    }

    private static BossStateGraph GetCurrentGraph(StateMachineGraphView gv)
    {
        // reflect internal field currentGraph
        var fi = typeof(StateMachineGraphView).GetField("currentGraph", BindingFlags.Instance | BindingFlags.NonPublic);
        return fi?.GetValue(gv) as BossStateGraph;
    }
}