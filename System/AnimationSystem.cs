using UnityEngine;

public class AnimationSystem : ISystem
{
    public void Tick(EntityManager manager, float deltaTime)
    {
        foreach (var (entityID, entityAnimRefComp) in manager.GetAllOfType<EntityAnimatorRefComponent>())
        {
            if (manager.HasComponent<DestroyComponent>(entityID)) continue;

            if (!manager.TryGetComponent<EntityEffectorRefComponent>(entityID, out var entityEffectRefComp)) continue;

            if(manager.HasComponent<UnitTagComponent>(entityID))
            {
                ProcessUnitAnimation(manager, entityID, entityAnimRefComp, entityEffectRefComp);
                continue;
            }

            if(manager.HasComponent<BuildingTagComponent>(entityID))
            {
                ProcessBuildingAnimation(manager, entityID, entityAnimRefComp, entityEffectRefComp);
                continue;
            }
        }
    }


    private void ProcessUnitAnimation(EntityManager inManager,int inEntityID, EntityAnimatorRefComponent inEntityAnimRefComp, EntityEffectorRefComponent inEntityEffectRefComp)
    {
        if (inEntityAnimRefComp.entityAnimator == null)
            return;

        if (!inManager.HasComponent<DeathFlagComponent>(inEntityID) && inManager.TryGetComponent<AttackAnimationTriggerComponent>(inEntityID, out var attackAnimTriggerComp))
        {
            string attackAnimationName = "Attack";
            if (inEntityAnimRefComp.currentAnimName.IsNullOrEmpty() || inEntityAnimRefComp.currentAnimName.Equals(attackAnimationName) == false)
            {
                inEntityAnimRefComp.currentAnimName = attackAnimationName;
                inEntityAnimRefComp.entityAnimator.PlayAnimator(attackAnimationName,
                    inEffectCallback: () => {
                        attackAnimTriggerComp.triggerEvent?.Invoke(inManager, inEntityID, inEntityEffectRefComp);
                    },
                    inCompleteCallback: () => {
                        inManager.RemoveComponent<AttackingFlagComponent>(inEntityID);

                        var latestAnimRefComp = inManager.GetComponent<EntityAnimatorRefComponent>(inEntityID);
                        latestAnimRefComp.currentAnimName = null;
                        inManager.AddComponent(inEntityID, latestAnimRefComp);
                    }
                );

                inManager.AddComponent(inEntityID, inEntityAnimRefComp);
            }

            inManager.RemoveComponent<AttackAnimationTriggerComponent>(inEntityID);
            return;
        }

        if (inManager.HasComponent<GetHitTriggerComponent>(inEntityID))
        {
            inEntityEffectRefComp.entityEffector.PlayHitEffect();
            inManager.AddComponent(inEntityID, inEntityAnimRefComp);
            inManager.RemoveComponent<GetHitTriggerComponent>(inEntityID);
            return;
        }

        if (inManager.TryGetComponent<DeathAnimationTriggerComponent>(inEntityID, out var deathAnimTriggerComp))
        {

            string deathAnimationName = "Death";
            if (inEntityAnimRefComp.currentAnimName.IsNullOrEmpty() || inEntityAnimRefComp.currentAnimName.Equals(deathAnimationName) == false)
            {
                inEntityAnimRefComp.currentAnimName = deathAnimationName;
                inEntityAnimRefComp.entityAnimator.PlayAnimator(deathAnimationName,
                    inCompleteCallback: () => {
                        deathAnimTriggerComp.triggerEvent?.Invoke(inManager, inEntityID, inEntityEffectRefComp);
                        
                        var latestAnimRefComp = inManager.GetComponent<EntityAnimatorRefComponent>(inEntityID);
                        latestAnimRefComp.currentAnimName = null;
                        inManager.AddComponent(inEntityID, latestAnimRefComp);
                    }
                );

                inManager.AddComponent(inEntityID, inEntityAnimRefComp);
            }
            inManager.RemoveComponent<DeathAnimationTriggerComponent>(inEntityID);
            return;
        }


        bool isMoving = inManager.HasComponent<MoveTargetComponent>(inEntityID);
        string animName = isMoving ? "Walk" : "Idle";

        if (!string.IsNullOrEmpty(animName) && CanPlayLoopAnimation(inEntityAnimRefComp.currentAnimName) && inEntityAnimRefComp.currentAnimName != animName)
        {
            inEntityAnimRefComp.currentAnimName = animName;
            inEntityAnimRefComp.entityAnimator.PlayAnimator(animName);
            inManager.AddComponent(inEntityID, inEntityAnimRefComp);
        }
    }

    private void ProcessBuildingAnimation(EntityManager inManager, int inEntityID, EntityAnimatorRefComponent inEntityAnimRefComp, EntityEffectorRefComponent inEntityEffectRefComp)
    {
        if (inEntityAnimRefComp.entityAnimator == null)
            return;

        if (!inManager.HasComponent<DeathFlagComponent>(inEntityID) && inManager.TryGetComponent<AttackAnimationTriggerComponent>(inEntityID, out var attackAnimTriggerComp))
        {

            string attackAnimationName = "Attack";
            if (inEntityAnimRefComp.currentAnimName.IsNullOrEmpty() || inEntityAnimRefComp.currentAnimName.Equals(attackAnimationName) == false)
            {
                inEntityAnimRefComp.currentAnimName = attackAnimationName;
                inEntityAnimRefComp.entityAnimator.PlayAnimator(attackAnimationName,
                    inEffectCallback: () => {
                        attackAnimTriggerComp.triggerEvent?.Invoke(inManager, inEntityID, inEntityEffectRefComp);
                    },
                    inCompleteCallback: () => {
                        inManager.RemoveComponent<AttackingFlagComponent>(inEntityID);

                        var latestAnimRefComp = inManager.GetComponent<EntityAnimatorRefComponent>(inEntityID);
                        latestAnimRefComp.currentAnimName = null;
                        inManager.AddComponent(inEntityID, latestAnimRefComp);
                    }
                );

                inManager.AddComponent(inEntityID, inEntityAnimRefComp);
            }

            inManager.RemoveComponent<AttackAnimationTriggerComponent>(inEntityID);
            return;
        }


        if (inManager.HasComponent<GetHitTriggerComponent>(inEntityID))
        {
            inEntityEffectRefComp.entityEffector.PlayHitEffect();
            inManager.AddComponent(inEntityID, inEntityAnimRefComp);
            inManager.RemoveComponent<GetHitTriggerComponent>(inEntityID);

            return;
        }

        if (inManager.TryGetComponent<DeathAnimationTriggerComponent>(inEntityID, out var deathAnimTriggerComp))
        {

            var obj = inManager.GetComponent<GameObjectRefComponent>(inEntityID).gameObject;
            obj?.SetActive(false);

            inManager.AddComponent(inEntityID
                , new EffectDataComponent 
                { 
                    effectNameKey = "Explosion", 
                    position = inEntityEffectRefComp.entityEffector.GetTransformPos(),
                    completeCallback = 
                    ()=>
                    {
                        inManager.RemoveComponent<EffectDataComponent>(inEntityID);
                    }
                });

            inManager.RemoveComponent<DeathAnimationTriggerComponent>(inEntityID);
            return;
        }


        if(inEntityAnimRefComp.entityAnimator != null)
        {
            string animName = "Idle";
            if (!string.IsNullOrEmpty(animName) && CanPlayLoopAnimation(inEntityAnimRefComp.currentAnimName) && inEntityAnimRefComp.currentAnimName != animName)
            {
                inEntityEffectRefComp.entityEffector.Clear();
                inEntityAnimRefComp.currentAnimName = animName;
                inEntityAnimRefComp.entityAnimator.PlayAnimator(animName);
                inManager.AddComponent(inEntityID, inEntityAnimRefComp);
            }
        }
    }


    private bool CanPlayLoopAnimation(string currentAnim)
    {
        return string.IsNullOrEmpty(currentAnim)
            || (currentAnim != "Attack" && currentAnim != "Death");
    }
}
