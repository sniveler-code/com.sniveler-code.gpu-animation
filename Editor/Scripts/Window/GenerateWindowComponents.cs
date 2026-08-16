#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using SnivelerCode.GpuAnimation.Runtime.Components;
using UnityEditor.Animations;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public sealed class LodInstance
    {
        public int Percent;
        public SkinnedMeshRenderer[] Skins;
    }

    public sealed class ClipInstance
    {
        public bool Enable;
        private int _fps = 60;
        public int Fps
        {
            get => _fps;
            set
            {
                if (value > 255)
                    Debug.LogWarning($"[GPU Animation] FPS clamped from {value} to 255 (byte limit)");
                _fps = Mathf.Clamp(value, 1, 255);
            }
        }
        public float Speed;
        public string StateName;
    }

    public sealed class PrefabInstance
    {
        public string Name;
        public GameObject Source;
        public Shader Shader;
        public AnimatorController Animator { get; private set; }

        public Transform[] MasterBones;
        public Matrix4x4[] MasterBindposes;

        private readonly Dictionary<string, int> _bonesMap = new();
        public readonly List<LodInstance> Lods = new();
        public readonly List<ClipInstance> Clips = new();
        public readonly List<MonoBlobAnimatorParameter> Parameters = new();

        public void SetAnimator(AnimatorController animator)
        {
            Animator = animator;
            Parameters.Clear();
            foreach (var parameter in Animator.parameters)
            {
                Parameters.Add(new MonoBlobAnimatorParameter
                {
                    Name = parameter.name,
                    IsTrigger = parameter.type == AnimatorControllerParameterType.Trigger,
                    Value = parameter.type switch
                    {
                        AnimatorControllerParameterType.Bool => parameter.defaultBool ? 1.0f : 0.0f,
                        AnimatorControllerParameterType.Float => parameter.defaultFloat,
                        AnimatorControllerParameterType.Int => parameter.defaultInt,
                        AnimatorControllerParameterType.Trigger => parameter.defaultBool ? 1.0f : 0.0f,
                        _ => 0.0f
                    }
                });
            }
        }

        private static string FixedName(string value) => value.Replace("/", "_");

        public void Clear()
        {
            _bonesMap.Clear();
            Lods.Clear();
            Clips.Clear();
            MasterBones = null;
            MasterBindposes = null;
        }

        public void SetSkins(GameObject root)
        {
            Lods.Clear();

            var lodGroup = root.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                var unityLods = lodGroup.GetLODs();
                for (int i = 0; i < unityLods.Length; i++)
                {
                    var lodRenderers = unityLods[i].renderers.OfType<SkinnedMeshRenderer>().ToArray();
                    if (lodRenderers.Length > 0)
                    {
                        Lods.Add(new LodInstance
                        {
                            Percent = (int) (unityLods[i].screenRelativeTransitionHeight * 100f),
                            Skins = lodRenderers
                        });
                    }
                }
            }
            else
            {
                var lodRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (lodRenderers.Length > 0)
                {
                    Lods.Add(new LodInstance
                    {
                        Percent = 60,
                        Skins = lodRenderers
                    });
                }
            }

            var bonesList = new List<Transform>();
            var bindposesList = new List<Matrix4x4>();
            Matrix4x4 rootLocalToWorld = root.transform.localToWorldMatrix;

            foreach (var skin in Lods[0].Skins)
            {
                foreach (Transform bone in skin.bones)
                {
                    string fixedBoneName = FixedName(bone.name);
                    if (_bonesMap.ContainsKey(fixedBoneName))
                    {
                        continue;
                    }

                    bonesList.Add(bone);
                    Matrix4x4 bindpose = bone.worldToLocalMatrix * rootLocalToWorld;
                    bindposesList.Add(bindpose);
                    _bonesMap[fixedBoneName] = bonesList.Count - 1;
                }
            }

            MasterBones = bonesList.ToArray();
            MasterBindposes = bindposesList.ToArray();
        }

        public int BonesIndex(string name)
        {
            string fixedBoneName = FixedName(name);
            return _bonesMap.GetValueOrDefault(fixedBoneName, 0);
        }

        public LodInstance AddLod()
        {
            Lods.Add(new LodInstance {Percent = (int) (Lods[^1].Percent * 0.5f)});
            return Lods[^1];
        }
    }
}

#endif