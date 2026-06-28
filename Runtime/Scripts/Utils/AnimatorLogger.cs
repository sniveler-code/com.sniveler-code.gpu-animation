using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;

namespace SnivelerCode.GpuAnimation.Runtime.Utils
{
    public static class AnimatorLogger
    {
        public static BurstLogBuilder BurstLog() => new BurstLogBuilder(true);

        [Conditional("SNC_DEBUG_INFO")]
        public static void LogManaged(string message) => UnityEngine.Debug.Log($"[SCGpuAnimator] {message}");

        public static void ErrorManaged(string message) => UnityEngine.Debug.LogError($"[SCGpuAnimator] {message}");
    }

    public ref struct BurstLogBuilder
    {
        private FixedString512Bytes _buffer;

        public BurstLogBuilder(bool isInitialized) => _buffer = new FixedString512Bytes();

        public BurstLogBuilder Append(in FixedString64Bytes s)
        {
            _buffer.Append(s);
            return this;
        }

        public BurstLogBuilder Append(in FixedString128Bytes s)
        {
            _buffer.Append(s);
            return this;
        }

        public BurstLogBuilder Append(int value)
        {
            _buffer.Append(value);
            return this;
        }

        public BurstLogBuilder Append(uint value)
        {
            _buffer.Append(value);
            return this;
        }

        public BurstLogBuilder Append(float value)
        {
            _buffer.Append(value);
            return this;
        }

        [Conditional("SNC_DEBUG_INFO")]
        [BurstDiscard]
        public void Log() => UnityEngine.Debug.Log($"[SCGpuAnimator] {_buffer}");

        [Conditional("UNITY_EDITOR")]
        [Conditional("SNC_DEBUG_WARNINGS")]
        [BurstDiscard]
        public void LogWarning() => UnityEngine.Debug.LogWarning($"[SCGpuAnimator] {_buffer}");

        public void LogError() => UnityEngine.Debug.LogError(_buffer);
    }
}
