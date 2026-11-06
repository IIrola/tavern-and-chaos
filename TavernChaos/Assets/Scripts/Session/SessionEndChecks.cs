using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TavernChaos
{
    public sealed class SessionEndChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string role,scenario,version; public bool passed; public List<string> checks=new(),failures=new(),errors=new(); }
        private readonly Report report=new();
        private DemoGame g; private string folder,role,scenario; private ushort port;
        private double Now=>Time.realtimeSinceStartupAsDouble;
        private PoolRoomView Pool=>g.Lan.View?.pool;
        private bool Visual=>SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;
        private void Check(bool ok,string name) { (ok?report.checks:report.failures).Add(name); Debug.Log((ok?"PASS ":"FAIL ")+name); }
        private void Log(string value,string trace,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) report.errors.Add(value+"\n"+trace); }
        private void Signal(string name,string value="ready")=>File.WriteAllText(Path.Combine(folder,name+".ready"),value);
        private bool Has(string name)=>File.Exists(Path.Combine(folder,name+".ready"));
        private IEnumerator Wait(Func<bool> condition,float seconds=6) { double until=Now+seconds;while(Now<until&&!condition()) yield return null; }
        private IEnumerator Capture(string name)
        {
            if(!Visual) yield break;
            yield return null; yield return new WaitForEndOfFrame();
            string path=Path.Combine(folder,name+".png"); ScreenCapture.CaptureScreenshot(path);
            yield return Wait(()=>File.Exists(path),3); Check(File.Exists(path),"Capture completed for "+name); yield return null;
        }
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>(); Application.logMessageReceived+=Log;
            string Arg(string key,string fallback) { var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback; }
            role=Arg("-end-role","host");scenario=Arg("-end-case","time-cancel");folder=Arg("-evidencePath",Application.persistentDataPath);port=ushort.Parse(Arg("-end-port","7794"));Directory.CreateDirectory(folder);
            report.role=role;report.scenario=scenario;report.version=Application.version;yield return null;
            if(role=="host") yield return Host(); else if(role=="late") yield return Late(); else yield return Peer();
            Check(report.errors.Count==0,"No runtime errors in "+role);report.passed=report.failures.Count==0&&report.errors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+".json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=Log;
            if(role=="host") Signal("finish");g.Menu();yield return new WaitForSecondsRealtime(.2f);Application.Quit(report.passed?0:1);
        }
        private ulong Id(string name) { foreach(var a in g.Lan.View.actors) if(a.name==name) return a.id;return ulong.MaxValue; }
        private IEnumerator Host()
        {
            Check(g.Lan.Host("Host",port,20),"Host creates a twenty-minute room");
            if(Visual) yield return new WaitForEndOfFrame();Signal("host");
            yield return Wait(()=>(g.Lan.View?.actors?.Length??0)==3,20);
            Check(g.Lan.View?.actors?.Length==3,"Three independent participants connect before closure");if(g.Lan.View?.actors?.Length!=3)yield break;
            if(scenario=="time-cancel") for(int n=1;n<=5;n++) {
                Signal("stage","rejoin-"+n);yield return Wait(()=>Has("rejoined-"+n),10);
                Check(Has("rejoined-"+n),"Real departing connection returns with a new history entry "+n);
                if(!Has("rejoined-"+n))yield break;
            }
            ulong client=Id("Client");yield return Wait(()=>Id("Watcher"+(scenario=="time-cancel"?"5":"0"))!=ulong.MaxValue);
            var ledger=g.Lan.Roster.Ledger;
            g.Player.Teleport(PoolRoomController.SeatPosition(0)+Vector3.back*.6f,Quaternion.identity);
            g.Lan.ActorFor(client).Teleport(PoolRoomController.SeatPosition(1)+Vector3.back*.6f,Quaternion.identity);
            g.Lan.Send(LanAction.PoolSit);yield return Wait(()=>Pool?.IsSeated(0)==true);
            Signal("stage","sit");yield return Wait(()=>Pool?.IsSeated(client)==true);
            Check(Pool.seated.Length==2&&Pool.organizer==0,"Server confirms both seats and organizer");
            if(Pool.seated.Length!=2)yield break;
            uint old=Pool.proposal;g.Lan.Send(LanAction.PoolWager,wager:50,proposal:old);yield return Wait(()=>Pool.stake==50);
            g.Lan.Send(LanAction.PoolReady,proposal:Pool.proposal);Signal("stage","ready-"+Pool.proposal);
            yield return Wait(()=>Pool.ready.Length==2);Check(Pool.ready.Length==2,"Both sides confirm the same fifty-chip wager");if(Pool.ready.Length!=2)yield break;
            g.Lan.PoolTable.FirstSideForCheck=0;g.Lan.Send(LanAction.PoolStart,proposal:Pool.proposal);yield return Wait(()=>Pool.phase==TablePhase.Playing);
            Check(Pool.phase==TablePhase.Playing&&ledger.Balance(0)==940&&ledger.Balance(client)==950&&Pool.heldPot==100,"Opening commits fee and stakes into a real held pot");
            if(Pool.phase!=TablePhase.Playing)yield break;
            bool win=scenario!="time-cancel";
            if(win) Check(g.Lan.PoolTable.PrepareClosingEightForCheck(),"Controlled rule history prepares the eight; final outcome still requires a physical pot");
            else { g.Lan.Send(LanAction.PoolPlace,PoolPractice.Center+new Vector3(0,0,-1.8f));yield return Wait(()=>Pool.playPhase==EightBallPhase.Aiming);g.Pool.ArrangeMatchCheck(1); }
            if(scenario=="insolvency") {
                ledger.Spend("spend-host",0,ledger.Balance(0),"service");ledger.Spend("spend-client",client,ledger.Balance(client),"service");
                yield return new WaitForSecondsRealtime(.25f);
                Check(g.Lan.Lifecycle.Phase==RoomPhase.Open&&ledger.HasPending,"Empty wallets do not evict an active round with a pending prize");
            }
            g.Lan.Send(LanAction.PoolShoot,Vector3.right,ball:win?8:0,pocket:win?3:-1,power:.8f);
            yield return Wait(()=>g.Pool.Moving);Check(g.Pool.Moving,"Closing scenario starts an actual host-physics shot");if(!g.Pool.Moving)yield break;
            if(scenario=="time-cancel") { var b=g.Bottles[0];Check(b.Grab(g.Rival),"Released-bottle fixture uses existing authoritative object");b.Release(Vector3.up*3+Vector3.back*2); }
            int rejectedBefore=g.Lan.RejectedActions;
            if(scenario!="insolvency") {
                if(scenario=="time-cancel") {g.SetPause(true);g.Settings=true;Check(Time.timeScale==1,"Local pause and settings keep the shared deadline advancing");}
                g.Lan.ExpireForCheck();g.Lan.Send(LanAction.Interact);g.Lan.Send(LanAction.PoolStart,proposal:Pool.proposal);
                yield return Wait(()=>g.Lan.Lifecycle.Phase==RoomPhase.Draining);
                Check(g.Lan.Lifecycle.Phase==RoomPhase.Draining&&!g.Lan.Lifecycle.Accepting(Now),"Deadline closes admission and new actions while the shot continues");
                Signal("closing");Signal("stage","blocked");
                yield return Capture("01-draining");
                if(scenario=="time-cancel") {
                    yield return Wait(()=>!g.Pool.Moving,8);
                    Check(g.Lan.Lifecycle.Phase==RoomPhase.Draining&&g.Bottles[0].Flying,"Already-released bottle delays finalization after the table settles");
                }
            } else {
                yield return Wait(()=>Pool.settled,8);
                Check(Pool.settled&&ledger.Balance(0)==100&&ledger.Find(0).Wins==1,"Physical legal eight pays the pending pot exactly once");
                yield return new WaitForSecondsRealtime(.3f);
                Check(g.Lan.Lifecycle.Phase==RoomPhase.Open,"Recovered winnings prevent premature insolvency closure");
                ledger.Spend("spend-prize",0,100,"service");
                yield return Wait(()=>g.Lan.Lifecycle.Phase==RoomPhase.Ejecting);
                Signal("closing");Signal("stage","blocked");
                Check(g.Lan.Lifecycle.Phase==RoomPhase.Ejecting&&g.Lan.Ending.reason==RoomEndReason.Insolvency,"All unaffordable wallets start shared eviction after settlement");
                double started=g.Lan.Lifecycle.PhaseStarted;
                yield return new WaitForSecondsRealtime(2);
                Check(g.Lan.Lifecycle.Phase==RoomPhase.Ejecting,"Shared cinematic remains active before six seconds");
                yield return Capture("02-ejection");
                g.Rig.ReduceMotion=true;yield return new WaitForSecondsRealtime(.15f);
                var stage=GameObject.Find("Session ejection | visual only");
                bool gentle=stage&&stage.GetComponentsInChildren<Collider>().Length==0;
                if(stage) foreach(Transform t in stage.transform) if(t.name.StartsWith("Guest")) gentle&=Quaternion.Angle(t.localRotation,Quaternion.identity)<.1f;
                Check(gentle,"Reduced motion removes guest spins; cinematic has no physical colliders");
                yield return Capture("03-reduced-motion");
                yield return Wait(()=>g.Lan.Lifecycle.Phase==RoomPhase.Results,8);
                Check(Now-started>=RoomLifecycle.EjectionSeconds,"Eviction ends only after six authority seconds");
            }
            yield return Wait(()=>g.Lan.Ending?.phase==RoomPhase.Results,12);
            Check(g.Lan.Ending?.phase==RoomPhase.Results&&g.Lan.StatsPage?.final==true,"Every closure reaches a frozen final report instead of dropping to menu");
            if(g.Lan.Ending?.phase!=RoomPhase.Results)yield break;
            Check(ledger.Closed&&!ledger.HasPending&&ledger.AuditDelta==0&&ledger.Balance(0)==0&&ledger.Balance(client)==0,"Closing clears all balances and pots with balanced accounting");
            Check(!g.Player.Seated&&!g.Lan.ActorFor(client).Seated&&!g.Input.Move.enabled&&!g.Input.PoolShoot.enabled&&g.Input.Accept.enabled,"Final screen releases seats and routes input only to menus");
            var hostRow=Array.Find(g.Lan.StatsPage.players,a=>a.id==0);var clientRow=Array.Find(g.Lan.StatsPage.players,a=>a.id==client);
            long expectedHost=scenario=="time-cancel"?990:scenario=="time-win"?1040:0;
            long expectedClient=scenario=="time-cancel"?1000:scenario=="time-win"?950:0;
            Check(hostRow!=null&&hostRow.balance==expectedHost&&clientRow!=null&&clientRow.balance==expectedClient,"Frozen report retains exact balances before room funds are destroyed");
            Check(hostRow.wins==(win?1:0)&&hostRow.won==(win?"50":"0")&&clientRow.lost==(win?"50":"0"),"Report records a physical winner once, or no invented winner for cancelled play");
            if(scenario=="time-cancel") {
                Check(g.Lan.Ending.pages==2&&g.Lan.FinalPagesReceived==2,"Real reconnection history spans two immutable report pages");
                float volume=g.Audio.Volume;yield return MenuKey(Key.RightArrow);
                Check(g.Lan.StatsPage.page==1&&Mathf.Abs(g.Audio.Volume-volume)<.0001f,"Results navigation works through the real action map without changing hidden settings");
                Check(g.Lan.StatsPage.players.Length==2&&g.Lan.StatsPage.players[0].name.StartsWith("Watcher"),"Second history page contains earlier/returning participants");
                yield return Capture("05-history");g.Lan.RequestStats(0);
            }
            yield return Capture("04-results");
            if(Visual&&scenario=="time-cancel") {
                Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.3f);yield return Capture("07-results-1280x720");
                Screen.SetResolution(1920,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.3f);yield return Capture("08-results-wide");
                Screen.SetResolution(1440,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.3f);
            }
            yield return Wait(()=>Has("client-final")&&Has("watcher-final")&&Has("late"),10);
            Check(Has("client-final")&&Has("watcher-final"),"Both remote peers receive all final pages");
            Check(g.Lan.RejectedActions>rejectedBefore,"Authority rejects raw gameplay intents during closing and results");
            Check(Has("late")&&g.Lan.Roster.Members.Count==3,"Closing room rejects a late fourth participant without a grant");
            if(scenario=="time-win") {
                if(Visual) yield return MenuKey(Key.Enter); else yield return g.CreateRoomAfterEnding();
                yield return Wait(()=>g.Lan.IsHost&&g.Lan.View?.actors?.Length==1);
                Check(g.Lan.IsHost&&!g.Lan.BlocksPlay&&g.Lan.Roster.Members.Count==1&&g.Lan.Roster.Ledger.Balance(0)==1000&&!g.Paused,"Create-another-room action resets clock, grant, history, input and server");
                yield return Capture("06-new-room");
            } else g.Menu();
            Signal("host-left");yield return Wait(()=>Has("client-detached")&&Has("watcher-detached"),8);
            Check(Has("client-detached")&&Has("watcher-detached"),"Remote final results survive the host leaving");
            Signal("finish");yield return new WaitForSecondsRealtime(.4f);
        }
        private IEnumerator MenuKey(Key key)
        {
            var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard=InputSystem.AddDevice<Keyboard>();var maps=new[]{g.Input.Move.actionMap,g.Input.Point.actionMap,g.Input.Accept.actionMap};var previous=Array.ConvertAll(maps,m=>m.devices);
            foreach(var m in maps)m.devices=new InputDevice[]{keyboard};g.TestDriveInput=true;
            try { yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.4f); }
            finally {g.TestDriveInput=false;for(int i=0;i<maps.Length;i++)maps[i].devices=previous[i];InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;}
        }
        private IEnumerator Peer()
        {
            bool watcher=role=="watcher";int returns=0;Check(g.Lan.Join(watcher?"Watcher0":"Client","127.0.0.1",port),"Peer starts actual loopback connection");
            yield return Wait(()=>g.Lan.View!=null,20);if(g.Lan.View==null){Check(false,"Peer receives initial state");yield break;}
            string acted="";bool finalized=false,blocked=false;double deadline=Now+100;
            while(Now<deadline&&!Has("finish")) {
                string stage=Has("stage")?File.ReadAllText(Path.Combine(folder,"stage.ready")):"";
                if(watcher&&stage.StartsWith("rejoin-")&&stage!=acted) {
                    acted=stage;returns++;g.Menu();yield return new WaitForSecondsRealtime(.25f);
                    g.Lan.Join("Watcher"+returns,"127.0.0.1",port);yield return Wait(()=>g.Lan.View!=null&&g.Lan.Connected,10);
                    Check(g.Lan.View!=null&&g.Lan.Connected,"Watcher returns without stale room state "+returns);Signal("rejoined-"+returns);
                }
                if(!watcher&&stage=="sit"&&acted!=stage) {acted=stage;g.Lan.Send(LanAction.PoolSit);}
                if(!watcher&&stage.StartsWith("ready-")&&acted!=stage&&Pool!=null&&stage=="ready-"+Pool.proposal) {acted=stage;g.Lan.Send(LanAction.PoolReady,proposal:Pool.proposal);}
                if(stage=="blocked"&&g.Lan.BlocksPlay&&!blocked) {
                    blocked=true;g.Lan.Send(LanAction.Move,Vector3.forward);g.Lan.Send(LanAction.Interact);g.Lan.Send(LanAction.PoolSit);
                    Check(!g.Input.Move.enabled&&!g.Input.PoolShoot.enabled&&g.Input.Accept.enabled,"Closing peer cannot drive gameplay action maps");
                }
                if(!finalized&&g.Lan.Ending?.phase==RoomPhase.Results&&g.Lan.FinalPagesReceived==g.Lan.Ending.pages&&g.Lan.StatsPage?.final==true) {
                    finalized=true;Check(g.Lan.StatsPage.audit=="0","Peer receives authoritative final balanced report");
                    if(g.Lan.Ending.pages==2) {g.Lan.RequestStats(1);Check(g.Lan.StatsPage.page==1&&g.Lan.StatsPage.players.Length==2,"Second final page is cached on the remote peer");g.Lan.RequestStats(0);}
                    Signal(watcher?"watcher-final":"client-final");
                }
                if(finalized&&!g.Lan.Running) {
                    Check(g.Started&&g.Lan.BlocksPlay&&g.Lan.ResultsDisconnected&&g.Lan.StatsPage.final&&g.Lan.FinalPagesReceived==g.Lan.Ending.pages,"Host loss preserves complete received final history and usable result screen");
                    Signal(watcher?"watcher-detached":"client-detached");yield return Wait(()=>Has("finish"),10);
                    if(!watcher) {yield return MenuKey(Key.Escape);Check(!g.Started&&!g.Lan.BlocksPlay&&!g.Lan.Running,"Escape leaves offline results and clears personal room state");}
                    break;
                }
                yield return null;
            }
            Check(finalized&&blocked&&Has("finish"),"Peer completes closure and remains usable until explicit exit");
        }
        private IEnumerator Late()
        {
            yield return Wait(()=>Has("closing"),70);Check(Has("closing"),"Late entrant waits for actual closing phase");
            if(!Has("closing"))yield break;
            Check(g.Lan.Join("Too late","127.0.0.1",port),"Late entry request reaches the transport");
            yield return Wait(()=>!g.Lan.Running,12);
            Check(!g.Lan.Running&&!g.Started&&g.Lan.Status.Contains("terminando"),"Late entrant sees explicit room-ending rejection and usable menu");Signal("late");
        }
    }
}
