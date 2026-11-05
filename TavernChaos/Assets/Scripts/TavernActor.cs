using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    public sealed class TavernActor : MonoBehaviour
    {
        public bool IsPlayer { get; private set; }
        public bool Fallen { get; private set; }
        public bool Seated { get; private set; }
        public void SetSeated(bool value)
        {
            Seated=value; momentum=Vector3.zero;
            if(replica||Fallen) return;
            model.localPosition=value?Vector3.down*.28f:Vector3.zero;
            parts[2].transform.localRotation=parts[3].transform.localRotation=Quaternion.Euler(value?-80:0,0,0);
        }
        public float RecoverAt { get; private set; }
        public float EffectUntil { get; private set; }
        public float EffectRemaining => Mathf.Max(0, EffectUntil-Time.time);
        public Vector3 Home { get; private set; }
        public Transform Hand { get; private set; }
        public Vector3 Focus => replica ? transform.TransformPoint(replicaFocusOffset) : Fallen ? parts[0].position : transform.position+Vector3.up*1.1f;
        public Vector3 EyePosition => replica ? transform.TransformPoint(replicaEyeOffset) : Fallen ? parts[1].position+Vector3.up*.08f : transform.position+Vector3.up*1.81f;
        public bool IsReplica => replica;
        public float ReplicaCharge { get; set; }=-1;
        private bool replica,replicaReady;
        private Vector3 replicaEyeOffset,replicaFocusOffset,fromPosition,fromEye,fromFocus;
        private Quaternion fromRotation;
        private LanActorState replicaTarget;
        private readonly List<LanBodyPose> fromBody=new();
        private float blendTime;
        [Min(.1f)] public float SlipFallSpeed=3f;
        private CharacterController controller;
        private Renderer[] headRenderers;
        private bool firstPersonView, slipCollision;
        private Vector3 slipImpulse;
        private Transform model, leftArm, rightArm;
        private readonly List<Rigidbody> parts = new();
        private readonly List<Vector3> poses = new();
        private readonly List<Quaternion> rotations = new();
        private readonly List<Collider> colliders = new();
        private readonly List<CharacterJoint> joints = new();
        private Vector3 momentum;
        private float gait, punchUntil;
        public void SetReplica(bool value)
        {
            if(replica==value) return;
            replica=value; replicaReady=false; ReplicaCharge=-1; ToggleRagdoll(false); Fallen=Seated=false;
            model.localRotation=Quaternion.identity; model.localPosition=Vector3.zero; controller.enabled=!value;
            momentum=Vector3.zero; EffectUntil=0; replicaEyeOffset=Vector3.up*1.81f; replicaFocusOffset=Vector3.up*1.1f; replicaTarget=null;
            for(int i=0;i<parts.Count;i++) { parts[i].transform.localPosition=poses[i]; parts[i].transform.localRotation=rotations[i]; }
        }
        public void ApplyReplica(LanActorState state,bool snap=false)
        {
            if(!replica) SetReplica(true);
            if(state.body==null||state.body.Length!=parts.Count) return;
            snap|=!replicaReady||state.fallen!=Fallen||state.resetRevision!=(replicaTarget?.resetRevision??state.resetRevision)||Vector3.Distance(transform.position,state.position)>3;
            fromPosition=transform.position; fromRotation=transform.rotation; fromEye=replicaEyeOffset; fromFocus=replicaFocusOffset;
            fromBody.Clear(); fromBody.AddRange(CaptureBodyPose());
            Fallen=state.fallen; Seated=state.seated; RecoverAt=Time.time+state.recovery; EffectUntil=Time.time+state.effect;
            replicaTarget=state; blendTime=snap?.05f:0;
            replicaReady=true; ReplicaCharge=state.charge;
            if(snap) RenderReplica(1);
        }
        private void LateUpdate()
        {
            if(!replica||!replicaReady) return;
            blendTime+=Time.unscaledDeltaTime;
            RenderReplica(Mathf.Clamp01(blendTime/.05f));
        }
        private void RenderReplica(float t)
        {
            Quaternion targetRotation=Quaternion.Euler(0,replicaTarget.yaw,0);
            Quaternion renderedRotation=Quaternion.Slerp(fromRotation,targetRotation,t);
            // Only local visual yaw follows the mouse immediately. Position and physics stay authoritative.
            var game=DemoGame.Instance;
            if(game&&this==game.Player&&!Fallen&&!Seated) renderedRotation=Quaternion.Euler(0,game.Rig.Yaw,0);
            transform.SetPositionAndRotation(Vector3.Lerp(fromPosition,replicaTarget.position,t),renderedRotation);
            replicaEyeOffset=Vector3.Lerp(fromEye,Quaternion.Inverse(targetRotation)*(replicaTarget.eye-replicaTarget.position),t);
            replicaFocusOffset=Vector3.Lerp(fromFocus,Quaternion.Inverse(targetRotation)*(replicaTarget.focus-replicaTarget.position),t);
            model.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
            for(int i=0;i<parts.Count;i++) {
                Vector3 position=transform.TransformPoint(Vector3.Lerp(fromBody[i].position,replicaTarget.body[i].position,t));
                Quaternion rotation=(transform.rotation*Quaternion.Slerp(fromBody[i].rotation,replicaTarget.body[i].rotation,t)).normalized;
                // No PhysX interpolation on replicas: it otherwise restores stale world poses after root motion.
                parts[i].position=position; parts[i].rotation=rotation;
                parts[i].transform.SetPositionAndRotation(position,rotation);
            }
        }
        public LanBodyPose[] CaptureBodyPose()
        {
            var result=new LanBodyPose[parts.Count]; Quaternion inverse=Quaternion.Inverse(transform.rotation);
            for(int i=0;i<parts.Count;i++) {
                Vector3 p=Fallen&&!replica?parts[i].position:parts[i].transform.position;
                Quaternion r=Fallen&&!replica?parts[i].rotation:parts[i].transform.rotation;
                result[i]=new LanBodyPose { position=transform.InverseTransformPoint(p),rotation=(inverse*r).normalized };
            }
            return result;
        }
        public void Initialize(bool player, Vector3 home)
        {
            IsPlayer=player; Home=home; transform.position=home;
            controller=gameObject.AddComponent<CharacterController>(); controller.height=1.95f; controller.radius=.36f; controller.center=Vector3.up; controller.stepOffset=.25f; controller.skinWidth=.035f;
            model=new GameObject("Articulated character").transform; model.SetParent(transform,false);
            Material shirt=TavernWorld.Mat(player?"Player petrol":"Rival burnt orange",player?new Color(.06f,.4f,.4f):new Color(.78f,.23f,.075f));
            Material denim=TavernWorld.Mat("Denim",new Color(.08f,.12f,.18f));
            Rigidbody torso=Part("Torso",new(0,1.18f,0),new(.66f,.68f,.42f),shirt,PrimitiveType.Cube,null);
            Part("Head",new(0,1.81f,0),new(.5f,.52f,.46f),TavernWorld.Skin,PrimitiveType.Sphere,torso);
            Part("Left leg",new(-.19f,.48f,0),new(.27f,.43f,.29f),denim,PrimitiveType.Capsule,torso);
            Part("Right leg",new(.19f,.48f,0),new(.27f,.43f,.29f),denim,PrimitiveType.Capsule,torso);
            leftArm=Part("Left arm",new(-.48f,1.13f,0),new(.23f,.35f,.24f),TavernWorld.Skin,PrimitiveType.Capsule,torso).transform;
            rightArm=Part("Right arm",new(.48f,1.13f,0),new(.23f,.35f,.24f),TavernWorld.Skin,PrimitiveType.Capsule,torso).transform;
            // Accessories follow their body, never add a second physics body.
            TavernWorld.Box("Boot",new(0,-.7f,.12f),new(1.1f,.4f,1.6f),TavernWorld.Black,parts[2].transform,false);
            TavernWorld.Box("Boot",new(0,-.7f,.12f),new(1.1f,.4f,1.6f),TavernWorld.Black,parts[3].transform,false);
            TavernWorld.Box("Hair",new(0,.42f,-.05f),new(1.04f,.38f,.95f),TavernWorld.Black,parts[1].transform,false);
            TavernWorld.Box("Left eye",new(-.21f,.05f,-.48f),new(.14f,.13f,.07f),TavernWorld.Black,parts[1].transform,false);
            TavernWorld.Box("Right eye",new(.21f,.05f,-.48f),new(.14f,.13f,.07f),TavernWorld.Black,parts[1].transform,false);
            // Character front is +Z; eyes and hair follow that orientation.
            foreach(Transform t in parts[1].transform) if(t.name.Contains("eye")) t.localPosition=new(t.localPosition.x,t.localPosition.y,.48f);
            headRenderers=parts[1].GetComponentsInChildren<Renderer>();
            TavernWorld.Box("Belt",new(0,-.37f,0),new(1.02f,.12f,1.02f),TavernWorld.Black,torso.transform,false);
            Hand=new GameObject("Carry socket").transform; Hand.SetParent(transform,false); Hand.localPosition=new(.55f,1.25f,.65f);
            foreach(var c in colliders) foreach(var other in colliders) if(c!=other) Physics.IgnoreCollision(c,other);
            ToggleRagdoll(false);
        }
        public void SetFirstPersonView(bool value)
        {
            if(firstPersonView==value) return;
            firstPersonView=value;
            // Only hide the local head and its accessories; retain the body, hands and bottle.
            foreach(var renderer in headRenderers) renderer.forceRenderingOff=value;
        }
        private Rigidbody Part(string name, Vector3 pos, Vector3 size, Material m, PrimitiveType shape, Rigidbody connected)
        {
            var g=TavernWorld.Shape(name,shape,pos,size,m,model); var rb=g.AddComponent<Rigidbody>(); rb.mass=connected?2:7; rb.isKinematic=true;
            rb.interpolation=RigidbodyInterpolation.None; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative; rb.maxAngularVelocity=12;
            if(connected) {
                var j=g.AddComponent<CharacterJoint>(); j.connectedBody=connected; j.enablePreprocessing=false;
                j.swing1Limit=new SoftJointLimit{limit=40}; j.swing2Limit=new SoftJointLimit{limit=30}; j.lowTwistLimit=new SoftJointLimit{limit=-30}; j.highTwistLimit=new SoftJointLimit{limit=30}; joints.Add(j);
            }
            parts.Add(rb); poses.Add(pos); rotations.Add(Quaternion.identity); colliders.Add(g.GetComponent<Collider>()); return rb;
        }
        private void ToggleRagdoll(bool on)
        {
            foreach(var c in colliders) c.enabled=on;
            foreach(var r in parts) { r.interpolation=on?RigidbodyInterpolation.Interpolate:RigidbodyInterpolation.None; r.isKinematic=!on; if(on) r.WakeUp(); }
        }
        public void Move(Vector3 direction, bool run)
        {
            if(replica || Fallen || Seated || !controller.enabled) return;
            direction=Vector3.ClampMagnitude(direction,1);
            float speed=run?5.6f:3.5f;
            // Laguna Azul: low friction, with a readable expiry and no permanent parameter changes.
            float response=EffectRemaining>0?1.5f:14f;
            momentum=Vector3.Lerp(momentum,direction*speed,1-Mathf.Exp(-response*Time.fixedDeltaTime));
            slipCollision=false;
            controller.Move((momentum+Vector3.down*3)*Time.fixedDeltaTime);
            // Defer disabling the controller until its Move/collision callbacks have completed.
            if(slipCollision) {
                Fall(slipImpulse);
                var game=DemoGame.Instance;
                if(game&&(this==game.Player||(game.Lan&&game.Lan.IsHost))) game.SlipFeedback(Focus);
                return;
            }
            if(!IsPlayer&&direction.sqrMagnitude>.03f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-14*Time.fixedDeltaTime));
            gait+=momentum.magnitude*Time.fixedDeltaTime*2.5f;
            float walk=Mathf.Sin(gait)*Mathf.Min(momentum.magnitude/4,1)*22;
            parts[2].transform.localRotation=Quaternion.Euler(walk,0,0); parts[3].transform.localRotation=Quaternion.Euler(-walk,0,0);
            leftArm.localRotation=Quaternion.Euler(-walk*.6f,0,10); rightArm.localRotation=Quaternion.Euler(Time.time<punchUntil?-95:walk*.6f,0,-10);
            model.localPosition=new(0,Mathf.Abs(Mathf.Sin(gait))*.035f,0);
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(Fallen||slipCollision||Mathf.Abs(hit.normal.y)>.65f) return;
            Vector3 normal=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
            Vector3 relative=momentum;
            if(hit.rigidbody) relative-=hit.rigidbody.GetPointVelocity(hit.point);
            // Normal approach speed excludes floor contacts and fast but harmless glancing motion.
            if(EffectRemaining>0&&-Vector3.Dot(relative,normal)>=SlipFallSpeed) {
                slipCollision=true;
                slipImpulse=Vector3.ClampMagnitude(Vector3.Reflect(momentum,normal)*.6f,6);
            } else if(Vector3.Dot(momentum,normal)<0) {
                // A blocked player cannot build imaginary impact speed by holding into a wall.
                momentum=Vector3.ProjectOnPlane(momentum,normal);
            }
        }
        public void Aim(Vector3 direction) { if(!Fallen && direction.sqrMagnitude>.01f) transform.rotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.z)); }
        public void Face(Vector3 target)
        {
            if(Fallen) return; Vector3 d=target-transform.position; d.y=0;
            if(d.sqrMagnitude>.1f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),Time.fixedDeltaTime*3);
        }
        public void PunchPose(float charge) { if(!Fallen) rightArm.localRotation=Quaternion.Euler(-25-65*charge,0,-35); }
        public void Swing() { punchUntil=Time.time+.3f; }
        public void Fall(Vector3 impulse)
        {
            if(replica||Fallen) return; Seated=false; Fallen=true; RecoverAt=Time.time+3; EffectUntil=0; momentum=Vector3.zero;
            controller.enabled=false; ToggleRagdoll(true);
            foreach(var r in parts) { r.linearVelocity=impulse+Vector3.up*1.5f; r.angularVelocity=new Vector3(2,.4f,1.2f); }
            DemoGame.Instance?.ActorFell(this);
        }
        public void TickRecovery() { if(Fallen && Time.time>=RecoverAt) Recover(); }
        public void Recover()
        {
            if(replica) return;
            Vector3 candidate=Fallen?parts[0].position:transform.position; candidate.y=.08f;
            if(!IsPlayer) candidate=Home;
            if(!TryFindStandingPosition(candidate,this,out var free)&&!TryFindStandingPosition(Home,this,out free)) { RecoverAt=Time.time+.5f; return; }
            Teleport(free,transform.rotation);
        }
        public static bool TryFindStandingPosition(Vector3 preferred,TavernActor ignore,out Vector3 free)
        {
            Physics.SyncTransforms(); preferred.y=.08f;
            for(int ring=0;ring<=6;ring++) for(int step=0;step<(ring==0?1:12);step++) {
                float angle=step*Mathf.PI/6;
                Vector3 p=preferred+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(ring*.85f);
                if(Mathf.Abs(p.x)>=10.8f||Mathf.Abs(p.z)>=7.8f) continue;
                bool blocked=false;
                foreach(var collider in Physics.OverlapCapsule(p+Vector3.up*.45f,p+Vector3.up*1.6f,.4f,~0,QueryTriggerInteraction.Ignore))
                    if(!ignore||collider.GetComponentInParent<TavernActor>()!=ignore) { blocked=true; break; }
                if(!blocked) { free=p; return true; }
            }
            free=default; return false;
        }
        public void Teleport(Vector3 p, Quaternion facing)
        {
            if(Fallen) { ToggleRagdoll(false); Fallen=false; }
            controller.enabled=false; transform.SetPositionAndRotation(p,facing);
            model.localPosition=Vector3.zero; model.localRotation=Quaternion.identity; gait=punchUntil=0; momentum=Vector3.zero; slipCollision=false;
            for(int i=0;i<parts.Count;i++) { parts[i].transform.localPosition=poses[i]; parts[i].transform.localRotation=rotations[i]; }
            // The controller must see the new pose before held movement can call Move again.
            Physics.SyncTransforms(); controller.enabled=!replica;
        }
        public void Drink() { EffectUntil=Time.time+8; }
        public void ClearEffect() { EffectUntil=0; }
    }
}
