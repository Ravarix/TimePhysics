using Hitbox;
using UnityEngine;

namespace Demo
{
    public class DemoSpawner : MonoBehaviour
    {
        public GameObject DummyPrefab;
        public int Count = 50;
        public int SpawnRadius = 100;
        public float SpawnHeight = .1f;

        public static int[] Frequencies =
        {
            1,
            2,
            3,
            5,
            6,
            10
        };

        private void Start()
        {
            for (int i = 0; i < Count; i++)
            {
                var rand = Random.insideUnitCircle;
                var go = Instantiate(DummyPrefab, new Vector3(rand.x * SpawnRadius, SpawnHeight, rand.y * SpawnRadius), Quaternion.identity);
//                go.GetComponent<HitboxBody>().SnapshotFrequency = Frequencies[Random.Range(0, Frequencies.Length)];
            }
        }
    }
}