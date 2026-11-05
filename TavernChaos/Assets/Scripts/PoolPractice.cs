using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    public enum PoolPhase { Aiming, Rolling, BallInHand, Complete }
    public enum PoolDrill { Straight, Cut, Bank, Rack }

    public sealed class PoolPractice
    {
        public static readonly Vector3 Center = new(6, 1.28f, 1.1f);
        public event Action Changed;
        public bool Active { get; private set; }
        public PoolPhase Phase { get; private set; } = PoolPhase.Aiming;
        public PoolDrill Drill { get; private set; } = PoolDrill.Straight;
        public int Potted { get; private set; }
        public int Shots { get; private set; }
        public int TotalTargets => Drill == PoolDrill.Rack ? 6 : 1;
        public bool Moving => Phase == PoolPhase.Rolling;
        public bool Transitioning => enterDelay>0;
        public bool CanShoot => Active && !DemoGame.Instance.Paused && Phase == PoolPhase.Aiming && !Transitioning;
        public float Power { get; private set; } = .45f;
        public float Angle;
        public Vector3 Direction => Quaternion.Euler(0, Angle, 0) * Vector3.forward;
        public Rigidbody Cue => balls[0];
        public IReadOnlyList<Rigidbody> Balls => balls;
        public bool CanPlace { get; private set; }
        public Vector3 Placement { get; private set; }
        public string Result { get; private set; } = "Apunta a la bola amarilla y embócala en la tronera derecha.";
        public string DrillName => Drill switch { PoolDrill.Straight => "01 · Emboque recto", PoolDrill.Cut => "02 · Tiro de corte", PoolDrill.Bank => "03 · Prueba las bandas", _ => "04 · Triángulo de seis" };
        public string Status => Phase switch {
            PoolPhase.Rolling => "TIRO EN CURSO", PoolPhase.BallInHand => "COLOCA LA BLANCA",
            PoolPhase.Complete => "EJERCICIO COMPLETADO", _ => "TU TIRO" };
        public int PreviewBall { get; private set; } = -1;
        public bool ResetPending => Time.unscaledTime < resetUntil;
        private readonly List<Rigidbody> balls = new();
        private readonly List<SphereCollider> colliders = new();
        private readonly List<Vector3> previous = new();
        private readonly List<float> sinking = new();
        private readonly List<Vector3> sinkFrom = new(), sinkTo = new();
        private readonly Vector3[] pockets = {
            new(-1.5f,0,-2.55f), new(1.5f,0,-2.55f), new(-1.61f,0,0),
            new(1.61f,0,0), new(-1.5f,0,2.55f), new(1.5f,0,2.55f)
        };
        private readonly LineRenderer guide, objectGuide, reboundGuide, ghost, cueStick, placementRing;
        private readonly RaycastHit[] guideHits = new RaycastHit[16];
        private Vector3 returnPosition;
        private Quaternion returnRotation;
        private float stillTime, shotTime, enterDelay, resetUntil;
        private int pottedBefore;
        private bool scratch, outOfBounds;
        private float soundCooldown;
        public static void BuildTable()
        {
            // Cabinet and cloth remain world obstacles for people, but never collide with balls.
            TavernWorld.Box("Pool cabinet", Center + new Vector3(0,-.5f,0), new(3.5f,.7f,5.7f), TavernWorld.DarkWood);
            TavernWorld.Box("Green baize", Center + Vector3.down * (PoolPhysics.Radius + .06f), new(3.46f,.12f,5.56f), TavernWorld.Mat("Baize",new(.045f,.31f,.25f)));
            var rubber = PoolPhysics.Material("Cushion · independent restitution", PoolPhysics.CushionRestitution);
            // Bevelled mouths, with real gaps at the six pockets. The visible mesh and collider agree.
            foreach (int sx in new[]{-1,1}) foreach (int sz in new[]{-1,1})
                Cushion(new[]{new Vector2(sx*1.5f,sz*.36f),new Vector2(sx*1.5f,sz*2.2f),new Vector2(sx*1.73f,sz*2.43f),new Vector2(sx*1.73f,sz*.18f)},rubber);
            foreach (int sz in new[]{-1,1})
                Cushion(new[]{new Vector2(-1.18f,sz*2.55f),new Vector2(1.18f,sz*2.55f),new Vector2(1.41f,sz*2.78f),new Vector2(-1.41f,sz*2.78f)},rubber);
            foreach(float x in new[]{-1.2f,1.2f}) foreach(float z in new[]{-2,2})
                TavernWorld.Box("Pool leg",new(Center.x+x,.4f,Center.z+z),new(.28f,.8f,.28f),TavernWorld.Metal);
            foreach(var p in new[]{new Vector3(-1.5f,0,-2.55f),new(1.5f,0,-2.55f),new(-1.61f,0,0),new(1.61f,0,0),new(-1.5f,0,2.55f),new(1.5f,0,2.55f)})
                TavernWorld.Cylinder("Pocket mouth",Center+p+Vector3.down*(PoolPhysics.Radius-.006f),new(.49f,.009f,.49f),TavernWorld.Black,null,false);
            TavernWorld.Text("02  /  BILLAR LIBRE",new(6,3.6f,8.6f),.25f,new(.3f,1,.8f));
        }
        private static void Cushion(Vector2[] polygon, PhysicsMaterial material)
        {
            var go=new GameObject("Bevelled pool cushion"); go.layer=PoolPhysics.CushionLayer; go.transform.SetParent(TavernWorld.Root,false); go.transform.position=Center;
            var vertices=new Vector3[8]; for(int i=0;i<4;i++) { vertices[i]=new(polygon[i].x,-.12f,polygon[i].y); vertices[i+4]=new(polygon[i].x,.13f,polygon[i].y); }
            int[] triangles={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
            // Mirrored polygons must keep outward winding for the render mesh.
            float signed=0; for(int i=0;i<4;i++) signed+=polygon[i].x*polygon[(i+1)%4].y-polygon[(i+1)%4].x*polygon[i].y;
            if(signed>0) for(int i=0;i<triangles.Length;i+=3) (triangles[i+1],triangles[i+2])=(triangles[i+2],triangles[i+1]);
            var mesh=new Mesh{name="Cushion hull"}; mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=TavernWorld.Wood;
            var c=go.AddComponent<MeshCollider>(); c.sharedMesh=mesh; c.convex=true; c.sharedMaterial=material; c.contactOffset=.0015f;
        }
        public PoolPractice()
        {
            // Isolate the simulation from the cabinet, player capsules, bottles and ragdolls.
            for(int layer=0;layer<32;layer++) Physics.IgnoreLayerCollision(PoolPhysics.BallLayer,layer,layer!=PoolPhysics.BallLayer&&layer!=PoolPhysics.CushionLayer);
            Physics.bounceThreshold=.05f;
            var surface=PoolPhysics.Material("Billiard ball · equal mass",PoolPhysics.BallRestitution);
            Color[] colors={Color.white,new(1,.7f,.12f),new(.14f,.35f,1),new(.95f,.18f,.13f),new(.64f,.17f,.8f),new(1,.4f,.08f),new(.04f,.7f,.33f)};
            for(int i=0;i<7;i++) {
                var go=TavernWorld.Shape(i==0?"Cue ball":"Object ball "+i,PrimitiveType.Sphere,Center,Vector3.one*PoolPhysics.Radius*2,TavernWorld.Mat("Ball "+i,colors[i],.15f)); go.layer=PoolPhysics.BallLayer;
                var c=go.GetComponent<SphereCollider>(); c.sharedMaterial=surface; c.contactOffset=.0015f;
                var rb=go.AddComponent<Rigidbody>(); PoolPhysics.Configure(rb);
                go.AddComponent<PoolBallSound>(); balls.Add(rb); colliders.Add(c); previous.Add(Center); sinking.Add(-1); sinkFrom.Add(Center); sinkTo.Add(Center);
            }
            guide=Line("Cue path",new(.95f,.94f,.82f,.9f),.022f);
            objectGuide=Line("Object ball direction",new(.35f,.95f,.78f,.85f),.022f);
            reboundGuide=Line("Cushion reflection",new(.9f,.8f,.5f,.5f),.014f);
            ghost=Line("Ghost contact ball",new(1,.92f,.7f,.8f),.015f,49);
            cueStick=Line("Cue alignment",new(.8f,.58f,.32f),.045f);
            placementRing=Line("Cue placement",new(.35f,1,.7f),.025f,49);
            Reset();
        }
        private static LineRenderer Line(string name,Color color,float width,int count=2)
        {
            var a=new GameObject(name); a.transform.SetParent(TavernWorld.Root,false);
            var line=a.AddComponent<LineRenderer>(); line.sharedMaterial=new Material(Shader.Find("Sprites/Default")); line.startColor=color; line.endColor=color;
            line.startWidth=width; line.endWidth=width; line.positionCount=count; line.enabled=false; return line;
        }
        private void Emit() => Changed?.Invoke();
        public void Enter()
        {
            if(Active) return;
            var player=DemoGame.Instance.Player; returnPosition=player.transform.position; returnRotation=player.transform.rotation;
            player.Teleport(new(3.7f,.08f,.4f),Quaternion.Euler(0,90,0)); Active=true; enterDelay=.25f; resetUntil=0; Emit();
        }
        public void Exit()
        {
            if(!Active) return; Active=false; HideGuides(); resetUntil=0;
            var player=DemoGame.Instance.Player; if(!player.Fallen) player.Teleport(returnPosition,returnRotation); Emit();
        }
        public void SetPower(float value)
        {
            if(!float.IsFinite(value)) return; value=Mathf.Clamp(value,.05f,1);
            if(Mathf.Approximately(value,Power)) return; Power=value; Emit();
        }
        public bool PointAt(Vector3 point)
        {
            if(Phase==PoolPhase.BallInHand) { UpdatePlacement(point); return true; }
            if(Phase!=PoolPhase.Aiming || !Cue.gameObject.activeSelf) return false;
            Vector3 delta=point-Cue.position; delta.y=0; if(delta.sqrMagnitude<.035f) return false;
            Angle=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg; return true;
        }
        public bool Shoot(float power) => Shoot(power,Direction);
        public bool Shoot(float power,Vector3 direction)
        {
            if(!CanShoot || !float.IsFinite(power) || !Finite(direction)) return false;
            direction.y=0; if(direction.sqrMagnitude<.001f) return false;
            Cue.WakeUp(); Cue.linearVelocity=direction.normalized*PoolPhysics.ShotSpeed(power);
            Shots++; pottedBefore=Potted; scratch=outOfBounds=false; Phase=PoolPhase.Rolling; stillTime=shotTime=0; resetUntil=0;
            Result="Espera a que se detengan las bolas."; HideGuides(); DemoGame.Instance.Audio.Play("clink",.5f); Emit(); return true;
        }
        public void FixedTick()
        {
            enterDelay=Mathf.Max(0,enterDelay-Time.fixedDeltaTime);
            bool anyMoving=false, anySinking=false;
            for(int i=0;i<balls.Count;i++) {
                var b=balls[i]; if(!b.gameObject.activeSelf) continue;
                if(sinking[i]>=0) { TickSink(i); anySinking=true; continue; }
                Vector3 local=b.position-Center, before=previous[i]-Center; local.y=before.y=0;
                int pocket=FindPocket(before,local);
                if(pocket>=0) { StartSink(i,pocket); anySinking=true; continue; }
                if(Mathf.Abs(local.x)>1.96f||Mathf.Abs(local.z)>3.02f) { ReSpotOutOfBounds(i); outOfBounds=true; continue; }
                Vector3 v=PoolPhysics.Slow(b.linearVelocity,Time.fixedDeltaTime);
                if(v.magnitude<PoolPhysics.StopSpeed) v=Vector3.zero;
                b.linearVelocity=v; b.angularVelocity=Vector3.Cross(Vector3.up,v)/PoolPhysics.Radius;
                if(v.sqrMagnitude>0) anyMoving=true;
                previous[i]=b.position;
            }
            if(Phase!=PoolPhase.Rolling) return;
            shotTime+=Time.fixedDeltaTime;
            stillTime=anyMoving||anySinking?0:stillTime+Time.fixedDeltaTime;
            if(shotTime>18) { StopAll(); stillTime=PoolPhysics.SettleTime; }
            if(stillTime>=PoolPhysics.SettleTime) FinishShot();
        }
        private int FindPocket(Vector3 a,Vector3 b)
        {
            Vector3 ab=b-a;
            for(int i=0;i<pockets.Length;i++) {
                float t=ab.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(pockets[i]-a,ab)/ab.sqrMagnitude):0;
                Vector3 nearest=a+ab*t;
                bool mouth=i==2||i==3?Mathf.Abs(nearest.x)>1.47f&&Mathf.Abs(nearest.z)<.235f:Mathf.Abs(nearest.x)>1.27f&&Mathf.Abs(nearest.z)>2.32f;
                if(mouth&&Vector3.Distance(nearest,pockets[i])<.24f) return i;
            }
            return -1;
        }
        private void StartSink(int i,int pocket)
        {
            var b=balls[i]; b.linearVelocity=b.angularVelocity=Vector3.zero; b.isKinematic=true; colliders[i].enabled=false;
            sinking[i]=0; sinkFrom[i]=b.position; sinkTo[i]=Center+pockets[pocket];
            if(i==0) scratch=true; else Potted++;
            DemoGame.Instance.Audio.Play("clink",.65f); Emit();
        }
        private void TickSink(int i)
        {
            sinking[i]+=Time.fixedDeltaTime;
            float t=Mathf.Clamp01(sinking[i]/.3f); var b=balls[i];
            b.MovePosition(Vector3.Lerp(sinkFrom[i],sinkTo[i]+Vector3.down*.35f,t*t));
            b.transform.localScale=Vector3.one*(PoolPhysics.Radius*2*Mathf.Lerp(1,.5f,t));
            if(t>=1) { b.gameObject.SetActive(false); sinking[i]=-1; }
        }
        private void ReSpotOutOfBounds(int i)
        {
            if(i==0) { scratch=true; balls[0].gameObject.SetActive(false); return; }
            Put(i,FindFree(new(0,0,.65f),i)); Result="Bola fuera de la mesa: repuesta, sin sumar un emboque.";
        }
        private void FinishShot()
        {
            StopAll();
            if(Potted==TotalTargets) { Phase=PoolPhase.Complete; Result=$"¡Mesa limpia en {Shots} "+(Shots==1?"tiro!":"tiros!"); }
            else if(scratch) { Phase=PoolPhase.BallInHand; Result="Blanca embocada o fuera. Colócala sobre el paño para continuar."; Placement=FindFree(new(0,0,-1.5f),0); CanPlace=true; }
            else { Phase=PoolPhase.Aiming; Result=outOfBounds?"Bola fuera: repuesta sin sumar puntos.":Potted>pottedBefore?$"¡Dentro! {Potted} / {TotalTargets}. Prepara tu siguiente tiro.":"Prueba otra línea o ajusta la potencia."; }
            Emit();
        }
        public void UpdatePlacement(Vector3 world)
        {
            Placement=new(world.x,Center.y,world.z);
            Vector3 p=Placement-Center;
            CanPlace=Finite(Placement)&&Mathf.Abs(p.x)<=PoolPhysics.HalfWidth-PoolPhysics.Radius-.04f&&Mathf.Abs(p.z)<=PoolPhysics.HalfLength-PoolPhysics.Radius-.04f;
            if(CanPlace) foreach(var b in balls) if(b!=Cue&&b.gameObject.activeSelf&&Vector3.Distance(Placement,b.position)<PoolPhysics.Radius*2+.025f) CanPlace=false;
        }
        public bool ConfirmPlacement()
        {
            if(!Active||DemoGame.Instance.Paused||Phase!=PoolPhase.BallInHand||!CanPlace) return false;
            Put(0,Placement); scratch=false; Phase=PoolPhase.Aiming; enterDelay=.15f; Result="Blanca colocada. Apunta y tira."; Emit(); return true;
        }
        private Vector3 FindFree(Vector3 local,int skip)
        {
            for(int j=0;j<121;j++) {
                Vector3 p=Center+(j==0?local:new Vector3((j%11-5)*.24f,0,(j/11-5)*.4f)); bool free=true;
                for(int i=0;i<balls.Count;i++) if(i!=skip&&balls[i].gameObject.activeSelf&&sinking[i]<0&&Vector3.Distance(p,balls[i].position)<PoolPhysics.Radius*2+.025f) free=false;
                if(free) return p;
            }
            return Center+local;
        }
        public bool RequestReset()
        {
            if(Phase==PoolPhase.Complete) { Reset(); return true; }
            if(!ResetPending) { resetUntil=Time.unscaledTime+3; Emit(); return false; }
            Reset(); return true;
        }
        public void CancelReset() { resetUntil=0; Emit(); }
        public void SelectDrill(PoolDrill drill) { Drill=drill; Reset(); }
        private void Put(int i,Vector3 p)
        {
            var b=balls[i]; b.gameObject.SetActive(true); b.isKinematic=false; colliders[i].enabled=true;
            b.linearVelocity=b.angularVelocity=Vector3.zero; b.position=p; b.rotation=Quaternion.identity; b.transform.localScale=Vector3.one*PoolPhysics.Radius*2;
            previous[i]=p; sinking[i]=-1; b.Sleep();
        }
        private void StopAll()
        {
            foreach(var b in balls) if(b.gameObject.activeSelf&&!b.isKinematic) { b.linearVelocity=b.angularVelocity=Vector3.zero; b.Sleep(); }
        }
        public void Reset()
        {
            Potted=Shots=0; stillTime=shotTime=resetUntil=0; enterDelay=.15f; scratch=outOfBounds=false; CanPlace=false;
            for(int i=0;i<balls.Count;i++) { Put(i,Center); balls[i].gameObject.SetActive(false); }
            if(Drill==PoolDrill.Straight) { Put(0,Center+new Vector3(-.9f,0,0)); Put(1,Center+new Vector3(.7f,0,0)); Angle=90; }
            else if(Drill==PoolDrill.Cut) { Put(0,Center+new Vector3(-.7f,0,-1.3f)); Put(1,Center+new Vector3(.4f,0,.15f)); Angle=40; }
            else if(Drill==PoolDrill.Bank) { Put(0,Center+new Vector3(-.7f,0,-1.2f)); Put(1,Center+new Vector3(.75f,0,.8f)); Angle=-35; }
            else {
                Put(0,Center+new Vector3(0,0,-1.65f)); int i=1;
                for(int row=0;row<3;row++) for(int col=0;col<=row;col++) Put(i++,Center+new Vector3((col-row*.5f)*.244f,0,.62f+row*.212f)); Angle=0;
            }
            Phase=PoolPhase.Aiming; Result=Drill==PoolDrill.Straight?"Apunta a la amarilla y embócala en la tronera derecha.":"Emboca "+TotalTargets+" "+(TotalTargets==1?"bola":"bolas")+" en los menos tiros posibles."; HideGuides(); Emit();
        }
        public void UpdateGuide()
        {
            HideGuides(); PreviewBall=-1;
            if(!Active||DemoGame.Instance.Paused) return;
            if(Phase==PoolPhase.BallInHand) { Circle(placementRing,Placement,PoolPhysics.Radius,CanPlace?new(.3f,1,.65f):new(1,.3f,.2f)); return; }
            if(Phase!=PoolPhase.Aiming) return;
            Vector3 origin=Cue.position, direction=Direction;
            int count=Physics.SphereCastNonAlloc(origin,PoolPhysics.Radius,direction,guideHits,7,(1<<PoolPhysics.BallLayer)|(1<<PoolPhysics.CushionLayer),QueryTriggerInteraction.Ignore);
            float distance=7; RaycastHit nearest=default; bool found=false;
            for(int i=0;i<count;i++) if(guideHits[i].rigidbody!=Cue&&guideHits[i].distance<distance) { nearest=guideHits[i]; distance=nearest.distance; found=true; }
            Vector3 end=origin+direction*distance;
            Segment(guide,origin,end); Circle(ghost,end,PoolPhysics.Radius,new(1,.94f,.8f,.8f));
            Segment(cueStick,origin-direction*.25f,origin-direction*(.9f+Power*.35f));
            if(found&&nearest.rigidbody) {
                PreviewBall=balls.IndexOf(nearest.rigidbody);
                Vector3 normal=nearest.rigidbody.position-end; normal.y=0; normal.Normalize();
                float transfer=Mathf.Clamp01(Vector3.Dot(direction,normal));
                Segment(objectGuide,nearest.rigidbody.position,nearest.rigidbody.position+normal*Mathf.Lerp(.4f,1.2f,transfer));
            } else if(found) {
                Vector3 reflected=Vector3.Reflect(direction,nearest.normal); reflected.y=0;
                Segment(reboundGuide,end,end+reflected.normalized*.9f);
            }
        }
        private void HideGuides() { guide.enabled=objectGuide.enabled=reboundGuide.enabled=ghost.enabled=cueStick.enabled=placementRing.enabled=false; }
        private static void Segment(LineRenderer line,Vector3 a,Vector3 b) { line.enabled=true; a.y=b.y=Center.y+.025f; line.SetPosition(0,a); line.SetPosition(1,b); }
        private static void Circle(LineRenderer line,Vector3 center,float radius,Color color)
        {
            line.enabled=true; line.startColor=line.endColor=color; center.y=Center.y+.025f;
            for(int i=0;i<line.positionCount;i++) { float a=i*Mathf.PI*2/(line.positionCount-1); line.SetPosition(i,center+new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius)); }
        }
        private static bool Finite(Vector3 p) => float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        public void ContactSound(float speed)
        {
            if(speed<.15f||Time.unscaledTime<soundCooldown) return; soundCooldown=Time.unscaledTime+.035f;
            DemoGame.Instance.Audio.Play("clink",Mathf.Lerp(.08f,.55f,Mathf.Clamp01(speed/4)));
        }
        // Deterministic arrangements for opt-in physics regression tests; same bodies/materials as play.
        internal void TestLayout(Vector3[] positions,Vector3[] velocities)
        {
            Reset(); for(int i=0;i<balls.Count;i++) balls[i].gameObject.SetActive(false);
            for(int i=0;i<positions.Length;i++) { Put(i,Center+positions[i]); balls[i].linearVelocity=velocities[i]; previous[i]=balls[i].position; }
            Drill=PoolDrill.Rack; Phase=PoolPhase.Rolling; pottedBefore=0; enterDelay=0; Emit();
        }
    }
}
