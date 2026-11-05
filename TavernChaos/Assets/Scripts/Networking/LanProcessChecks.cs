using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    // Separate built-player processes connect over UTP. No in-process transport mocks.
    public sealed class LanProcessChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string role,unity; public bool passed; public List<string> checks=new(),failures=new(),runtimeErrors=new();
            public int snapshots,maxParticipants,rejected; public long bytesReceived;
        }
        private readonly Report report=new();
        private DemoGame g; private string folder,role; private ushort port; private int index;
        private static double Now=>Time.realtimeSinceStartupAsDouble;
        private void Check(bool ok,string name) { (ok?report.checks:report.failures).Add(name); Debug.Log((ok?"PASS: ":"FAIL: ")+name); }
        private void Log(string text,string trace,LogType type) { if(type==LogType.Exception||type==LogType.Assert||type==LogType.Error) report.runtimeErrors.Add(text+"\n"+trace); }
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>(); Application.logMessageReceived+=Log;
            string[] args=Environment.GetCommandLineArgs(); string Arg(string key,string fallback) { int p=Array.IndexOf(args,key); return p>=0&&p+1<args.Length?args[p+1]:fallback; }
            role=Arg("-lan-role","host"); folder=Arg("-evidencePath",Path.Combine(Application.persistentDataPath,"lan-checks")); port=ushort.Parse(Arg("-lan-port","7789")); index=int.Parse(Arg("-lan-index","0"));
            Directory.CreateDirectory(folder); report.role=role+index; report.unity=Application.unityVersion;
            yield return null;
            if(role=="host") yield return Host(); else if(role=="overflow") yield return Overflow(); else yield return Client();
            report.snapshots=g.Lan.SnapshotsReceived; report.bytesReceived=g.Lan.BytesReceived; report.rejected=g.Lan.RejectedActions;
            Check(report.runtimeErrors.Count==0,"No runtime errors or exceptions in this process");
            report.passed=report.failures.Count==0&&report.runtimeErrors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+index+".json"),JsonUtility.ToJson(report,true));
            g.Menu(); Application.logMessageReceived-=Log; Debug.Log("LAN_CHECK_DONE: "+report.passed); yield return new WaitForSecondsRealtime(.3f); Application.Quit(report.passed?0:1);
        }
        private IEnumerator Host()
        {
            RosterChecks();
            bool visual=SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;
            if(visual) {
                g.GetComponent<DemoHUD>().ShowLanPanelForCheck(true); yield return new WaitForSecondsRealtime(.3f);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"01-lan-lobby.png")); yield return new WaitForEndOfFrame();
                g.GetComponent<DemoHUD>().ShowLanPanelForCheck(false);
            }
            Check(g.Lan.Host("Anfitrión",port),"Host starts with official NGO/UTP transport");
            double deadline=Now+25;
            while(Now<deadline&&(g.Lan.View?.actors?.Length??0)<6) yield return null;
            Check((g.Lan.View?.actors?.Length??0)==6,"Six independent player processes share one authoritative room");
            if((g.Lan.View?.actors?.Length??0)<6) yield break;
            File.WriteAllText(Path.Combine(folder,"all-six.ready"),"ready");
            if(visual) {
                yield return new WaitForSecondsRealtime(.2f);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"02-six-players.png")); yield return new WaitForEndOfFrame();
                g.SetPause(true); g.GetComponent<DemoHUD>().ShowRosterForCheck(); yield return new WaitForSecondsRealtime(.2f);
                Check(Cursor.visible&&Cursor.lockState==CursorLockMode.None&&Time.timeScale==1,"Vote roster frees the cursor while the shared simulation keeps running");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"03-vote-roster.png")); yield return new WaitForEndOfFrame(); g.SetPause(false);
            }
            var initial=new Dictionary<ulong,Vector3>(); foreach(var a in g.Lan.View.actors) initial[a.id]=a.position;
            bool moved=false,fell=false,recovered=false,held=false,disconnected=false,rejoined=false,notDuplicate=true;
            double began=Now;
            while(Now-began<28) {
                var view=g.Lan.View;
                if(view?.actors!=null) {
                    report.maxParticipants=Math.Max(report.maxParticipants,view.actors.Length);
                    var owned=new HashSet<int>();
                    foreach(var a in view.actors) {
                        if(initial.TryGetValue(a.id,out var p)&&Vector3.Distance(p,a.position)>.2f) moved=true;
                        if(a.held>=0) { held=true; if(!owned.Add(a.held)) notDuplicate=false; }
                    }
                    if(view.rival.fallen) fell=true; else if(fell) recovered=true;
                    if(view.actors.Length<6&&Now-began>4) disconnected=true;
                    if(disconnected&&view.actors.Length==6) rejoined=true;
                }
                yield return null;
            }
            Check(report.maxParticipants==6,"Room capacity never exceeds six, including host");
            Check(moved,"Client movement is applied by host simulation"); Check(fell&&recovered,"Client punch knocks down and recovers the authoritative NPC");
            Check(held&&notDuplicate,"Host observes bottle possession without duplicate owners");
            Check(disconnected&&rejoined,"Disconnect releases resources and the room accepts a return");
            g.Menu(); yield return new WaitForSecondsRealtime(.5f);
            Check(!g.Lan.Running,"Host can close the room without leaving the local game stuck");
        }
        private IEnumerator Overflow()
        {
            g.Lan.Join("Séptimo", "127.0.0.1",port); double until=Now+12;
            while(Now<until&&g.Lan.Running) yield return null;
            Check(!g.Lan.Connected&&g.Lan.Status.Contains("llena"),"A seventh participant receives a clear full-room rejection");
        }
        private LanActorState Self()
        {
            if(g.Lan.View?.actors!=null) foreach(var a in g.Lan.View.actors) if(a.id==g.Lan.LocalId) return a; return null;
        }
        private IEnumerator Drive(Vector3 target,float timeout=6)
        {
            double until=Now+timeout;
            while(Now<until) {
                var self=Self(); if(self==null) { yield return null; continue; }
                Vector3 delta=target-self.position; delta.y=0; if(delta.magnitude<.28f) break;
                g.Lan.LocalMove(delta.normalized,false,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg); yield return null;
            }
            g.Lan.Send(LanAction.Stop); yield return new WaitForSecondsRealtime(.2f);
        }
        private IEnumerator Client()
        {
            Check(g.Lan.Join("Peer"+index,"127.0.0.1",port),"Client starts a direct LAN connection");
            double until=Now+25; while(Now<until&&(g.Lan.View?.actors?.Length??0)<6) yield return null;
            Check(g.Lan.Connected&&(g.Lan.View?.actors?.Length??0)==6,"Client receives the complete six-person authoritative snapshot");
            if(!g.Lan.Connected||Self()==null) yield break;
            Check(g.Input.Move.enabled&&!g.Input.PoolShoot.enabled&&!g.Input.Accept.enabled,"Room routes player controls to gameplay instead of table or menu maps");
            Vector3 before=Self().position;
            if(index==1) {
                // Route around the host and the bottle cabinet instead of walking through other people.
                yield return Drive(new Vector3(2.7f,.08f,-6.8f));
                yield return Drive(new Vector3(2.7f,.08f,-1.5f));
                yield return Drive(new Vector3(0,.08f,-1.5f));
                Check(Self()!=null&&Vector3.Distance(Self().position,g.Lan.View.rival.position)<2.05f,"Attacking client reaches the NPC through open tavern aisles");
                Check(Vector3.Distance(before,Self().position)>1,"Real client intent changes its authoritative position");
                g.Lan.Send(LanAction.Move,Vector3.zero,false,0); yield return new WaitForSecondsRealtime(.15f);
                g.Lan.Send(LanAction.Charge); yield return new WaitForSecondsRealtime(.5f);
                Check(Self()!=null&&Self().charge>.2f&&g.Player.ReplicaCharge>.2f,"Charge telegraph is replicated before the remote strike resolves"); g.Lan.Send(LanAction.Release);
                until=Now+1.5; while(Now<until&&g.Lan.View?.rival?.fallen!=true) yield return null;
                Check(g.Lan.View?.rival?.fallen==true,"Remote charged punch result reaches the attacking client");
                yield return new WaitForSecondsRealtime(3.4f); Check(g.Lan.View?.rival?.fallen==false,"Remote NPC recovers without client-side physics authority");
                yield return Drive(new Vector3(-.7f,.08f,-3.5f)); yield return Drive(new Vector3(-2.2f,.08f,-3.5f));
                g.Lan.Send(LanAction.Move,Vector3.zero,false,0); yield return new WaitForSecondsRealtime(.15f);
                g.Lan.Send(LanAction.Interact); yield return new WaitForSecondsRealtime(.6f);
                Check(Self()?.held>=0,"Client grabs a server-owned throwable through validated interaction");
                g.Menu(); yield return new WaitForSecondsRealtime(.7f);
                Check(!g.Lan.Running,"Client can leave while holding an object");
                g.Lan.Join("Peer1 vuelve","127.0.0.1",port); until=Now+8; while(Now<until&&Self()==null) yield return null;
                Check(g.Lan.Connected&&Self()!=null&&Self().chips==0,"Returning IP does not receive another starting chip grant (chips="+Self()?.chips+")");
                Check(Self()!=null&&Self().held==-1,"Returning player has no stale object ownership");
            } else {
                double moving=Now+.35; while(Now<moving) { g.Lan.LocalMove(Vector3.forward,false,0); yield return null; }
                g.Lan.Send(LanAction.Stop); yield return new WaitForSecondsRealtime(.3f);
                Check(Self()!=null&&Vector3.Distance(before,Self().position)>.2f,"Client receives movement applied by the server");
                g.SetPause(true); yield return new WaitForSecondsRealtime(.2f);
                Check(Time.timeScale==1&&g.Lan.Running,"Local pause does not freeze the shared room clock"); g.SetPause(false);
                if(index==5) {
                    yield return Drive(new Vector3(2.7f,.08f,-7.5f));
                    g.Lan.Send(LanAction.Move,Vector3.zero,false,180); yield return new WaitForSecondsRealtime(.15f);
                    g.Lan.Send(LanAction.Charge); yield return new WaitForSecondsRealtime(.35f); g.Lan.Send(LanAction.Release); g.Lan.Send(LanAction.Stop);
                    yield return new WaitForSecondsRealtime(.65f);
                    Check(Self()?.fallen==true,"Stopping input or opening a menu cannot cancel a released missed punch");
                    yield return new WaitForSecondsRealtime(3.3f); Check(Self()?.fallen==false,"A remote player recovers after missing a punch");
                }
            }
            until=Now+42; while(Now<until&&g.Lan.Running) yield return null;
            Check(!g.Lan.Running&&!g.Started&&g.Lan.Status.Contains("anfitrión"),"Loss of host closes the client session and returns usable menu controls ["+g.Lan.Status+"]");
        }
        private void RosterChecks()
        {
            var r=new RoomRoster(); for(ulong i=0;i<6;i++) r.Add(i,"P"+i,"ip"+i,0);
            Check(r.Add(7,"extra","extra",0)==null,"Authority roster limits room to six");
            Check(r.Accept(0,1,0)&&!r.Accept(0,1,0)&&!r.Accept(0,0,0),"Duplicate and stale command sequences are rejected");
            Check(r.Spend(0,20)&&r.Members[0].Chips==980&&!r.Spend(0,1000),"Beverage cost cannot overdraw the session ledger");
            r.Remove(0); Check(r.Add(10,"return","ip0",1).Chips==0,"IP credit survives departure and return inside one room");
            var fresh=new RoomRoster(); Check(fresh.Add(0,"fresh","ip0",0).Chips==1000,"A different room resets the IP starting grant");
            Check(r.BeginVote(1,2,1)&&!r.Vote(2)&&r.Vote(3)&&r.Vote(4)&&r.ResolveVote(2)==null&&r.Vote(5)&&r.ResolveVote(2)==2,"Kick vote excludes target and requires strictly more than half");
            var v=new RoomRoster(); for(ulong i=0;i<3;i++) v.Add(i,"p","i"+i,0);
            v.BeginVote(0,1,0); v.Add(3,"late","late",1); Check(!v.Vote(3),"Late joiners cannot vote or change an active electorate");
            v.Remove(2); Check(v.ResolveVote(2)==null&&v.Vote(0)==false,"Leaving members recalculate the majority without duplicate votes");
            v.ResolveVote(16); Check(!v.VoteTarget.HasValue,"Kick vote expires after fifteen seconds");
            var pair=new RoomRoster(); pair.Add(0,"a","a",0); pair.Add(1,"b","b",0); pair.BeginVote(0,1,0);
            Check(pair.ResolveVote(1)==null,"Two-person room cannot expel a target with a single vote");
            Check(LanSession.TryDecodeCode(LanSession.EncodeCode("192.168.1.44",7777),out var address,out var p)&&address=="192.168.1.44"&&p==7777,"LAN room code round-trips IPv4 and port");
        }
    }
}
