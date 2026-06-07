using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Entities;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed partial class AnimatorProviderSystem : SystemBase
    {
        private Plane[] _managedPlanes;

        protected override void OnCreate()
        {
            _managedPlanes = new Plane[6];
            var singletonEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(singletonEntity, new AnimatorCameraData());
        }

        protected override void OnUpdate()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            GeometryUtility.CalculateFrustumPlanes(mainCamera, _managedPlanes);
            SystemAPI.SetSingleton(new AnimatorCameraData
            {
                Position = mainCamera.transform.position,
                Forward = mainCamera.transform.forward,
                Planes = new AnimatorFrustumPlanes(_managedPlanes)
            });
        }
    }
}
