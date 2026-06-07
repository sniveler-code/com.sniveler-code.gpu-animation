#if UNITY_EDITOR

using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public interface ILodMeshGenerator
    {
        public GameObject BuildLodMeshProcess(PrefabInstance prefab, string folder);
    }
}

#endif
