using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TavernChaos
{
    public sealed class PoolRoomChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string role,version; public bool passed; public List<string> checks=new(),failures=new(),errors=new(); }
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
            role=Arg("-pool-role","host"); folder=Arg("-evidencePath",Application.persistentDataPath); port=ushort.Parse(Arg("-pool-port","7792")); report.role=role; report.version=Application.version; Directory.CreateDirectory(folder);
            yield return null;
            if(role=="host") yield return Host(); else yield return Client();
            if(role=="host"&&report.failures.Count>0) { SetStage("abort"); yield return new WaitForSecondsRealtime(.3f); }
            Check(report.errors.Count==0,"No runtime errors in "+role); report.passed=report.failures.Count==0&&report.errors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+".json"),JsonUtility.ToJson(report,true)); Application.logMessageReceived-=Log;
            g.Menu(); yield return new WaitForSecondsRealtime(.2f); Application.Quit(report.passed?0:1);
        }
        private void SetStage(string value) { File.WriteAllText(Path.Combine(folder,"step.ready"),value); pendingHostStage=Act(value)?null:value; }
        private void Update() { if(role=="host"&&pendingHostStage!=null&&Act(pendingHostStage)) pendingHostStage=null; }
        private IEnumerator ConfirmWithKeyboard()
        {
            var background=InputSystem.settings.backgroundBehavior; var disabled=new List<InputDevice>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach(var device in InputSystem.devices) if(device.enabled&&(device is Keyboard||device is Mouse||device is Gamepad)) { disabled.Add(device); InputSystem.DisableDevice(device); }
            var keyboard=InputSystem.AddDevice<Keyboard>(); g.TestDriveInput=true;
            var maps=new[]{g.Input.Move.actionMap,g.Input.Point.actionMap,g.Input.Accept.actionMap};
            var previous=Array.ConvertAll(maps,map=>map.devices); foreach(var map in maps) map.devices=new InputDevice[]{keyboard};
            try {
                yield return null; InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter)); yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null; yield return new WaitForSecondsRealtime(.15f);
            } finally {
                g.TestDriveInput=false; for(int i=0;i<maps.Length;i++) maps[i].devices=previous[i];
                InputSystem.RemoveDevice(keyboard); foreach(var device in disabled) InputSystem.EnableDevice(device);
                InputSystem.settings.backgroundBehavior=background;
            }
        }
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
            if(value=="invalid"&&role=="client"&&View?.stake==50) { room.Send(LanAction.PoolWager,wager:99,proposal:View.proposal); room.Send(LanAction.PoolStart,proposal:View.proposal); return true; }
            if(value.StartsWith("ready-")&&role!="host"&&View?.phase==TablePhase.Preparation&&View.IsSeated(room.LocalId)&&value=="ready-"+View.proposal) { room.Send(LanAction.PoolReady,proposal:View.proposal); return true; }
            if(value.StartsWith("zero-")&&View?.organizer==room.LocalId&&View.phase==TablePhase.Preparation&&value=="zero-"+View.proposal) { room.Send(LanAction.PoolWager,wager:0,proposal:View.proposal); return true; }
            if(value=="again"&&View?.organizer==room.LocalId&&View.phase==TablePhase.Preparation&&View.ready.Length==2) { room.Send(LanAction.PoolStart,proposal:View.proposal); return true; }
            if(value=="stats") { room.RequestStats(); return true; }
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
            var ledger=g.Lan.Roster.Ledger; long chips=ledger.Balance(0); uint oldProposal=View.proposal;
            Check(ledger.Balance(client)==1000&&ledger.Balance(spectator)==0,"One grant per room/IP: returning loopback endpoint receives no duplicate grant");
            g.Lan.Send(LanAction.PoolWager,wager:50,proposal:oldProposal); yield return Wait(()=>View.stake==50&&View.proposal!=oldProposal);
            int setupRejected=g.Lan.RejectedActions;
            g.Lan.Send(LanAction.PoolReady,proposal:oldProposal); g.Lan.Send(LanAction.PoolStart,proposal:View.proposal); SetStage("invalid");
            yield return new WaitForSecondsRealtime(.4f);
            Check(View.phase==TablePhase.Preparation&&View.stake==50&&View.ready.Length==0&&ledger.Balance(0)==1000&&ledger.Balance(client)==1000&&g.Lan.RejectedActions>=setupRejected+4,"Stale consent, non-organizer wager and premature start cost nothing");
            yield return ConfirmWithKeyboard();
            Check(View.ready.Length==1&&View.phase==TablePhase.Preparation&&ledger.Balance(0)==chips,"Enter confirms the visible quote without starting or charging the other person");
            SetStage("ready-"+View.proposal); yield return Wait(()=>View.ready.Length==2);
            uint agreed=View.proposal; g.Lan.Send(LanAction.PoolWager,wager:25,proposal:agreed); yield return Wait(()=>View.stake==25);
            Check(View.proposal!=agreed&&View.ready.Length==0&&ledger.Balance(0)==1000&&ledger.Balance(client)==1000,"Changing an agreed wager clears both confirmations before any charge");
            g.Lan.Send(LanAction.PoolWager,wager:50,proposal:View.proposal); yield return Wait(()=>View.stake==50);
            yield return ConfirmWithKeyboard(); SetStage("ready-"+View.proposal); yield return Wait(()=>View.ready.Length==2);
            g.Lan.PoolTable.FailOpeningForCheck=true; uint failedProposal=View.proposal;
            yield return ConfirmWithKeyboard(); yield return Wait(()=>View.proposal!=failedProposal);
            Check(View.phase==TablePhase.Preparation&&View.ready.Length==0&&ledger.Balance(0)==1000&&ledger.Balance(client)==1000&&ledger.BarRevenue==0&&!ledger.HasPending&&ledger.AuditDelta==0,"Failed physical opening refunds both reservations and invalidates consent");
            yield return ConfirmWithKeyboard(); SetStage("ready-"+View.proposal); yield return Wait(()=>View.ready.Length==2);
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) { ScreenCapture.CaptureScreenshot(Path.Combine(folder,"00-confirmation.png")); yield return new WaitForEndOfFrame(); }
            string[] args=Environment.GetCommandLineArgs(); int firstArg=Array.IndexOf(args,"-pool-first");
            g.Lan.PoolTable.FirstSideForCheck=firstArg>=0?int.Parse(args[firstArg+1]):0;
            uint startProposal=View.proposal; yield return ConfirmWithKeyboard(); g.Lan.Send(LanAction.PoolStart,proposal:startProposal);
            yield return Wait(()=>View.phase==TablePhase.Playing);
            Check(View.phase==TablePhase.Playing&&ledger.Balance(0)==chips-60&&ledger.Balance(client)==950&&ledger.BarRevenue==10&&View.heldPot==100&&ledger.AuditDelta==0,"Opening commits once: organizer pays 10 plus 50, rival stakes 50, pot holds 100");
            if(View.phase!=TablePhase.Playing) yield break;
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
            yield return Wait(()=>View.settled);
            ulong loser=shooter==0?client:0;
            Check(View.settled&&View.heldPot==0&&View.pot==100&&ledger.Find(shooter).Wins==1&&ledger.Find(shooter).Won==50&&ledger.Find(loser).Lost==50&&ledger.Find(shooter).Falls==1&&ledger.AuditDelta==0,"Physical match pays 100 once, records net gain/loss 50 and one fall/victory");
            long hostAfter=ledger.Balance(0),clientAfter=ledger.Balance(client);
            g.Lan.Send(LanAction.PoolStart,proposal:startProposal); yield return new WaitForSecondsRealtime(.2f);
            Check(ledger.Balance(0)==hostAfter&&ledger.Balance(client)==clientAfter&&ledger.Find(shooter).Wins==1,"Late start cannot change a paid result or duplicate its winnings");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) { ScreenCapture.CaptureScreenshot(Path.Combine(folder,"02-result.png")); yield return new WaitForEndOfFrame(); }
            g.Lan.Send(LanAction.PoolLeave); yield return Wait(()=>View.organizer==client&&!View.IsSeated(0)&&!g.Pool.Active);
            Check(View.organizer==client&&!g.Pool.Active,"Organizer transfers and leaving restores local first-person controls");
            SetStage("close"); yield return Wait(()=>View.phase==TablePhase.Preparation);
            g.Player.Teleport(LanSession.SpawnPoint(0),Quaternion.identity);
            g.Lan.ActorFor(spectator).Teleport(PoolRoomController.SeatPosition(1)+Vector3.back*.6f,Quaternion.identity);
            SetStage("nextseat"); yield return Wait(()=>View.IsSeated(spectator));
            Check(View.IsSeated(spectator)&&View.seated.Length==2,"Waiting person takes next-round free seat after result closes");
            Check(View.ready.Length==0&&ledger.Balance(0)==hostAfter&&ledger.Balance(client)==clientAfter,"Changing seats and organizer clears consent without redistributing the paid pot");
            SetStage("zero-"+View.proposal); yield return Wait(()=>View.stake==0);
            Check(View.stake==0,"New organizer receives current proposal before changing the next wager to zero");
            if(View.stake!=0) yield break;
            SetStage("ready-"+View.proposal); yield return Wait(()=>View.ready.Length==2);
            Check(View.ready.Length==2,"Both next-round participants confirm the new zero-wager revision");
            if(View.ready.Length!=2) yield break;
            SetStage("again"); yield return Wait(()=>View.phase==TablePhase.Playing);
            Check(View.phase==TablePhase.Playing,"Next organizer can open a fresh match");
            Check(ledger.Balance(client)==clientAfter-10&&ledger.BarRevenue==20&&View.heldPot==0&&ledger.AuditDelta==0,"Zero wager still charges opening only to the new organizer");
            if(View.phase!=TablePhase.Playing) yield break;
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
            yield return Wait(()=>View.settled);
            Check(ledger.Find(client).Wins==(shooter==client?2:1)&&ledger.Find(spectator)!=null&&!ledger.Find(spectator).Connected&&!ledger.HasPending&&ledger.BarRevenue==20&&ledger.AuditDelta==0,"Disconnected history survives; zero-bet win creates no prize and all pots settle");
            SetStage("stats"); yield return Wait(()=>g.Lan.StatsPage?.players?.Length==3);
            Check(g.Lan.StatsPage?.players?.Length==3&&g.Lan.StatsPage.audit=="0","On-demand statistics include departed connection and balanced totals");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) {
                var hud=g.GetComponent<DemoHUD>(); hud.ShowStats(); yield return new WaitForSecondsRealtime(.2f);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"04-statistics.png")); yield return new WaitForEndOfFrame(); hud.CloseStats();
            }
            yield return new WaitForSecondsRealtime(.4f);
            SetStage("done"); yield return new WaitForSecondsRealtime(1);
        }
        private IEnumerator Client()
        {
            Check(g.Lan.Join(role=="spectator"?"Spectator":"Client","127.0.0.1",port),"Client starts real UDP connection");
            yield return Wait(()=>View!=null,15); Check(View!=null,"Client receives table snapshot"); if(View==null) yield break;
            double deadline=Now+100,seatedAt=Now; bool sawRolling=false,sawResult=false,sawSeated=false,passive=true,seatedFacing=true,wasSeated=false,sawPaid=false; int peak=0,seatedSamples=0; string acted="";
            while(Now<deadline&&g.Lan.Running) {
                string next=File.Exists(Path.Combine(folder,"step.ready"))?File.ReadAllText(Path.Combine(folder,"step.ready")):"";
                if(next!=stage) { stage=next; acted=""; }
                if(stage!=acted&&Act(stage)) acted=stage;
                if(stage=="abort") break;
                if(stage=="disconnect"&&role=="spectator") { g.Menu(); break; }
                if(View!=null) {
                    sawRolling|=View.playPhase==EightBallPhase.Rolling; sawResult|=View.phase==TablePhase.Result; sawSeated|=View.IsSeated(g.Lan.LocalId);
                    sawPaid|=View.settled&&View.pot==100&&View.payouts!=null&&Array.Exists(View.payouts,p=>p==100);
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
            Check(sawPaid,"Authoritative paid result reaches the client including exact pot/payout");
            if(role=="client") Check(g.Lan.StatsPage?.players?.Length==3&&g.Lan.StatsPage.audit=="0","Statistics page arrives on demand across UDP with departed history");
        }
    }
}
