using UnityEngine;
using System;
using UnityEngine.UIElements;
using NaughtyAttributes;

public interface IUseStamina
{
    public bool isUsingStamina { get; }
    public bool canDrainStamina { get; }
    void DrainStamina(float amount);
}

public interface ICancleGravity
{
    public bool canApplyGravity { get; set; }
}

public interface IInteractable
{
    void Interact();
}
public interface INodeInspectorContributor
{
    /// Build your node inspector controls under `container`.
    /// Implementers should keep references to their own fields if they need to toggle visibility later.
    void BuildInspectorUI(VisualElement container);

    /// Called by the node view when values change and you want to re-check visibility / refresh.
    void RefreshInspectorUI();
}