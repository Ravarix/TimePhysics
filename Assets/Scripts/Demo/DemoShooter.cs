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
        
        private LayerMask _defaultLayer;
        private Camera _cam;
        public Camera Camera => _cam ?? (_cam = GetComponent<Camera>());
        
        private DemoShooterDebug _shooterDebug;
        public DemoShooterDebug ShooterDebug => _shooterDebug ?? (_shooterDebug = GetComponent<DemoShooterDebug>());
        
        public void Awake()
        {
            _defaultLayer = LayerMask.GetMask("Default");
        }

        public struct Command
        {
            public bool Primary;
            public bool Secondary;
            public Vector3 Position;
            public Vector3 Direction;

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
                Position = camRay.origin,
                Direction = camRay.direction
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
            Profiler.BeginSample("Rewind & Cast");
            using (TimePhysics.RewindSeconds(Random.Range(Ping - PingJitter, Ping + PingJitter))) // or using (TimePhysics.RewindFrames(FrameDelay))
            {
                //perform all raycasts for this rewindframe within block
                //proximity colliders have moved, will be reset after scope
                if (cmd.Primary)
                {
                    //Shoot(cmd.Frame, cmd.Position, cmd.Direction, Distance);
                    Spherecast(cmd.Position, .25f, cmd.Direction, Distance);
                }
                if (cmd.Secondary)
                {
                    AOE(cmd.Position, 10f, cmd.Direction, Distance);
                }
            }
            Profiler.EndSample();
        }

//example usage snippet
/*
private void Shoot(Vector3 origin, Vector3 direction, float distance, int layer)
{
    using (TimePhysics.RewindSeconds(Ping))
    {
        RaycastHit hit;
        if (TimePhysics.Raycast(origin, direction, distance, out hit, layer))
        {
            // hit code
        }
    }
}
*/

        public void Shoot(Vector3 origin, Vector3 direction, float distance)
        {
            RaycastHit hit;
            if (TimePhysics.Raycast(origin, direction, distance, out hit, _defaultLayer))
            {
                var marker = hit.collider.GetComponent<HitboxMarkerDebug>();
                if (marker != null && TimePhysics.DebugMode)
                    ShooterDebug?.DebugHitRewind(marker, 2f);

                HitCount++;
            }
            
        }
        
        public void AOE(Vector3 origin, float radius, Vector3 direction, float distance)
        {
            var hits = TimePhysics.OverlapSphereNonAlloc(origin, radius, ColliderCache, _defaultLayer);
            for (int i = 0; i < hits; i++)
            {
                var marker = ColliderCache[i].GetComponent<HitboxMarkerDebug>();
                if (marker != null && TimePhysics.DebugMode)
                    ShooterDebug?.DebugHitRewind(marker, 1f);

                HitCount++;
            }
        }

        public void Spherecast(Vector3 origin, float radius, Vector3 direction, float distance)
        {
            var hits = TimePhysics.SphereCastNonAlloc(origin, radius, direction, HitsCache, distance, _defaultLayer);
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