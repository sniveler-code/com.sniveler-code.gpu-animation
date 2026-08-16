using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    [Serializable]
    public class MonoBlobBones
    {
        public List<string> BonesNames;

        public MonoBlobBones(Transform[] bones)
        {
            BonesNames = new List<string>();
            Array.ForEach(bones, bone => BonesNames.Add(bone.name));
        }
    }
}
