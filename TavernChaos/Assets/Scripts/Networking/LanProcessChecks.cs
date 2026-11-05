using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    // Separate built-player processes connect over UTP. No in-process transport mocks.
    [DefaultExecutionOrder(200)]
    public sealed class LanProcessChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string role,unity; public bool passed; public List<string> checks=new(),failures=new(),runtimeErrors=new();
            public int snapshots,maxParticipants,rejected; public long bytesReceived; public ulong rttMilliseconds;
            public float maxStandingBodyDrift,maxStandingCameraDrift;
            public int renderedFrames;
        }
        private readonly Report report=new();
        private DemoGame g; private string folder,role; private ushort port; private int index;
        private static double Now=>Time.realtimeSinceStartupAsDouble;
        private void Check(bool ok,string name) { (ok?report.checks:report.failures).Add(name); Debug.Log((ok?"PASS: ":"FAIL: ")+name); }
        private void Log(string text,string trace,LogType type) { if(type==LogType.Exception||type==LogType.Assert||type==LogType.Error) report.runtimeErrors.Add(text+"\n"+trace); }
        private void LateUpdate()
        {
            if(role!="replica-client"||g?.Lan?.View?.actors==null) return;
            report.renderedFrames++;
            var states=new List<LanActorState>(g.Lan.View.actors); states.Add(g.Lan.View.rival);
            foreach(var state in states) {
                var actor=state.id==ulong.MaxValue?g.Rival:g.Lan.ActorFor(state.id);
                if(!actor||state.fallen||actor.Fallen||state.body==null) continue;
                var bodies=actor.GetComponentsInChildren<Rigidbody>();
                for(int i=0;i<Math.Min(bodies.Length,state.body.Length);i++)
                    report.maxStandingBodyDrift=Mathf.Max(report.maxStandingBodyDrift,Vector3.Distance(bodies[i].transform.position,actor.transform.TransformPoint(state.body[i].position)));
                if(actor==g.Player) report.maxStandingCameraDrift=Mathf.Max(report.maxStandingCameraDrift,Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition));
            }
        }
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>(); Application.logMessageReceived+=Log;
            string[] args=Environment.GetCommandLineArgs(); string Arg(string key,string fallback) { int p=Array.IndexOf(args,key); return p>=0&&p+1<args.Length?args[p+1]:fallback; }
            role=Arg("-lan-role","host"); folder=Arg("-evidencePath",Path.Combine(Application.persistentDataPath,"lan-checks")); port=ushort.Parse(Arg("-lan-port","7789")); index=int.Parse(Arg("-lan-index","0"));
            Directory.CreateDirectory(folder); report.role=role+index; report.unity=Application.unityVersion;
            yield return null;
            if(role=="replica-host") yield return ReplicaHost(); else if(role=="replica-client") yield return ReplicaClient();
            else if(role=="smoke-host") yield return SmokeHost(); else if(role=="smoke-client") yield return SmokeClient();
            else if(role=="host") yield return Host(); else if(role=="overflow") yield return Overflow(); else yield return Client();
            if(role=="replica-client") {
                Check(report.renderedFrames>100&&report.maxStandingBodyDrift<.08f,"No cumulative rendered-body drift while moving ("+report.maxStandingBodyDrift.ToString("0.000")+" m)");
                Check(report.maxStandingCameraDrift<.05f,"Standing first-person camera stays aligned during movement");
            }
            report.snapshots+=g.Lan.SnapshotsReceived; report.bytesReceived+=g.Lan.BytesReceived; report.rejected=g.Lan.RejectedActions;
            Check(report.runtimeErrors.Count==0,"No runtime errors or exceptions in this process");
            report.passed=report.failures.Count==0&&report.runtimeErrors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+index+".json"),JsonUtility.ToJson(report,true));
            g.Menu(); Application.logMessageReceived-=Log; Debug.Log("LAN_CHECK_DONE: "+report.passed); yield return new WaitForSecondsRealtime(.3f); Application.Quit(report.passed?0:1);
        }
        private string Marker(string name)=>Path.Combine(folder,name+".ready");
        private void CheckPose(LanActorState state,TavernActor actor,string label,float tolerance=.14f)
        {
            var bodies=actor.GetComponentsInChildren<Rigidbody>();
            if(state?.body==null||state.body.Length!=6||bodies.Length!=6) { Check(false,label+": six authoritative body poses"); return; }
            Quaternion rotation=actor==g.Player&&!state.fallen?Quaternion.Euler(0,g.Rig.Yaw,0):Quaternion.Euler(0,state.yaw,0);
            float maxPosition=0,maxAngle=0; bool noPhysics=true;
            for(int i=0;i<6;i++) {
                Vector3 expected=state.position+rotation*state.body[i].position;
                maxPosition=Mathf.Max(maxPosition,Vector3.Distance(bodies[i].transform.position,expected));
                maxAngle=Mathf.Max(maxAngle,Quaternion.Angle(bodies[i].transform.rotation,rotation*state.body[i].rotation));
                noPhysics&=bodies[i].isKinematic&&bodies[i].interpolation==RigidbodyInterpolation.None;
            }
            Check(maxPosition<tolerance&&maxAngle<25,label+": rendered head, torso, arms and feet match snapshot ("+maxPosition.ToString("0.000")+" m, "+maxAngle.ToString("0.0")+" deg)");
            Check(noPhysics,label+": PhysX cannot overwrite replica interpolation");
        }
        private IEnumerator ReplicaCapture(string file)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            Vector3 delta=g.Lan.View.rival.focus-g.Player.EyePosition;
            g.Rig.Yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            g.Rig.Pitch=-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg;
            yield return null; yield return new WaitForEndOfFrame();
            // A hidden Windows swapchain cannot reliably be screen-captured. Render the actual client camera offscreen.
            var camera=Camera.main; var previous=camera.targetTexture; var active=RenderTexture.active;
            var target=new RenderTexture(1280,720,24); var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,file),pixels.EncodeToPNG());
            } finally { camera.targetTexture=previous; RenderTexture.active=active; target.Release(); Destroy(target); Destroy(pixels); }
            yield return null;
        }
        private IEnumerator ReplicaHost()
        {
            Check(g.Lan.Host("Anfitrión",port),"Replication: host starts"); Signal("host");
            double until=Now+20; while(Now<until&&(g.Lan.View?.actors?.Length??0)<2) yield return null;
            Check(g.Lan.View?.actors?.Length==2,"Replication: client connects"); if(g.Lan.View?.actors?.Length!=2) yield break;
            ulong peer=0; foreach(var state in g.Lan.View.actors) if(state.id!=0) peer=state.id;
            var client=g.Lan.ActorFor(peer);
            g.Player.Teleport(new Vector3(2,.08f,-6),Quaternion.identity);
            client.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity);
            for(int i=0;i<4;i++) {
                float yaw=i*90; Vector3 move=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                g.Rival.Aim(move); g.Lan.Send(LanAction.Move,move,false,yaw); yield return new WaitForSecondsRealtime(.65f);
                g.Lan.Send(LanAction.Stop); yield return new WaitForSecondsRealtime(.5f);
                Signal("pose"+i); yield return WaitSignal("pose-checked"+i);
            }
            Signal("move-client"); yield return WaitSignal("client-moved");
            for(int i=0;i<3;i++) {
                g.Rival.Teleport(g.Rival.Home,Quaternion.Euler(0,180,0));
                if(i==2) {
                    client.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity);
                    g.Player.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity);
                    g.Lan.Send(LanAction.Interact,yaw:0,pitch:0); yield return new WaitForSecondsRealtime(.2f);
                }
                if(i==1) {
                    client.Teleport(new Vector3(-.55f,.08f,-1.8f),Quaternion.identity);
                    g.Player.Teleport(new Vector3(2.7f,.08f,-2.6f),Quaternion.identity);
                } else g.Player.Teleport(new Vector3(-.55f,.08f,i==2?-2.6f:-1.8f),Quaternion.identity);
                int hits=g.LanHitEffects,sounds=g.Audio.OneShotsPlayed;
                int eventCount=g.Lan.FeedbackCount(i==2?LanFeedbackKind.BottleHit:LanFeedbackKind.PunchHit);
                yield return new WaitForSecondsRealtime(.2f); Signal("attack"+i); yield return WaitSignal("attack-watched"+i);
                if(i!=1) {
                    g.Lan.Send(LanAction.Charge,yaw:0,pitch:0); yield return new WaitForSecondsRealtime(.4f);
                    g.Lan.Send(LanAction.Release,yaw:0,pitch:0);
                }
                until=Now+2; while(Now<until&&!g.Rival.Fallen) yield return null;
                Check(g.Rival.Fallen,"Replication: authority resolves "+(i==1?"guest punch":i==2?"host bottle":"host punch")+" against Bruno");
                Check(g.LanHitEffects==hits+1&&g.Audio.OneShotsPlayed>sounds&&g.Audio.LastCue=="hit","Host presents impact particles and audio, cycle "+i);
                Check(g.Lan.FeedbackCount(i==2?LanFeedbackKind.BottleHit:LanFeedbackKind.PunchHit)==eventCount+1,"Host presents one impact event per attack, cycle "+i);
                Signal("fallen"+i); yield return WaitSignal("impact-captured"+i);
                // Clear the observer's sightline so the falling body and feet are visible in the capture.
                g.Player.Teleport(new Vector3(2.7f,.08f,-2.6f),Quaternion.identity);
                yield return WaitSignal("fall-checked"+i);
                until=Now+5; while(Now<until&&g.Rival.Fallen) yield return null;
                yield return new WaitForSecondsRealtime(.2f); Signal("recovered"+i); yield return WaitSignal("recovery-checked"+i);
            }
            client.Teleport(new Vector3(2.7f,.08f,-7.5f),Quaternion.Euler(0,180,0));
            Signal("self-miss"); yield return WaitSignal("self-checked");
            g.Player.Teleport(new Vector3(-2.7f,.08f,-7.5f),Quaternion.Euler(0,180,0));
            g.Lan.Send(LanAction.Charge,yaw:180,pitch:0); yield return new WaitForSecondsRealtime(.35f); g.Lan.Send(LanAction.Release,yaw:180,pitch:0);
            Signal("host-miss"); yield return WaitSignal("host-checked");
            Signal("closing"); yield return new WaitForSecondsRealtime(.2f); g.Menu();
        }
        private IEnumerator ReplicaClient()
        {
            Check(g.Lan.Join("Invitado","127.0.0.1",port),"Replication: client starts");
            double until=Now+20; while(Now<until&&Self()==null) yield return null;
            if(Self()==null) { Check(false,"Replication: state arrives"); yield break; }
            g.Rig.ReduceMotion=true;
            for(int i=0;i<4;i++) {
                yield return WaitSignal("pose"+i); yield return new WaitForSecondsRealtime(.2f);
                foreach(var state in g.Lan.View.actors) CheckPose(state,g.Lan.ActorFor(state.id),"Standing cycle "+i+" player "+state.id);
                CheckPose(g.Lan.View.rival,g.Rival,"Standing cycle "+i+" Bruno");
                Signal("pose-checked"+i);
            }
            yield return WaitSignal("move-client"); g.Rig.Yaw=90; double began=Now;
            while(Now-began<.7) { g.Lan.LocalMove(Vector3.right,false,90); yield return null; }
            g.Lan.Send(LanAction.Stop); yield return new WaitForSecondsRealtime(.4f);
            CheckPose(Self(),g.Player,"Own body after movement");
            Check(Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition)<.05f,"Own body and first-person camera share one rendered pose");
            Signal("client-moved");
            for(int i=0;i<3;i++) {
                yield return WaitSignal("attack"+i);
                int hits=g.LanHitEffects,sounds=g.Audio.OneShotsPlayed;
                int eventCount=g.Lan.FeedbackCount(i==2?LanFeedbackKind.BottleHit:LanFeedbackKind.PunchHit);
                Signal("attack-watched"+i);
                if(i==1) {
                    g.Rig.Yaw=0; g.Rig.Pitch=0;
                    g.Lan.Send(LanAction.Charge,yaw:0,pitch:0); yield return new WaitForSecondsRealtime(.4f);
                    g.Lan.Send(LanAction.Release,yaw:0,pitch:0);
                }
                yield return WaitSignal("fallen"+i);
                until=Now+1; while(Now<until&&(!g.Lan.View.rival.fallen||g.LanHitEffects==hits)) yield return null;
                Check(g.Lan.View.rival.fallen,"Client sees Bruno fall, cycle "+i);
                Check(g.LanHitEffects==hits+1&&g.Audio.OneShotsPlayed>sounds&&g.Audio.LastCue=="hit","Client presents impact particles and audio, cycle "+i);
                Check(g.Lan.FeedbackCount(i==2?LanFeedbackKind.BottleHit:LanFeedbackKind.PunchHit)==eventCount+1,"One impact event per attack, cycle "+i);
                yield return ReplicaCapture("impact-"+i+".png"); Signal("impact-captured"+i);
                int count=g.Audio.OneShotsPlayed;
                yield return new WaitForSecondsRealtime(1.6f);
                CheckPose(g.Lan.View.rival,g.Rival,"Actual ragdoll cycle "+i,.25f);
                Check(g.Lan.View.rival.eye.y<1.4f,"Ragdoll head follows fall instead of hovering, cycle "+i);
                Check(g.Audio.OneShotsPlayed==count,"Snapshots do not repeat impact audio, cycle "+i);
                yield return ReplicaCapture("fallen-"+i+".png"); Signal("fall-checked"+i);
                yield return WaitSignal("recovered"+i); yield return new WaitForSecondsRealtime(.2f);
                CheckPose(g.Lan.View.rival,g.Rival,"Recovered cycle "+i);
                Check(!g.Rival.Fallen,"Bruno returns to a standing body, cycle "+i);
                yield return ReplicaCapture("recovered-"+i+".png"); Signal("recovery-checked"+i);
            }
            yield return WaitSignal("self-miss"); g.Rig.Yaw=180;
            g.Lan.Send(LanAction.Charge,yaw:180,pitch:0); yield return new WaitForSecondsRealtime(.35f); g.Lan.Send(LanAction.Release,yaw:180,pitch:0);
            until=Now+2; while(Now<until&&!Self().fallen) yield return null;
            yield return new WaitForSecondsRealtime(1.5f); CheckPose(Self(),g.Player,"Own ragdoll",.25f);
            Check(Self().fallen&&Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition)<.2f,"Own fall keeps camera with actual head");
            until=Now+5; while(Now<until&&Self().fallen) yield return null;
            Signal("self-checked"); yield return WaitSignal("host-miss");
            yield return new WaitForSecondsRealtime(1.7f);
            foreach(var state in g.Lan.View.actors) if(state.id==0) { Check(state.fallen,"Client sees host fall"); CheckPose(state,g.Lan.ActorFor(0),"Host ragdoll",.25f); }
            Signal("host-checked"); yield return WaitSignal("closing");
        }
        private void Signal(string name)=>File.WriteAllText(Marker(name),"ready");
        private IEnumerator WaitSignal(string name,float seconds=15)
        {
            double until=Now+seconds;
            while(!File.Exists(Marker(name))&&Now<until) yield return null;
            Check(File.Exists(Marker(name)),"Fixture stage reached: "+name);
        }
        // Short component integration tests with explicit server-side arrangements.
        // This is not the user's fifteen-minute Hito 1 acceptance session.
        private IEnumerator SmokeHost()
        {
            RosterChecks();
            Check(g.Lan.Host("Anfitrión",port),"Pre-Hito: authority starts on loopback"); Signal("host");
            double until=Now+20;
            while(Now<until&&(g.Lan.View?.actors?.Length??0)!=2) yield return null;
            Check(g.Lan.View?.actors?.Length==2,"Pre-Hito: independent host and client connect");
            if(g.Lan.View?.actors?.Length!=2) yield break;
            ulong peer=0; foreach(var state in g.Lan.View.actors) if(state.id!=0) peer=state.id;
            var remote=g.Lan.ActorFor(peer);
            Check(Vector3.Distance(remote.transform.position,g.Player.transform.position)>.8f,"Pre-Hito: player spawns do not overlap");
            g.Player.Teleport(new Vector3(-.8f,.08f,-3.5f),Quaternion.identity);
            remote.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity);
            for(int i=1;i<g.Bottles.Count;i++) g.Bottles[i].gameObject.SetActive(false);
            g.Bottles[0].transform.position=new Vector3(-1.5f,1.18f,-2.3f); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.2f); Signal("race");
            g.Lan.Send(LanAction.Interact,yaw:0,pitch:0);
            yield return new WaitForSecondsRealtime(.5f);
            int owners=0; foreach(var actor in g.Lan.View.actors) if(actor.held>=0) owners++;
            Check(owners==1&&g.Bottles[0].Owner,"Pre-Hito: competing requests give one bottle a single owner");
            g.Lan.Send(LanAction.Drop); yield return new WaitForSecondsRealtime(.15f);
            foreach(var bottle in g.Bottles) { bottle.gameObject.SetActive(true); bottle.ResetToRack(); }
            // Preserve authority bookkeeping if the remote client won the race.
            remote.Fall(Vector3.zero); remote.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity);
            yield return new WaitForSecondsRealtime(.2f); Signal("claim");
            yield return WaitSignal("reset");
            Check(Vector3.Distance(g.Player.transform.position,new Vector3(-.8f,.08f,-3.5f))<.1f,"Pre-Hito: a client reset does not reset the host");
            remote.Teleport(new Vector3(-5.7f,.08f,1.1f),Quaternion.identity);
            yield return new WaitForSecondsRealtime(.2f); Signal("bar");
            yield return WaitSignal("drink");
            Check(remote.EffectRemaining>0,"Pre-Hito: drink effect exists on the authority");
            var wall=TavernWorld.Box("Network slip test wall",new(0,1.5f,-3.5f),new(2,3,.2f),TavernWorld.DarkWood);
            remote.Teleport(new Vector3(0,.08f,-7),Quaternion.identity); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.2f); Signal("run");
            yield return WaitSignal("fall");
            Check(remote.Fallen&&remote.EffectRemaining==0,"Pre-Hito: only the authority resolves the Laguna Azul collision");
            yield return WaitSignal("recover"); wall.SetActive(false); Destroy(wall);
            Check(!remote.Fallen,"Pre-Hito: authoritative recovery restores the controller");
            remote.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity);
            yield return new WaitForSecondsRealtime(.2f); Signal("throw");
            yield return WaitSignal("launched");
            bool launched=false; foreach(var bottle in g.Bottles) if(bottle.Flying&&bottle.Body.position.y>1.7f) launched=true;
            Check(launched,"Pre-Hito: vertical aim affects the authoritative bottle trajectory");
            yield return WaitSignal("left"); yield return new WaitForSecondsRealtime(.3f);
            bool dangling=false; foreach(var bottle in g.Bottles) if(bottle.Owner==remote&&bottle.Held) dangling=true;
            Check(g.Lan.View?.actors?.Length==1&&!dangling,"Pre-Hito: disconnect clears actor and object ownership");
            yield return WaitSignal("returned");
            Check(g.Lan.View?.actors?.Length==2,"Pre-Hito: room accepts a returning client");
            Signal("closing"); yield return new WaitForSecondsRealtime(.3f); g.Menu();
            Check(!g.Lan.Running,"Pre-Hito: host closes without leaving the demo stuck");
        }
        private IEnumerator SmokeClient()
        {
            Check(g.Lan.Join("Cliente","127.0.0.1",port),"Pre-Hito: client starts a real UDP connection");
            double until=Now+20; while(Now<until&&Self()==null) yield return null;
            Check(Self()!=null,"Pre-Hito: client receives authoritative state"); if(Self()==null) yield break;
            yield return new WaitForSecondsRealtime(.2f);
            Check(g.Player.IsReplica&&Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition)<.05f,"Pre-Hito: network client uses the first-person eye view");
            yield return WaitSignal("race"); g.Lan.Send(LanAction.Interact,yaw:0,pitch:0);
            yield return WaitSignal("claim"); g.Lan.Send(LanAction.Interact,yaw:0,pitch:0); yield return new WaitForSecondsRealtime(.3f);
            Check(Self().held>=0,"Pre-Hito: client can claim an available bottle");
            g.Rig.Yaw=90; g.Rig.Pitch=25; g.Lan.Send(LanAction.Move,Vector3.right,false,90,pitch:25);
            g.Lan.Send(LanAction.Charge); yield return new WaitForSecondsRealtime(.15f); g.Lan.Send(LanAction.Stop);
            g.Lan.Send(LanAction.Reset); until=Now+3; while(Now<until&&Self()?.resetRevision==0) yield return null;
            Check(Self()?.resetRevision==1&&Self().held==-1&&Vector3.Distance(Self().position,LanSession.SpawnPoint(1))<.1f,"Pre-Hito: reset clears held state and returns only this client to spawn");
            Check(g.Rig.Yaw==0&&g.Rig.Pitch==0,"Pre-Hito: camera resets on authoritative acknowledgement"); Signal("reset");
            yield return WaitSignal("bar"); long chips=Self().chips;
            g.Lan.Send(LanAction.Interact,yaw:270,pitch:0); yield return new WaitForSecondsRealtime(.3f);
            Check(Self().effect>7&&Self().chips==chips-20,"Pre-Hito: authority charges once and starts Laguna Azul"); Signal("drink");
            yield return WaitSignal("run"); double began=Now;
            while(Now-began<.65) { g.Lan.LocalMove(Vector3.forward,true,0); yield return null; }
            Check(!Self().fallen,"Pre-Hito: floor contacts and speed alone do not cause a network fall");
            until=Now+3; while(Now<until&&!Self().fallen) { g.Lan.LocalMove(Vector3.forward,true,0); yield return null; }
            g.Lan.Send(LanAction.Stop);
            Check(Self().fallen&&Self().effect==0&&g.Player.Fallen,"Pre-Hito: collision fall and effect cleanup reach the client");
            bool kinematic=true; foreach(var body in g.Player.GetComponentsInChildren<Rigidbody>()) if(!body.isKinematic) kinematic=false;
            Check(kinematic,"Pre-Hito: client does not independently simulate ragdoll physics"); Signal("fall");
            until=Now+5; while(Now<until&&Self().fallen) yield return null;
            Check(!Self().fallen,"Pre-Hito: client receives recovery"); Signal("recover");
            yield return WaitSignal("throw"); g.Lan.Send(LanAction.Interact,yaw:0,pitch:0); yield return new WaitForSecondsRealtime(.3f);
            int held=Self().held; Check(held>=0,"Pre-Hito: bottle remains usable after recovery");
            g.Lan.Send(LanAction.Charge,yaw:0,pitch:-45); yield return new WaitForSecondsRealtime(.4f);
            g.Lan.Send(LanAction.Release,yaw:0,pitch:-45); yield return new WaitForSecondsRealtime(.15f);
            Check(held>=0&&Self().held==-1&&g.Lan.View.bottles[held].position.y>1.7f,"Pre-Hito: upward throw is replicated"); Signal("launched");
            g.Lan.Send(LanAction.Interact,yaw:0,pitch:0); yield return new WaitForSecondsRealtime(.2f);
            Check(Self().held>=0,"Pre-Hito: leave fixture holds an object");
            report.rttMilliseconds=g.Lan.RoundTripMilliseconds; report.snapshots=g.Lan.SnapshotsReceived; report.bytesReceived=g.Lan.BytesReceived;
            g.Menu(); Signal("left"); yield return new WaitForSecondsRealtime(.7f);
            g.Lan.Join("Cliente vuelve","127.0.0.1",port); until=Now+8; while(Now<until&&Self()==null) yield return null;
            Check(Self()!=null&&Self().held==-1&&Self().effect==0&&Self().chips==0,"Pre-Hito: return has no stale effect, object or repeated IP grant"); Signal("returned");
            yield return WaitSignal("closing"); until=Now+10; while(Now<until&&g.Lan.Running) yield return null;
            Check(!g.Lan.Running&&!g.Started,"Pre-Hito: host loss returns the client to the menu");
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
