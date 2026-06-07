#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SnivelerCode.GpuAnimation.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public sealed class AssetFileSystem : IAssetFileSystem
    {
        private const string _rootNamespace = "SnivelerCode.GpuAnimation.Generated";

        public string GetGeneratedFolder(string instanceName)
        {
            var folder = new List<string> { "Assets" };
            folder.AddRange(_rootNamespace.Split('.'));
            string generatedFolder = Path.Combine(folder.ToArray());
            ForceDirectory(generatedFolder);
            return Path.Combine(generatedFolder, instanceName);
        }

        public void ForceDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public async Task CreateAsmDef(string path, string namespaceName)
        {
            if (File.Exists(path)) return;

            var data = new AsmDefData
            {
                name = namespaceName,
                rootNamespace = namespaceName,
                references = new[]
                {
                    "SnivelerCode.GpuAnimation.Runtime",
                    "Unity.Collections"
                },
                autoReferenced = true,
                noEngineReferences = true
            };

            string json = JsonUtility.ToJson(data, true);
            await File.WriteAllTextAsync(path, json);
            AssetDatabase.ImportAsset(path);
        }

        public async Task WriteFile(string path, string content)
        {
            await File.WriteAllTextAsync(path, content);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        public void SaveAsset(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
        }

        [System.Serializable]
        private class AsmDefData
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public bool autoReferenced;
            public bool noEngineReferences;
        }
    }
}

#endif
