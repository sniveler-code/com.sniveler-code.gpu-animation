using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Utils
{
    internal sealed class DummyBuffer
    {
        private GraphicsBuffer _buffer;
        private GraphicsBuffer _stateBuffer;

        public void Init()
        {
            if (_buffer == null || !_buffer.IsValid())
            {
                _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, 48);
                _buffer.SetData(new[] {float3x4.zero, float3x4.zero});
                Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferLbs, _buffer);
            }

            if (_stateBuffer == null || !_stateBuffer.IsValid())
            {
                _stateBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, 16);
                _stateBuffer.SetData(new[] {default(GpuInstanceAnimState), default(GpuInstanceAnimState)});
                Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferState, _stateBuffer);
            }
        }

        public void Release()
        {
            if (_buffer != null)
            {
                Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferLbs, (GraphicsBuffer) null);
                if (_buffer.IsValid()) _buffer.Dispose();
                _buffer = null;
            }

            if (_stateBuffer != null)
            {
                Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferState, (GraphicsBuffer) null);
                if (_stateBuffer.IsValid()) _stateBuffer.Dispose();
                _stateBuffer = null;
            }
        }

        public void Set()
        {
            Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferLbs, _buffer);
            Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferState, _stateBuffer);
        }

        public void Set(GpuInstanceAnimState data)
        {
            if (_stateBuffer == null || !_stateBuffer.IsValid()) return;
            _stateBuffer.SetData(new[] {default(GpuInstanceAnimState), data});
            Shader.SetGlobalBuffer(AnimatorUtils.AnimBufferState, _stateBuffer);
        }
    }
}
