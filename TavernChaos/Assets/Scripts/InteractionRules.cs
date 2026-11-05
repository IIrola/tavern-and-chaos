using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    // The local demo and LAN authority use the same eligibility, aiming and impulse limits.
    public static class InteractionRules
    {
        public const float Reach=2.05f;
        public static bool CanInteract(TavernActor actor,bool holding,bool charging,bool attacking) => actor&&!actor.Fallen&&!holding&&!charging&&!attacking;
        public static bool CanStrike(TavernActor attacker,TavernActor target,Vector3 direction)
        {
            if(!attacker||!target||attacker==target||attacker.Fallen||target.Fallen) return false;
            Vector3 distance=target.transform.position-attacker.transform.position; distance.y=0;
            return distance.magnitude<=Reach&&Vector3.Dot(direction.normalized,(target.Focus-attacker.EyePosition).normalized)>=.45f
                &&!Physics.Linecast(attacker.EyePosition,target.Focus,1<<0,QueryTriggerInteraction.Ignore);
        }
        public static ThrowableBottle SelectBottle(TavernActor actor,Vector3 direction,IReadOnlyList<ThrowableBottle> bottles)
        {
            ThrowableBottle best=null; float bestAim=-1,bestDistance=float.MaxValue;
            // List order supplies a stable final tie-break for equal aim and distance.
            foreach(var bottle in bottles) if(bottle.gameObject.activeInHierarchy&&!bottle.Held&&!bottle.Flying) {
                Vector3 delta=bottle.transform.position-actor.EyePosition; float distance=delta.magnitude;
                float aim=Vector3.Dot(direction.normalized,delta.normalized);
                if(distance>Reach||aim<.25f||Physics.Linecast(actor.EyePosition,bottle.transform.position,1<<0,QueryTriggerInteraction.Ignore)) continue;
                if(aim>bestAim+.0001f||(Mathf.Abs(aim-bestAim)<.0001f&&distance<bestDistance-.0001f)) { best=bottle; bestAim=aim; bestDistance=distance; }
            }
            return best;
        }
        public static Vector3 LaunchVelocity(Vector3 direction,float power) => direction.normalized*Mathf.Lerp(7,15,Mathf.Clamp01(power));
    }
}
