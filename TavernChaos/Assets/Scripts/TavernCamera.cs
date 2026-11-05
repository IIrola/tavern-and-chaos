using UnityEngine;

namespace TavernChaos
{
    public sealed class TavernCamera : MonoBehaviour
    {
        public float Yaw=0, Pitch=23, Sensitivity=.13f;
        public bool ReduceMotion;
        private float trauma;
        private Vector3 smooth, velocity;
        private Camera lens;
        private bool wasPool;
        private void Awake() { lens=GetComponent<Camera>(); }
        public Vector3 Forward => Quaternion.Euler(0,Yaw,0)*Vector3.forward;
        public Vector3 Right => Quaternion.Euler(0,Yaw,0)*Vector3.right;
        public void AddImpact(float value) { trauma=Mathf.Clamp01(trauma+value); }
        public void Look(Vector2 delta) { Yaw+=delta.x*Sensitivity; Pitch=Mathf.Clamp(Pitch-delta.y*Sensitivity,8,58); }
        private void LateUpdate()
        {
            var g=DemoGame.Instance; if(!g || !g.Player) return;
            if(wasPool!=g.Pool.Active) { Snap(); wasPool=g.Pool.Active; }
            lens.orthographic=g.Pool.Active; lens.orthographicSize=4.1f;
            Vector3 target, desired;
            if(g.Pool.Active) { target=PoolPractice.Center; desired=target+new Vector3(0,4.1f,-.02f); }
            else if(!g.Started) { target=new Vector3(0,1.9f,3.8f); desired=new Vector3(9.8f,5.7f,-7.3f); }
            else {
                target=g.Player.Focus+Vector3.up*.48f;
                Vector3 offset=Quaternion.Euler(Pitch,Yaw,0)*new Vector3(.4f,0,-5.0f);
                float distance=offset.magnitude;
                // Ignore actor/prop layers. Only the environment may shorten the camera arm.
                if(Physics.SphereCast(target,.18f,offset.normalized,out var hit,distance,1<<0,QueryTriggerInteraction.Ignore)) distance=Mathf.Max(.55f,hit.distance-.1f);
                desired=target+offset.normalized*distance;
            }
            if(smooth==Vector3.zero) smooth=desired;
            smooth=Vector3.SmoothDamp(smooth,desired,ref velocity,g.Pool.Active?.18f:.1f,Mathf.Infinity,Time.unscaledDeltaTime);
            trauma=Mathf.Max(0,trauma-Time.unscaledDeltaTime*2.8f);
            Vector3 shake=ReduceMotion?Vector3.zero:new Vector3(Mathf.Sin(Time.unscaledTime*43),Mathf.Sin(Time.unscaledTime*57),0)*trauma*trauma*.1f;
            transform.position=smooth+shake; transform.rotation=Quaternion.LookRotation(target-transform.position);
        }
        public void Snap() { smooth=Vector3.zero; velocity=Vector3.zero; }
    }
}
