using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TavernChaos
{
    // Virtual devices exercise the shipped action maps and Update handlers, not just pool APIs.
    public static class PoolInputChecks
    {
        private static IEnumerator KeyTap(Keyboard keyboard,Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null; yield return new WaitForEndOfFrame();
        }
        private static void MouseStateAt(Mouse mouse,Vector2 point,bool down=false,float scroll=0) => InputSystem.QueueStateEvent(mouse,new MouseState { position=point, buttons=(ushort)(down?1:0), scroll=new Vector2(0,scroll) });
        public static IEnumerator Run(DemoGame g,Action<bool,string> check)
        {
            var disabled=new List<InputDevice>();
            foreach(var device in InputSystem.devices) if(device.enabled&&(device is Mouse||device is Keyboard||device is Gamepad)) { disabled.Add(device); InputSystem.DisableDevice(device); }
            var mouse=InputSystem.AddDevice<Mouse>(); var keyboard=InputSystem.AddDevice<Keyboard>();
            g.TestDriveInput=true; g.Menu(); yield return new WaitForSecondsRealtime(.4f);
            yield return KeyTap(keyboard,Key.Enter); yield return null;
            check(g.Started&&!g.Paused,"UX: Enter starts the session through the real menu actions");
            g.Player.Teleport(new Vector3(3.7f,.08f,.4f),Quaternion.Euler(0,90,0)); g.Pool.SelectDrill(PoolDrill.Straight); g.Pool.Enter();
            yield return new WaitForSeconds(.6f);
            check(Cursor.visible&&Cursor.lockState==CursorLockMode.None,"UX: billiards releases the cursor without freeing character movement");
            Vector2 aim=Camera.main.WorldToScreenPoint(PoolPractice.Center+new Vector3(.7f,0,0));
            MouseStateAt(mouse,aim); yield return null; yield return null;
            check(Vector2.Distance(g.Input.Point.ReadValue<Vector2>(),aim)<1,"UX: changing game context does not warp the pool cursor away from its input position");
            check(Vector3.Dot(g.Pool.Direction,Vector3.right)>.99f,"UX: mouse points directly from cue to the selected cloth position (direction="+g.Pool.Direction+")");
            Vector2 uiPoint=new(Screen.width*.84f,Screen.height*.75f);
            MouseStateAt(mouse,uiPoint,true); yield return null; MouseStateAt(mouse,uiPoint); yield return null;
            check(g.Pool.Shots==0,"UX: clicking the power panel does not also shoot");
            float before=g.Pool.Power;
            MouseStateAt(mouse,aim,false,120); yield return null; MouseStateAt(mouse,aim); yield return null;
            check(Mathf.Abs(g.Pool.Power-before-.05f)<.005f,"UX: one wheel step changes power by five percentage points");
            MouseStateAt(mouse,aim,true); yield return null; yield return new WaitForFixedUpdate(); yield return new WaitForSeconds(.12f);
            check(g.Pool.Shots==1&&g.Pool.Moving,"UX: one click fires the aimed shot at the selected power (shots="+g.Pool.Shots+", phase="+g.Pool.Phase+")");
            yield return new WaitForSeconds(.25f); check(g.Pool.Shots==1,"UX: holding the shot button cannot repeat-fire (shots="+g.Pool.Shots+")");
            MouseStateAt(mouse,aim); yield return null;
            yield return KeyTap(keyboard,Key.Escape);
            check(g.Paused&&g.Pool.Active,"UX: Escape pauses a pool shot without leaving the table");
            yield return KeyTap(keyboard,Key.Enter); yield return null;
            check(!g.Paused&&g.Pool.Active,"UX: Enter resumes the focused pause action");
            yield return KeyTap(keyboard,Key.E); yield return new WaitForSeconds(.2f);
            check(!g.Pool.Active&&Cursor.lockState==CursorLockMode.Locked&&g.Hits==0&&g.Misses==0,"UX: leaving pool restores walking controls without an accidental punch or re-entry");
            g.Menu(); yield return null; yield return KeyTap(keyboard,Key.DownArrow); yield return KeyTap(keyboard,Key.Enter); yield return new WaitForSeconds(.3f);
            check(g.Started&&g.Pool.Active,"UX: the keyboard-accessible main-menu shortcut opens billiards directly");
            g.TestDriveInput=false;
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
            foreach(var device in disabled) InputSystem.EnableDevice(device);
        }
    }
}
