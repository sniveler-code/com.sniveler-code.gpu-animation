#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using SnivelerCode.GpuAnimation.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public sealed class LodMeshGenerator : ILodMeshGenerator
    {
        public GameObject BuildLodMeshProcess(PrefabInstance instance, string folder)
        {
            var rootObject = new GameObject(instance.Source.name);
            var lodGroupComponent = rootObject.AddComponent<LODGroup>();
            var lodsGroups = new List<LOD>();

            for (int i = 0; i < instance.Lods.Count; ++i)
            {
                LodInstance lodInstance = instance.Lods[i];
                var combineInstances = new List<CombineInstance>();
                var allMaterials = new List<Material>();
                Vector3 localScale = instance.Source.transform.lossyScale;
                foreach (var skin in lodInstance.Skins)
                {
                    if (skin == null || skin.sharedMesh == null) continue;
                    Mesh originalMesh = skin.sharedMesh;
                    PrepareMesh(originalMesh);

                    Mesh tempMesh = Object.Instantiate(originalMesh);
                    Vector3[] originalVerts = tempMesh.vertices;
                    var scaleVectors = new Vector3[originalVerts.Length];
                    for (int k = 0; k < scaleVectors.Length; ++k)
                    {
                        scaleVectors[k] = Vector3.Scale(originalVerts[k], localScale);
                    }

                    tempMesh.vertices = scaleVectors;
                    tempMesh.RecalculateNormals();

                    var boneIndexes = new List<Vector4>(tempMesh.vertexCount);
                    var boneWeights = new List<Vector4>(tempMesh.vertexCount);
                    int[] boneMap = new int[skin.bones.Length];
                    for (int b = 0; b < skin.bones.Length; b++)
                    {
                        string boneName = skin.bones[b].name;
                        int masterIndex = -1;

                        for (int m = 0; m < instance.MasterBones.Length; m++)
                        {
                            if (instance.MasterBones[m].name != boneName)
                            {
                                continue;
                            }

                            masterIndex = m;
                            break;
                        }

                        if (masterIndex == -1)
                        {
                            Debug.LogWarning($"[SCGpuAnimation] Bone {skin.bones[b].name} not found in MasterBones!");
                            masterIndex = 0;
                        }

                        boneMap[b] = masterIndex;
                    }

                    foreach (BoneWeight bw in tempMesh.boneWeights)
                    {
                        boneIndexes.Add(new Vector4(
                            boneMap[bw.boneIndex0],
                            boneMap[bw.boneIndex1],
                            boneMap[bw.boneIndex2],
                            boneMap[bw.boneIndex3]
                        ));

                        boneWeights.Add(new Vector4(bw.weight0, bw.weight1, bw.weight2, bw.weight3));
                    }

                    tempMesh.SetUVs(2, boneIndexes);
                    tempMesh.SetUVs(3, boneWeights);

                    for (int subMeshIndex = 0; subMeshIndex < tempMesh.subMeshCount; subMeshIndex++)
                    {
                        var ci = new CombineInstance
                        {
                            mesh = tempMesh,
                            subMeshIndex = subMeshIndex,
                            transform = instance.Source.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix
                        };

                        combineInstances.Add(ci);
                        allMaterials.Add(subMeshIndex < skin.sharedMaterials.Length
                            ? skin.sharedMaterials[subMeshIndex]
                            : new Material(instance.Shader));
                    }
                }

                Mesh finalMergedMesh = new Mesh {indexFormat = IndexFormat.UInt32};
                finalMergedMesh.CombineMeshes(combineInstances.ToArray(), false, true);
                AssetDatabase.CreateAsset(finalMergedMesh, Path.Combine(folder, $"lod{i}.asset"));

                var lodObject = new GameObject($"_lod{i}");
                lodObject.transform.SetParent(rootObject.transform);

                var meshFilter = lodObject.AddComponent<MeshFilter>();
                meshFilter.mesh = finalMergedMesh;
                var meshRenderer = lodObject.AddComponent<MeshRenderer>();
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;

                var materialSet = new Dictionary<GUID, Material>();
                var sharedMaterials = new Material[finalMergedMesh.subMeshCount];

                for (int k = 0; k < finalMergedMesh.subMeshCount; ++k)
                {
                    var sourceMaterial = allMaterials[k];
                    var materialGuid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(sourceMaterial));

                    if (!materialSet.ContainsKey(materialGuid))
                    {
                        var material = new Material(instance.Shader);

                        // color
                        if (sourceMaterial.HasProperty(AnimatorShaderProperty.BaseColorId))
                            material.SetColor(AnimatorShaderProperty.BaseColorId,
                                sourceMaterial.GetColor(AnimatorShaderProperty.BaseColorId));
                        else if (sourceMaterial.HasProperty(AnimatorShaderProperty.ColorId))
                            material.SetColor(AnimatorShaderProperty.ColorId,
                                sourceMaterial.GetColor(AnimatorShaderProperty.ColorId));

                        // texture
                        if (sourceMaterial.HasProperty(AnimatorShaderProperty.BaseMapId))
                            material.SetTexture(AnimatorShaderProperty.BaseMapId,
                                sourceMaterial.GetTexture(AnimatorShaderProperty.BaseMapId));
                        else if (sourceMaterial.HasProperty(AnimatorShaderProperty.MainTexId))
                            material.SetTexture(AnimatorShaderProperty.BaseMapId,
                                sourceMaterial.GetTexture(AnimatorShaderProperty.MainTexId));

                        material.enableInstancing = true;

                        materialSet[materialGuid] = material;
                        AssetDatabase.CreateAsset(material, Path.Combine(folder, $"lod{i}_{k}.mat"));
                        EditorUtility.SetDirty(material);
                    }

                    sharedMaterials[k] = materialSet[materialGuid];
                }

                meshRenderer.sharedMaterials = sharedMaterials;
                meshRenderer.shadowCastingMode = ShadowCastingMode.On;

                lodsGroups.Add(new LOD
                {
                    screenRelativeTransitionHeight = lodInstance.Percent * 0.01f,
                    renderers = new Renderer[] {meshRenderer}
                });
            }

            lodGroupComponent.SetLODs(lodsGroups.ToArray());
            return rootObject;
        }

        private static void PrepareMesh(Mesh mesh)
        {
            if (mesh == null) return;
            string assetPath = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(assetPath)) return;
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null || modelImporter.isReadable) return;

            modelImporter.isReadable = true;
            modelImporter.SaveAndReimport();
            Debug.Log($"[GPU Animation] Read/Write enable: {assetPath}");
        }
    }
}

#endif
