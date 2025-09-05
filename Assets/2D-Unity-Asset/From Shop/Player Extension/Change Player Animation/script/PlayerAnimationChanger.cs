using NaughtyAttributes;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

[ExecuteInEditMode]
public class PlayerAnimationChanger : PlayerExtension
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    [ShowAssetPreview] public Sprite playerSprite;

    [Header("State Names (โปรดอย่าแตะ)")]
    public string idleStateName = "Idle";
    public string walkStateName = "Walk";
    public string bowDrawStateName = "Bow_Draw";

    [Header("Options")]
    [Tooltip("Auto-apply changes in Edit Mode when fields are modified.")]
    public bool autoApply = true;

    [Tooltip("Duplicate the AnimatorController the first time you Apply, so edits are local to this Player.")]
    public bool duplicateControllerOnFirstApply = true;

    [Foldout("Idle Clips")]
    public AnimationClip Idle_Front;
    [Foldout("Idle Clips")] public AnimationClip Idle_Back;
    [Foldout("Idle Clips")] public AnimationClip Idle_Side;

    [Foldout("Walk Clips")]
    public AnimationClip Walk_Front;
    [Foldout("Walk Clips")] public AnimationClip Walk_Back;
    [Foldout("Walk Clips")] public AnimationClip Walk_Side;

    [Foldout("Bow_Draw Clips"),ShowIf(nameof(hasBowShotClass))]public AnimationClip Bow_Front;
    [Foldout("Bow_Draw Clips"),ShowIf(nameof(hasBowShotClass))] public AnimationClip Bow_Back;
    [Foldout("Bow_Draw Clips"),ShowIf(nameof(hasBowShotClass))] public AnimationClip Bow_Side;

    private bool hasBowShotClass = ClassChecker.Exists("BowShot");

#if UNITY_EDITOR
    // ----------------------- LIFECYCLE -----------------------
    void Reset()
    {
        if (!animator) animator = GetComponent<Animator>();
        RefreshFromAnimator();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        playerSprite = spriteRenderer.sprite;
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            if (!animator) animator = GetComponent<Animator>();
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            // Show current setup on first enable so students see what's already wired
            RefreshFromAnimator();
            playerSprite = spriteRenderer.sprite;
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;
        spriteRenderer.sprite = playerSprite;
        if (!autoApply) return;

        // Delay apply to avoid re-entrancy during inspector serialization
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            if (!gameObject.activeInHierarchy) return;
            ApplyToAnimator();
        };
    }

    // ----------------------- UI Buttons -----------------------
    [Button("Refresh From Animator")]
    public void RefreshFromAnimator() => TryDo(() =>
    {
        var ac = GetAnimatorController();
        if (ac == null) return;

        // Pull current clips from the three states & populate fields
        Idle_Front = GetChildClipByDirection(ac, idleStateName, Dir.Front);
        Idle_Back  = GetChildClipByDirection(ac, idleStateName, Dir.Back);
        Idle_Side  = GetChildClipByDirection(ac, idleStateName, Dir.SideAny);

        Walk_Front = GetChildClipByDirection(ac, walkStateName, Dir.Front);
        Walk_Back  = GetChildClipByDirection(ac, walkStateName, Dir.Back);
        Walk_Side  = GetChildClipByDirection(ac, walkStateName, Dir.SideAny);

        Bow_Front  = GetChildClipByDirection(ac, bowDrawStateName, Dir.Front);
        Bow_Back   = GetChildClipByDirection(ac, bowDrawStateName, Dir.Back);
        Bow_Side   = GetChildClipByDirection(ac, bowDrawStateName, Dir.SideAny);

        EditorUtility.SetDirty(this);
    });

    [Button("Apply Now")]
    public void ApplyToAnimator() => TryDo(() =>
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator == null) { Debug.LogWarning("[PlayerAnimationChanger] No Animator found."); return; }

        var ac = GetAnimatorController();
        if (ac == null) { Debug.LogWarning("[PlayerAnimationChanger] AnimatorController not found."); return; }

        // Duplicate controller once so edits are local to this player
        if (duplicateControllerOnFirstApply && !IsControllerOwnedByThisObject(ac))
        {
            var dup = DuplicateControllerAsset(ac);
            if (dup != null)
            {
                animator.runtimeAnimatorController = dup;
                ac = dup;
            }
        }

        // Write into Idle, Walk, Bow_Draw blend trees
        SetChildClipByDirection(ac, idleStateName, Dir.Front, Idle_Front);
        SetChildClipByDirection(ac, idleStateName, Dir.Back,  Idle_Back);
        SetChildClipByDirection(ac, idleStateName, Dir.SideR, Idle_Side);
        SetChildClipByDirection(ac, idleStateName, Dir.SideL, Idle_Side);

        SetChildClipByDirection(ac, walkStateName, Dir.Front, Walk_Front);
        SetChildClipByDirection(ac, walkStateName, Dir.Back,  Walk_Back);
        SetChildClipByDirection(ac, walkStateName, Dir.SideR, Walk_Side);
        SetChildClipByDirection(ac, walkStateName, Dir.SideL, Walk_Side);

        SetChildClipByDirection(ac, bowDrawStateName, Dir.Front, Bow_Front);
        SetChildClipByDirection(ac, bowDrawStateName, Dir.Back,  Bow_Back);
        SetChildClipByDirection(ac, bowDrawStateName, Dir.SideR, Bow_Side);
        SetChildClipByDirection(ac, bowDrawStateName, Dir.SideL, Bow_Side);

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(ac);
        Debug.Log("[PlayerAnimationChanger] Apply complete.");
    });

    [Button("Create Per-Object Copy Now")]
    public void CreatePerObjectCopy() => TryDo(() =>
    {
        var ac = GetAnimatorController();
        if (ac == null) return;
        var dup = DuplicateControllerAsset(ac, force:true);
        if (dup != null) animator.runtimeAnimatorController = dup;
    });

    // ----------------------- CORE HELPERS -----------------------
    AnimatorController GetAnimatorController()
    {
        if (animator == null) return null;
        var ctrl = animator.runtimeAnimatorController;
        if (ctrl == null) return null;

        // If OverrideController, get its base controller for editing
        if (ctrl is AnimatorOverrideController aoc)
            ctrl = aoc.runtimeAnimatorController;

        return ctrl as AnimatorController;
    }

    enum Dir { Front, Back, SideL, SideR, SideAny }

    // How we recognize the directional children in a 2D Freeform Directional tree
    static readonly Vector2 FRONT  = new Vector2(0f, -1f);
    static readonly Vector2 BACK   = new Vector2(0f, +1f);
    static readonly Vector2 SIDER  = new Vector2(+1f, 0f);
    static readonly Vector2 SIDEL  = new Vector2(-1f, 0f);
    const float POS_TOL = 0.25f; // tolerance when matching child positions

    AnimationClip GetChildClipByDirection(AnimatorController ac, string stateName, Dir dir)
    {
        var st = FindState(ac, stateName);
        if (st == null) return null;

        var bt = st.motion as BlendTree;
        if (bt == null)
        {
            // If not a tree, try to return the state's clip directly
            return st.motion as AnimationClip;
        }

        var (idx, _) = FindChildIndex(bt, dir);
        if (idx < 0) return null;
        return bt.children[idx].motion as AnimationClip;
    }

    void SetChildClipByDirection(AnimatorController ac, string stateName, Dir dir, AnimationClip clip)
    {
        if (clip == null) return; // nothing to set

        var st = FindState(ac, stateName);
        if (st == null)
        {
            Debug.LogWarning($"[PlayerAnimationChanger] State '{stateName}' not found.");
            return;
        }

        var bt = st.motion as BlendTree;
        if (bt == null)
        {
            // If state isn't a tree, and direction is 'Any', allow direct replacement
            if (dir == Dir.SideAny || dir == Dir.Front || dir == Dir.Back)
            {
                st.motion = clip;
                EditorUtility.SetDirty(st);
            }
            return;
        }

        // Replace the appropriate child(ren)
        if (dir == Dir.SideR || dir == Dir.SideL)
        {
            var (ir, _) = FindChildIndex(bt, Dir.SideR);
            var (il, _) = FindChildIndex(bt, Dir.SideL);
            if (ir >= 0)
            {
                var ch = bt.children;
                ch[ir].motion = clip;
                bt.children = ch;
                EditorUtility.SetDirty(bt);
            }
            if (il >= 0)
            {
                var ch = bt.children;
                ch[il].motion = clip;
                bt.children = ch;
                EditorUtility.SetDirty(bt);
            }
            return;
        }

        var (i, _) = FindChildIndex(bt, dir);
        if (i >= 0)
        {
            var ch = bt.children;
            ch[i].motion = clip;
            bt.children = ch;
            EditorUtility.SetDirty(bt);
        }
    }

    (int index, ChildMotion child) FindChildIndex(BlendTree bt, Dir dir)
    {
        if (bt == null) return (-1, default);

        Vector2 target = FRONT;
        switch (dir)
        {
            case Dir.Front:  target = FRONT; break;
            case Dir.Back:   target = BACK;  break;
            case Dir.SideR:  target = SIDER; break;
            case Dir.SideL:  target = SIDEL; break;
            case Dir.SideAny: // return whichever side exists first
                {
                    var right = FindChildIndex(bt, Dir.SideR);
                    if (right.index >= 0) return right;
                    return FindChildIndex(bt, Dir.SideL);
                }
        }

        var children = bt.children;
        for (int i = 0; i < children.Length; i++)
        {
            var pos = children[i].position;
            if (Vector2.Distance(pos, target) <= POS_TOL)
                return (i, children[i]);
        }

        // If positions aren't canonical, try name heuristics
        for (int i = 0; i < children.Length; i++)
        {
            var m = children[i].motion;
            if (m == null) continue;
            var n = m.name.ToLowerInvariant();
            if (dir == Dir.Front  && n.Contains("front")) return (i, children[i]);
            if (dir == Dir.Back   && n.Contains("back"))  return (i, children[i]);
            if (dir == Dir.SideR  && (n.Contains("side") || n.Contains("right"))) return (i, children[i]);
            if (dir == Dir.SideL  && (n.Contains("side") || n.Contains("left")))  return (i, children[i]);
            if (dir == Dir.SideAny && n.Contains("side")) return (i, children[i]);
        }

        return (-1, default);
    }

    AnimatorState FindState(AnimatorController ac, string stateName)
    {
        if (ac == null) return null;
        foreach (var layer in ac.layers)
        {
            var st = FindStateRecursive(layer.stateMachine, stateName);
            if (st != null) return st;
        }
        return null;
    }

    AnimatorState FindStateRecursive(AnimatorStateMachine sm, string stateName)
    {
        foreach (var s in sm.states)
            if (s.state != null && s.state.name == stateName) return s.state;

        foreach (var sub in sm.stateMachines)
        {
            var found = FindStateRecursive(sub.stateMachine, stateName);
            if (found != null) return found;
        }
        return null;
    }

    bool IsControllerOwnedByThisObject(AnimatorController ac)
    {
        if (ac == null) return false;
        string path = AssetDatabase.GetAssetPath(ac);
        if (string.IsNullOrEmpty(path)) return false;
        return path.Contains($"_{gameObject.name}_Override");
    }

    AnimatorController DuplicateControllerAsset(AnimatorController original, bool force = false)
    {
        if (original == null) return null;
        string src = AssetDatabase.GetAssetPath(original);
        if (string.IsNullOrEmpty(src))
        {
            Debug.LogWarning("[PlayerAnimationChanger] AnimatorController is not an asset; cannot duplicate.");
            return null;
        }

        if (!force && IsControllerOwnedByThisObject(original))
            return original;

        string newPath = AssetDatabase.GenerateUniqueAssetPath(
            src.Replace(".controller", $"_{gameObject.name}_Override.controller"));
        AssetDatabase.CopyAsset(src, newPath);
        AssetDatabase.ImportAsset(newPath);
        var dup = AssetDatabase.LoadAssetAtPath<AnimatorController>(newPath);
        if (dup != null)
        {
            Debug.Log($"[PlayerAnimationChanger] Duplicated AnimatorController → {newPath}");
        }
        return dup;
    }

    void TryDo(System.Action action)
    {
        try { action?.Invoke(); }
        catch (System.SystemException e) { Debug.LogError(e); }
        catch (System.Exception e) { Debug.LogError(e); }
    }
#endif
}
