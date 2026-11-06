using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TavernChaos
{
    // Opt-in native-process checks. Only positioning, wallet depletion and the room deadline are fixtures.
    public sealed class ArmRoomChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string role,scenario,version; public bool passed; public List<string> checks=new(),failures=new(),errors=new(); }
        private readonly Report report=new();
        private DemoGame g;private string role,scenario,folder;private ushort port;
        private double Now=>Time.realtimeSinceStartupAsDouble;
        private ArmRoomView View=>g.Lan.View?.arm;
        private bool Visual=>SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;
        private void Check(bool ok,string name){(ok?report.checks:report.failures).Add(name);Debug.Log((ok?"PASS ":"FAIL ")+name);}
        private void Log(string value,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(value+"\n"+trace);}
        private void Signal(string name,string value="ready")=>File.WriteAllText(Path.Combine(folder,name+".ready"),value);
        private bool Has(string name)=>File.Exists(Path.Combine(folder,name+".ready"));
        private string Stage=>Has("stage")?File.ReadAllText(Path.Combine(folder,"stage.ready")):"";
        private IEnumerator Wait(Func<bool> condition,float seconds=6){double until=Now+seconds;while(Now<until&&!condition())yield return null;}
        private IEnumerator Capture(string name)
        {
            if(!Visual)yield break;yield return null;yield return new WaitForEndOfFrame();
            string path=Path.Combine(folder,name+".png");ScreenCapture.CaptureScreenshot(path);yield return Wait(()=>File.Exists(path),3);Check(File.Exists(path),"Capture completes: "+name);
        }
        private IEnumerator CaptureDuel()
        {
            yield return Wait(()=>View?.phase==TablePhase.Playing&&g.Lan.EstimatedServerTime>=View.start+3,9);
            if(View?.phase!=TablePhase.Playing)yield break;
            yield return Capture("02-duel");
            g.Rig.ReduceMotion=true;yield return new WaitForSecondsRealtime(.2f);
            var grip=GameObject.Find("Joined hands");Check(grip&&Mathf.Abs(grip.transform.localScale.x-.25f)<.0001f,"Reduced motion restores the grip scale while the duel continues");
            yield return Capture("03-reduced-motion");g.Rig.ReduceMotion=false;
        }
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>();Application.logMessageReceived+=Log;
            string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
            role=Arg("-arm-role","host");scenario=Arg("-arm-case","draw");folder=Arg("-evidencePath",Application.persistentDataPath);port=ushort.Parse(Arg("-arm-port","7797"));Directory.CreateDirectory(folder);
            report.role=role;report.scenario=scenario;report.version=Application.version;yield return null;
            if(role=="host")yield return Host();else yield return Peer();
            if(role=="host")Signal("finish");
            Check(report.errors.Count==0,"No runtime errors in "+role);report.passed=report.failures.Count==0&&report.errors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+".json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=Log;
            yield return new WaitForSecondsRealtime(.3f);g.Menu();yield return null;Application.Quit(report.passed?0:1);
        }
        private IEnumerator KeyboardInput(Key key,Func<bool> until=null,float timeout=.15f)
        {
            var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard=InputSystem.AddDevice<Keyboard>();var maps=new[]{g.Input.Move.actionMap,g.Input.Point.actionMap,g.Input.Accept.actionMap,g.Input.ArmPush.actionMap};
            var previous=Array.ConvertAll(maps,m=>m.devices);foreach(var m in maps)m.devices=new InputDevice[]{keyboard};g.TestDriveInput=true;
            try{yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
                if(until!=null)yield return Wait(until,timeout);else yield return new WaitForSecondsRealtime(timeout);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.12f);
            }finally{g.TestDriveInput=false;for(int i=0;i<maps.Length;i++)maps[i].devices=previous[i];InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;}
        }
        private IEnumerator Host()
        {
            Check(g.Lan.Host("Host",port),"Host opens an actual UDP room");yield return null;Signal("host");
            yield return Wait(()=>g.Lan.View?.actors?.Length==3,18);Check(g.Lan.View?.actors?.Length==3,"Three independent processes connect");if(g.Lan.View?.actors?.Length!=3)yield break;
            ulong client=Array.Find(g.Lan.View.actors,a=>a.name=="Client").id,spectator=Array.Find(g.Lan.View.actors,a=>a.name=="Spectator").id;
            var ledger=g.Lan.Roster.Ledger;
            if(scenario=="drunk"){
                g.Lan.ActorFor(client).Teleport(TavernWorld.BarPoint,Quaternion.identity);Signal("stage","drink");yield return Wait(()=>g.Lan.ActorFor(client).Ebriety==1);
                Check(g.Lan.ActorFor(client).Ebriety==1&&ledger.Balance(client)==980&&ledger.Find(client).Drinks==1,"Actual bar interaction charges twenty and adds independent intoxication");
            }
            g.Player.Teleport(ArmRoomController.SeatPosition(0)+Vector3.back*.6f,Quaternion.identity);
            g.Lan.ActorFor(client).Teleport(ArmRoomController.SeatPosition(1)+Vector3.back*.6f,Quaternion.identity);
            g.Lan.ActorFor(spectator).Teleport(ArmRoomController.SeatPosition(0)+Vector3.back*1.2f,Quaternion.identity);
            g.Lan.Send(LanAction.ArmSit);yield return Wait(()=>View?.IsSeated(0)==true);Signal("stage","sit");yield return Wait(()=>View?.seated.Length==2);
            Check(View?.seated.Length==2&&g.Input.ArmPush.enabled&&!g.Input.Move.enabled&&!g.Pool.Active,"Confirmed arm seats select their camera and input map");if(View?.seated.Length!=2)yield break;
            Check(View.SeatOf(0)==0&&View.SeatOf(client)==1,"Each person takes the nearby seat");
            uint old=View.proposal;g.Lan.Send(LanAction.ArmWager,wager:25,proposal:old);yield return Wait(()=>View.stake==25&&View.proposal!=old);
            int rejected=g.Lan.RejectedActions;g.Lan.Send(LanAction.ArmReady,proposal:old);g.Lan.Send(LanAction.ArmStart,proposal:View.proposal);yield return new WaitForSecondsRealtime(.2f);
            Check(View.phase==TablePhase.Preparation&&View.ready.Length==0&&ledger.BarRevenue==(scenario=="drunk"?20:0)&&g.Lan.RejectedActions>=rejected+2,"Stale consent and premature start never charge either participant");
            yield return KeyboardInput(Key.RightArrow);Check(View.efforts[0]==2,"Arrow input changes the visible effort");yield return KeyboardInput(Key.LeftArrow);
            yield return KeyboardInput(Key.M);Check(View.modes[0]==1,"Mode key selects rhythm");yield return KeyboardInput(Key.M);
            var hud=g.GetComponent<DemoHUD>();hud.ShowArmRules();yield return null;Check(g.Paused&&Time.timeScale==1&&!g.Input.ArmPush.enabled&&g.Input.Accept.enabled,"Rules block only local input while shared simulation continues");yield return Capture("01-rules");hud.CloseTableRules();
            Check(!g.Paused&&g.Input.ArmPush.enabled,"Returning from rules restores arm controls");
            Check(View.drunk[1]==(scenario=="drunk")&&ArmRules.CostFor(View.efforts[1],View.drunk[1])==(scenario=="drunk"?24:12),"Quote exposes normal or doubled resistance cost before consent");
            yield return KeyboardInput(Key.Enter);Check(View.ready.Length==1&&View.phase==TablePhase.Preparation,"Enter confirms the displayed quote without starting the duel");
            Signal("stage","ready-"+View.proposal);yield return Wait(()=>View.ready.Length==2);Check(View.ready.Length==2,"Both participants accept the current revision");yield return Capture("00-confirmation");
            yield return KeyboardInput(Key.Enter);yield return Wait(()=>View.phase==TablePhase.Playing);
            Check(View.phase==TablePhase.Playing&&View.gamePhase==ArmPhase.Countdown&&View.heldPot==50&&ledger.Balance(0)==970&&ledger.Balance(client)==(scenario=="drunk"?955:975)&&ledger.AuditDelta==0,"Start charges opening five once, reserves both stakes and starts a three-second countdown");if(View.phase!=TablePhase.Playing)yield break;
            Signal("stage","queue");yield return Wait(()=>View.waiting.Length==1);Check(View.waiting.Length==1&&ledger.Balance(spectator)==0,"Spectator waits without a grant or a charge");
            if(scenario=="draw"){
                g.Lan.Send(LanAction.ArmLeave);yield return Wait(()=>!View.IsSeated(0));Check(View.IsParticipant(0)&&View.phase==TablePhase.Playing,"Leaving keeps participation and the duel clock");
                g.Player.Teleport(PoolRoomController.SeatPosition(0)+Vector3.back*.5f,Quaternion.identity);g.Lan.Send(LanAction.PoolSit);yield return new WaitForSecondsRealtime(.15f);
                Check(!g.Player.Seated&&!g.Lan.View.pool.IsSeated(0),"Active participant cannot enter a second game after leaving their seat");
                g.Player.Teleport(ArmRoomController.SeatPosition(0)+Vector3.back*.6f,Quaternion.identity);g.Lan.Send(LanAction.ArmSit);yield return Wait(()=>View.IsSeated(0));Check(View.IsSeated(0),"Participant can return to their ongoing duel");
            }
            Signal("stage","play");
            if(Visual&&scenario!="closing"&&scenario!="disconnect")StartCoroutine(CaptureDuel());
            if(scenario=="closing"){
                ledger.Spend("fixture/deplete-host",0,ledger.Balance(0),"service");ledger.Spend("fixture/deplete-client",client,ledger.Balance(client),"service");
                yield return new WaitForSecondsRealtime(.25f);Check(g.Lan.Lifecycle.Phase==RoomPhase.Open&&ledger.HasPending,"An active arm duel and pending pot prevent insolvency eviction");
                yield return Wait(()=>Now>=View.start+.23,5);g.Lan.Send(LanAction.ArmHold,run:true);yield return Wait(()=>g.Lan.ArmTable.Rules.HasPending,1);
                Check(g.Lan.ArmTable.Rules.HasPending,"Authority has admitted an impulse before closure");g.Lan.ExpireForCheck();g.Lan.Send(LanAction.ArmTap);
                yield return Wait(()=>g.Lan.Ending?.phase==RoomPhase.Results,5);Check(g.Lan.Ending?.phase==RoomPhase.Results&&ledger.Closed&&!ledger.HasPending&&ledger.AuditDelta==0,"Deadline resolves the admitted impulse and cancels unfinished play before final accounting");
                var rows=g.Lan.StatsPage?.players;var h=rows==null?null:Array.Find(rows,a=>a.id==0);var c=rows==null?null:Array.Find(rows,a=>a.id==client);
                Check(h?.balance==25&&c?.balance==25&&h.wins==0&&c.wins==0&&h.draws==0&&ledger.BarRevenue==1950,"Cancelled duel returns both stakes, keeps service fees and invents no winner or draw");
                yield return Capture("03-final-results");Signal("stage","final");yield return Wait(()=>Has("client-final")&&Has("spectator-final"),7);Check(Has("client-final")&&Has("spectator-final"),"Both remote peers receive the immutable final report");
            }else{
                if(scenario=="disconnect"){Signal("stage","disconnect");yield return Wait(()=>Has("client-detached"));}
                else yield return KeyboardInput(Key.Space,()=>View.phase==TablePhase.Result,22);
                yield return Wait(()=>View.phase==TablePhase.Result&&View.settled,5);
                Check(View.phase==TablePhase.Result&&View.settled&&View.heldPot==0&&ledger.AuditDelta==0,"Authoritative duel reaches a paid result with balanced accounting");
                if(scenario=="draw"){
                    File.WriteAllText(Path.Combine(folder,"result-state.json"),JsonUtility.ToJson(View,true));
                    Check(View.winner==-1&&View.position==0&&View.pulses[0]==20&&View.pulses[1]==20,"Twenty real hold/rhythm pulses tie [winner="+View.winner+", position="+View.position+", pulses="+View.pulses[0]+"/"+View.pulses[1]+"]");
                    Check(ledger.Balance(0)==995&&ledger.Balance(client)==1000&&ledger.Find(0).Draws==1&&ledger.Find(client).Draws==1,"Draw returns each stake and records one draw [balances="+ledger.Balance(0)+"/"+ledger.Balance(client)+", draws="+ledger.Find(0).Draws+"/"+ledger.Find(client).Draws+"]");
                }
                else Check(View.winner==0&&ledger.Balance(0)==1020&&ledger.Find(0).Wins==1&&ledger.Find(0).Won==25&&ledger.Find(client).Lost==25,"Winning side receives fifty with net gain/loss twenty-five exactly once");
                if(scenario=="drunk")Check(ledger.Balance(client)==955&&View.pulses[0]==20&&View.drunk[1]&&g.Lan.ActorFor(client).EffectRemaining==0,"Duel still uses intoxication after the eight-second cocktail effect expires");
                long paid=ledger.Balance(0);g.Lan.Send(LanAction.ArmStart,proposal:View.proposal);g.Lan.Send(LanAction.ArmTap);yield return new WaitForSecondsRealtime(.2f);Check(ledger.Balance(0)==paid,"Late input cannot duplicate a paid result");
                yield return Capture("04-result");
                if(Visual&&scenario=="draw"){
                    Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.3f);yield return Capture("05-result-1280x720");
                    Screen.SetResolution(1920,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.3f);yield return Capture("06-result-wide");
                    Screen.SetResolution(1440,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.3f);
                }
                Signal("stage","result");yield return Wait(()=>Has("spectator-result")&&(scenario=="disconnect"||Has("client-result")),6);
                if(scenario=="disconnect"){
                    g.Lan.Send(LanAction.ArmClose);yield return Wait(()=>View.phase==TablePhase.Preparation);
                    g.Lan.ActorFor(spectator).Teleport(ArmRoomController.SeatPosition(1)+Vector3.back*.6f,Quaternion.identity);
                    Signal("stage","nextseat");yield return Wait(()=>View.IsSeated(spectator));Check(View.IsSeated(spectator)&&View.ready.Length==0,"Waiting spectator takes the disconnected player's nearby free seat after the result closes");
                }
            }
            Signal("finish");yield return new WaitForSecondsRealtime(.4f);
        }
        private IEnumerator Peer()
        {
            bool spectator=role=="spectator";Check(g.Lan.Join(spectator?"Spectator":"Client","127.0.0.1",port),"Peer starts real connection");yield return Wait(()=>View!=null,18);Check(View!=null,"Arm snapshot arrives over UDP");if(View==null)yield break;
            string acted="";int beat=-1;double heartbeat=0,until=Now+85;bool sawPlaying=false,sawPaid=false,sawDrunk=false;
            while(Now<until&&!Has("finish")){
                string stage=Stage;
                if(stage!=acted){bool done=true;
                    if(stage=="drink"&&!spectator)yield return BarFlowChecks.ServeAndDrink(g,0);
                    else if(stage=="sit"&&!spectator)g.Lan.Send(LanAction.ArmSit);
                    else if(stage.StartsWith("ready-")&&!spectator){done=stage=="ready-"+View.proposal&&View.IsSeated(g.Lan.LocalId);if(done){g.Lan.Send(LanAction.ArmMode,choice:scenario=="draw"?1:0);g.Lan.Send(LanAction.ArmReady,proposal:View.proposal);}}
                    else if(stage=="queue"&&spectator)g.Lan.Send(LanAction.ArmSit);
                    else if(stage=="disconnect"&&!spectator){g.Menu();Signal("client-detached");yield return Wait(()=>Has("finish"),10);yield break;}
                    else if(stage=="nextseat"&&spectator)g.Lan.Send(LanAction.ArmSit);
                    else if(stage=="result"){done=View.settled;if(done){Check(View.receipt?.pot==50&&View.receipt.players.Length==2,"Exact paid receipt reaches peer");Signal(role+"-result");}}
                    else if(stage=="final"){done=g.Lan.StatsPage?.final==true;if(done){Check(g.Lan.BlocksPlay&&!g.Input.ArmPush.enabled&&g.Input.Accept.enabled,"Final report blocks arm input and enables menu navigation");Signal(role+"-final");}}
                    if(done)acted=stage;
                }
                sawPlaying|=View.phase==TablePhase.Playing;sawPaid|=View.settled;sawDrunk|=View.drunk[1];
                if(!spectator&&stage=="play"&&View.phase==TablePhase.Playing&&scenario!="closing"){
                    if(scenario=="draw"){
                        double center=View.start+ArmRules.Period*(View.beat+.5);double delta=g.Lan.EstimatedServerTime-center;
                        if(View.beat!=beat&&delta>=-.025&&delta<.1){beat=View.beat;g.Lan.Send(LanAction.ArmTap);}
                    }else if(Now>=heartbeat){heartbeat=Now+.08;g.Lan.Send(LanAction.ArmHold,run:true);}
                }
                yield return null;
            }
            Check(Has("finish")&&sawPlaying,"Peer observes the active duel through completion");
            if(scenario!="closing")Check(sawPaid,"Peer receives the settled result");if(scenario=="drunk")Check(sawDrunk,"Independent intoxication modifier is replicated");
        }
    }
}
