using UnityEngine;
namespace TavernChaos
{
    public sealed class PoolBallSound : MonoBehaviour
    {
        private void OnCollisionEnter(Collision c) => DemoGame.Instance?.Pool?.ContactSound(c.relativeVelocity.magnitude);
    }
}
