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
        private static bool _wasDirty;
        private static GraphicsBuffer _gpuBufferDqs;
        private static GraphicsBuffer _gpuBufferLbs;

        static PrefabModeObserver()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
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

                Shader.SetGlobalBuffer(AnimationUtils.PropertyAnimBufferLbs, _gpuBufferLbs);
            }
        }

        private static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            _gpuBufferDqs?.Dispose();
            _gpuBufferLbs?.Dispose();
        }
    }
}
