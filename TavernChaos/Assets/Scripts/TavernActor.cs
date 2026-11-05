using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    public sealed class TavernActor : MonoBehaviour
    {
        public bool IsPlayer { get; private set; }
        public bool Fallen { get; private set; }
        public float RecoverAt { get; private set; }
        public float EffectUntil { get; private set; }
        public float EffectRemaining => Mathf.Max(0, EffectUntil-Time.time);
        public Vector3 Home { get; private set; }
        public Transform Hand { get; private set; }
        public Vector3 Focus => Fallen ? parts[0].position : transform.position+Vector3.up*1.1f;
        private CharacterController controller;
        private Transform model, leftArm, rightArm;
        private readonly List<Rigidbody> parts = new();
        private readonly List<Vector3> poses = new();
        private readonly List<Quaternion> rotations = new();
        private readonly List<Collider> colliders = new();
        private readonly List<CharacterJoint> joints = new();
        private Vector3 momentum;
        private float gait, punchUntil;
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
            TavernWorld.Box("Belt",new(0,-.37f,0),new(1.02f,.12f,1.02f),TavernWorld.Black,torso.transform,false);
            Hand=new GameObject("Carry socket").transform; Hand.SetParent(transform,false); Hand.localPosition=new(.55f,1.25f,.65f);
            foreach(var c in colliders) foreach(var other in colliders) if(c!=other) Physics.IgnoreCollision(c,other);
            ToggleRagdoll(false);
        }
        private Rigidbody Part(string name, Vector3 pos, Vector3 size, Material m, PrimitiveType shape, Rigidbody connected)
        {
            var g=TavernWorld.Shape(name,shape,pos,size,m,model); var rb=g.AddComponent<Rigidbody>(); rb.mass=connected?2:7; rb.isKinematic=true;
            rb.interpolation=RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative; rb.maxAngularVelocity=12;
            if(connected) {
                var j=g.AddComponent<CharacterJoint>(); j.connectedBody=connected; j.enablePreprocessing=false;
                j.swing1Limit=new SoftJointLimit{limit=40}; j.swing2Limit=new SoftJointLimit{limit=30}; j.lowTwistLimit=new SoftJointLimit{limit=-30}; j.highTwistLimit=new SoftJointLimit{limit=30}; joints.Add(j);
            }
            parts.Add(rb); poses.Add(pos); rotations.Add(Quaternion.identity); colliders.Add(g.GetComponent<Collider>()); return rb;
        }
        private void ToggleRagdoll(bool on)
        {
            foreach(var c in colliders) c.enabled=on;
            foreach(var r in parts) { r.isKinematic=!on; if(on) r.WakeUp(); }
        }
        public void Move(Vector3 direction, bool run)
        {
            if(Fallen || !controller.enabled) return;
            direction=Vector3.ClampMagnitude(direction,1);
            float speed=run?5.6f:3.5f;
            // Laguna Azul: low friction, with a readable expiry and no permanent parameter changes.
            float response=EffectRemaining>0?1.5f:14f;
            momentum=Vector3.Lerp(momentum,direction*speed,1-Mathf.Exp(-response*Time.fixedDeltaTime));
            controller.Move((momentum+Vector3.down*3)*Time.fixedDeltaTime);
            if(direction.sqrMagnitude>.03f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-14*Time.fixedDeltaTime));
            gait+=momentum.magnitude*Time.fixedDeltaTime*2.5f;
            float walk=Mathf.Sin(gait)*Mathf.Min(momentum.magnitude/4,1)*22;
            parts[2].transform.localRotation=Quaternion.Euler(walk,0,0); parts[3].transform.localRotation=Quaternion.Euler(-walk,0,0);
            leftArm.localRotation=Quaternion.Euler(-walk*.6f,0,10); rightArm.localRotation=Quaternion.Euler(Time.time<punchUntil?-95:walk*.6f,0,-10);
            model.localPosition=new(0,Mathf.Abs(Mathf.Sin(gait))*.035f,0);
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
            if(Fallen) return; Fallen=true; RecoverAt=Time.time+3; EffectUntil=0; momentum=Vector3.zero;
            controller.enabled=false; ToggleRagdoll(true);
            foreach(var r in parts) { r.linearVelocity=impulse+Vector3.up*1.5f; r.angularVelocity=new Vector3(2,.4f,1.2f); }
            DemoGame.Instance?.ActorFell(this);
        }
        public void TickRecovery() { if(Fallen && Time.time>=RecoverAt) Recover(); }
        public void Recover()
        {
            ToggleRagdoll(false);
            Vector3 candidate=Fallen?parts[0].position:transform.position; candidate.y=.08f;
            if(!IsPlayer || !Safe(candidate)) candidate=Home;
            transform.position=candidate; transform.rotation=Quaternion.identity;
            model.localPosition=Vector3.zero;
            for(int i=0;i<parts.Count;i++) { parts[i].transform.localPosition=poses[i]; parts[i].transform.localRotation=rotations[i]; }
            Fallen=false; controller.enabled=true; momentum=Vector3.zero;
        }
        private bool Safe(Vector3 p) => Mathf.Abs(p.x)<10.8f && Mathf.Abs(p.z)<7.8f && !Physics.CheckCapsule(p+Vector3.up*.45f,p+Vector3.up*1.6f,.4f,~0,QueryTriggerInteraction.Ignore);
        public void Teleport(Vector3 p, Quaternion facing)
        {
            if(Fallen) Recover(); controller.enabled=false; transform.SetPositionAndRotation(p,facing); controller.enabled=true; momentum=Vector3.zero;
        }
        public void Drink() { EffectUntil=Time.time+8; }
        public void ClearEffect() { EffectUntil=0; }
    }
}
