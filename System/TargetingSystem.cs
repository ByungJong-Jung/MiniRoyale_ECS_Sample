public class TargetingSystem : ISystem
{
    public void Tick(EntityManager manager, float deltaTime)
    {
        TickUnits(manager);
        TickBuildings(manager);
    }

    private void TickUnits(EntityManager manager)
    {
        foreach (var (entityID, _) in manager.GetAllOfType<UnitTagComponent>())
        {
            if (manager.HasComponent<ReadyToSpawnComponent>(entityID))
                continue;

            if (manager.HasComponent<DeathFlagComponent>(entityID) || manager.HasComponent<DestroyComponent>(entityID))
                continue;

            if (!manager.TryGetComponent<PositionComponent>(entityID, out var myPosComp)
                || !manager.TryGetComponent<AttackComponent>(entityID, out var attackComp))
                continue;

            if (manager.TryGetComponent<AttackTargetComponent>(entityID, out var attackTargetComp))
            {
                if (AttackRangeChecker.IsInAttackRange(manager, entityID, myPosComp.value, attackTargetComp.targetEntityID)
                    && !manager.HasComponent<DeathFlagComponent>(attackTargetComp.targetEntityID))
                    continue;
            }

            int targetEntityID = TargetFinder.FindClosestForUnit(manager, entityID, myPosComp.value, attackComp.targetDetectionRange);

            UpdateMoveTarget(manager, entityID, targetEntityID);

            if (EntityManager.IsValid(targetEntityID) && AttackRangeChecker.IsInAttackRange(manager, entityID, myPosComp.value, targetEntityID))
            {
                TargetStopHandler.StopAndLockTarget(manager, entityID, targetEntityID);
            }
            else
            {
                manager.RemoveComponent<AttackTargetComponent>(entityID);

                if (EntityManager.IsValid(targetEntityID) == false)
                    manager.RemoveComponent<MoveTargetComponent>(entityID);
            }
        }
    }

    private void TickBuildings(EntityManager manager)
    {
        foreach (var (entityID, _) in manager.GetAllOfType<BuildingTagComponent>())
        {
            if (!manager.TryGetComponent<PositionComponent>(entityID, out var myPosComp)
                || !manager.TryGetComponent<AttackComponent>(entityID, out var attackComp))
                continue;

            if (manager.TryGetComponent<AttackTargetComponent>(entityID, out var currentTargetComp))
            {
                var currentTargetID = currentTargetComp.targetEntityID;
                if (AttackRangeChecker.IsInAttackRange(manager, entityID, myPosComp.value, currentTargetID)
                    && !manager.HasComponent<DeathFlagComponent>(currentTargetID))
                    continue;
            }

            int targetEntityID = TargetFinder.FindClosestForBuilding(manager, entityID, myPosComp.value, attackComp.targetDetectionRange);

            if (EntityManager.IsValid(targetEntityID) && AttackRangeChecker.IsInAttackRange(manager, entityID, myPosComp.value, targetEntityID))
            {
                manager.AddComponent(entityID, new AttackTargetComponent { targetEntityID = targetEntityID });
            }
            else
            {
                manager.RemoveComponent<AttackTargetComponent>(entityID);
            }
        }
    }

    private void UpdateMoveTarget(EntityManager manager, int entityID, int targetEntityID)
    {
        if (manager.TryGetComponent<MoveTargetComponent>(entityID, out var existingComp))
        {
            existingComp.targetEntityID = targetEntityID;
            manager.AddComponent(entityID, existingComp);
        }
        else
        {
            manager.AddComponent(entityID, new MoveTargetComponent { targetEntityID = targetEntityID });
        }
    }
}
