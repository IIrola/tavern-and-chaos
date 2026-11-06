using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    // Opt-in diagnostic: runs the REAL (non-smoke) host/join path with player input and logs whether the room survives.
    public sealed class RoomSoak : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var g=GetComponent<DemoGame>(); var args=Environment.GetCommandLineArgs();
            string Arg(string k,string d){int i=Array.IndexOf(args,k);return i>=0&&i+1<args.Length?args[i+1]:d;}
            string role=Arg("-room-soak","host"),folder=Arg("-evidencePath",Application.persistentDataPath);ushort port=ushort.Parse(Arg("-soak-port","7830"));float seconds=float.Parse(Arg("-soak-seconds","40"));
            Directory.CreateDirectory(folder);string path=Path.Combine(folder,role+".soak.txt");
            void Log(string text)=>File.AppendAllText(path,Time.realtimeSinceStartup.ToString("0.0")+" "+text+"\n");
            yield return new WaitForSecondsRealtime(1);
            Log(role+" start smoke="+g.SmokeMode);
            bool ok=role=="host"?g.Lan.Host("Soak"+role,port):g.Lan.Join("Soak"+role,"127.0.0.1",port);Log("open="+ok+" status="+g.Lan.Status);
            float end=Time.realtimeSinceStartup+seconds;bool left=false;
            while(Time.realtimeSinceStartup<end){
                Log("started="+g.Started+" running="+g.Lan.Running+" connected="+g.Lan.Connected+" snaps="+g.Lan.SnapshotsReceived+" actors="+(g.Lan.View?.actors?.Length??0)+" status="+g.Lan.Status+" toast="+g.Toast);
                if(!g.Started&&!left){left=true;Log("RETURNED TO MENU: "+g.Lan.Status+" | "+g.Toast);}
                yield return new WaitForSecondsRealtime(1);
            }
            Log("done left="+left);Application.Quit(left?1:0);
        }
    }
}
