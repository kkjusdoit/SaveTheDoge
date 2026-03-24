using UnityEngine;
using System.Collections.Generic;

namespace SaveTheDoge
{
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform levelRoot;
        [SerializeField] private Transform drawLayer;
        [SerializeField] private Transform beeSpawnRoot;
        [SerializeField] private LineDrawController lineDrawController;
        [SerializeField] private GameHudController hudController;
        [SerializeField] private LevelInstaller[] levelPrefabs;

        private int currentLevelIndex;
        private LevelInstaller activeLevel;
        private LevelConfig activeConfig;
        private float countdown;

        public GameState CurrentState { get; private set; }

        private void Start()
        {
            EnsureLevelPrefabsLoaded();
            LoadLevel(0);
        }

        private void Update()
        {
            if (CurrentState != GameState.Survival)
            {
                return;
            }

            countdown -= Time.deltaTime;
            hudController.UpdateCountdown(countdown, false);
            if (countdown <= 0f)
            {
                Win();
            }
        }

        public void NotifyDrawingStarted()
        {
            if (CurrentState == GameState.Ready)
            {
                CurrentState = GameState.Drawing;
                hudController.SetHint("Release to lock the line and start survival.");
            }
        }

        public void UpdateRemainingLineLength(float remaining, float maxLength, bool locked)
        {
            hudController.UpdateRemainingLength(remaining, maxLength, locked);
        }

        public void NotifyDrawingFinished(bool committed)
        {
            if (!committed)
            {
                CurrentState = GameState.Ready;
                hudController.SetHint("Draw a longer line to protect Doge.");
                return;
            }

            CurrentState = GameState.Survival;
            activeConfig.BeginSurvival();
            hudController.SetHint("Hold on until the timer ends.");
            hudController.UpdateCountdown(countdown, false);
        }

        public void Lose(string reason)
        {
            if (CurrentState == GameState.Win || CurrentState == GameState.Lose)
            {
                return;
            }

            CurrentState = GameState.Lose;
            hudController.ShowLose(reason);
            hudController.SetHint("Tap Retry to try the same puzzle again.");
        }

        public void Win()
        {
            if (CurrentState == GameState.Win || CurrentState == GameState.Lose)
            {
                return;
            }

            CurrentState = GameState.Win;
            hudController.ShowWin("Doge survived the bee rush.");
            hudController.SetHint("You can move to the next sample level.");
        }

        public void RetryLevel()
        {
            LoadLevel(currentLevelIndex);
        }

        public void LoadNextLevel()
        {
            int nextIndex = (currentLevelIndex + 1) % levelPrefabs.Length;
            LoadLevel(nextIndex);
        }

        private void LoadLevel(int levelIndex)
        {
            EnsureLevelPrefabsLoaded();
            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelPrefabs.Length - 1);

            ClearChildren(levelRoot);
            ClearChildren(drawLayer);
            ClearChildren(beeSpawnRoot);

            activeLevel = Instantiate(levelPrefabs[currentLevelIndex], levelRoot);
            activeConfig = activeLevel.Initialize(this, beeSpawnRoot);
            countdown = activeConfig.SurvivalDuration;

            lineDrawController.Configure(this, gameplayCamera, drawLayer, activeConfig.MaxLineLength);
            hudController.HideResults();
            hudController.SetHint($"Level {currentLevelIndex + 1}: draw one line to save Doge. Watch the line meter.");
            hudController.UpdateCountdown(countdown, true);
            hudController.UpdateRemainingLength(activeConfig.MaxLineLength, activeConfig.MaxLineLength, false);
            CurrentState = GameState.Ready;
        }

        private void EnsureLevelPrefabsLoaded()
        {
            List<LevelInstaller> validPrefabs = new List<LevelInstaller>();
            if (levelPrefabs != null)
            {
                for (int i = 0; i < levelPrefabs.Length; i++)
                {
                    if (levelPrefabs[i] != null)
                    {
                        validPrefabs.Add(levelPrefabs[i]);
                    }
                }
            }

            if (validPrefabs.Count == 0)
            {
                LevelInstaller[] loaded = Resources.LoadAll<LevelInstaller>("Levels");
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null)
                    {
                        validPrefabs.Add(loaded[i]);
                    }
                }
            }

            levelPrefabs = validPrefabs.ToArray();
            if (levelPrefabs.Length == 0)
            {
                Debug.LogError("No level prefabs configured. Expected scene references or prefabs under Resources/Levels.");
            }
        }

        private void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
