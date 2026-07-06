using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Entities;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class AnimatorUploadSystem : SystemBase
    {
        private static readonly int _snivelerInstanceAnimState =
            Shader.PropertyToID("_SnivelerInstanceAnimState");

        private GraphicsBuffer _gpuStateBuffer0;
        private GraphicsBuffer _gpuStateBuffer1;
        private int _currentBufferCapacity;

        protected override void OnCreate()
        {
            RequireForUpdate<AnimatorProcessSystem.DoubleBufferData>();
            RequireForUpdate<AnimatorIndexState>();
        }

        protected override void OnDestroy()
        {
            _gpuStateBuffer0?.Release();
            _gpuStateBuffer1?.Release();
        }

        protected override void OnUpdate()
        {
            var bufferData = SystemAPI.GetSingleton<AnimatorProcessSystem.DoubleBufferData>();
            var indexState = SystemAPI.GetSingleton<AnimatorIndexState>();

            if (bufferData.Capacity == 0 || indexState.Value == 0) return;

            int readIndex = 1 - bufferData.WriteIndex;
            if (readIndex == 0) bufferData.Handle0.Complete();
            else bufferData.Handle1.Complete();

            var arrayToUpload = readIndex == 0 ? bufferData.Array0 : bufferData.Array1;

            if (bufferData.Capacity > _currentBufferCapacity)
            {
                _gpuStateBuffer0?.Release();
                _gpuStateBuffer1?.Release();

                _currentBufferCapacity = bufferData.Capacity;
                _gpuStateBuffer0 = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _currentBufferCapacity, 16);
                _gpuStateBuffer1 = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _currentBufferCapacity, 16);
            }

            var gpuBufferToBind = readIndex == 0 ? _gpuStateBuffer0 : _gpuStateBuffer1;
            gpuBufferToBind.SetData(arrayToUpload, 0, 0, indexState.Value);
            Shader.SetGlobalBuffer(_snivelerInstanceAnimState, gpuBufferToBind);
        }
    }
}
