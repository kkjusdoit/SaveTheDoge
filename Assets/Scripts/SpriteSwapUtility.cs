using UnityEngine;

namespace SaveTheDoge
{
    public static class SpriteSwapUtility
    {
        public static void TryApplySprite(SpriteRenderer renderer, string resourcePath)
        {
            if (renderer == null || string.IsNullOrWhiteSpace(resourcePath))
            {
                return;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                renderer.sprite = sprite;
            }
        }
    }
}
