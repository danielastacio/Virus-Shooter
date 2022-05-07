using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class BulletCollisionEventSystem : JobComponentSystem
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    struct CollisionEventImpulseJob: ICollisionEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<BulletData> BulletGroup;
        public ComponentDataFromEntity<VirusData> VirusGroup;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.Entities.EntityA;
            Entity entityB = collisionEvent.Entities.EntityB;

            bool isTargetA = VirusGroup.Exists(entityA);
            bool isTargetB = VirusGroup.Exists(entityB);

            bool isBulletA = BulletGroup.Exists(entityA);
            bool isBulletB = BulletGroup.Exists(entityB);

            if(isBulletA && isTargetB)
            {
                var aliveComponent = VirusGroup[entityB];
                aliveComponent.alive = false;
                VirusGroup[entityB] = aliveComponent;
            }

            if (isBulletB && isTargetA)
            {
                var aliveComponent = VirusGroup[entityA];
                aliveComponent.alive = false;
                VirusGroup[entityA] = aliveComponent;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle jobHandle = new CollisionEventImpulseJob
        {
            BulletGroup = GetComponentDataFromEntity<BulletData>(),
            VirusGroup = GetComponentDataFromEntity<VirusData>()
        }
        .Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);

        jobHandle.Complete();
        return jobHandle;
    }
}
