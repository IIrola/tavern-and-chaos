using UnityEngine;

namespace TavernChaos
{
    // Planar billiards: PhysX solves sphere/cushion contacts; cloth resistance is constant,
    // rather than air drag. English, jumping and masse shots are outside this practice mode.
    public static class PoolPhysics
    {
        public const int BallLayer = 10, CushionLayer = 11;
        public const float Radius = .12f, Mass = .17f;
        public const float HalfWidth = 1.5f, HalfLength = 2.55f;
        public const float RollingResistance = .38f, StopSpeed = .018f, SettleTime = .22f;
        public const float BallRestitution = .96f, CushionRestitution = .76f;
        public const float MaxSpeed = 4.6f;
        public static float ShotSpeed(float power) => Mathf.Lerp(.45f, MaxSpeed, Mathf.Pow(Mathf.Clamp01(power), 1.35f));
        public static Vector3 Slow(Vector3 velocity, float dt)
        {
            velocity.y = 0;
            return Vector3.MoveTowards(Vector3.ClampMagnitude(velocity, MaxSpeed), Vector3.zero, RollingResistance * dt);
        }
        public static PhysicsMaterial Material(string name, float bounce) => new(name)
        {
            bounciness = bounce, dynamicFriction = 0, staticFriction = 0,
            bounceCombine = PhysicsMaterialCombine.Minimum, frictionCombine = PhysicsMaterialCombine.Minimum
        };
        public static void Configure(Rigidbody rb)
        {
            rb.mass = Mass; rb.useGravity = false; rb.linearDamping = 0; rb.angularDamping = 0;
            rb.constraints = RigidbodyConstraints.FreezePositionY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.solverIterations = 12; rb.solverVelocityIterations = 12;
            rb.sleepThreshold = .00001f; rb.maxAngularVelocity = MaxSpeed / Radius + 1;
            rb.maxDepenetrationVelocity = 1;
        }
    }
}
