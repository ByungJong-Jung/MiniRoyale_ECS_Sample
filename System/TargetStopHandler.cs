using UnityEngine;
using DG.Tweening;

public static class TargetStopHandler
{
    public static void StopAndLockTarget(EntityManager manager, int entityID, int targetEntityID)
    {
        if (manager.HasComponent<AirUnitTagComponent>(entityID))
            StopAirUnit(manager, entityID, targetEntityID);
        else
            StopGroundUnit(manager, entityID, targetEntityID);
    }

    private static void StopGroundUnit(EntityManager manager, int entityID, int targetEntityID)
    {
        if (!manager.TryGetComponent<NavMeshAgentRefComponent>(entityID, out var agentRefComp)) return;

        var agent = agentRefComp.agent;
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        agentRefComp.agent.isStopped = true;
        agentRefComp.agent.ResetPath();
        agentRefComp.agent.velocity = Vector3.zero;

        agentRefComp.lastRequestedDestination = InGameData.INFINITY_POS;
        manager.RemoveComponent<MoveTargetComponent>(entityID);
        manager.AddComponents(entityID
            , agentRefComp
            , new AttackTargetComponent { targetEntityID = targetEntityID });
    }

    private static void StopAirUnit(EntityManager manager, int entityID, int targetEntityID)
    {
        var gameObjRefComp = manager.GetComponent<GameObjectRefComponent>(entityID);
        if (gameObjRefComp.gameObject == null) return;

        gameObjRefComp.gameObject.transform.DOKill();

        if (!manager.TryGetComponent<PositionComponent>(entityID, out var posComp)) return;

        posComp.value = gameObjRefComp.gameObject.transform.position.SetY(0f);
        manager.AddComponent(entityID, posComp);

        manager.RemoveComponent<MoveTargetComponent>(entityID);
        manager.AddComponents(entityID
            , gameObjRefComp
            , new AttackTargetComponent { targetEntityID = targetEntityID });
    }
}
