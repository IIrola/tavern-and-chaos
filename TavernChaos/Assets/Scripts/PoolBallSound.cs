using UnityEngine;
namespace TavernChaos
{
    public sealed class PoolBallSound : MonoBehaviour
    {
        public int Number;
        private void OnCollisionEnter(Collision c) { var pool=DemoGame.Instance?.Pool; pool?.ContactSound(c.relativeVelocity.magnitude,transform.position); pool?.ObserveContact(Number,c); }
    }
}
