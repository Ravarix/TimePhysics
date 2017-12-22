using Hitbox;
using UnityEngine;
using UnityEngine.AI;

namespace Demo
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class DemoAI : MonoBehaviour
    {
        public bool Roam = true;
        public float RoamDistance = 10f;
        public float Speed = 10f;
        public float StoppingDistance = .5f;
        
        public bool Shoot = true;
        public int BasePingMs = 80;
        public int PingRandomMs = 100;
        public float ShootHeight = 1.5f;
        public float ShootDistance = 5f;
        public float ShootFrequency = 2f;
        public float ShootRandom = 5f;
        public Color ShotColor = new Color(0f, 1f, 1f, .5f);
        
        public NavMeshAgent agent { get; private set; }
        public Animator animator { get; private set; }

        public Transform Transform { get; private set; }
        private float targetDistance = 10f;
        
        private DemoShooterDebug _shooterDebug;
        public DemoShooterDebug ShooterDebug => _shooterDebug ?? (_shooterDebug = GetComponent<DemoShooterDebug>());

        private float shotCooldown = 1f; //wait a second for history to build

        private void Start()
        {
            agent = GetComponentInChildren<NavMeshAgent>();
            agent.speed = Speed;
            animator = GetComponent<Animator>();
            Transform = transform;
            
            animator.SetFloat("Forward", .5f);
        }

        private void Update()
        {
            if (!Roam)
                return;

            if (agent.remainingDistance <= agent.stoppingDistance)
                agent.SetDestination(FindNewTarget());
            else
                animator.SetFloat("Forward", Mathf.Max(agent.remainingDistance / targetDistance, .8f) + .2f);
        }

        private void FixedUpdate()
        {
            if (!Shoot)
                return;
            
            shotCooldown -= Time.fixedDeltaTime;
            if (shotCooldown <= 0f)
            {
                Fire();
                shotCooldown = ShootFrequency + Random.Range(0f, ShootRandom);
            }
        }

        private void Fire()
        {
            //convert ping to float seconds
            var ping = (BasePingMs + Random.Range(0, PingRandomMs)) / 1000f;

            using (TimePhysics.RewindSeconds(ping))
            {
                RaycastHit hit;
                var origin = Transform.position + Vector3.up * ShootHeight;
                Debug.DrawRay(origin, Transform.forward * ShootDistance, ShotColor);
                if(TimePhysics.Raycast(origin, Transform.forward, ShootDistance, out hit, LayerMask.GetMask("Default")))
                {
                    var marker = hit.collider.GetComponent<HitboxMarkerDebug>();
                    if (marker != null && TimePhysics.DebugMode)
                        ShooterDebug?.DebugHitRewind(marker, 2f);
                }
            }
        }

        private Vector3 FindNewTarget()
        {
            var rand = Random.insideUnitCircle;
            var currPos = Transform.position;
            var dest = new Vector3(
                currPos.x + rand.x * RoamDistance,
                currPos.y,
                currPos.z + rand.y * RoamDistance);
            targetDistance = Vector3.Distance(currPos, dest);
            return dest;
        }

        private void MoveToDestination()
        {
            Transform.position += Transform.forward * (Speed * Time.deltaTime);
        }
    }
}