using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    // Opt-in native-process checks of the playable bar over real UDP loopback. Only positioning is a fixture.
    public sealed class BarRoomChecks : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string role,version; public bool passed; public List<string> checks=new(),failures=new(),errors=new(); }
        private readonly Report report=new();
        private DemoGame g;private string role,folder;private ushort port;
        private double Now=>Time.realtimeSinceStartupAsDouble;
        private void Check(bool ok,string name){(ok?report.checks:report.failures).Add(name);Debug.Log((ok?"PASS ":"FAIL ")+name);}
        private void Log(string value,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(value+"\n"+trace);}
        private void Signal(string name,string value="ready")=>File.WriteAllText(Path.Combine(folder,name+".ready"),value);
        private bool Has(string name)=>File.Exists(Path.Combine(folder,name+".ready"));
        private string Stage=>Has("stage")?File.ReadAllText(Path.Combine(folder,"stage.ready")):"";
        private IEnumerator Wait(Func<bool> condition,float seconds=8){double until=Now+seconds;while(Now<until&&!condition())yield return null;}
        private BarView Bar=>g.Lan.View?.bar;
        private IEnumerator Start()
        {
            g=GetComponent<DemoGame>();Application.logMessageReceived+=Log;
            string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
            role=Arg("-bar-role","host");folder=Arg("-evidencePath",Application.persistentDataPath);port=ushort.Parse(Arg("-bar-port","7810"));Directory.CreateDirectory(folder);
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
            yield return Wait(()=>g.Lan.View?.actors?.Length==2,18);Check(g.Lan.View?.actors?.Length==2,"Two independent processes connect");if(g.Lan.View?.actors?.Length!=2)yield break;
            ulong client=Array.Find(g.Lan.View.actors,a=>a.name=="Client").id;var ledger=g.Lan.Roster.Ledger;var table=g.Lan.BarTable;
            g.Lan.ActorFor(client).Teleport(TavernWorld.BarPoint,Quaternion.identity);g.Player.Teleport(new Vector3(-7,.08f,3.5f),Quaternion.identity);
            long clientStart=ledger.Balance(client),hostStart=ledger.Balance(0);
            // 1. Serve: one charge, one vessel, exclusive possession.
            Signal("stage","serve");yield return Wait(()=>table.Service.VesselCount==1);yield return Wait(()=>Bar?.HeldBy(client)!=null&&g.Lan.BarVesselVisualCount==1,2);
            Check(table.Service.VesselCount==1&&ledger.Balance(client)==clientStart-20&&Bar.HeldBy(client)!=null,"Serving charges twenty once and the vessel is held by exactly one person");
            Check(g.Lan.BarVesselVisualCount==1,"The vessel is presented in the world");
            // 2. Offer and accept: free for the recipient, ownership transfers once.
            Signal("stage","offer");yield return Wait(()=>Bar.OfferedTo(0)!=null);Check(Bar.OfferedTo(0)!=null,"Offer reaches the intended target with its name and effect");
            g.Lan.Send(LanAction.Interact);yield return Wait(()=>Bar.HeldBy(0)!=null);
            Check(Bar.HeldBy(0)!=null&&Bar.HeldBy(client)==null&&ledger.Balance(0)==hostStart&&ledger.Balance(client)==clientStart-20,"Accepting transfers the vessel without a second charge");
            // 3. Consume applies the recipe once and the vessel disappears.
            g.Lan.Send(LanAction.Interact);yield return Wait(()=>g.Player.HasCocktail(CocktailKind.Blue));
            Check(g.Player.HasCocktail(CocktailKind.Blue)&&g.Player.Ebriety==1&&ledger.Find(0).Drinks==1,"Drinking applies Laguna Azul and one intoxication level");
            yield return Wait(()=>table.Service.VesselCount==0,2);Check(table.Service.VesselCount==0,"A consumed vessel cannot be used again");
            // 4. Exclusive station and cancellation without charge.
            g.Lan.Send(LanAction.Interact);yield return Wait(()=>table.Service.Station.Owner==0);
            Check(table.Service.Station.Owner==0&&g.Lan.LocalBarOpen,"The host opens the exclusive station");
            Signal("stage","busy");yield return new WaitForSecondsRealtime(.6f);Check(table.Service.Station.Owner==0,"A second person cannot take an occupied station");
            g.Lan.Send(LanAction.BarCancel);yield return Wait(()=>!table.Service.Station.Owner.HasValue);
            Check(!table.Service.Station.Owner.HasValue&&ledger.Balance(0)==hostStart,"Cancelling preparation never charges");
            // 5. Throw a Poison vessel at the host: K.O. through the thrown-vessel path.
            g.Player.ClearAllEffects();Signal("stage","throw");yield return Wait(()=>g.Player.Fallen,14);
            Check(g.Player.Fallen&&g.Player.HasCocktail(CocktailKind.Poison),"A thrown vessel hits and applies its effect to the target");
            yield return new WaitForSecondsRealtime(.4f);
            Check(table.Service.VesselCount==0&&ledger.AuditDelta==0&&ledger.BarRevenue==40,"Thrown vessel is spent once; economy stays balanced with 40 bar revenue");
        }
        private IEnumerator Peer()
        {
            Check(g.Lan.Join("Client","127.0.0.1",port),"Peer starts real connection");yield return Wait(()=>Bar!=null,18);Check(Bar!=null,"Bar snapshot arrives over UDP");if(Bar==null)yield break;
            string acted="";double until=Now+80;
            while(Now<until&&!Has("finish")){
                string stage=Stage;
                if(stage!=acted){acted=stage;
                    if(stage=="serve"){yield return BarFlowChecks.Serve(g,0);Check(BarFlowChecks.Mine(g)!=null,"Client prepares and serves through the intent protocol");}
                    else if(stage=="offer")g.Lan.Send(LanAction.BarOffer,yaw:0,pitch:0);
                    else if(stage=="busy")g.Lan.Send(LanAction.Interact,yaw:0,pitch:0);
                    else if(stage=="throw"){
                        yield return BarFlowChecks.Serve(g,4);Check(BarFlowChecks.Mine(g)!=null,"Client serves a Poison cocktail");
                        g.Lan.Send(LanAction.Charge,yaw:0,pitch:0);yield return new WaitForSecondsRealtime(.25f);g.Lan.Send(LanAction.Release,yaw:0,pitch:0);
                    }
                }
                yield return null;
            }
        }
    }
}
