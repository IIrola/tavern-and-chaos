using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TavernChaos
{
    // Opt-in packaged-player tests. Runs real scene physics; writes evidence and a nonzero exit on failure.
    public sealed class DemoSmokeTests : MonoBehaviour
    {
        [Serializable] private sealed class Result { public string unity,platform; public bool passed; public List<string> checks=new(); public List<string> failures=new(); public List<string> runtimeErrors=new(); }
        private readonly Result result=new();
        private string output;
        private DemoGame g;
        private void Check(bool ok,string name) { (ok?result.checks:result.failures).Add(name); Debug.Log((ok?"PASS: ":"FAIL: ")+name); }
        private void Log(string condition,string trace,LogType type) { if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert) result.runtimeErrors.Add(condition+"\n"+trace); }
        private IEnumerator Start()
        {
            Application.logMessageReceived+=Log; g=DemoGame.Instance;
            var args=Environment.GetCommandLineArgs(); int p=Array.IndexOf(args,"-evidencePath"); output=p>=0&&p+1<args.Length?args[p+1]:Path.Combine(Application.persistentDataPath,"Evidence"); Directory.CreateDirectory(output);
            result.unity=Application.unityVersion; result.platform=Application.platform.ToString();
            if(Array.IndexOf(args,"-pool-input-only")>=0) {
                yield return PoolInputChecks.Run(g,Check);
                Check(result.runtimeErrors.Count==0,"No runtime errors or exceptions"); result.passed=result.failures.Count==0&&result.runtimeErrors.Count==0;
                File.WriteAllText(Path.Combine(output,"player-tests.json"),JsonUtility.ToJson(result,true));
                Application.logMessageReceived-=Log; Application.Quit(result.passed?0:1); yield break;
            }
            yield return new WaitForSecondsRealtime(1.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-menu.png")); yield return new WaitForEndOfFrame();
            g.Begin(); yield return new WaitForSeconds(.3f);
            Check(g.Started&&!g.Paused,"Session starts with active controls");
            g.Player.Teleport(new Vector3(0,.08f,-1.8f),Quaternion.identity); yield return new WaitForFixedUpdate(); g.QueueAction(.8f); yield return new WaitForSeconds(.2f);
            Check(g.Rival.Fallen&&g.Hits==1&&g.Goals[0],"Charged in-range punch knocks down rival exactly once");
            yield return new WaitForSeconds(.4f); Check(g.Hits==1,"Impact cannot repeat within one attack");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-impact.png")); yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(3); Check(!g.Rival.Fallen,"Articulated rival recovers automatically after three seconds");
            g.Player.Teleport(new Vector3(0,.08f,-4),Quaternion.Euler(0,180,0)); g.QueueAction(.5f); yield return new WaitForSeconds(.4f);
            Check(g.Player.Fallen&&g.Misses==1&&g.Goals[1],"Missed punch knocks down player after impact window");
            yield return new WaitForSeconds(3.2f); Check(!g.Player.Fallen,"Player regains control after a missed punch");
            g.Player.Teleport(TavernWorld.BarPoint,Quaternion.identity); g.Interact();
            Check(g.Player.EffectRemaining>7&&g.Goals[2],"Bar interaction activates Laguna Azul");
            yield return new WaitForSeconds(1); g.Interact(); Check(g.Player.EffectRemaining>7.5f,"Repeated drink refreshes duration without stacking");
            g.Player.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity); g.Interact();
            Check(g.Held!=null,"Nearby bottle can be grabbed");
            if(g.Held) {
                g.Player.Teleport(new Vector3(-.55f,.08f,-2.6f),Quaternion.identity); g.QueueAction(.8f); yield return new WaitForSeconds(.5f);
                Check(g.Held==null&&g.Throws==1,"Charged release throws the held physics body once");
                Check(g.Rival.Fallen&&g.Goals[3],"Swept bottle impact knocks down rival");
            }
            Check(g.Complete,"All four first-round objectives can be completed");
            yield return new WaitForSeconds(3.3f);
            g.Player.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity); g.Interact();
            bool hadBottle=g.Held!=null; g.Player.Fall(Vector3.back*2); yield return new WaitForFixedUpdate();
            Check(hadBottle&&g.Held==null&&g.Player.EffectRemaining==0,"Falling releases bottle and clears drink effect");
            yield return new WaitForSeconds(3.2f);
            g.Drink(); yield return new WaitForSeconds(8.2f); Check(g.Player.EffectRemaining==0,"Drink expires and restores normal movement modifier");
            g.SetPause(true); float before=Time.time; yield return new WaitForSecondsRealtime(.3f); Check(g.Paused&&Mathf.Approximately(Time.time,before),"Pause freezes simulation"); g.SetPause(false);
            g.Player.Teleport(TavernWorld.PoolPoint,Quaternion.identity); g.Interact(); Check(g.Pool.Active&&g.Player.transform.position.x<4,"Pool table enters dedicated camera and moves player clear of balls");
            yield return new WaitForSeconds(.3f); g.QueueAction(.8f); yield return new WaitForSeconds(.15f); Check(g.Pool.Moving,"Practice cue applies impulse to the real physics ball");
            yield return new WaitForSeconds(4);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-pool.png")); yield return new WaitForEndOfFrame();
            g.Pool.Reset(); Check(!g.Pool.Moving&&g.Pool.Potted==0,"Pool reset stops and restores all balls"); g.Interact(); Check(!g.Pool.Active,"Pool exit returns to character controls");
            yield return PoolPhysicsChecks.Run(g,Check,output);
            yield return PoolInputChecks.Run(g,Check);
            yield return new WaitForSeconds(2.1f); int objects=FindObjectsByType<ThrowableBottle>().Length; Check(objects==5,"Throwable inventory stays bounded at five bottles");
            g.ResetSession(); Check(g.Hits==0&&g.Misses==0&&!g.Complete&&!g.Player.Fallen&&!g.Rival.Fallen&&g.Held==null,"Session reset clears objectives, actors, props and effects");
            yield return new WaitForSeconds(.5f); ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-gameplay.png")); yield return new WaitForEndOfFrame();
            Check(result.runtimeErrors.Count==0,"No runtime errors or exceptions"); result.passed=result.failures.Count==0&&result.runtimeErrors.Count==0;
            File.WriteAllText(Path.Combine(output,"player-tests.json"),JsonUtility.ToJson(result,true)); Debug.Log("DEMO_PLAYER_TESTS: "+(result.passed?"PASSED":"FAILED"));
            Application.logMessageReceived-=Log; yield return new WaitForSecondsRealtime(.3f); Application.Quit(result.passed?0:1);
        }
    }
}
