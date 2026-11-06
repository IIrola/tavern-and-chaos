using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    // Opt-in integration checks. Recipe application and positioning are explicit fixtures; physics and UDP are real.
    public sealed class CocktailActorChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string role,version; public bool passed; public List<string> checks=new(),failures=new(),errors=new(),gravityContacts=new(),highProbeContacts=new(); public float baselineDistance,unstableDistance,normalGravity,heliumGravity,highProbeGravity; }
        private readonly Report report=new();private DemoGame g;private string role,folder;private ushort port;
        private CocktailRecipes recipes;private double Now=>Time.realtimeSinceStartupAsDouble;
        private void Check(bool ok,string name){(ok?report.checks:report.failures).Add(name);Debug.Log((ok?"PASS ":"FAIL ")+name);}
        private void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(message+"\n"+trace);}
        private void Signal(string name,string value="ready")=>File.WriteAllText(Path.Combine(folder,name+".ready"),value);
        private bool Has(string name)=>File.Exists(Path.Combine(folder,name+".ready"));
        private string Stage=>Has("stage")?File.ReadAllText(Path.Combine(folder,"stage.ready")):"";
        private IEnumerator Wait(Func<bool> condition,float seconds=6){double end=Now+seconds;while(Now<end&&!condition())yield return null;}
        private IEnumerator Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
            yield return null;yield return new WaitForEndOfFrame();string path=Path.Combine(folder,name+".png");ScreenCapture.CaptureScreenshot(path);yield return Wait(()=>File.Exists(path),3);Check(File.Exists(path),"Capture completes: "+name);
        }
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>();Application.logMessageReceived+=Log;
            string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
            role=Arg("-effects-role","host");folder=Arg("-evidencePath",Application.persistentDataPath);port=ushort.Parse(Arg("-effects-port","7801"));Directory.CreateDirectory(folder);
            report.role=role;report.version=Application.version;recipes=Resources.Load<CocktailCatalog>("CocktailCatalog").Snapshot();yield return null;
            if(role=="host")yield return Host();else yield return Peer();
            Check(report.errors.Count==0,"No runtime errors in "+role);report.passed=report.failures.Count==0&&report.errors.Count==0;
            File.WriteAllText(Path.Combine(folder,role+".json"),JsonUtility.ToJson(report,true));Application.logMessageReceived-=Log;g.Menu();yield return new WaitForSecondsRealtime(.3f);Application.Quit(report.passed?0:1);
        }
        private IEnumerator Walk(float seconds,Vector3 direction,bool run=false)
        {
            float until=Time.time+seconds;double timeout=Now+seconds+8;
            while(Time.time<until&&Now<timeout){g.Lan.LocalMove(direction,run,0);yield return null;}
            g.Lan.Send(LanAction.Stop);yield return new WaitForFixedUpdate();
        }
        private static float AverageY(Rigidbody[] bodies){float momentum=0,mass=0;foreach(var body in bodies){momentum+=body.mass*body.linearVelocity.y;mass+=body.mass;}return momentum/mass;}
        private static void RecordContacts(TavernActor actor,Rigidbody[] bodies,List<string> names)
        {
            foreach(var body in bodies){var observer=body.gameObject.AddComponent<CocktailGravityContacts>();observer.Owner=actor;observer.Records=names;}
        }
        private IEnumerator Host()
        {
            Check(g.Lan.Host("Effects host",port),"Host opens a real room with the current protocol");yield return null;Signal("host");
            yield return Wait(()=>g.Lan.View?.actors?.Length==2,18);Check(g.Lan.View?.actors?.Length==2,"Two independent native processes connect");if(g.Lan.View?.actors?.Length!=2)yield break;
            ulong peer=Array.Find(g.Lan.View.actors,a=>a.id!=0).id;var other=g.Lan.ActorFor(peer);
            other.Teleport(new Vector3(8,.08f,-5),Quaternion.identity);g.Rival.Teleport(new Vector3(-7,.08f,-2),Quaternion.identity);
            Vector3 origin=new(0,.08f,-6);g.Player.ClearAllEffects();g.Player.ClearIntoxication();g.Player.Teleport(origin,Quaternion.identity);
            yield return Walk(1,Vector3.forward);report.baselineDistance=Vector3.Distance(origin,g.Player.transform.position);
            g.Player.Teleport(origin,Quaternion.identity);g.Player.ApplyCocktail(recipes.Entries[2],"probe/speed");
            yield return Walk(1,Vector3.forward);report.unstableDistance=Vector3.Distance(origin,g.Player.transform.position);
            Check(report.unstableDistance/report.baselineDistance>1.43f&&report.unstableDistance/report.baselineDistance<1.57f,"Actual capsule travel increases to 150 percent under instability");
            g.Player.ClearAllEffects();g.Player.ClearIntoxication();g.Player.Teleport(origin,Quaternion.identity);g.Player.Drink();yield return Walk(.6f,Vector3.forward,true);
            float a=g.Player.transform.position.z;yield return Walk(.2f,Vector3.zero);float b=g.Player.transform.position.z;yield return Walk(.2f,Vector3.zero);float c=g.Player.transform.position.z;
            Check(b-a>.95f&&Mathf.Abs((c-b)-(b-a))<.15f,"Blue preserves running momentum after releasing both movement and run without hidden friction");
            g.Player.ClearEffect();yield return Walk(.4f,Vector3.zero);a=g.Player.transform.position.z;yield return Walk(.2f,Vector3.zero);
            Check(g.Player.transform.position.z-a<.08f,"Clearing blue restores normal braking without permanent drift");
            g.Player.Teleport(origin,Quaternion.identity);g.Player.ApplyCocktail(recipes.Entries[3],"probe/capsule-gravity");yield return Walk(.6f,Vector3.zero);
            Check(g.Player.transform.position.y>origin.y+.45f,"Helium produces real capsule lift under reduced gravity");
            g.Player.SetSeated(true);Vector3 seated=g.Player.transform.position;yield return new WaitForSeconds(.35f);Check(Vector3.Distance(seated,g.Player.transform.position)<.005f,"Movement effects do not translate a seated actor");g.Player.SetSeated(false);
            g.Player.ClearAllEffects();g.Player.ClearIntoxication();g.Player.Teleport(origin,Quaternion.identity);
            Vector3 gravity=Physics.gravity;
            var normal=g.CreateRoomActor("Normal gravity probe",new Vector3(0,2,-4));var light=g.CreateRoomActor("Helium gravity probe",new Vector3(2,2,-4));
            var high=g.CreateRoomActor("Ceiling clearance diagnostic",new Vector3(-2,3,-4));high.ApplyCocktail(recipes.Entries[3],"probe/high-gravity");high.Fall(Vector3.zero);
            light.ApplyCocktail(recipes.Entries[3],"probe/ragdoll-gravity");normal.Fall(Vector3.zero);light.Fall(Vector3.zero);
            var normalBodies=normal.GetComponentsInChildren<Rigidbody>();var lightBodies=light.GetComponentsInChildren<Rigidbody>();var highBodies=high.GetComponentsInChildren<Rigidbody>();
            RecordContacts(normal,normalBodies,report.gravityContacts);RecordContacts(light,lightBodies,report.gravityContacts);RecordContacts(high,highBodies,report.highProbeContacts);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            float normalBefore=AverageY(normalBodies),lightBefore=AverageY(lightBodies),highBefore=AverageY(highBodies),began=Time.fixedTime;
            while(Time.fixedTime-began<.25f)yield return new WaitForFixedUpdate();
            float elapsed=Time.fixedTime-began;
            report.normalGravity=(AverageY(normalBodies)-normalBefore)/elapsed;report.heliumGravity=(AverageY(lightBodies)-lightBefore)/elapsed;
            report.highProbeGravity=(AverageY(highBodies)-highBefore)/elapsed;
            Check(report.gravityContacts.Count==0,"Gravity measurement has no environment contacts");
            Check(Mathf.Abs(report.normalGravity-Physics.gravity.y)<.7f&&Mathf.Abs(report.heliumGravity-Physics.gravity.y*.2f)<.7f,"Mass-weighted ragdoll acceleration measures full versus twenty-percent gravity ["+report.normalGravity.ToString("0.000")+" / "+report.heliumGravity.ToString("0.000")+"]");
            Check(Physics.gravity==gravity,"Helium leaves global world gravity unchanged");Destroy(normal.gameObject);Destroy(light.gameObject);Destroy(high.gameObject);
            g.Player.Teleport(new Vector3(0,.08f,-4),Quaternion.identity);g.Rival.Teleport(new Vector3(0,.08f,-2.5f),Quaternion.identity);other.Teleport(new Vector3(1.5f,.08f,-4),Quaternion.identity);
            g.Player.ApplyCocktail(recipes.Entries[1],"probe/blast");Check(g.Rival.Fallen&&!other.Fallen,"Volcano wave knocks down nearby NPC while leaving nearby player standing");yield return Capture("00-volcano");
            g.Player.ClearAllEffects();g.Player.ClearIntoxication();g.Player.Teleport(ArmRoomController.SeatPosition(0)+Vector3.back*.6f,Quaternion.identity);other.Teleport(ArmRoomController.SeatPosition(1)+Vector3.back*.6f,Quaternion.identity);
            g.Lan.Send(LanAction.ArmSit);yield return Wait(()=>g.Lan.LocalArmSeated);Signal("stage","sit");yield return Wait(()=>g.Lan.View.arm.seated.Length==2);
            Check(g.Lan.View.arm.seated.Length==2,"Both actual arm seats confirm before an effect interruption");if(g.Lan.View.arm.seated.Length!=2)yield break;
            g.Lan.Send(LanAction.ArmReady,proposal:g.Lan.View.arm.proposal);Signal("stage","ready-"+g.Lan.View.arm.proposal);yield return Wait(()=>g.Lan.View.arm.ready.Length==2);
            g.Lan.Send(LanAction.ArmStart,proposal:g.Lan.View.arm.proposal);yield return Wait(()=>g.Lan.View.arm.phase==TablePhase.Playing);
            float applied=Time.time;g.Player.ApplyCocktail(recipes.Entries[2],"probe/seated-instability");yield return Wait(()=>g.Player.Fallen,7);
            Check(g.Player.Fallen&&Time.time-applied>=4.99f&&!g.Lan.LocalArmSeated&&g.Lan.ArmTable.Table.Participates(0),"Five-second instability releases a real seat while preserving match participation");
            applied=Time.time;g.Player.ApplyCocktail(recipes.Entries[4],"probe/ko");yield return new WaitForSeconds(8);
            Check(g.Player.Fallen&&g.Player.RecoverAt-Time.time>1.8f,"K.O. extends an already-fallen actor beyond ordinary recovery");yield return Wait(()=>!g.Player.Fallen,6);
            Check(!g.Player.Fallen&&Time.time-applied>=9.99f,"K.O. permits recovery only after ten actual simulation seconds");
            other.ApplyCocktail(recipes.Entries[4],"probe/client/ko");other.ApplyCocktail(recipes.Entries[0],"probe/client/blue");other.ApplyCocktail(recipes.Entries[2],"probe/client/unstable");other.ApplyCocktail(recipes.Entries[3],"probe/client/helium");other.ApplyCocktail(recipes.Entries[1],"probe/client/volcano");
            Signal("stage","all");yield return Wait(()=>Has("peer-all"),5);Check(Has("peer-all"),"Peer confirms all five authoritative effect snapshots and local visibility controls");
            g.Rig.enabled=false;Vector3 eye=other.Focus+new Vector3(0,1,-2.5f);Camera.main.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(other.Focus+Vector3.up*.3f-eye));yield return Capture("01-combined-effects");
            g.Rig.EffectIntensity=0;yield return null;yield return null;Check(!other.GetComponent<CocktailPresentation>().BlueVisible&&!other.GetComponent<CocktailPresentation>().SmokeVisible&&other.HasCocktail(CocktailKind.Helium),"Zero visual intensity hides presentation without cancelling physics rules");yield return Capture("02-zero-intensity");g.Rig.EffectIntensity=1;g.Rig.enabled=true;
            g.SetPause(true);g.Settings=true;yield return Capture("03-settings");g.Settings=false;g.SetPause(false);
            g.Lan.ExpireForCheck();yield return Wait(()=>g.Lan.Ending?.phase==RoomPhase.Results,6);
            Check(g.Lan.Ending?.phase==RoomPhase.Results&&g.Player.CaptureEffects().Length==0&&other.CaptureEffects().Length==0,"Finishing the room restores every actor's effect modifiers before freezing results");
            Signal("stage","ended");yield return Wait(()=>Has("peer-ended"),6);Check(Has("peer-ended"),"Client receives cleared effects and the immutable final report");
            Signal("stage","detach");g.Menu();yield return Wait(()=>Has("peer-detached"),8);
            Check(g.Player.CaptureEffects().Length==0&&g.Rival.CaptureEffects().Length==0&&Has("peer-detached"),"Host and client clear all effect types on room exit");Signal("finish");yield return new WaitForSecondsRealtime(.3f);
        }
        private IEnumerator Peer()
        {
            Check(g.Lan.Join("Effects peer","127.0.0.1",port),"Peer starts a real UDP connection");yield return Wait(()=>g.Lan.View!=null,18);Check(g.Lan.View!=null,"Peer receives current-protocol snapshots");if(g.Lan.View==null)yield break;
            string acted="";double deadline=Now+85;
            while(Now<deadline&&!Has("finish")){
                string stage=Stage;
                if(stage!=acted){bool done=true;
                    if(stage=="sit")g.Lan.Send(LanAction.ArmSit);
                    else if(stage.StartsWith("ready-")){done=stage=="ready-"+g.Lan.View.arm.proposal;if(done)g.Lan.Send(LanAction.ArmReady,proposal:g.Lan.View.arm.proposal);}
                    else if(stage=="all"){
                        int count=0;for(var kind=CocktailKind.Blue;kind<=CocktailKind.Poison;kind++)if(g.Player.HasCocktail(kind))count++;
                        done=count==5;if(done){var state=Array.Find(g.Lan.View.actors,a=>a.id==g.Lan.LocalId);bool metadata=state.effects.Length==5;foreach(var effect in state.effects)metadata&=!string.IsNullOrEmpty(effect.origin)&&effect.duration>0&&double.IsFinite(effect.start);
                            Check(metadata,"Each replicated effect retains kind, origin, start, duration and remaining time");bool passive=true;foreach(var body in g.Player.GetComponentsInChildren<Rigidbody>())passive&=body.isKinematic;Check(passive,"Replica ragdoll remains kinematic under helium");
                            var visual=g.Player.GetComponent<CocktailPresentation>();yield return null;Check(visual.BlueVisible&&visual.FireVisible&&visual.SmokeVisible,"Replicated effects activate client light, fire and smoke");g.Rig.EffectIntensity=0;yield return null;yield return null;Check(!visual.BlueVisible&&!visual.FireVisible&&!visual.SmokeVisible&&g.Player.HasCocktail(CocktailKind.Poison),"Client can hide effects without altering the server state");g.Rig.EffectIntensity=1;Signal("peer-all");
                        }
                    }else if(stage=="ended"){
                        done=g.Lan.Ending?.phase==RoomPhase.Results&&g.Lan.StatsPage?.final==true;
                        if(done){bool clear=true;for(var kind=CocktailKind.Blue;kind<=CocktailKind.Poison;kind++)clear&=!g.Player.HasCocktail(kind);Check(clear,"Final authoritative snapshot clears every active effect on the client");Signal("peer-ended");}
                    }else if(stage=="detach"){
                        yield return Wait(()=>!g.Lan.Running,8);Check(g.Lan.ResultsDisconnected&&g.Lan.StatsPage?.final==true,"Host departure retains the cleared final report");g.Menu();Check(!g.Started&&g.Player.CaptureEffects().Length==0&&!g.Player.HasCocktail(CocktailKind.Helium),"Explicit exit returns to usable menu and clears old-room modifiers");Signal("peer-detached");
                    }
                    if(done)acted=stage;
                }
                yield return null;
            }
            Check(Has("finish"),"Effect integration scenario completes");
        }
    }
}
