using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    public static class PoolPhysicsChecks
    {
        [Serializable] private sealed class Measurement { public string name,unit; public float actual,reference; public bool passed; }
        [Serializable] private sealed class Report { public string model="Planar PhysX contacts, constant cloth resistance, no English/jump shots"; public float fixedStep; public List<Measurement> measurements=new(); }
        private static IEnumerator Ticks(int count) { for(int i=0;i<count;i++) yield return new WaitForFixedUpdate(); }
        public static IEnumerator Run(DemoGame game,Action<bool,string> check,string output)
        {
            var p=game.Pool; var report=new Report{fixedStep=Time.fixedDeltaTime};
            void Record(string name,bool pass,float actual,float reference,string unit) { report.measurements.Add(new(){name=name,passed=pass,actual=actual,reference=reference,unit=unit}); check(pass,name+" ["+actual.ToString("0.000")+" "+unit+"]"); }
            p.Enter(); p.SelectDrill(PoolDrill.Straight); yield return Ticks(35);
            Vector3 initial=p.Cue.position; yield return Ticks(100);
            float drift=Vector3.Distance(initial,p.Cue.position);
            Record("Pool: idle balls do not drift or overlap",drift<.001f,drift,0,"m");
            p.UpdateGuide(); check(p.PreviewBall==1,"Pool: guide identifies the first object ball, not geometry behind it");
            check(!DemoHUD.PoolUIContains(Camera.main.WorldToScreenPoint(PoolPractice.Center)),"Pool: cloth is an aim/shot region, not a UI blocker");
            check(DemoHUD.PoolUIContains(new Vector2(Screen.width*.84f,Screen.height*.75f)),"Pool: power-panel clicks cannot fire a shot");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-pool-aim.png")); yield return new WaitForEndOfFrame();
            p.TestLayout(new[]{new Vector3(-.65f,0,0),new Vector3(-.05f,0,0)},new[]{Vector3.right*1.5f,Vector3.zero});
            yield return Ticks(50);
            float cue=p.Cue.linearVelocity.magnitude, target=p.Balls[1].linearVelocity.x;
            Record("Pool: equal-mass head-on contact transfers speed",target>1&&cue<.12f,target,1.3f,"m/s");
            Record("Pool: head-on contact does not invent sideways motion",Mathf.Abs(p.Balls[1].linearVelocity.z)<.015f,Mathf.Abs(p.Balls[1].linearVelocity.z),0,"m/s");
            float energy=p.Cue.linearVelocity.sqrMagnitude+p.Balls[1].linearVelocity.sqrMagnitude;
            Record("Pool: head-on contact does not add kinetic energy",energy<=2.25f*1.02f,energy,2.25f,"speed² sum");
            p.TestLayout(new[]{new Vector3(-.65f,0,-.12f),Vector3.zero},new[]{Vector3.right*1.8f,Vector3.zero}); yield return Ticks(55);
            check(p.Balls[1].linearVelocity.z>.2f&&p.Cue.linearVelocity.z<-.2f,"Pool: cut shot separates cue/object along contact normal");
            p.TestLayout(new[]{new Vector3(0,0,.95f)},new[]{Vector3.right*2});
            float before=2, ratio=0; bool reflected=false;
            for(int i=0;i<150;i++) {
                yield return new WaitForFixedUpdate(); float v=p.Cue.linearVelocity.x;
                if(!reflected&&v<-.05f) { ratio=-v/before; reflected=true; } if(v>.05f) before=v;
            }
            Record("Pool: cushion absorbs energy independently of ball restitution",reflected&&ratio>.65f&&ratio<.86f,ratio,PoolPhysics.CushionRestitution,"rebound ratio");
            p.TestLayout(new[]{new Vector3(1.25f,0,1)},new[]{Vector3.right*.45f}); yield return Ticks(60);
            Record("Pool: a gentle cushion contact still rebounds",p.Cue.linearVelocity.x<-.02f,p.Cue.linearVelocity.x,-.15f,"m/s");
            p.TestLayout(new[]{new Vector3(0,0,-1)},new[]{Vector3.forward}); yield return Ticks(50); float first=p.Cue.linearVelocity.magnitude;
            yield return Ticks(50); float second=p.Cue.linearVelocity.magnitude;
            Record("Pool: cloth slows at a constant rolling rate",Mathf.Abs((first-second)-PoolPhysics.RollingResistance*.5f)<.02f,first-second,PoolPhysics.RollingResistance*.5f,"m/s per 0.5s");
            yield return Ticks(200);
            check(!p.Moving&&p.Cue.linearVelocity==Vector3.zero&&p.Cue.angularVelocity==Vector3.zero,"Pool: shot ends after sustained rest, without endless creeping");
            p.TestLayout(new[]{new Vector3(-.8f,0,-1.2f)},new[]{Vector3.forward});
            int shots=p.Shots; check(!p.Shoot(.9f)&&p.Shots==shots,"Pool: a second shot is rejected while balls move");
            p.TestLayout(new[]{new Vector3(-.8f,0,-1.2f),new Vector3(1.1f,0,0)},new[]{Vector3.zero,Vector3.right}); yield return Ticks(150);
            check(p.Potted==1&&!p.Balls[1].gameObject.activeSelf,"Pool: crossing the side pocket mouth pots one object ball once");
            p.TestLayout(new[]{new Vector3(-.8f,0,-1.2f),new Vector3(1.1f,0,2.15f)},new[]{Vector3.zero,new Vector3(.6f,0,.6f)}); yield return Ticks(150);
            check(p.Potted==1&&!p.Balls[1].gameObject.activeSelf,"Pool: diagonal corner-pocket entry is captured");
            p.TestLayout(new[]{new Vector3(1.12f,0,.46f)},new[]{Vector3.right*.7f}); yield return Ticks(120);
            check(p.Potted==0&&p.Cue.gameObject.activeSelf,"Pool: grazing a pocket jaw does not magically pot the cue");
            p.TestLayout(new[]{new Vector3(-.8f,0,-1.2f),new Vector3(2.2f,0,1)},new[]{Vector3.zero,Vector3.zero}); yield return Ticks(40);
            check(p.Potted==0&&p.Balls[1].gameObject.activeSelf&&Mathf.Abs(p.Balls[1].position.x-PoolPractice.Center.x)<1.4f,"Pool: out-of-bounds object is re-spotted, never counted as a pot");
            p.TestLayout(new[]{new Vector3(1.1f,0,0),new Vector3(-.5f,0,-.5f)},new[]{Vector3.right*.9f,Vector3.zero}); yield return Ticks(140);
            check(p.Phase==PoolPhase.BallInHand&&p.Potted==0&&!p.Cue.gameObject.activeSelf,"Pool: scratched cue waits for deliberate placement after rest");
            p.UpdatePlacement(p.Balls[1].position); check(!p.CanPlace&&!p.ConfirmPlacement(),"Pool: ball-in-hand cannot overlap another ball");
            p.UpdatePlacement(PoolPractice.Center+new Vector3(1.48f,0,0)); check(!p.CanPlace&&!p.ConfirmPlacement(),"Pool: ball-in-hand cannot be placed in a pocket or cushion");
            p.UpdatePlacement(PoolPractice.Center+new Vector3(0,0,-1.4f)); check(p.CanPlace&&p.ConfirmPlacement()&&p.Cue.gameObject.activeSelf,"Pool: valid ball-in-hand placement restores aiming");
            yield return Ticks(25); game.SetPause(true); check(!p.Shoot(.5f),"Pool: paused simulation cannot accept a shot"); game.SetPause(false);
            check(!p.Shoot(float.NaN,Vector3.forward)&&!p.Shoot(.5f,new Vector3(float.NaN,0,1)),"Pool: invalid shot parameters cannot corrupt physics");
            p.SelectDrill(PoolDrill.Straight); yield return Ticks(30); check(!p.RequestReset()&&p.ResetPending,"Pool: reset requires confirmation and does not instantly clear progress"); p.CancelReset();
            p.SetPower(.45f); p.Angle=90; check(p.Shoot(p.Power),"Pool: practice accepts a legal prepared shot");
            for(int i=0;i<900&&p.Moving;i++) yield return new WaitForFixedUpdate();
            check(p.Phase==PoolPhase.Complete&&p.Potted==1&&p.Shots==1,"Pool: introductory exercise can be completed in one controlled shot");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"06-pool-result.png")); yield return new WaitForEndOfFrame();
            p.SelectDrill(PoolDrill.Rack); yield return Ticks(30); p.Angle=0; p.Shoot(1);
            bool bounded=true, noOverlap=true; float maximum=0;
            for(int i=0;i<1500&&p.Moving;i++) {
                yield return new WaitForFixedUpdate();
                foreach(var ball in p.Balls) if(ball.gameObject.activeSelf&&!ball.isKinematic) {
                    Vector3 v=ball.linearVelocity, q=ball.position-PoolPractice.Center; maximum=Mathf.Max(maximum,v.magnitude);
                    if(!float.IsFinite(v.sqrMagnitude)||Mathf.Abs(q.x)>1.97f||Mathf.Abs(q.z)>3.03f) bounded=false;
                }
            }
            for(int i=0;i<p.Balls.Count;i++) for(int j=i+1;j<p.Balls.Count;j++) if(p.Balls[i].gameObject.activeSelf&&p.Balls[j].gameObject.activeSelf&&Vector3.Distance(p.Balls[i].position,p.Balls[j].position)<PoolPhysics.Radius*2-.015f) noOverlap=false;
            Record("Pool: maximum-power break stays bounded and loses speed",bounded&&!p.Moving&&maximum<=PoolPhysics.MaxSpeed*1.03f,maximum,PoolPhysics.MaxSpeed,"m/s peak");
            check(noOverlap,"Pool: break settles without interpenetrating balls");
            int previousVsync=QualitySettings.vSyncCount; QualitySettings.vSyncCount=0;
            Application.targetFrameRate=30; p.TestLayout(new[]{new Vector3(0,0,-1)},new[]{Vector3.forward}); yield return Ticks(100); Vector3 low=p.Cue.position;
            Application.targetFrameRate=120; p.TestLayout(new[]{new Vector3(0,0,-1)},new[]{Vector3.forward}); yield return Ticks(100); Vector3 high=p.Cue.position;
            float difference=Vector3.Distance(low,high); Record("Pool: rolling travel agrees at 30 and 120 render FPS",difference<.025f,difference,0,"m difference after 1s");
            QualitySettings.vSyncCount=previousVsync; Application.targetFrameRate=60;
            p.SelectDrill(PoolDrill.Cut); yield return Ticks(35);
            Screen.SetResolution(1280,720,FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.35f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"07-pool-1280x720.png")); yield return new WaitForEndOfFrame();
            Screen.SetResolution(1920,900,FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.35f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"08-pool-wide.png")); yield return new WaitForEndOfFrame();
            Screen.SetResolution(1440,900,FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(.35f);
            p.SelectDrill(PoolDrill.Straight); yield return Ticks(30); p.Exit();
            File.WriteAllText(Path.Combine(output,"pool-physics.json"),JsonUtility.ToJson(report,true));
        }
    }
}
