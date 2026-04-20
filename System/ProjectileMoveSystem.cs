using DG.Tweening;
using UnityEngine;
using System.Collections.Generic;

public class ProjectileMoveSystem : ISystem
{
    public void Tick(EntityManager manager, float deltaTime)
    {
        var removeEntityList = new List<int>();
        var removeList = new List<int>();
        foreach (var (projectileEntityID, projectileMoveComp) in manager.GetAllOfType<ProjectileMoveComponent>())
        {
            if (!manager.TryGetComponent<GameObjectRefComponent>(projectileEntityID, out var projectileObjRefComp) ||
                !manager.TryGetComponent<GameObjectRefComponent>(projectileMoveComp.targetEntityID, out var targetObjRefComp) ||
                !manager.TryGetComponent<TargetableTagComponent>(projectileMoveComp.targetEntityID, out var targetTargetableComp))
                continue;

            if (manager.HasComponent<DeathFlagComponent>(projectileMoveComp.targetEntityID))
            {
                PoolAndRemove(manager, projectileObjRefComp, projectileEntityID, projectileMoveComp.attackEntityID);
                removeEntityList.Add(projectileEntityID);
                continue;
            }

            GameObject projectileObject = projectileObjRefComp.gameObject;
            Vector3 moveTargetPos = CalculateProjectileTargetPos(
                projectileObject.transform.position,
                targetObjRefComp.gameObject.transform.position,
                targetTargetableComp.targetingSize
            );

            float moveTime = projectileMoveComp.moveLength;

            projectileObject.transform
                .DOMove(moveTargetPos, moveTime)
                .SetEase(Ease.Linear)
                .OnUpdate(() => SetProjectileDirection(projectileObject.transform, moveTargetPos))
                .OnComplete(() =>
                {
                    OnProjectileArrive(manager, projectileEntityID, projectileMoveComp, projectileObjRefComp);
                });

            removeList.Add(projectileEntityID);
        }

        foreach (var entity in removeList)
            manager.RemoveComponent<ProjectileMoveComponent>(entity);

        foreach (var entity in removeEntityList)
            manager.RemoveEntity(entity);
    }

    private Vector3 CalculateProjectileTargetPos(Vector3 inFromPos, Vector3 inTargetCenter, float inTargetRadius)
    {
        Vector3 dir = (inTargetCenter - inFromPos).normalized;
        return inTargetCenter - dir * inTargetRadius;
    }

    private void SetProjectileDirection(Transform inTransform, Vector3 inTargetPos)
    {
        Vector3 dir = (inTargetPos - inTransform.position).normalized;
        inTransform.up = dir;
    }

    private void OnProjectileArrive(EntityManager inManager, int inProjectileEntityId, ProjectileMoveComponent inProjectileMoveComp, GameObjectRefComponent inProjectileObjRefComp)
    {
        if (inManager.TryGetComponent<EntityEffectorRefComponent>(inProjectileEntityId, out var projectileEffectRefComp))
        {

            inManager.AddComponents(inProjectileMoveComp.targetEntityID,
                new EffectDataComponent
                {
                    effectNameKey = "Hit",
                    position = projectileEffectRefComp.entityEffector.GetEffectTransformPos()
                },
                new GetHitTriggerComponent(),
                new TakeDamageComponent { amount = inProjectileMoveComp.damage }
            );
        }

        PoolAndRemove(inManager, inProjectileObjRefComp, inProjectileEntityId, inProjectileMoveComp.attackEntityID);
        inManager.RemoveEntity(inProjectileEntityId);
    }

    private void PoolAndRemove(EntityManager inManager, GameObjectRefComponent inObjRefComp, int inEntityId, int inAttackEntityId)
    {
        inObjRefComp.gameObject.transform.DOKill();

        ObjectPoolManager.Instance.PoolDic[inObjRefComp.resourcePath].Enqueue(
            inObjRefComp.gameObject,
            obj => obj.transform.position = InGameData.INFINITY_POS
        );
        inManager.RemoveComponent<ProjectilePendingComponent>(inAttackEntityId);
    }

}
