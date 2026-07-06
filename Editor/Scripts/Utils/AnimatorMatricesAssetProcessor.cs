using System;
using SnivelerCode.GpuAnimation.Runtime.Components;
using UnityEditor;

namespace SnivelerCode.GpuAnimation.Editor.Utils
{
    [InitializeOnLoad]
    public class AnimatorMatricesAssetProcessor: AssetPostprocessor
    {
        static AnimatorMatricesAssetProcessor()
        {
            EditorApplication.delayCall += ScanAndAssignIds;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths) => ScanAndAssignIds();

        private static void ScanAndAssignIds()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimatorMatricesAsset");
            bool anyChanged = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimatorMatricesAsset so = AssetDatabase.LoadAssetAtPath<AnimatorMatricesAsset>(path);
                if (so == null || so.UniqueId != 0) continue;

                so.UniqueId = GenerateId();
                EditorUtility.SetDirty(so);
                anyChanged = true;
            }

            if (anyChanged) AssetDatabase.SaveAssets();
        }

        private static ulong GenerateId()
        {
            byte[] guidBytes = Guid.NewGuid().ToByteArray();
            return BitConverter.ToUInt64(guidBytes, 0);
        }
    }
}
