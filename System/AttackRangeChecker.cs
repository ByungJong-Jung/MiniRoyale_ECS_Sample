using UnityEngine;

public static class AttackRangeChecker
{
    public static bool IsInAttackRange(EntityManager manager, int entityID, Vector3 myPos, int targetEntityID)
    {
        if (!manager.TryGetComponent<AttackComponent>(entityID, out var attackComp)
            || !manager.TryGetComponent<PositionComponent>(targetEntityID, out var targetPosComp))
            return false;

        if (!manager.TryGetComponent<TargetableTagComponent>(targetEntityID, out var targetable)) return false;
        float targetSize = targetable.targetingSize;

        float attackDistance = attackComp.attackStopDistance;
        float attackRange = attackComp.attackRange;

        float dist = Vector3.Distance(myPos, targetPosComp.value);
        return dist <= attackDistance + attackRange + targetSize;
    }
}
