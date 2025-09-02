using NaughtyAttributes;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;

[ExecuteInEditMode]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator => GetComponent<Animator>();
    private string walk = "Walk";//blend tree
    private string run = "Run";//blend tree
    private string crouch = "Crouch";//blend tree
    private string idle = "Idle";
    private string jump = "Jump";
    private string wallRideLeft = "WallRide_L";
    private string wallRideRight = "WallRide_R";
    [Foldout("Model"),SerializeField] GameObject defaultModel; // This is the default player model 
    [Foldout("Model")] public bool overrideModel = false;
    [SerializeField,ShowIf("overrideModel"),Foldout("Model")] GameObject modelContainer; // This is the container for the player model
    [SerializeField,ShowIf("overrideModel"),Foldout("Model")] GameObject modelPrefab;
    [SerializeField, Tooltip("PLS DONT TOUCH THIS"),Foldout("Animation")] private PlayerAnimator defaultAnimation;
    [SerializeField,Foldout("Animation")] private bool overrideAnimations = false;

    [ShowIf("overrideAnimations"), SerializeField,Foldout("Animation")] private PlayerAnimator playerAnimator;

    public void OnValidate()
    {
        if (!Application.isPlaying && overrideAnimations && playerAnimator == null)
        {
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Ensure Assets/Resources/PlayerAnimatorOverrides exists
            string overridesPath = $"{resourcesPath}/PlayerAnimatorOverrides";
            if (!AssetDatabase.IsValidFolder(overridesPath))
            {
                AssetDatabase.CreateFolder(resourcesPath, "PlayerAnimatorOverrides");
            }

            // Create the PlayerAnimator asset
            playerAnimator = ScriptableObject.CreateInstance<PlayerAnimator>();
            playerAnimator.name = $"{gameObject.name}_PlayerAnimatorOverride";

            string assetPath = $"{overridesPath}/{playerAnimator.name}.asset";
            AssetDatabase.CreateAsset(playerAnimator, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created new PlayerAnimator asset at: {assetPath}");
            // override player model by destroy everythinbg in model container and instantiate new model prefab


        }
        
        
    }

    public void Update()
    {
        if (!Application.isPlaying && overrideModel && modelContainer != null && modelPrefab != null)
        {
            bool modelExists = false;
            foreach (Transform child in modelContainer.transform)
            {
                if (child.gameObject.name == modelPrefab.name)
                {
                    modelExists = true;
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            if (!modelExists)
            {
                GameObject newModel = Instantiate(modelPrefab, modelContainer.transform);
                newModel.transform.localPosition = Vector3.zero; // Reset position
                newModel.name = modelPrefab.name;
                Debug.Log($"✅ Instantiated new model: {newModel.name} in {modelContainer.name}");
            }
            else
            {
                Debug.Log($"✅ Model {modelPrefab.name} already exists in {modelContainer.name}");
            }
        }
        if (!Application.isPlaying && animator != null)
        {
            if (overrideAnimations)
            {
                Debug.Log("Overriding Player Animation");
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller != null)
                {
                    foreach (var layer in controller.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            // Handle Blend Trees
                            if (state.state.motion is BlendTree blendTree)
                            {
                                if (state.state.name == walk)
                                {
                                    ReplaceBlendTreeClip(blendTree, 0, playerAnimator.walk_left);
                                    ReplaceBlendTreeClip(blendTree, 1, playerAnimator.walk_right);
                                    ReplaceBlendTreeClip(blendTree, 2, playerAnimator.walk_front);
                                    ReplaceBlendTreeClip(blendTree, 3, playerAnimator.walk_back);
                                    // Add more indices if needed for walk_left, walk_right, etc.
                                }
                                else if (state.state.name == run)
                                {
                                    ReplaceBlendTreeClip(blendTree, 0, playerAnimator.run_left);
                                    ReplaceBlendTreeClip(blendTree, 1, playerAnimator.run_right);
                                    ReplaceBlendTreeClip(blendTree, 2, playerAnimator.run_front);
                                    ReplaceBlendTreeClip(blendTree, 3, playerAnimator.run_back);
                                }
                                else if (state.state.name == crouch)
                                {
                                    ReplaceBlendTreeClip(blendTree, 0, playerAnimator.crouch_front);
                                    ReplaceBlendTreeClip(blendTree, 1, playerAnimator.crouch_back);
                                    ReplaceBlendTreeClip(blendTree, 2, playerAnimator.crouch_left);
                                    ReplaceBlendTreeClip(blendTree, 3, playerAnimator.crouch_right);
                                    ReplaceBlendTreeClip(blendTree, 3, playerAnimator.crouch_idle);
                                }
                            }
                            else
                            {
                                // Handle normal animation states
                                switch (state.state.name)
                                {
                                    case "Idle":
                                        state.state.motion = playerAnimator.idle;
                                        break;
                                    case "Jump":
                                        state.state.motion = playerAnimator.jump;
                                        break;
                                    case "WallRide_L":
                                        state.state.motion = playerAnimator.wallRideLeft;
                                        break;
                                    case "WallRide_R":
                                        state.state.motion = playerAnimator.wallRideRight;
                                        break;
                                    case "Dash":
                                        state.state.motion = playerAnimator.dash;
                                        break;
                                    case "Roll":
                                        state.state.motion = playerAnimator.roll;
                                        break;
                                }
                                //Debug.Log($"State '{state.state.name}' motion set to '{state.state.motion.name}'");
                            }
                            EditorUtility.SetDirty(controller);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Using Default Player Animation");
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller != null)
                {
                    foreach (var layer in controller.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            // Handle Blend Trees
                            if (state.state.motion is BlendTree blendTree)
                            {
                                if (state.state.name == walk)
                                {
                                    ReplaceBlendTreeClip(blendTree, 0, defaultAnimation.walk_left);
                                    ReplaceBlendTreeClip(blendTree, 1, defaultAnimation.walk_right);
                                    ReplaceBlendTreeClip(blendTree, 2, defaultAnimation.walk_front);
                                    ReplaceBlendTreeClip(blendTree, 3, defaultAnimation.walk_back);
                                    // Add more indices if needed for walk_left, walk_right, etc.
                                }
                                else if (state.state.name == run)
                                {
                                    ReplaceBlendTreeClip(blendTree, 0, defaultAnimation.run_left);
                                    ReplaceBlendTreeClip(blendTree, 1, defaultAnimation.run_right);
                                    ReplaceBlendTreeClip(blendTree, 2, defaultAnimation.run_front);
                                    ReplaceBlendTreeClip(blendTree, 3, defaultAnimation.run_back);
                                }
                                else if (state.state.name == crouch)
                                {
                                    ReplaceBlendTreeClip(blendTree, 0, defaultAnimation.crouch_front);
                                    ReplaceBlendTreeClip(blendTree, 1, defaultAnimation.crouch_back);
                                    ReplaceBlendTreeClip(blendTree, 2, defaultAnimation.crouch_left);
                                    ReplaceBlendTreeClip(blendTree, 3, defaultAnimation.crouch_right);
                                    ReplaceBlendTreeClip(blendTree, 3, defaultAnimation.crouch_idle);
                                }
                            }
                            else
                            {
                                // Handle normal animation states
                                switch (state.state.name)
                                {
                                    case "Idle":
                                        state.state.motion = defaultAnimation.idle;
                                        break;
                                    case "Jump":
                                        state.state.motion = defaultAnimation.jump;
                                        break;
                                    case "WallRide_L":
                                        state.state.motion = defaultAnimation.wallRideLeft;
                                        break;
                                    case "WallRide_R":
                                        state.state.motion = defaultAnimation.wallRideRight;
                                        break;
                                    case "Dash":
                                        state.state.motion = defaultAnimation.dash;
                                        break;
                                    case "Roll":
                                        state.state.motion = defaultAnimation.roll;
                                        break;
                                }
                                //Debug.Log($"State '{state.state.name}' motion set to '{state.state.motion.name}'");
                            }
                            EditorUtility.SetDirty(controller);
                        }
                    }
                }

            }
        }
    }
    public void ReplaceBlendTreeClip(BlendTree blendTree, int index, AnimationClip newClip)
    {
        if (newClip == null) return;

        var children = blendTree.children;
        if (index >= 0 && index < children.Length)
        {
            children[index].motion = newClip;
            blendTree.children = children; // Apply changes
            EditorUtility.SetDirty(blendTree);
            Debug.Log($"BlendTree '{blendTree.name}' motion[{index}] replaced with '{newClip.name}'");
        }
    }
}
