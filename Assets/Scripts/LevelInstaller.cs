using UnityEngine;

namespace SaveTheDoge
{
    public sealed class LevelInstaller : MonoBehaviour
    {
        [SerializeField] private LevelConfig config;

        public LevelConfig Initialize(GameFlowController flow, Transform beeRoot)
        {
            config.Initialize(flow, beeRoot);
            return config;
        }
    }
}
