// NodeUIHelpers.cs
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Timeline;
using UnityEditor.UIElements;

namespace NodeHelper
{
    public static class NodeUIHelpers
    {
        public static EnumField EnumField<TEnum>(string label, Func<TEnum> getter, Action<TEnum> setter)
            where TEnum : Enum
        {
            var field = new EnumField(label, getter());
            field.RegisterValueChangedCallback(evt =>
            {
                if (!Equals((TEnum)evt.newValue, getter()))
                {
                    setter((TEnum)evt.newValue);
                }
            });
            return field;
        }

        public static FloatField FloatField(string label, Func<float> getter, Action<float> setter)
        {
            var field = new FloatField(label) { value = getter() };
            field.RegisterValueChangedCallback(evt =>
            {
                if (Math.Abs(evt.newValue - getter()) > float.Epsilon)
                    setter(evt.newValue);
            });
            return field;
        }

        public static IntegerField IntField(string label, Func<int> getter, Action<int> setter)
        {
            var field = new IntegerField(label) { value = getter() };
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != getter())
                    setter(evt.newValue);
            });
            return field;
        }

        public static Toggle Toggle(string label, Func<bool> getter, Action<bool> setter)
        {
            var field = new Toggle(label) { value = getter() };
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != getter())
                    setter(evt.newValue);
            });
            return field;
        }

        // ---------- NEW: StringField ----------
        public static TextField StringField(string label, Func<string> getter, Action<string> setter, bool isDelayed = false)
        {
            var field = new TextField(label)
            {
                value = getter(),
                isDelayed = isDelayed
            };
            field.RegisterValueChangedCallback(evt =>
            {
                // Avoid redundant sets
                var current = getter();
                if (!string.Equals(evt.newValue, current, StringComparison.Ordinal))
                    setter(evt.newValue);
            });
            return field;
        }

        // ---------- NEW: BoolField (alias of Toggle) ----------
        public static Toggle BoolField(string label, Func<bool> getter, Action<bool> setter)
        {
            return Toggle(label, getter, setter);
        }

        // ---------- NEW: GameObjectField ----------
        public static ObjectField GameObjectField(string label, Func<GameObject> getter, Action<GameObject> setter, bool allowSceneObjects = true)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(GameObject),
                allowSceneObjects = allowSceneObjects,
                value = getter()
            };
            field.RegisterValueChangedCallback(evt =>
            {
                var newVal = evt.newValue as GameObject;
                if (newVal != getter())
                    setter(newVal);
            });
            return field;
        }

        // ---------- NEW: TransformField ----------
        public static ObjectField TransformField(string label, Func<Transform> getter, Action<Transform> setter, bool allowSceneObjects = true)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(Transform),
                allowSceneObjects = allowSceneObjects,
                value = getter()
            };
            field.RegisterValueChangedCallback(evt =>
            {
                var newVal = evt.newValue as Transform;
                if (newVal != getter())
                    setter(newVal);
            });
            return field;
        }

        // ---------- NEW: TimelineField ----------
        public static ObjectField TimelineField(string label, Func<TimelineAsset> getter, Action<TimelineAsset> setter)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(TimelineAsset),
                allowSceneObjects = false,
                value = getter()
            };
            field.RegisterValueChangedCallback(evt =>
            {
                var newVal = evt.newValue as TimelineAsset;
                if (newVal != getter())
                    setter(newVal);
            });
            return field;
        }

        public static void Show(VisualElement el, bool show)
        {
            el.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
