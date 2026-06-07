using System;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    public sealed class Demo3SpawnerDebug : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            int count = transform.childCount;
            Gizmos.color = Color.blue;
            for (int i = 0; i < count; i++)
            {
                Gizmos.DrawSphere(transform.GetChild(i).position, 0.5f);
            }
        }
    }
}
