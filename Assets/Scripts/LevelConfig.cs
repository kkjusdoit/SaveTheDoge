using UnityEngine;

namespace SaveTheDoge
{
    public sealed class LevelConfig : MonoBehaviour
    {
        [field: SerializeField] public string LevelName { get; private set; } = "Level";
        [field: SerializeField] public float SurvivalDuration { get; private set; } = 8f;
        [field: SerializeField] public float MaxLineLength { get; private set; } = 8f;
        [field: SerializeField] public int BeeCount { get; private set; } = 5;
        [field: SerializeField] public DogController Dog { get; private set; }
        [field: SerializeField] public BeeHiveController BeeHive { get; private set; }

        public void Initialize(GameFlowController flow, Transform beeRoot)
        {
            Dog.Initialize(flow);
            Dog.PrepareForRound();
            BeeHive.ConfigureSpawn(BeeCount);
            BeeHive.Initialize(Dog.transform, beeRoot);
        }

        public void BeginSurvival()
        {
            Dog.BeginSurvival();
            BeeHive.BeginSpawning();
        }
    }
}
