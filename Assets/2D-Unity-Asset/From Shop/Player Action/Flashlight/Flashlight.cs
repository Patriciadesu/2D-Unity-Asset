using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class Flashlight : PlayerExtension
{
    public KeyCode activateKey = KeyCode.F;

    // SCENE INSTANCE (if you already placed one in the Hierarchy, assign it here)
    public GameObject flashLight;

    // PREFAB ASSET (optional). If not set, it will be loaded from Resources.
    public GameObject flashLightPrefab;

    public Vector3 offSet = new Vector3(0.5f, -0.35f, 0.8f);
    public Light light;

    [MinMaxSlider(0, 180)]
    public Vector2 innerOuter = new Vector2(25, 65);
    public float intensity = 5;
    public float lightRange = 10;

    private bool isOn = false;

    void OnEnable()
    {
        // In both Edit Mode and Play Mode, make sure we have a SCENE INSTANCE
        EnsureInstance();
        ApplySettings();
#if UNITY_EDITOR
        // In the editor, keep the light visible so you can tweak values
        if (!Application.isPlaying && light != null)
            light.gameObject.SetActive(true);
#endif
    }

    void Start()
    {
        // Re-fetch light if needed (covers play-mode domain reloads)
        if (light == null) TryCacheLight();
        isOn = false;
        if (Application.isPlaying && light != null)
            light.gameObject.SetActive(isOn);
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            if (Input.GetKeyDown(activateKey))
            {
                isOn = !isOn;
                if (light != null) light.gameObject.SetActive(isOn);
            }
            return;
        }

        // --- EDIT MODE PATH ---
        // Never touch prefab assets; only the scene instance.
        EnsureInstance();
        ApplySettings();

        // Keep the instance parented/positioned for preview in the editor
        if (flashLight != null && _player != null && _player.camera != null)
        {
            if (flashLight.transform.parent != _player.camera.transform)
                flashLight.transform.SetParent(_player.camera.transform, false);

            flashLight.transform.localPosition = offSet;
        }
    }

    // ----------------- Helpers -----------------

    private void EnsureInstance()
    {
        // Don’t run on prefab assets / Prefab Mode root
        if (!gameObject.scene.IsValid()) return;

        // Need a player camera to mount to
        if (_player == null || _player.camera == null) return;

        // If we already have a SCENE instance, we’re done
        if (flashLight != null && flashLight.scene.IsValid()) return;

        // Figure out the prefab to use
        var prefab = flashLightPrefab;
        if (prefab == null)
        {
            // Fallback to Resources path you were using
            prefab = Resources.Load<GameObject>("Flashlight/FlashlightPrefab");
            flashLightPrefab = prefab; // cache so we don’t keep loading
        }
        if (prefab == null) return; // Nothing to instantiate

        // Instantiate an instance and parent it to the camera
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Use PrefabUtility in Edit Mode to keep proper prefab linkage
            var parent = _player.camera.transform;
            var go = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (go == null) return;
            flashLight = go;
            flashLight.transform.localPosition = offSet;
        }
        else
        {
            flashLight = Instantiate(prefab, _player.camera.transform);
            flashLight.transform.localPosition = offSet;
        }
#else
        flashLight = Instantiate(prefab, _player.camera.transform);
        flashLight.transform.localPosition = offSet;
#endif

        TryCacheLight();
    }

    private void TryCacheLight()
    {
        if (flashLight == null) return;

        light = flashLight.GetComponent<Light>();
        if (light == null) light = flashLight.GetComponentInChildren<Light>(true);
    }

    private void ApplySettings()
    {
        if (flashLight == null) return;
        if (light == null) TryCacheLight();
        if (light == null) return;

        // Apply spotlight settings safely
        if (light.type == UnityEngine.LightType.Spot)
        {
            light.innerSpotAngle = innerOuter.x;
            light.spotAngle = innerOuter.y;
            light.intensity = intensity;
            light.range = lightRange;
        }
    }
    void OnDestroy()
    {
        DestroyImmediate(flashLight);
    }
}
