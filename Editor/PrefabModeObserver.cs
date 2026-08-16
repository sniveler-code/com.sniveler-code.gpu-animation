using System;
using SnivelerCode.GpuAnimation.Editor.Utils;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RuntimeAnimatorUtils = SnivelerCode.GpuAnimation.Runtime.Utils.AnimatorUtils;

namespace SnivelerCode.GpuAnimation.Editor
{
    [InitializeOnLoad]
    public static class PrefabModeObserver
    {
        private static GraphicsBuffer _gpuBufferLbs;

        static PrefabModeObserver()
        {
            RuntimeAnimatorUtils.InitDummyBuffer();
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnEditorQuitting;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            GameObject root = prefabStage.prefabContentsRoot;
            var animator = root.GetComponent<AnimatorAuthoring>();
            if (animator == null) return;

            TogglePropertyBlock(animator, 1f);

            if (animator.Matrices.MatricesLbs.Length > 0)
            {
                int lengthLbs = animator.Matrices.MatricesLbs.Length;
                _gpuBufferLbs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, lengthLbs, 48);

                var tempArrayLbs = new NativeArray<float3x4>(lengthLbs, Allocator.Temp);
                for (int i = 0; i < lengthLbs; i++)
                {
                    tempArrayLbs[i] = animator.Matrices.MatricesLbs[i];
                }

                _gpuBufferLbs.SetData(tempArrayLbs);
                tempArrayLbs.Dispose();

                if (_gpuBufferLbs == null || !_gpuBufferLbs.IsValid()) return;
                Shader.SetGlobalBuffer(RuntimeAnimatorUtils.AnimBufferLbs, _gpuBufferLbs);
            }
        }

        private static void OnDomainUnload(object sender, EventArgs e) => Cleanup();

        private static void OnBeforeAssemblyReload() => Cleanup();

        private static void OnEditorQuitting() => Cleanup();

        private static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            RuntimeAnimatorUtils.SetDummyBuffer();

            if (_gpuBufferLbs != null)
            {
                _gpuBufferLbs.Dispose();
                _gpuBufferLbs = null;
            }

            GameObject root = prefabStage.prefabContentsRoot;
            var animator = root.GetComponent<AnimatorAuthoring>();
            if (animator == null) return;
            TogglePropertyBlock(animator, 0);
        }

        private static void TogglePropertyBlock(AnimatorAuthoring animator, float value)
        {
            var renderers = animator.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var propertyBlock = new MaterialPropertyBlock();
                r.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(AnimatorShaderProperty.InstanceID, value);
                r.SetPropertyBlock(propertyBlock);
            }
        }

        private static void Cleanup()
        {
            if (_gpuBufferLbs != null)
            {
                if (_gpuBufferLbs.IsValid()) _gpuBufferLbs.Dispose();
                _gpuBufferLbs = null;
            }

            RuntimeAnimatorUtils.ReleaseDummyBuffer();
        }
    }
}
