using System.Linq;
using SnivelerCode.GpuAnimation.Editor.Utils;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if UNITY_EDITOR

namespace SnivelerCode.GpuAnimation.Editor.Components
{
    [CustomEditor(typeof(AnimatorAuthoring))]
    public sealed class AnimatorAuthoringEditor : UnityEditor.Editor
    {
        private Material _material;
        private int _animationIndex;
        private int _currentFrame;
        private MaterialPropertyBlock _previewPropBlock;
        private Renderer[] _renderers;

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

                _material ??= AnimatorUtils.GetDebugMaterial();
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

            _renderers ??= animator.GetComponentsInChildren<Renderer>(true);
            if (_previewPropBlock == null)
            {
                _previewPropBlock = new MaterialPropertyBlock();
                foreach (var r in _renderers)
                {
                    if (r != null) r.GetPropertyBlock(_previewPropBlock);
                }
            }

            if (animator.Animations is not {Count: > 0}) return;
            string[] names = animator.Animations.Select(a => a.Name).ToArray();
            EditorGUI.BeginChangeCheck();
            int nextAnimation = EditorGUILayout.Popup("Animation", _animationIndex, names);
            if (nextAnimation != _animationIndex)
            {
                _animationIndex = nextAnimation;
                _currentFrame = 0;
            }

            var currentAnim = animator.Animations[_animationIndex];
            if (currentAnim.Frames > 0)
            {
                _currentFrame = EditorGUILayout.IntSlider(_currentFrame, 0, currentAnim.Frames - 1);
            }

            if (!EditorGUI.EndChangeCheck()) return;

            uint boneCount = (uint) animator.BonesCount;
            uint frameOffset = (uint) (currentAnim.Start + _currentFrame * boneCount);
            Vector4 renderFrames = new Vector4(frameOffset, frameOffset, 0, 0);
            _previewPropBlock.SetVector(AnimatorShaderProperty.RenderFramesId, renderFrames);
            foreach (var r in _renderers)
            {
                if (r != null) r.SetPropertyBlock(_previewPropBlock);
            }

            SceneView.RepaintAll();
        }
    }
}
#endif
