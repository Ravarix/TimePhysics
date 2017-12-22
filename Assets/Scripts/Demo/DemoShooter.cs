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
        public float RayDuration = .5f;
        public float Distance = 30f;
        public Color RayColor = Color.blue;

        private Command currentCommand;
        
        private readonly RaycastHit[] hitsCache = new RaycastHit[512];
        private readonly Collider[] colliderCache = new Collider[512];
        private readonly Vector3 camViewport = new Vector3(.5f, .5f, 0f);

        public int HitCount { get; private set; }
        
        private LayerMask defaultLayer;
        private Camera _cam;
        public Camera Camera => _cam ?? (_cam = GetComponent<Camera>());
        
        private DemoShooterDebug _shooterDebug;
        public DemoShooterDebug ShooterDebug => _shooterDebug ?? (_shooterDebug = GetComponent<DemoShooterDebug>());
        
        public void Awake()
        {
            defaultLayer = LayerMask.GetMask("Default");
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
            var camRay = Camera.ViewportPointToRay(camViewport);
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
            Profiler.BeginSample("Rewind & Cast");
//            using (TimePhysics.RewindSeconds(Ping))
            using (TimePhysics.RewindFrames(FrameDelay))
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

        public void Shoot(Vector3 origin, Vector3 direction, float distance)
        {
            RaycastHit hit;
            if (TimePhysics.Raycast(origin, direction, distance, out hit, defaultLayer))
            {
                var marker = hit.collider.GetComponent<HitboxMarkerDebug>();
                if (marker != null && TimePhysics.DebugMode)
                    ShooterDebug?.DebugHitRewind(marker, 2f);

                HitCount++;
            }
            
            Debug.DrawLine(origin, origin + direction * distance, RayColor, RayDuration);
        }
        
        public void AOE(Vector3 origin, float radius, Vector3 direction, float distance)
        {
            var hits = TimePhysics.OverlapSphereNonAlloc(origin, radius, colliderCache, defaultLayer);
            for (int i = 0; i < hits; i++)
            {
                var marker = colliderCache[i].GetComponent<HitboxMarkerDebug>();
                if (marker != null && TimePhysics.DebugMode)
                    ShooterDebug?.DebugHitRewind(marker, 1f);

                HitCount++;
            }
        }

        public void Spherecast(Vector3 origin, float radius, Vector3 direction, float distance)
        {
            var hits = TimePhysics.SphereCastNonAlloc(origin, radius, direction, hitsCache, distance, defaultLayer);
            for (int i = 0; i < hits; i++)
            {
                var marker = hitsCache[i].collider.GetComponent<HitboxMarkerDebug>();
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