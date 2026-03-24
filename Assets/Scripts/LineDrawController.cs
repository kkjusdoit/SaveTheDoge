using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SaveTheDoge
{
    public sealed class LineDrawController : MonoBehaviour
    {
        [SerializeField] private float minPointDistance = 0.18f;
        [SerializeField] private float lineWidth = 0.22f;
        [SerializeField] private float lineDensity = 1f;

        private readonly List<Vector2> worldPoints = new();

        private Camera gameplayCamera;
        private Transform drawRoot;
        private GameFlowController gameFlow;
        private LineRenderer previewRenderer;
        private bool isDrawing;
        private bool hasCommitted;
        private float maxLineLength;
        private float currentLength;

        public void Configure(GameFlowController flow, Camera targetCamera, Transform targetRoot, float maxLength)
        {
            gameFlow = flow;
            gameplayCamera = targetCamera;
            drawRoot = targetRoot;
            maxLineLength = maxLength;
            hasCommitted = false;
            isDrawing = false;
            currentLength = 0f;
            worldPoints.Clear();
            ClearPreview();
            gameFlow.UpdateRemainingLineLength(maxLineLength, maxLineLength, false);
        }

        private void Update()
        {
            if (gameFlow == null || hasCommitted || gameplayCamera == null)
            {
                return;
            }

            if (gameFlow.CurrentState != GameState.Ready && gameFlow.CurrentState != GameState.Drawing)
            {
                return;
            }

            if (GetPointerDown(out Vector2 downPosition))
            {
                if (IsPointerBlockedByUi())
                {
                    return;
                }

                BeginDraw(downPosition);
            }

            if (!isDrawing)
            {
                return;
            }

            if (GetPointerHeld(out Vector2 holdPosition))
            {
                AppendPoint(holdPosition);
            }

            if (GetPointerUp())
            {
                CommitLine();
            }
        }

        private void BeginDraw(Vector2 screenPosition)
        {
            if (hasCommitted)
            {
                return;
            }

            gameFlow.NotifyDrawingStarted();
            ClearPreview();

            GameObject preview = new GameObject("PreviewLine");
            preview.transform.SetParent(drawRoot, false);
            previewRenderer = preview.AddComponent<LineRenderer>();
            ConfigureLineRenderer(previewRenderer, false);

            worldPoints.Clear();
            currentLength = 0f;
            isDrawing = true;

            Vector2 worldPoint = ScreenToWorld(screenPosition);
            worldPoints.Add(worldPoint);
            UpdatePreview();
            gameFlow.UpdateRemainingLineLength(maxLineLength - currentLength, maxLineLength, false);
        }

        private void AppendPoint(Vector2 screenPosition)
        {
            Vector2 worldPoint = ScreenToWorld(screenPosition);
            Vector2 lastPoint = worldPoints[^1];
            float distance = Vector2.Distance(lastPoint, worldPoint);
            if (distance < minPointDistance)
            {
                return;
            }

            float remainingLength = maxLineLength - currentLength;
            if (remainingLength <= 0f)
            {
                CommitLine();
                return;
            }

            if (distance > remainingLength)
            {
                worldPoint = Vector2.Lerp(lastPoint, worldPoint, remainingLength / distance);
                distance = remainingLength;
            }

            worldPoints.Add(worldPoint);
            currentLength += distance;
            UpdatePreview();
            gameFlow.UpdateRemainingLineLength(maxLineLength - currentLength, maxLineLength, false);

            if (currentLength >= maxLineLength - 0.001f)
            {
                CommitLine();
            }
        }

        private void CommitLine()
        {
            if (!isDrawing)
            {
                return;
            }

            isDrawing = false;
            bool valid = worldPoints.Count >= 2;
            if (valid)
            {
                CreateSolidLine();
                hasCommitted = true;
            }

            ClearPreview();
            gameFlow.UpdateRemainingLineLength(maxLineLength - currentLength, maxLineLength, valid);
            gameFlow.NotifyDrawingFinished(valid);
        }

        private void CreateSolidLine()
        {
            GameObject lineObject = new GameObject("DrawnLine");
            lineObject.transform.SetParent(drawRoot, false);

            var marker = lineObject.AddComponent<DrawnLine>();
            var body = lineObject.AddComponent<Rigidbody2D>();
            body.gravityScale = lineDensity;
            body.mass = 1.2f;
            body.angularDamping = 0.4f;

            var collider = lineObject.AddComponent<EdgeCollider2D>();
            collider.edgeRadius = lineWidth * 0.25f;
            collider.points = ToLocalPoints(worldPoints);

            var renderer = lineObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer(renderer, true);
            renderer.positionCount = worldPoints.Count;
            for (int i = 0; i < worldPoints.Count; i++)
            {
                renderer.SetPosition(i, drawRoot.InverseTransformPoint(worldPoints[i]));
            }

            _ = marker;
        }

        private void ConfigureLineRenderer(LineRenderer renderer, bool usePhysicsColor)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.useWorldSpace = false;
            renderer.startWidth = lineWidth;
            renderer.endWidth = lineWidth;
            renderer.numCapVertices = 4;
            renderer.numCornerVertices = 4;
            renderer.sortingOrder = usePhysicsColor ? 3 : 5;
            Color tint = usePhysicsColor
                ? new Color(0.27f, 0.20f, 0.12f, 1f)
                : new Color(0.34f, 0.27f, 0.18f, 0.9f);
            renderer.startColor = tint;
            renderer.endColor = tint;
        }

        private void UpdatePreview()
        {
            if (previewRenderer == null)
            {
                return;
            }

            previewRenderer.positionCount = worldPoints.Count;
            for (int i = 0; i < worldPoints.Count; i++)
            {
                previewRenderer.SetPosition(i, drawRoot.InverseTransformPoint(worldPoints[i]));
            }
        }

        private void ClearPreview()
        {
            if (previewRenderer != null)
            {
                Destroy(previewRenderer.gameObject);
                previewRenderer = null;
            }
        }

        private Vector2[] ToLocalPoints(List<Vector2> points)
        {
            Vector2[] localPoints = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                localPoints[i] = points[i];
            }

            return localPoints;
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Vector3 world = gameplayCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -gameplayCamera.transform.position.z));
            world.z = 0f;
            return world;
        }

        private bool GetPointerDown(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    position = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = default;
            return false;
        }

        private bool GetPointerHeld(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    position = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButton(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = default;
            return false;
        }

        private bool GetPointerUp()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }

            return Input.GetMouseButtonUp(0);
        }

        private bool IsPointerBlockedByUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
