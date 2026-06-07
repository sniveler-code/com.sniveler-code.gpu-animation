#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using UnityEditor;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    public abstract class Demo3AnimatorPropertyDrawer : PropertyDrawer
    {
        private string[] _displayNames;
        private bool _isInitialized;

        private void Initialize(SerializedProperty property)
        {
            if (_isInitialized) return;
            var sObject = (Demo3AgentAuthoring)property.serializedObject.targetObject;
            var animator = sObject?.GetComponent<AnimatorAuthoring>();
            _displayNames = InitValues(animator);
            _isInitialized = true;
        }

        protected abstract string[] InitValues(AnimatorAuthoring animator);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.numericType != SerializedPropertyNumericType.UInt8)
            {
                EditorGUI.LabelField(position, label.text, "Use [Animation] with byte only.");
                return;
            }

            Initialize(property);

            int currentIndex = (int)property.uintValue;

            const float buttonWidth = 28f;
            Rect popupRect = new Rect(position.x, position.y, position.width - buttonWidth - 2f, position.height);
            property.uintValue = (uint)EditorGUI.Popup(popupRect, label.text, currentIndex, _displayNames);
        }
    }

    [CustomPropertyDrawer(typeof(Demo3AnimationAttribute))]
    public class Demo3AnimationPropertyDrawer : Demo3AnimatorPropertyDrawer
    {
        protected override string[] InitValues(AnimatorAuthoring animator)
        {
            var values = new List<string>();
            if (animator != null)
            {
                values.AddRange(animator.Animations.Select(animation => animation.Name));
            }

            return values.ToArray();
        }
    }

    [CustomPropertyDrawer(typeof(Demo3ParamsAttribute))]
    public class Demo3ParamsPropertyDrawer : Demo3AnimatorPropertyDrawer
    {
        protected override string[] InitValues(AnimatorAuthoring animator)
        {
            var values = new List<string>();
            if (animator != null)
            {
                values.AddRange(animator.Parameters.Select(animation => animation.Name));
            }

            return values.ToArray();
        }
    }
}
#endif
