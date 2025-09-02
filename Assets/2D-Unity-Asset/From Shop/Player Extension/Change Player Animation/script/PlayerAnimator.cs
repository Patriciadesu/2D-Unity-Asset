using UnityEngine;
using NaughtyAttributes;
using System;

[Serializable,CreateAssetMenu(fileName = "PlayerAnimator", menuName = "ScriptableObjects/PlayerAnimator", order = 1)]
public class PlayerAnimator : ScriptableObject
{
    [Foldout("Walk Animation")] public AnimationClip walk_front;
    [Foldout("Walk Animation")] public AnimationClip walk_back;
    [Foldout("Walk Animation")] public AnimationClip walk_left;
    [Foldout("Walk Animation")] public AnimationClip walk_right;
    [Foldout("Run Animation")] public AnimationClip run_front;
    [Foldout("Run Animation")] public AnimationClip run_back;
    [Foldout("Run Animation")] public AnimationClip run_left;
    [Foldout("Run Animation")] public AnimationClip run_right;
    [Foldout("Crouch Animation")] public AnimationClip crouch_idle;
    [Foldout("Crouch Animation")] public AnimationClip crouch_front;
    [Foldout("Crouch Animation")] public AnimationClip crouch_back;
    [Foldout("Crouch Animation")] public AnimationClip crouch_left;
    [Foldout("Crouch Animation")] public AnimationClip crouch_right;
    public AnimationClip idle;
    public AnimationClip jump;
    public AnimationClip wallRideLeft;
    public AnimationClip wallRideRight;
    public AnimationClip dash;
    public AnimationClip roll;

}

