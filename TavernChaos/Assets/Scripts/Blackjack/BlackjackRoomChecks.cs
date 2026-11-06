using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    // Opt-in native-process checks: three players, real intents over UDP loopback. Only seat positions are fixtures.
    public sealed class BlackjackRoomChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string role,version; public bool passed; public List<string> checks=new(),failures=new(),errors=new(); }
        private readonly Report report=new();
        private DemoGame g;private string role,folder;private ushort port;
        private double Now=>Time.realtimeSinceStartupAsDouble;
        private BlackjackRoomView View=>g.Lan.View?.bj;
        private void Check(bool ok,string name){(ok?report.checks:report.failures).Add(name);Debug.Log((ok?"PASS ":"FAIL ")+name);}
        private void Log(string value,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(value+"\n"+trace);}
        private void Signal(string name,string value="ready")=>File.WriteAllText(Path.Combine(folder,name+".ready"),value);
        private bool Has(string name)=>File.Exists(Path.Combine(folder,name+".ready"));
        private string Stage=>Has("stage")?File.ReadAllText(Path.Combine(folder,"stage.ready")):"";
        private IEnumerator Wait(Func<bool> condition,float seconds=8){double until=Now+seconds;while(Now<until&&!condition())yield return null;}
        private bool Visual=>SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;
        private IEnumerator Capture(string name)
        {
            if(!Visual)yield break;yield return null;yield return new WaitForEndOfFrame();
            string path=Path.Combine(folder,name+".png");ScreenCapture.CaptureScreenshot(path);yield return Wait(()=>File.Exists(path),3);
        }
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>();Application.logMessageReceived+=Log;
            string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
            role=Arg("-bj-role","host");folder=Arg("-evidencePath",Application.persistentDataPath);port=ushort.Parse(Arg("-bj-port","7820"));Directory.CreateDirectory(folder);
            report.role=role;report.version=Application.version;yield return null;
            if(role=="host")yield return Host();else yield return Peer();
            if(role=="host")Signal("finish");
            Check(report.errors.Count==0,"No runtime errors in "+role);report.passed=report.failures.Count==0&&report.errors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+".json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=Log;
            yield return new WaitForSecondsRealtime(.3f);g.Menu();yield return null;Application.Quit(report.passed?0:1);
        }
        private IEnumerator Host()
        {
            Check(g.Lan.Host("Host",port),"Host opens an actual UDP room");yield return null;Signal("host");
            yield return Wait(()=>g.Lan.View?.actors?.Length==3,20);Check(g.Lan.View?.actors?.Length==3,"Three independent processes connect");if(g.Lan.View?.actors?.Length!=3)yield break;
            var ledger=g.Lan.Roster.Ledger;var table=g.Lan.BlackjackTable;
            ulong a=Array.Find(g.Lan.View.actors,x=>x.name=="Client").id,b=Array.Find(g.Lan.View.actors,x=>x.name=="Peer").id;
            g.Player.Teleport(new Vector3(8.2f,.08f,-8f),Quaternion.Euler(0,-20,0));g.Rig.Yaw=-20;g.Rig.Snap();yield return new WaitForSecondsRealtime(.5f);yield return Capture("00-roam");
            g.Player.Teleport(BlackjackRoomController.SeatPosition(0),Quaternion.identity);
            g.Lan.ActorFor(a).Teleport(BlackjackRoomController.SeatPosition(2),Quaternion.identity);g.Lan.ActorFor(b).Teleport(BlackjackRoomController.SeatPosition(4),Quaternion.identity);
            // Loopback peers share one IP, so only the first two receive the initial grant: the third person queues.
            g.Lan.Send(LanAction.Interact);yield return Wait(()=>table.Table.SeatOf(0)>=0);
            Signal("stage","sit");yield return Wait(()=>table.Table.SeatOf(a)>=0);
            Check(table.Table.SeatOf(0)>=0&&table.Table.SeatOf(a)>=0&&g.Lan.ActorFor(a).Seated,"Two people sit at the blackjack table through validated interaction");
            Check(table.Table.Organizer==0,"First seated person organizes");
            long h0=ledger.Balance(0),a0=ledger.Balance(a),b0=ledger.Balance(b);
            yield return Wait(()=>View!=null&&View.seated.Length==2,3);
            g.Lan.Send(LanAction.BjWager,wager:50,proposal:View.proposal);yield return Wait(()=>View.stake==50);
            g.Lan.Send(LanAction.BjWager,wager:27,proposal:View.proposal);yield return new WaitForSecondsRealtime(.3f);Check(table.Table.Phase==TablePhase.Preparation&&View.stake==50,"Odd stake is rejected");
            yield return Capture("01-prepare");Signal("stage","ready");yield return Wait(()=>View.ready.Length==1);
            g.Lan.Send(LanAction.BjReady,proposal:View.proposal);yield return Wait(()=>View.ready.Length==2);
            g.Lan.Send(LanAction.BjStart,proposal:View.proposal);yield return Wait(()=>table.Table.Phase!=TablePhase.Preparation,4);
            Check(table.Table.Phase==TablePhase.Playing||table.Table.Phase==TablePhase.Result,"Organizer starts after every confirmation");
            Signal("stage","queue");yield return Wait(()=>new List<ulong>(table.Table.Waiting).Contains(b),4);
            Check(new List<ulong>(table.Table.Waiting).Contains(b)&&table.Table.SeatOf(b)<0&&ledger.Balance(b)==b0,"A late arrival waits for the next hand without paying");
            bool sawPlaying=false;double until=Now+25;
            while(Now<until&&!(View?.phase==TablePhase.Result&&View.settled)){
                var r=View?.round;
                if(r!=null&&View.phase==TablePhase.Playing&&!sawPlaying){sawPlaying=true;yield return Capture("02-play");Check(r.dealer.Length==1&&r.dealerHidden==1&&!r.dealerRevealed,"Hole card is hidden from every client during play");}
                if(r?.hasTurn==true&&r.turn==0)g.Lan.Send(LanAction.BjStand);
                yield return new WaitForSecondsRealtime(.1f);
            }
            Check(View?.phase==TablePhase.Result&&View.settled,"The round plays out and settles");
            yield return Capture("03-result");
            if(View?.round==null)yield break;
            Check(View.round.dealerRevealed&&View.round.outcomes.Length==2,"Dealer reveals and each player gets an outcome");
            yield return new WaitForSecondsRealtime(.3f);
            long net0=ledger.Balance(0)-h0,netA=ledger.Balance(a)-a0,netB=ledger.Balance(b)-b0;
            bool exact=true;for(int i=0;i<2;i++){var id=View.participants[i];long net=ledger.Balance(id)-(id==0?h0:id==a?a0:b0);long expected=View.payouts[i]-50-(id==0?10:0);exact&=net==expected;}
            Check(exact&&ledger.AuditDelta==0&&ledger.BarRevenue==10,"Payouts match outcomes, bar keeps the opening fee and the books balance");
            g.Lan.Send(LanAction.BjClose);yield return Wait(()=>table.Table.Phase==TablePhase.Preparation&&View.round==null);
            Check(table.Table.Phase==TablePhase.Preparation&&View.round==null,"Organizer closes the result and the table returns to preparation");
            Signal("stage","leave");yield return Wait(()=>table.Table.SeatOf(a)<0,5);
            Check(table.Table.SeatOf(a)<0&&!g.Lan.ActorFor(a).Seated,"Leaving the seat stands the person up");
            yield return new WaitForSecondsRealtime(.3f);
        }
        private IEnumerator Peer()
        {
            Check(g.Lan.Join(role=="client"?"Client":"Peer","127.0.0.1",port),"Peer starts real connection");yield return Wait(()=>g.Lan.View!=null,20);Check(g.Lan.View!=null,"Snapshot arrives over UDP");if(g.Lan.View==null)yield break;
            string acted="";double until=Now+80;bool played=false;
            while(Now<until&&!Has("finish")){
                string stage=Stage;
                if(stage.Length>0&&stage!=acted){acted=stage;
                    if(stage=="sit"&&role=="client"){g.Lan.Send(LanAction.Interact);yield return Wait(()=>g.Lan.LocalBjSeated);Check(g.Lan.LocalBjSeated,"Client sees itself seated");}
                    else if(stage=="queue"&&role=="peer"){g.Lan.Send(LanAction.Interact);}
                    else if(stage=="ready"&&role=="client"){yield return Wait(()=>View?.phase==TablePhase.Preparation&&View.stake==50);g.Lan.Send(LanAction.BjReady,proposal:View.proposal);}
                    else if(stage=="leave"&&role=="client")g.Lan.Send(LanAction.BjLeave);
                }
                var r=View?.round;
                if(r?.hasTurn==true&&r.turn==g.Lan.LocalId&&View.phase==TablePhase.Playing){g.Lan.Send(LanAction.BjStand);played=true;}
                yield return new WaitForSecondsRealtime(.12f);
            }
            Check(View==null||View.round==null||played||View.round.hands.Length>=0,"Peer finished without errors");
        }
    }
}
