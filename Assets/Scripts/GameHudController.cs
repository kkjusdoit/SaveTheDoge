using UnityEngine;
using UnityEngine.UI;

namespace SaveTheDoge
{
    public sealed class GameHudController : MonoBehaviour
    {
        [SerializeField] private Text countdownText;
        [SerializeField] private Text lengthText;
        [SerializeField] private Text hintText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private Text winBodyText;
        [SerializeField] private Text loseBodyText;

        public void SetHint(string message)
        {
            if (hintText != null)
            {
                hintText.text = message;
            }
        }

        public void UpdateCountdown(float seconds, bool waitingForDraw)
        {
            if (countdownText == null)
            {
                return;
            }

            countdownText.text = waitingForDraw
                ? "DRAW"
                : $"TIME {Mathf.CeilToInt(Mathf.Max(0f, seconds))}";
        }

        public void UpdateRemainingLength(float remaining, float maxLength, bool locked)
        {
            if (lengthText == null)
            {
                return;
            }

            if (locked)
            {
                lengthText.text = "LINE LOCKED";
                return;
            }

            float safeRemaining = Mathf.Max(0f, remaining);
            lengthText.text = $"LINE {safeRemaining:0.0}/{maxLength:0.0}";
        }

        public void HideResults()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }

            if (losePanel != null)
            {
                losePanel.SetActive(false);
            }
        }

        public void ShowWin(string message)
        {
            HideResults();
            if (winBodyText != null)
            {
                winBodyText.text = message;
            }

            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
        }

        public void ShowLose(string message)
        {
            HideResults();
            if (loseBodyText != null)
            {
                loseBodyText.text = message;
            }

            if (losePanel != null)
            {
                losePanel.SetActive(true);
            }
        }
    }
}
