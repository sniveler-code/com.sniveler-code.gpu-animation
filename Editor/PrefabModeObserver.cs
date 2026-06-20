using System;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor
{
    [InitializeOnLoad]
    public static class PrefabModeObserver
    {
        private static GraphicsBuffer _gpuBufferLbs;

        static PrefabModeObserver()
        {
            AnimationUtils.InitDummyBuffer();
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            GameObject root = prefabStage.prefabContentsRoot;
            var animator = root.GetComponent<AnimatorAuthoring>();
            if (animator == null) return;

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
                Shader.SetGlobalBuffer(AnimationUtils.PropertyAnimBufferLbs, _gpuBufferLbs);
            }
        }

        private static void OnDomainUnload(object sender, EventArgs e) => Cleanup();

        private static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            Shader.SetGlobalBuffer(AnimationUtils.PropertyAnimBufferLbs, AnimationUtils.DummyBuffer);
            if (_gpuBufferLbs != null)
            {
                _gpuBufferLbs.Dispose();
                _gpuBufferLbs = null;
            }
        }

        private static void Cleanup()
        {
            if (_gpuBufferLbs != null)
            {
                if (_gpuBufferLbs.IsValid()) _gpuBufferLbs.Dispose();
                _gpuBufferLbs = null;
            }

            AnimationUtils.ReleaseDummyBuffer();
        }
    }
}
