using UnityEngine;

namespace TavernChaos
{
    [DefaultExecutionOrder(100)]
    public sealed class TavernCamera : MonoBehaviour
    {
        public float Yaw=0, Pitch=0, Sensitivity=.13f;
        public bool ReduceMotion;
        private float trauma;
        private Vector3 smooth, velocity;
        private Camera lens;
        private CameraClearFlags normalClearFlags;
        private bool wasPool;
        private bool snapPending=true;
        private SessionEndPresentation ending;
        private void Awake() { lens=GetComponent<Camera>(); normalClearFlags=lens.clearFlags; }
        public Vector3 Forward => Quaternion.Euler(0,Yaw,0)*Vector3.forward;
        public Vector3 Right => Quaternion.Euler(0,Yaw,0)*Vector3.right;
        public Vector3 ViewForward => Quaternion.Euler(Pitch,Yaw,0)*Vector3.forward;
        public void AddImpact(float value) { trauma=Mathf.Clamp01(trauma+value); }
        public void Look(Vector2 delta) { Yaw=Mathf.Repeat(Yaw+delta.x*Sensitivity,360); Pitch=Mathf.Clamp(Pitch-delta.y*Sensitivity,-85,85); }
        private void LateUpdate()
        {
            var g=DemoGame.Instance; if(!g || !g.Player) return;
            if(!ending) ending=g.GetComponent<SessionEndPresentation>();
            if(ending&&ending.TryCamera(out var endPosition,out var endTarget)) {
                g.Player.SetFirstPersonView(false); lens.orthographic=false; lens.fieldOfView=60; lens.farClipPlane=20; lens.clearFlags=CameraClearFlags.SolidColor;
                transform.SetPositionAndRotation(endPosition,Quaternion.LookRotation(endTarget-endPosition)); return;
            }
            lens.farClipPlane=70; lens.clearFlags=normalClearFlags;
            if(wasPool!=g.Pool.Active) { Snap(); wasPool=g.Pool.Active; }
            bool arm=g.Lan&&g.Lan.LocalArmSeated;
            bool firstPerson=g.Started&&!g.Pool.Active&&!arm;
            g.Player.SetFirstPersonView(firstPerson);
            lens.orthographic=g.Pool.Active; lens.orthographicSize=4.1f;
            lens.fieldOfView=firstPerson?75:arm?50:60;
            Vector3 target, desired;
            if(arm){target=ArmRoomController.Center+Vector3.up*1.2f;desired=ArmRoomController.Center+new Vector3(0,2.8f,-3.8f);}
            else if(g.Pool.Active) { target=PoolPractice.Center; desired=target+new Vector3(0,4.1f,-.02f); }
            else if(!g.Started) { target=new Vector3(0,1.9f,3.8f); desired=new Vector3(9.8f,5.7f,-7.3f); }
            else {
                desired=g.Player.EyePosition;
                target=desired+ViewForward;
            }
            // Standing first-person view follows directly; resets must never drag the eye from its old location.
            if(snapPending || (firstPerson&&!g.Player.Fallen)) { smooth=desired; velocity=Vector3.zero; snapPending=false; }
            else smooth=Vector3.SmoothDamp(smooth,desired,ref velocity,g.Pool.Active?.18f:.08f,Mathf.Infinity,Time.unscaledDeltaTime);
            trauma=Mathf.Max(0,trauma-Time.unscaledDeltaTime*2.8f);
            Vector3 shake=ReduceMotion?Vector3.zero:new Vector3(Mathf.Sin(Time.unscaledTime*43),Mathf.Sin(Time.unscaledTime*57),0)*trauma*trauma*.1f;
            Vector3 sway=firstPerson&&!ReduceMotion&&g.Player.Ebriety>0?new Vector3(Mathf.Sin(Time.unscaledTime*1.7f),Mathf.Sin(Time.unscaledTime*2.1f),0)*(.008f*g.Player.Ebriety):Vector3.zero;
            transform.position=smooth+shake+sway;
            // Keep the horizon stable while the ragdoll tumbles; mouse look remains available.
            transform.rotation=firstPerson?Quaternion.Euler(Pitch,Yaw,0):Quaternion.LookRotation(target-transform.position);
        }
        public void Snap()
        {
            snapPending=true; velocity=Vector3.zero; trauma=0;
            var game=DemoGame.Instance;
            if(game&&game.Player&&game.Started&&!game.Pool.Active)
                transform.SetPositionAndRotation(game.Player.EyePosition,Quaternion.Euler(Pitch,Yaw,0));
        }
    }
}
