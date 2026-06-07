#if UNITY_EDITOR

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public interface ICodeGenerator
    {
        public string GenerateParamsCode(PrefabInstance instance, string[] anims, string namespaceName);
    }
}

#endif
