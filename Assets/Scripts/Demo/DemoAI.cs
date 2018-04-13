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
        public GameObject RayOrigin;
        public float SphereRadius = .25f;
        public float ShootDistance = 5f;
        public float ShootFrequency = 2f;
        public float ShootRandom = 5f;
        public Color ShotColor = new Color(0f, 1f, 1f, .5f);
        
        public NavMeshAgent Agent { get; private set; }
        public Animator Animator { get; private set; }

        public Transform Transform { get; private set; }
        private float _targetDistance = 10f;
        
        private DemoShooterDebug _shooterDebug;
        public DemoShooterDebug ShooterDebug => _shooterDebug ?? (_shooterDebug = GetComponent<DemoShooterDebug>());
        private HitboxBody _hitboxBody;
        private HitboxBody HitboxBody => _hitboxBody ?? (_hitboxBody = GetComponent<HitboxBody>());
        
        private float _shotCooldown = 1f; //wait a second for history to build

        private void Start()
        {
            Agent = GetComponentInChildren<NavMeshAgent>();
            Agent.speed = Speed;
            Animator = GetComponent<Animator>();
            Transform = transform;
            
            Animator.SetFloat("Forward", .5f);
        }

        private void Update()
        {
            if (!Roam)
                return;

            if (Agent.remainingDistance <= Agent.stoppingDistance)
                Agent.SetDestination(FindNewTarget());
            else
                Animator.SetFloat("Forward", Mathf.Max(Agent.remainingDistance / _targetDistance, .8f) + .2f);
        }

        private void FixedUpdate()
        {
            if (!Shoot)
                return;
            
            _shotCooldown -= Time.fixedDeltaTime;
            if (_shotCooldown <= 0f)
            {
                Fire();
                _shotCooldown = ShootFrequency + Random.Range(0f, ShootRandom);
            }
        }

        private void Fire()
        {
            //convert ping to float seconds
            var ping = (BasePingMs + Random.Range(0, PingRandomMs)) / 1000f;

            using (TimePhysics.RewindSeconds(ping, HitboxBody))
            {
                Ray ray = new Ray(RayOrigin.transform.position, RayOrigin.transform.forward * ShootDistance);
                RaycastHit hit;
                Debug.DrawRay(ray.origin, ray.direction, ShotColor);
                if(TimePhysics.SphereCast(ray, SphereRadius, out hit, ShootDistance))
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
            _targetDistance = Vector3.Distance(currPos, dest);
            return dest;
        }

        private void MoveToDestination()
        {
            Transform.position += Transform.forward * (Speed * Time.deltaTime);
        }
    }
}