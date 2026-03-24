using UnityEngine;

namespace SaveTheDoge
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class HazardController : MonoBehaviour
    {
        private void Reset()
        {
            Collider2D area = GetComponent<Collider2D>();
            area.isTrigger = true;
        }
    }
}
