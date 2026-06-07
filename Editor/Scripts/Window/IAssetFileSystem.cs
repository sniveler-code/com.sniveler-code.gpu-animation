#if UNITY_EDITOR

using System.Threading.Tasks;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public interface IAssetFileSystem
    {
        public string GetGeneratedFolder(string instanceName);
        public void ForceDirectory(string path);
        public Task CreateAsmDef(string path, string namespaceName);
        public Task WriteFile(string path, string content);
        public void SaveAsset(Object asset, string path);
    }
}

#endif
