using UnityEngine;

public static class TargetFinder
{
    public static int FindClosestForUnit(EntityManager manager, int seekerEntityID, Vector3 seekerPos, float targetDetectionRange)
    {
        int closestEntityID = FindClosestTargetable(manager, seekerEntityID, seekerPos, targetDetectionRange);

        // 감지 범위 내 타겟 없으면 적군 빌딩 전체 중 최단거리
        if (!EntityManager.IsValid(closestEntityID))
            closestEntityID = FindClosestEnemyBuilding(manager, seekerEntityID, seekerPos);

        return closestEntityID;
    }

    public static int FindClosestForBuilding(EntityManager manager, int seekerEntityID, Vector3 seekerPos, float targetDetectionRange)
    {
        return FindClosestTargetable(manager, seekerEntityID, seekerPos, targetDetectionRange);
    }

    private static int FindClosestTargetable(EntityManager manager, int seekerEntityID, Vector3 seekerPos, float targetDetectionRange)
    {
        if (manager.HasComponent<AttackOnlyBuildingComponent>(seekerEntityID))
            return default;

        float minDist = float.MaxValue;
        int closestEntityID = default;
        var myTeam = GetTeam(manager, seekerEntityID);

        foreach (var (targetEntityID, _) in manager.GetAllOfType<TargetableTagComponent>())
        {
            if (targetEntityID == seekerEntityID) continue;
            if (manager.HasComponent<DeathFlagComponent>(targetEntityID)) continue;
            if (!manager.TryGetComponent<PositionComponent>(targetEntityID, out var targetPosComp)) continue;

            var targetTeam = GetTeam(manager, targetEntityID);
            if (myTeam.Equals(targetTeam)) continue;

            if (manager.HasComponent<AirUnitTagComponent>(targetEntityID))
            {
                if (manager.HasComponent<AttackRangedComponent>(seekerEntityID) == false)
                    continue;
            }

            float dist = Vector3.Distance(seekerPos, targetPosComp.value);
            if (dist < minDist && dist <= targetDetectionRange)
            {
                minDist = dist;
                closestEntityID = targetEntityID;
            }
        }

        return closestEntityID;
    }

    private static int FindClosestEnemyBuilding(EntityManager manager, int seekerEntityID, Vector3 seekerPos)
    {
        float minDist = float.MaxValue;
        int closestEntityID = default;
        var myTeam = GetTeam(manager, seekerEntityID);

        foreach (var (buildingEntityID, _) in manager.GetAllOfType<BuildingTagComponent>())
        {
            if (!manager.TryGetComponent<PositionComponent>(buildingEntityID, out var buildingPosComp)) continue;

            if (manager.HasComponent<DeathFlagComponent>(buildingEntityID)) continue;

            var buildingTeam = GetTeam(manager, buildingEntityID);
            if (myTeam.Equals(buildingTeam)) continue;

            float dist = Vector3.Distance(seekerPos, buildingPosComp.value);
            if (dist < minDist)
            {
                minDist = dist;
                closestEntityID = buildingEntityID;
            }
        }

        return closestEntityID;
    }

    private static ETeamType GetTeam(EntityManager manager, int entityID)
    {
        return manager.HasComponent<TeamComponent>(entityID)
            ? manager.GetComponent<TeamComponent>(entityID).teamType
            : ETeamType.Ally;
    }
}
