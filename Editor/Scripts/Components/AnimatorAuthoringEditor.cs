using System.Linq;
using SnivelerCode.GpuAnimation.Editor.Utils;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using SnivelerCode.GpuAnimation.Runtime.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AnimatorUtils = SnivelerCode.GpuAnimation.Runtime.Utils.AnimatorUtils;

#if UNITY_EDITOR

namespace SnivelerCode.GpuAnimation.Editor.Components
{
    [CustomEditor(typeof(AnimatorAuthoring))]
    public sealed class AnimatorAuthoringEditor : UnityEditor.Editor
    {
        private int _animationIndex;
        private int _currentFrame = -1;
        private Material _material;

        private void OnSceneGUI()
        {
            var animator = (AnimatorAuthoring) target;
            PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(animator.gameObject);
            if (prefabStage != null) return;

            var lodGroup = animator.GetComponent<LODGroup>();
            if (lodGroup == null) return;

            var lods = lodGroup.GetLODs();
            if (lods.Length == 0) return;
            var lod0 = lods[0];

            foreach (var renderer in lod0.renderers)
            {
                if (renderer == null) continue;
                Mesh mesh = null;
                switch (renderer)
                {
                    case SkinnedMeshRenderer smr:
                        mesh = smr.sharedMesh;
                        break;

                    case MeshRenderer mr:
                    {
                        if (mr.TryGetComponent<MeshFilter>(out var filter))
                        {
                            mesh = filter.sharedMesh;
                        }

                        break;
                    }
                }

                if (mesh == null) continue;

                _material ??= Utils.AnimatorUtils.GetDebugMaterial();
                _material.SetPass(0);

                GL.wireframe = true;
                Graphics.DrawMeshNow(mesh, renderer.transform.localToWorldMatrix);
                GL.wireframe = false;
            }
        }

        public override void OnInspectorGUI()
        {
            var animator = (AnimatorAuthoring) target;
#if SC_GPU_ANIMATION_DEBUG

            GUI.enabled = false;
            serializedObject.Update();
            EditorGUILayout.TextField("Animations", animator.Animations.Count.ToString());
            EditorGUILayout.TextField($"Parameters", animator.Parameters.Count.ToString());
            EditorGUILayout.TextField("DefaultAnimation", animator.Animations[animator.DefaultAnimation].Name);
            GUI.enabled = true;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnimatorAuthoring.Matrices)));

            serializedObject.ApplyModifiedProperties();
#else
            base.OnInspectorGUI();
#endif

            PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(animator.gameObject);
            if (prefabStage == null) return;

            if (animator.Animations is not {Count: > 0}) return;
            string[] names = animator.Animations.Select(a => a.Name).ToArray();
            EditorGUI.BeginChangeCheck();
            int nextAnimation = EditorGUILayout.Popup("Animation", _animationIndex, names);
            if (nextAnimation != _animationIndex)
            {
                _animationIndex = nextAnimation;
                _currentFrame = -1;
            }

            var currentAnim = animator.Animations[_animationIndex];
            if (currentAnim.Frames > 0)
            {
                int currentFrame = EditorGUILayout.IntSlider(_currentFrame, 0, currentAnim.Frames - 1);
                if (currentFrame == _currentFrame) return;
                _currentFrame = currentFrame;
            }

            uint boneCount = (uint) animator.BonesCount;
            uint frameOffset = (uint) (currentAnim.Start + _currentFrame * boneCount);

            AnimatorUtils.SetDummyBuffer(new GpuInstanceAnimState
            {
                FrameA0 = frameOffset,
                FrameA1 = frameOffset,
                LerpA = 0f
            });

            EditorApplication.delayCall = SceneView.RepaintAll;
        }
    }
}
#endif
