using System.Collections;
using UnityEngine;

namespace SaveTheDoge
{
    public sealed class BeeHiveController : MonoBehaviour
    {
        [SerializeField] private BeeController beePrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private SpriteRenderer hiveRenderer;
        [SerializeField] private int beeCount = 5;
        [SerializeField] private float spawnInterval = 0.45f;

        private Transform target;
        private Transform beeParent;
        private Coroutine spawnRoutine;

        private void Awake()
        {
            if (hiveRenderer == null)
            {
                hiveRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            SpriteSwapUtility.TryApplySprite(hiveRenderer, "Sprites/beehive");
        }

        public void Initialize(Transform dogTarget, Transform beesRoot)
        {
            target = dogTarget;
            beeParent = beesRoot;
        }

        public void ConfigureSpawn(int count)
        {
            beeCount = count;
        }

        public void BeginSpawning()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }

            spawnRoutine = StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            for (int i = 0; i < beeCount; i++)
            {
                SpawnOne();
                yield return new WaitForSeconds(spawnInterval);
            }

            spawnRoutine = null;
        }

        private void SpawnOne()
        {
            if (beePrefab == null || target == null)
            {
                return;
            }

            Transform spawnTransform = spawnPoint != null ? spawnPoint : transform;
            BeeController bee = Instantiate(beePrefab, spawnTransform.position, Quaternion.identity, beeParent);
            bee.Initialize(target);
        }
    }
}
