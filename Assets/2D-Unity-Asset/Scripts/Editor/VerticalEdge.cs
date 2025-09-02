using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class VerticalEdge : Edge
{
    public VerticalEdge()
    {
        capabilities |= Capabilities.Selectable | Capabilities.Deletable;
        pickingMode = PickingMode.Position;

        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", _ =>
            {
                var gv = this.GetFirstAncestorOfType<GraphView>();
                if (gv == null) return;
                gv.RemoveElement(this);
            });
        }));
    }

    public override bool UpdateEdgeControl()
    {
        if (edgeControl == null) return false;

        Vector2 fromWorld = output != null ? output.GetGlobalCenter() : Vector2.zero;
        Vector2 toWorld   = input  != null ? input.GetGlobalCenter()  : Vector2.zero;

        Vector2 from = edgeControl.WorldToLocal(fromWorld);
        Vector2 to   = edgeControl.WorldToLocal(toWorld);

        edgeControl.outputOrientation = Orientation.Vertical;
        edgeControl.inputOrientation  = Orientation.Vertical;
        edgeControl.from = from;
        edgeControl.to   = to;

        // VFX-like vertical polyline feel (if your API supports controlPoints)
        float midY = (from.y + to.y) * 0.5f;
#if UNITY_6000_0_OR_NEWER
        edgeControl.outputOrientation = Orientation.Vertical;
        edgeControl.inputOrientation = Orientation.Vertical;
        edgeControl.from = from; edgeControl.to = to;
#endif
        edgeControl.MarkDirtyRepaint();
        return true;
    }
}

