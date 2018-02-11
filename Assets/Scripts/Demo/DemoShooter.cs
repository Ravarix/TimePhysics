using System.Collections;
using Hitbox;
using UnityEngine;
using UnityEngine.Profiling;

namespace Demo
{

    public class DemoShooter : MonoBehaviour
    {
        
        public int FrameDelay = 30;
        public float Ping = .5f;
        public float PingJitter = .1f;
        public float RayDuration = .5f;
        public float Distance = 30f;
        public Color RayColor = Color.blue;

        private Command currentCommand;
        
        private static readonly RaycastHit[] HitsCache = new RaycastHit[512];
        private static readonly Collider[] ColliderCache = new Collider[512];
        private static readonly Vector3 CamViewport = new Vector3(.5f, .5f, 0f);

        public int HitCount { get; private set; }
        
        private Camera _cam;
        public Camera Camera => _cam ?? (_cam = GetComponent<Camera>());
        
        private DemoShooterDebug _shooterDebug;
        public DemoShooterDebug ShooterDebug => _shooterDebug ?? (_shooterDebug = GetComponent<DemoShooterDebug>());

        public struct Command
        {
            public bool Primary;
            public bool Secondary;
            public Ray Ray;

            // Whether we need to rewind to execute this command
            public bool Rewind => Primary || Secondary;
        }

        private Command PollInput()
        {
            var camRay = Camera.ViewportPointToRay(CamViewport);
            return new Command
            {
                Primary = Input.GetKeyDown(KeyCode.Mouse0) || currentCommand.Primary,
                Secondary = Input.GetKeyDown(KeyCode.Mouse1) || currentCommand.Secondary,
                Ray = camRay
            };
        }

        private void Update()
        {
            currentCommand = PollInput();
        }

        private void FixedUpdate()
        {
            ExecuteCommand(currentCommand);
            currentCommand = new Command();
        }

        private void ExecuteCommand(Command cmd)
        {
            if (!cmd.Rewind) // don't rewind if we don't have to
                return;
            var ping = Random.Range(Ping - PingJitter, Ping + PingJitter);
            Profiler.BeginSample("Rewind & Cast");
            using (TimePhysics.RewindSeconds(ping)) // or using (TimePhysics.RewindFrames(FrameDelay))
            {
                //perform all raycasts for this rewindframe within the 'using' block
                if (cmd.Primary)
                {
                    //Shoot(cmd.Ray, Distance);
                    Spherecast(cmd.Ray, .25f, Distance);
                }
                if (cmd.Secondary)
                {
                    AOE(cmd.Ray.origin, 10f, Distance);
                }
            }
            Profiler.EndSample();
        }

//example usage snippet
/*
private void Shoot(Ray ray)
{
    using (TimePhysics.RewindSeconds(Ping))
    {
        RaycastHit hit;
        if (TimePhysics.Raycast(ray, out hit))
        {
            // hit code
        }
    }
}
*/

        public void Shoot(Ray ray, float distance)
        {
            RaycastHit hit;
            if (TimePhysics.Raycast(ray, out hit, distance))
            {
                var marker = hit.collider.GetComponent<HitboxMarkerDebug>();
                if (marker != null && TimePhysics.DebugMode)
                    ShooterDebug?.DebugHitRewind(marker, 2f);
                HitCount++;
            }
            
        }
        
        public void AOE(Vector3 origin, float radius, float distance)
        {
            var hits = TimePhysics.OverlapSphereNonAlloc(origin, radius, ColliderCache);
            for (int i = 0; i < hits; i++)
            {
                var marker = ColliderCache[i].GetComponent<HitboxMarkerDebug>();
                if (marker != null && TimePhysics.DebugMode)
                    ShooterDebug?.DebugHitRewind(marker, 1f);
                HitCount++;
            }
        }

        public void Spherecast(Ray ray, float radius, float distance)
        {
            var hits = TimePhysics.SphereCastNonAlloc(ray, radius, HitsCache, distance);
            for (int i = 0; i < hits; i++)
            {
                var marker = HitsCache[i].collider.GetComponent<HitboxMarkerDebug>();
                if (marker != null && TimePhysics.DebugMode)
                    ShooterDebug?.DebugHitRewind(marker, 1f);
                HitCount++;
            }
        }
        
        private void OnGUI()
        {
            GUI.Label(new Rect(10,10,200,50), $"Hits: {HitCount}");
            GUI.Label(new Rect(10,60,200,50), $"FPS: {1f / Time.deltaTime}");
        }
        
    }
}