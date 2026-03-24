using UnityEngine;

namespace SaveTheDoge
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DogController : MonoBehaviour, IGameOverTarget
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private float outOfBoundsY = -8.5f;

        private GameFlowController gameFlow;
        private Color baseColor;
        private float baseGravityScale;

        public bool IsEliminated { get; private set; }

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            SpriteSwapUtility.TryApplySprite(bodyRenderer, "Sprites/dog_idle");

            baseGravityScale = body.gravityScale;
            baseColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
        }

        private void Update()
        {
            if (IsEliminated || gameFlow == null || gameFlow.CurrentState != GameState.Survival)
            {
                return;
            }

            if (transform.position.y <= outOfBoundsY)
            {
                Eliminate("Doge fell out of the level.");
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsEliminated)
            {
                return;
            }

            var bee = collision.collider.GetComponentInParent<BeeController>();
            if (bee != null)
            {
                Eliminate("Doge got stung by a bee.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsEliminated)
            {
                return;
            }

            if (other.GetComponentInParent<HazardController>() != null)
            {
                Eliminate("Doge fell into the water.");
            }
        }

        public void Initialize(GameFlowController flow)
        {
            gameFlow = flow;
        }

        public void PrepareForRound()
        {
            IsEliminated = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.simulated = true;

            if (bodyRenderer != null)
            {
                bodyRenderer.color = baseColor;
            }
        }

        public void BeginSurvival()
        {
            body.gravityScale = baseGravityScale;
            body.constraints = RigidbodyConstraints2D.None;
        }

        public void Eliminate(string reason)
        {
            if (IsEliminated)
            {
                return;
            }

            IsEliminated = true;
            if (bodyRenderer != null)
            {
                bodyRenderer.color = new Color(0.95f, 0.35f, 0.35f, 1f);
            }

            gameFlow?.Lose(reason);
        }
    }
}
