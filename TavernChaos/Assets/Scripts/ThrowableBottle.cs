using UnityEngine;

namespace TavernChaos
{
    [DefaultExecutionOrder(50)]
    public sealed class ThrowableBottle : MonoBehaviour
    {
        public bool Held { get; private set; }
        public bool Flying { get; private set; }
        public Rigidbody Body { get; private set; }
        public TavernActor Owner { get; private set; }
        private Vector3 rack, previous;
        private Collider hitbox;
        private float resetAt;
        private bool used,replica;
        private Vector3 replicaPosition;
        private Quaternion replicaRotation;
        private TavernActor thrower;
        private TavernActor replicaOwner;
        public void SetReplicaOwner(TavernActor actor) { replicaOwner=actor; }
        public void SetReplica(bool value)
        {
            replica=value; replicaOwner=null; replicaPosition=transform.position; replicaRotation=transform.rotation;
            Body.interpolation=value?RigidbodyInterpolation.None:RigidbodyInterpolation.Interpolate;
            if(value) { Body.isKinematic=true; hitbox.enabled=false; transform.SetParent(null,true); Owner=null; }
            else ResetToRack();
        }
        public void ApplyReplica(LanBottleState state)
        {
            if(!replica) SetReplica(true);
            replicaPosition=state.position; replicaRotation=state.rotation.normalized; Held=state.held; Flying=state.flying;
            if(Vector3.Distance(transform.position,replicaPosition)>5) transform.position=replicaPosition;
        }
        private void LateUpdate()
        {
            if(!replica) return; float t=1-Mathf.Exp(-25*Time.unscaledDeltaTime);
            Vector3 p=replicaOwner&&Held?replicaOwner.Hand.position:Vector3.Lerp(transform.position,replicaPosition,t);
            Quaternion r=(replicaOwner&&Held?replicaOwner.Hand.rotation:Quaternion.Slerp(transform.rotation,replicaRotation,t)).normalized;
            Body.position=p; Body.rotation=r; transform.SetPositionAndRotation(p,r);
        }
        public static ThrowableBottle Create(Vector3 p)
        {
            var go=new GameObject("Throwable bottle"); go.layer=9; go.transform.position=p;
            var b=go.AddComponent<ThrowableBottle>(); b.rack=p;
            TavernWorld.BottleVisual(Vector3.zero,go.transform,TavernWorld.Blue);
            b.hitbox=go.AddComponent<CapsuleCollider>(); ((CapsuleCollider)b.hitbox).radius=.12f; ((CapsuleCollider)b.hitbox).height=.65f; ((CapsuleCollider)b.hitbox).center=Vector3.up*.1f;
            b.Body=go.AddComponent<Rigidbody>(); b.Body.mass=.6f; b.Body.isKinematic=true; b.Body.interpolation=RigidbodyInterpolation.Interpolate; b.Body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            return b;
        }
        public bool Grab(TavernActor actor)
        {
            if(replica||!actor||actor.Fallen||actor.IsReplica||Held||Flying) return false;
            Owner=actor;
            Held=true; Body.interpolation=RigidbodyInterpolation.None; Body.isKinematic=true; hitbox.enabled=false; transform.SetParent(actor.Hand,false); transform.localPosition=Vector3.zero; transform.localRotation=Quaternion.identity;
            return true;
        }
        public void Release(Vector3 velocity)
        {
            if(!Held) return;
            thrower=Owner; Owner=null;
            transform.SetParent(null,true); hitbox.enabled=true; Body.interpolation=RigidbodyInterpolation.Interpolate; Body.isKinematic=false; Body.linearVelocity=velocity; Body.angularVelocity=new(0,4,5);
            Held=false; Flying=true; used=false; resetAt=Time.time+6; previous=Body.position;
        }
        public void ResetToRack()
        {
            transform.SetParent(null,true);
            if(!Body.isKinematic) Body.linearVelocity=Body.angularVelocity=Vector3.zero;
            Body.interpolation=RigidbodyInterpolation.None; Body.isKinematic=true; hitbox.enabled=!replica; transform.SetPositionAndRotation(rack,Quaternion.identity); Flying=false; Held=false; used=false; Owner=thrower=null;
        }
        private void FixedUpdate()
        {
            if(replica||!Flying) return;
            var g=DemoGame.Instance;
            // Swept actor check supplements CCD, including CharacterControllers.
            Vector3 a=previous+Vector3.up*.1f, b=Body.position+Vector3.up*.1f;
            if(!used && Body.linearVelocity.magnitude>5) foreach(var target in g.CombatTargets()) if(target!=thrower&&!target.Fallen) {
                Vector3 ab=b-a; float t=ab.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(target.Focus-a,ab)/ab.sqrMagnitude);
                if(Vector3.Distance(a+ab*t,target.Focus)<.62f&&!Physics.Linecast(a,a+ab*t,1<<0,QueryTriggerInteraction.Ignore)) { Impact(target); break; }
            }
            previous=Body.position;
            if(Time.time>=resetAt || transform.position.y < -2) ResetToRack();
        }
        private void OnCollisionEnter(Collision c)
        {
            if(replica||!Flying || used) return;
            var actor=c.collider.GetComponentInParent<TavernActor>();
            if(actor && actor!=thrower && c.relativeVelocity.magnitude>5) Impact(actor);
            else if(c.relativeVelocity.magnitude>2) DemoGame.Instance.Audio.Play("clink",.2f);
        }
        private void Impact(TavernActor actor)
        {
            if(used || actor.Fallen) return; used=true;
            actor.Fall(Body.linearVelocity.normalized*4.5f); DemoGame.Instance.BottleHit(actor.Focus); resetAt=Time.time+2;
        }
    }
}
