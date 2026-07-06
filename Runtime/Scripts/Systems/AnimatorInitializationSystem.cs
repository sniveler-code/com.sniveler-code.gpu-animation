using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed partial class AnimatorInitializationSystem : SystemBase
    {
        private GraphicsBuffer _gpuBufferLbs;

        protected override void OnCreate()
        {
            RequireForUpdate<SceneAnimatorConfigData>();
        }

        protected override void OnStartRunning()
        {
            var configEntity = SystemAPI.GetSingletonEntity<SceneAnimatorConfigData>();
            var config = SystemAPI.GetComponent<SceneAnimatorConfigData>(configEntity);

            if (!config.Blob.IsCreated) return;
            ref var blobData = ref config.Blob.Value;

            if (blobData.MatricesLbs.Length > 0)
            {
                int lengthLbs = blobData.MatricesLbs.Length;
                _gpuBufferLbs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, lengthLbs, 48);
                var tempArrayLbs = new NativeArray<float3x4>(lengthLbs, Allocator.Temp);
                for (int i = 0; i < lengthLbs; i++)
                {
                    tempArrayLbs[i] = blobData.MatricesLbs[i];
                }

                _gpuBufferLbs.SetData(tempArrayLbs);
                tempArrayLbs.Dispose();

                if (_gpuBufferLbs == null || !_gpuBufferLbs.IsValid()) return;
                Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferLbs, _gpuBufferLbs);

#if UNITY_EDITOR
                long totalBytes = (long) lengthLbs * 48;
                double sizeInMb = (double) totalBytes / (1024 * 1024);

                AnimatorLogger.LogManaged($"GraphicsBuffer successfully initialized with {sizeInMb:F2} Mb.");
#endif
            }
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnStopRunning()
        {
            AnimatorUtils.SetDummyBuffer();
            DisposeSystem();
        }

        protected override void OnDestroy() => DisposeSystem();

        private void DisposeSystem()
        {
            if (_gpuBufferLbs == null) return;

            _gpuBufferLbs.Dispose();
            _gpuBufferLbs = null;
        }
    }
}
