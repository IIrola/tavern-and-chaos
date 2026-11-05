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
        public bool FirstPersonOnly;
        private void Check(bool ok,string name) { (ok?result.checks:result.failures).Add(name); Debug.Log((ok?"PASS: ":"FAIL: ")+name); }
        private void Log(string condition,string trace,LogType type) { if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert) result.runtimeErrors.Add(condition+"\n"+trace); }
        private static void Quit(int code)
        {
#if UNITY_EDITOR
            if(Application.isBatchMode) UnityEditor.EditorApplication.Exit(code);
            else UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(code);
#endif
        }
        private IEnumerator Start()
        {
            Application.logMessageReceived+=Log; g=DemoGame.Instance;
            var args=Environment.GetCommandLineArgs(); int p=Array.IndexOf(args,"-evidencePath"); output=p>=0&&p+1<args.Length?args[p+1]:FirstPersonOnly?Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/FirstPersonChecks")):Path.Combine(Application.persistentDataPath,"Evidence"); Directory.CreateDirectory(output);
            result.unity=Application.unityVersion; result.platform=Application.platform.ToString();
            if(FirstPersonOnly||Array.IndexOf(args,"-first-person-only")>=0) {
                yield return FirstPersonChecks();
                Check(result.runtimeErrors.Count==0,"No runtime errors or exceptions"); result.passed=result.failures.Count==0&&result.runtimeErrors.Count==0;
                File.WriteAllText(Path.Combine(output,"player-tests.json"),JsonUtility.ToJson(result,true));
                Application.logMessageReceived-=Log; Quit(result.passed?0:1); yield break;
            }
            if(Array.IndexOf(args,"-pool-input-only")>=0) {
                yield return PoolInputChecks.Run(g,Check);
                Check(result.runtimeErrors.Count==0,"No runtime errors or exceptions"); result.passed=result.failures.Count==0&&result.runtimeErrors.Count==0;
                File.WriteAllText(Path.Combine(output,"player-tests.json"),JsonUtility.ToJson(result,true));
                Application.logMessageReceived-=Log; Quit(result.passed?0:1); yield break;
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
            yield return FirstPersonChecks();
            yield return new WaitForSeconds(2.1f); int objects=FindObjectsByType<ThrowableBottle>().Length; Check(objects==5,"Throwable inventory stays bounded at five bottles");
            g.ResetSession(); Check(g.Hits==0&&g.Misses==0&&!g.Complete&&!g.Player.Fallen&&!g.Rival.Fallen&&g.Held==null,"Session reset clears objectives, actors, props and effects");
            yield return new WaitForSeconds(.5f); ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-gameplay.png")); yield return new WaitForEndOfFrame();
            Check(result.runtimeErrors.Count==0,"No runtime errors or exceptions"); result.passed=result.failures.Count==0&&result.runtimeErrors.Count==0;
            File.WriteAllText(Path.Combine(output,"player-tests.json"),JsonUtility.ToJson(result,true)); Debug.Log("DEMO_PLAYER_TESTS: "+(result.passed?"PASSED":"FAILED"));
            Application.logMessageReceived-=Log; yield return new WaitForSecondsRealtime(.3f); Quit(result.passed?0:1);
        }
        private IEnumerator FirstPersonChecks()
        {
            var backgroundBehavior=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
            // Virtual devices must keep receiving queued states when the automated player is hidden.
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            var disabled=new List<UnityEngine.InputSystem.InputDevice>();
            foreach(var device in UnityEngine.InputSystem.InputSystem.devices) if(device.enabled&&(device is UnityEngine.InputSystem.Mouse||device is UnityEngine.InputSystem.Keyboard||device is UnityEngine.InputSystem.Gamepad)) { disabled.Add(device); UnityEngine.InputSystem.InputSystem.DisableDevice(device); }
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            bool reduceMotion=g.Rig.ReduceMotion;
            GameObject wall=null;
            void Keys(params UnityEngine.InputSystem.Key[] keys) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));
            g.TestDriveInput=true;
            try {
                g.Begin(); g.Rig.ReduceMotion=true; yield return null; yield return null;
                Check(Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition)<.03f&&!Camera.main.orthographic,"First person: camera starts at the player's eyes");
                Check(g.Player.transform.Find("Articulated character/Head").GetComponent<Renderer>().forceRenderingOff,"First person: local head is hidden from inside view");
                g.Rig.Look(new Vector2(0,10000)); yield return null; yield return null;
                Check(g.Rig.Pitch==-85&&g.Rig.transform.forward.y>.99f,"First person: can look up without flipping");
                g.Rig.Look(new Vector2(0,-20000)); yield return null; yield return null;
                Check(g.Rig.Pitch==85&&g.Rig.transform.forward.y<-.99f,"First person: can look down without flipping");
                g.Rig.Pitch=0; g.Rig.Yaw=90;
                Keys(UnityEngine.InputSystem.Key.D); yield return new WaitForSeconds(.25f);
                Check(Vector3.Dot(g.Player.transform.forward,g.Rig.Forward)>.99f,"First person: strafing keeps the body facing the view");
                Keys(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift); yield return new WaitForSeconds(.4f);
                Keys(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift,UnityEngine.InputSystem.Key.R); yield return null; yield return null;
                Check(Vector3.Distance(g.Player.transform.position,g.Player.Home)<.15f&&g.Rig.Yaw==0&&g.Rig.Pitch==0,"Reset: held movement resets the controller and camera together");
                // Sample the rendered view after LateUpdate, not before its normal per-frame follow.
                yield return new WaitForEndOfFrame();
                float cameraOffset=Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition);
                Check(cameraOffset<.03f,"Reset: camera immediately follows the reset position (offset="+cameraOffset.ToString("0.000")+" m)");
                Keys(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift); yield return new WaitForSeconds(.15f);
                Check(g.Player.transform.position.z>g.Player.Home.z&&Mathf.Abs(g.Player.transform.position.x-g.Player.Home.x)<.03f,"Reset: held movement resumes from spawn in the reset direction");
                Keys(); yield return null; yield return null;
                wall=TavernWorld.Box("Slip regression wall",new(0,1.5f,-3.5f),new(10,3,.2f),TavernWorld.DarkWood);
                Physics.SyncTransforms();
                g.Player.Teleport(new Vector3(0,.08f,-7),Quaternion.identity);
                Keys(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift); yield return new WaitForSeconds(1.3f);
                Check(!g.Player.Fallen&&g.Player.transform.position.z<-3.8f,"Laguna Azul: a fast wall collision without the drink does not knock down");
                Keys(); yield return null; yield return null;
                g.Player.Teleport(new Vector3(0,.08f,-4.35f),Quaternion.identity); g.Player.Drink();
                Keys(UnityEngine.InputSystem.Key.W); yield return new WaitForSeconds(1.3f);
                Check(!g.Player.Fallen&&g.Player.EffectRemaining>0,"Laguna Azul: a slow collision and holding against a wall do not invent impact speed");
                Keys(); yield return null; yield return null;
                g.Player.Teleport(new Vector3(-3,.08f,-4.02f),Quaternion.identity); g.Player.Drink();
                Keys(UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.LeftShift); yield return new WaitForSeconds(1.1f);
                Check(!g.Player.Fallen,"Laguna Azul: running parallel to a wall and over the floor does not knock down");
                Keys(); yield return null; yield return null;
                g.Player.Teleport(new Vector3(0,.08f,-7),Quaternion.identity); g.Player.Drink();
                Keys(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift); yield return new WaitForSeconds(.65f);
                Check(!g.Player.Fallen,"Laguna Azul: reaching speed alone does not cause a fall");
                float until=Time.time+2;
                while(!g.Player.Fallen&&Time.time<until) yield return null;
                Keys();
                Check(g.Player.Fallen&&g.Player.EffectRemaining==0,"Laguna Azul: a fast frontal collision triggers ragdoll and clears the effect");
                yield return new WaitForSeconds(3.2f);
                Check(!g.Player.Fallen&&Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition)<.03f,"Laguna Azul: collision fall recovers to first-person control");
                g.Pool.Enter(); yield return null; yield return null;
                Check(Camera.main.orthographic&&!g.Player.transform.Find("Articulated character/Head").GetComponent<Renderer>().forceRenderingOff,"First person: billiards keeps its overhead camera and restores the visible head");
                g.Pool.Exit(); yield return null; yield return null;
                Check(!Camera.main.orthographic&&Vector3.Distance(g.Rig.transform.position,g.Player.EyePosition)<.03f,"First person: leaving billiards returns directly to eye view");
                wall.SetActive(false); Physics.SyncTransforms();
                g.ResetSession(); g.Player.Teleport(new Vector3(0,.08f,-1.8f),Quaternion.identity);
                g.QueueAction(.8f); yield return new WaitForSeconds(.2f);
                Check(g.Rival.Fallen&&g.Hits==1,"First person: a forward punch still knocks down Bruno");
                g.ResetSession(); g.Player.Teleport(new Vector3(-2.2f,.08f,-3.5f),Quaternion.identity); g.Interact();
                Check(g.Held!=null,"First person: the bottle rack remains reachable");
                if(g.Held) {
                    var bottle=g.Held;
                    g.Player.Teleport(new Vector3(0,.08f,-4),Quaternion.identity);
                    g.Rig.Pitch=-45; g.QueueAction(1); yield return new WaitForSeconds(.06f);
                    Check(!g.Held&&bottle.Flying&&bottle.Body.linearVelocity.y>8,"First person: looking up launches the bottle upward (vertical speed="+bottle.Body.linearVelocity.y.ToString("0.00")+" m/s)");
                }
                g.ResetSession();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState { buttons=1 });
                yield return new WaitForSeconds(.15f);
                Check(g.Charging,"Actions: mouse input starts a charge");
                Keys(UnityEngine.InputSystem.Key.Q); yield return null; yield return new WaitForFixedUpdate(); yield return null;
                Check(!g.Charging,"Actions: drop cancels the active charge even without a bottle");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState());
                Keys(); yield return new WaitForSeconds(.4f);
                Check(!g.Player.Fallen&&g.Misses==0,"Actions: releasing after cancellation cannot queue a phantom punch");
            } finally {
                if(wall) Destroy(wall);
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
                foreach(var device in disabled) UnityEngine.InputSystem.InputSystem.EnableDevice(device);
                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=backgroundBehavior;
                g.TestDriveInput=false; g.Rig.ReduceMotion=reduceMotion; g.ResetSession();
            }
        }
    }
}
