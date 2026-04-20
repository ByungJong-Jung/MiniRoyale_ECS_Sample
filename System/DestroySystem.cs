using UnityEngine;
using System.Collections.Generic;
public class DestroySystem : ISystem
{
    public void Tick(EntityManager manager, float deltaTime)
    {
        var removeList = new List<int>();

        foreach (var (entityID, destoyComp) in manager.GetAllOfType<DestroyComponent>())
        {
            if (manager.TryGetComponent<EntityEffectorRefComponent>(entityID, out var effectorRefComp))
            {
                effectorRefComp.entityEffector.Clear();
                manager.AddComponent(entityID, effectorRefComp);
            }

            if (manager.TryGetComponent<GameObjectRefComponent>(entityID, out var goRef))
            {
                ObjectPoolManager.Instance.PoolDic[goRef.resourcePath].Enqueue(goRef.gameObject);
            }

            removeList.Add(entityID);
        }

        foreach (var entity in removeList)
            manager.RemoveEntity(entity);
    }
}
