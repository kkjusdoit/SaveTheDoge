using UnityEngine;

namespace SaveTheDoge
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BeeController : MonoBehaviour, IDamageSource
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private float chaseForce = 10f;
        [SerializeField] private float maxSpeed = 4.5f;
        [SerializeField] private float retreatSpeed = 2.5f;
        [SerializeField] private float retreatDuration = 0.3f;

        private Transform target;
        private Vector2 retreatDirection;
        private float retreatTimer;
        private bool isActive;

        public string DamageId => "Bee";

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

            SpriteSwapUtility.TryApplySprite(bodyRenderer, "Sprites/bee");
        }

        private void FixedUpdate()
        {
            if (!isActive || target == null)
            {
                return;
            }

            if (retreatTimer > 0f)
            {
                retreatTimer -= Time.fixedDeltaTime;
                body.linearVelocity = retreatDirection * retreatSpeed;
                return;
            }

            Vector2 chaseDirection = ((Vector2)target.position - body.position).normalized;
            body.AddForce(chaseDirection * chaseForce, ForceMode2D.Force);
            body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity, maxSpeed);

            if (bodyRenderer != null && Mathf.Abs(body.linearVelocity.x) > 0.05f)
            {
                bodyRenderer.flipX = body.linearVelocity.x < 0f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isActive)
            {
                return;
            }

            DogController dog = collision.collider.GetComponentInParent<DogController>();
            if (dog != null)
            {
                dog.Eliminate("Doge got stung by a bee.");
                return;
            }

            if (collision.collider.GetComponentInParent<DrawnLine>() != null ||
                collision.collider.GetComponentInParent<PlatformMarker>() != null)
            {
                Vector2 away = (body.position - collision.GetContact(0).point).normalized;
                if (away.sqrMagnitude < 0.01f)
                {
                    away = Vector2.up;
                }

                retreatDirection = away;
                retreatTimer = retreatDuration;
            }
        }

        public void Initialize(Transform chaseTarget)
        {
            target = chaseTarget;
            isActive = true;
        }
    }
}
