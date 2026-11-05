using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    public sealed class PoolRoomChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string role; public bool passed; public List<string> checks=new(),failures=new(),errors=new(); }
        private readonly Report report=new();
        private DemoGame g; private string folder,role,stage="",pendingHostStage; private ushort port;
        private static double Now=>Time.realtimeSinceStartupAsDouble;
        private PoolRoomView View=>g.Lan.View?.pool;
        private bool mine=>View?.hasTurn==true&&View.turn==g.Lan.LocalId;
        private void Check(bool ok,string name) { (ok?report.checks:report.failures).Add(name); Debug.Log((ok?"PASS ":"FAIL ")+name); }
        private void Log(string text,string trace,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) report.errors.Add(text+"\n"+trace); }
        private IEnumerator Wait(Func<bool> predicate,float seconds=4)
        {
            double end=Now+seconds; while(Now<end&&!predicate()) yield return null;
        }
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>(); Application.logMessageReceived+=Log;
            string Arg(string key,string fallback) { var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,key); return i>=0&&i+1<args.Length?args[i+1]:fallback; }
            role=Arg("-pool-role","host"); folder=Arg("-evidencePath",Application.persistentDataPath); port=ushort.Parse(Arg("-pool-port","7792")); report.role=role; Directory.CreateDirectory(folder);
            yield return null;
            if(role=="host") yield return Host(); else yield return Client();
            if(role=="host"&&report.failures.Count>0) { SetStage("abort"); yield return new WaitForSecondsRealtime(.3f); }
            Check(report.errors.Count==0,"No runtime errors in "+role); report.passed=report.failures.Count==0&&report.errors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+".json"),JsonUtility.ToJson(report,true)); Application.logMessageReceived-=Log;
            g.Menu(); yield return new WaitForSecondsRealtime(.2f); Application.Quit(report.passed?0:1);
        }
        private void SetStage(string value) { File.WriteAllText(Path.Combine(folder,"step.ready"),value); pendingHostStage=Act(value)?null:value; }
        private void Update() { if(role=="host"&&pendingHostStage!=null&&Act(pendingHostStage)) pendingHostStage=null; }
        private bool Act(string value)
        {
            var room=g.Lan;
            if(value=="sit"&&role!="spectator") { room.Send(LanAction.PoolSit); return true; }
            if(value=="queue"&&role=="spectator") { room.Send(LanAction.PoolSit); return true; }
            if(value=="reject"&&!mine&&role!="spectator"&&View?.playPhase==EightBallPhase.Placement) { room.Send(LanAction.PoolPlace,PoolPractice.Center+new Vector3(0,0,-1.8f)); return true; }
            if(value=="badplace"&&mine&&View.playPhase==EightBallPhase.Placement) { room.Send(LanAction.PoolPlace,PoolPractice.Center); return true; }
            if((value=="place"||value=="place2")&&mine&&View.playPhase==EightBallPhase.Placement) { room.Send(LanAction.PoolPlace,PoolPractice.Center+new Vector3(0,0,-1.8f)); return true; }
            if(value=="weak"&&mine&&View.playPhase==EightBallPhase.Aiming&&View.breaking) { room.Send(LanAction.PoolShoot,Vector3.forward,power:.05f); return true; }
            if(value=="accept"&&mine&&View.playPhase==EightBallPhase.BreakDecision) { room.Send(LanAction.PoolAccept); return true; }
            if(value.StartsWith("call-")&&mine&&View.playPhase==EightBallPhase.Aiming&&!View.breaking) { room.Send(LanAction.PoolCall,ball:int.Parse(value.Substring(5)),pocket:3); return true; }
            if(value.StartsWith("shoot-")&&mine&&View.playPhase==EightBallPhase.Aiming&&View.callBall==int.Parse(value.Substring(6))) { room.Send(LanAction.PoolShoot,Vector3.right,ball:int.Parse(value.Substring(6)),pocket:3,power:.8f); return true; }
            if(value.StartsWith("return-")&&value=="return-"+room.LocalId) { room.Send(LanAction.PoolSit); return true; }
            if(value=="close"&&View?.organizer==room.LocalId&&View.phase==TablePhase.Result) { room.Send(LanAction.PoolClose); return true; }
            if(value=="nextseat"&&role=="spectator"&&View?.phase==TablePhase.Preparation) { room.Send(LanAction.PoolSit); return true; }
            if(value=="again"&&View?.organizer==room.LocalId&&View.phase==TablePhase.Preparation) { room.Send(LanAction.PoolStart); return true; }
            if(value=="break2"&&mine&&View.playPhase==EightBallPhase.Aiming&&View.breaking) { room.Send(LanAction.PoolShoot,Vector3.forward,power:1); return true; }
            return false;
        }
        private IEnumerator Host()
        {
            Check(g.Lan.Host("Host",port),"Host opens real NGO room");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) yield return new WaitForEndOfFrame();
            File.WriteAllText(Path.Combine(folder,"host.ready"),"ready");
            yield return Wait(()=>(g.Lan.View?.actors?.Length??0)==3,15);
            Check(g.Lan.View?.actors?.Length==3,"Three independent players connect"); if(g.Lan.View?.actors?.Length!=3) yield break;
            ulong client=0,spectator=0;
            foreach(var a in g.Lan.View.actors) { if(a.name=="Client") client=a.id; if(a.name=="Spectator") spectator=a.id; }
            g.Player.Teleport(PoolRoomController.SeatPosition(1)+Vector3.back*.6f,Quaternion.identity);
            g.Lan.ActorFor(client).Teleport(PoolRoomController.SeatPosition(0)+Vector3.back*.6f,Quaternion.identity);
            g.Lan.ActorFor(spectator).Teleport(PoolRoomController.SeatPosition(1)+Vector3.back*1.3f,Quaternion.identity);
            SetStage("sit"); yield return Wait(()=>View?.seated.Length==2);
            Check(View.seated.Length==2&&g.Player.Seated&&g.Lan.ActorFor(client).Seated,"Both server-confirmed seats are exclusive");
            Check(g.Lan.PoolTable.Table.SeatOf(0)==1&&g.Lan.PoolTable.Table.SeatOf(client)==0,"First arrival takes nearby right seat without teleporting across the table");
            if(View.seated.Length!=2) yield break;
            int chips=g.Lan.Roster.Members[0].Chips;
            string[] args=Environment.GetCommandLineArgs(); int firstArg=Array.IndexOf(args,"-pool-first");
            g.Lan.PoolTable.FirstSideForCheck=firstArg>=0?int.Parse(args[firstArg+1]):0;
            g.Lan.Send(LanAction.PoolStart); g.Lan.Send(LanAction.PoolStart);
            yield return Wait(()=>View.phase==TablePhase.Playing);
            Check(View.phase==TablePhase.Playing&&g.Lan.Roster.Members[0].Chips==chips-10,"Opening starts once and debits organizer exactly 10");
            int active=0; foreach(var b in View.balls) if(b.active) active++;
            Check(active==15&&View.playPhase==EightBallPhase.Placement,"Fifteen numbered balls await deliberate placement of cue");
            SetStage("queue"); yield return Wait(()=>View.waiting.Length==1);
            Check(View.waiting.Length==1&&!View.IsParticipant(spectator)&&!View.IsSeated(spectator),"Late third person waits without joining current game");
            int rejected=g.Lan.RejectedActions; SetStage("reject"); yield return new WaitForSecondsRealtime(.35f);
            Check(View.playPhase==EightBallPhase.Placement&&g.Lan.RejectedActions>rejected,"Other side cannot place cue outside its turn");
            SetStage("badplace"); yield return new WaitForSecondsRealtime(.35f);
            Check(View.playPhase==EightBallPhase.Placement,"Cue cannot be placed beyond head string on break");
            SetStage("place"); yield return Wait(()=>View.playPhase==EightBallPhase.Aiming);
            Check(View.playPhase==EightBallPhase.Aiming,"Active side places legal cue through a network intent");
            g.Pool.ArrangeMatchCheck(1,true); SetStage("weak"); yield return Wait(()=>View.playPhase==EightBallPhase.BreakDecision,5);
            Check(View.playPhase==EightBallPhase.BreakDecision&&!View.foul,"Real weak contact produces insufficient break decision without invented foul");
            if(View.playPhase!=EightBallPhase.BreakDecision) yield break;
            SetStage("accept"); yield return Wait(()=>View.playPhase==EightBallPhase.Aiming);
            Check(View.playPhase==EightBallPhase.Aiming&&!View.headOnly,"Opponent accepts physical table in position");
            if(View.playPhase!=EightBallPhase.Aiming) yield break;
            ulong shooter=View.turn;
            int shooterSeat=g.Lan.PoolTable.Table.SeatOf(shooter);
            for(int ball=1;ball<=8;ball++) {
                g.Pool.ArrangeMatchCheck(ball); SetStage("call-"+ball);
                yield return Wait(()=>View.callBall==ball&&View.callPocket==3);
                Check(View.callBall==ball&&View.callPocket==3,"Announced target/pocket replicated for ball "+ball);
                if(View.callBall!=ball) yield break;
                SetStage("shoot-"+ball); yield return Wait(()=>g.Pool.Moving);
                Check(g.Pool.Moving,"Authoritative physical shot starts for ball "+ball);
                if(!g.Pool.Moving) yield break;
                if(ball==1) {
                    g.Lan.ActorFor(shooter).Fall(Vector3.back*.4f);
                    yield return new WaitForSecondsRealtime(.15f);
                    Check(!View.IsSeated(shooter)&&View.IsParticipant(shooter)&&g.Pool.Moving,"Falling releases seat but keeps participation and ongoing shot");
                }
                yield return Wait(()=>!g.Pool.Moving,8);
                Check(!g.Pool.Moving&&!g.Lan.PoolTable.Rules.OnTable(ball),"Actual pocket event settles and resolves ball "+ball);
                if(ball==1) {
                    Check(g.Lan.PoolTable.Rules.Groups[Array.IndexOf(View.participants,shooter)]==BallGroup.Solids&&View.turn==shooter,"Called pot assigns group and retains absent shooter's turn");
                    yield return Wait(()=>!g.Lan.ActorFor(shooter).Fallen,5);
                    g.Lan.ActorFor(shooter).Teleport(PoolRoomController.SeatPosition(shooterSeat)+Vector3.back*.5f,Quaternion.identity);
                    SetStage("return-"+shooter); yield return Wait(()=>View.IsSeated(shooter));
                    Check(View.IsSeated(shooter)&&View.absenceRemaining>=59.9f,"Recovered participant returns and cancels absence clock");
                    if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) {
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"01-table.png")); yield return new WaitForEndOfFrame();
                        var hud=g.GetComponent<DemoHUD>(); hud.ShowTableRules(); yield return new WaitForSecondsRealtime(.1f);
                        Check(g.Paused&&Time.timeScale==1&&!g.Input.PoolShoot.enabled,"Rules overlay blocks local shots while shared clock continues");
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"03-rules.png")); yield return new WaitForEndOfFrame();
                        hud.CloseTableRules();
                        Check(!g.Paused&&g.Input.PoolShoot.enabled,"Closing rules restores table input");
                    }
                }
                if(ball<8&&View.turn!=shooter) { Check(false,"Expected controlled legal pot to keep turn"); yield break; }
            }
            yield return Wait(()=>View.phase==TablePhase.Result);
            Check(View.phase==TablePhase.Result&&View.winner==Array.IndexOf(View.participants,shooter),"Full physical match ends with called legal eight and agreed winner ["+View.phase+", winner="+View.winner+", "+View.notice+"]");
            Check(g.Lan.Roster.Members[0].Chips==chips-10,"No duplicate opening debit or prize emission in non-bet match");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) { ScreenCapture.CaptureScreenshot(Path.Combine(folder,"02-result.png")); yield return new WaitForEndOfFrame(); }
            g.Lan.Send(LanAction.PoolLeave); yield return Wait(()=>View.organizer==client&&!View.IsSeated(0)&&!g.Pool.Active);
            Check(View.organizer==client&&!g.Pool.Active,"Organizer transfers and leaving restores local first-person controls");
            SetStage("close"); yield return Wait(()=>View.phase==TablePhase.Preparation);
            g.Player.Teleport(LanSession.SpawnPoint(0),Quaternion.identity);
            g.Lan.ActorFor(spectator).Teleport(PoolRoomController.SeatPosition(1)+Vector3.back*.6f,Quaternion.identity);
            SetStage("nextseat"); yield return Wait(()=>View.IsSeated(spectator));
            Check(View.IsSeated(spectator)&&View.seated.Length==2,"Waiting person takes next-round free seat after result closes");
            SetStage("again"); yield return Wait(()=>View.phase==TablePhase.Playing);
            Check(View.phase==TablePhase.Playing,"Next organizer can open a fresh match");
            SetStage("place2"); yield return Wait(()=>View.playPhase==EightBallPhase.Aiming);
            SetStage("break2"); yield return Wait(()=>g.Pool.Moving);
            var moved=new HashSet<int>(); float peakSpeed=0; double began=Now;
            while(g.Pool.Moving&&Now-began<19) {
                for(int i=0;i<g.Pool.Balls.Count;i++) { float speed=g.Pool.Balls[i].linearVelocity.magnitude; peakSpeed=Mathf.Max(peakSpeed,speed); if(i>0&&speed>.05f) moved.Add(i); }
                yield return null;
            }
            Check(!g.Pool.Moving&&Now-began<18.5&&peakSpeed<=PoolPhysics.MaxSpeed*1.03f&&moved.Count>=10,"Standard 15-ball rack break settles within speed/time limits ["+moved.Count+" object balls moved, "+peakSpeed.ToString("0.00")+" m/s]");
            SetStage("disconnect"); yield return Wait(()=>View.phase==TablePhase.Result);
            Check(View.phase==TablePhase.Result&&View.winner==Array.IndexOf(View.participants,client),"Disconnect resolves next match in favour of remaining side");
            SetStage("done"); yield return new WaitForSecondsRealtime(1);
        }
        private IEnumerator Client()
        {
            Check(g.Lan.Join(role=="spectator"?"Spectator":"Client","127.0.0.1",port),"Client starts real UDP connection");
            yield return Wait(()=>View!=null,15); Check(View!=null,"Client receives table snapshot"); if(View==null) yield break;
            double deadline=Now+90,seatedAt=Now; bool sawRolling=false,sawResult=false,sawSeated=false,passive=true,seatedFacing=true,wasSeated=false; int peak=0,seatedSamples=0; string acted="";
            while(Now<deadline&&g.Lan.Running) {
                string next=File.Exists(Path.Combine(folder,"step.ready"))?File.ReadAllText(Path.Combine(folder,"step.ready")):"";
                if(next!=stage) { stage=next; acted=""; }
                if(stage!=acted&&Act(stage)) acted=stage;
                if(stage=="abort") break;
                if(stage=="disconnect"&&role=="spectator") { g.Menu(); break; }
                if(View!=null) {
                    sawRolling|=View.playPhase==EightBallPhase.Rolling; sawResult|=View.phase==TablePhase.Result; sawSeated|=View.IsSeated(g.Lan.LocalId);
                    bool sitting=View.IsSeated(g.Lan.LocalId);
                    if(sitting&&!wasSeated) seatedAt=Now;
                    if(sitting&&Now-seatedAt>.15&&g.Lan.View.actors!=null) foreach(var a in g.Lan.View.actors) if(a.id==g.Lan.LocalId) { seatedSamples++; seatedFacing&=Mathf.Abs(Mathf.DeltaAngle(g.Player.transform.eulerAngles.y,a.yaw))<2; }
                    wasSeated=sitting;
                    peak=Math.Max(peak,View.shots); foreach(var b in g.Pool.Balls) if(b.gameObject.activeSelf) passive&=b.isKinematic;
                }
                if(stage=="done") break; yield return null;
            }
            Check(stage=="done"||stage=="disconnect"&&role=="spectator"&&!g.Started,"Scenario finishes or disconnected spectator returns to usable menu");
            Check(sawRolling&&sawResult&&peak>=9,"Client observes physical shots and full-match result");
            Check(passive,"Client ball bodies remain kinematic; host alone simulates");
            Check(g.Lan.FeedbackCount(LanFeedbackKind.PoolShot)>=9&&g.Lan.FeedbackCount(LanFeedbackKind.PoolPot)>=8,"Client receives shot and pocket audio events from authority");
            Check(sawSeated,"Client enters server-confirmed table camera");
            Check(seatedSamples>0&&seatedFacing,"Own seated replica faces the table instead of retaining free-look yaw");
        }
    }
}
