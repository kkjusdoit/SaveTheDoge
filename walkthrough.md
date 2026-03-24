# SaveTheDoge 精简 Walkthrough

## 项目概述
这个项目的本质很简单：玩家只能画一次线，线会变成一个真实参与 2D 物理模拟的刚体结构；随后游戏进入生存阶段，Doge 需要在倒计时结束前躲过蜜蜂、不要掉进水里。

如果只看核心玩法，这个仓库真正值得理解的只有三层：
- `GameFlowController`：状态机和整局流程的总协调者
- `LineDrawController`：把输入轨迹变成物理对象
- `DogController` / `BeeHiveController` / `BeeController`：生存阶段的规则执行者

## 核心玩法主链
先不要看场景和 prefab。直接把“一局游戏”缩成下面这条链：

`LoadLevel -> Ready -> Drawing -> CommitLine -> Survival -> Win/Lose`

这条链决定了项目的理解顺序，也决定了以后改玩法时应该先改哪里。

> 代码片段来自 `Assets/Scripts/GameState.cs`

```csharp
namespace SaveTheDoge
{
    public enum GameState
    {
        Ready,
        Drawing,
        Survival,
        Win,
        Lose
    }
```

> 代码片段来自 `Assets/Scripts/GameFlowController.cs`

```csharp
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
```

这里体现的是一个很标准的“小型状态机 + 协调器”设计。

你可以这样理解各个状态：
- `Ready`：允许开始画线
- `Drawing`：正在收集轨迹点
- `Survival`：线已经锁定，物理对抗开始
- `Win` / `Lose`：终局态，防止重复结算

`GameFlowController` 本身不处理输入细节，也不处理蜜蜂移动。它只负责三件事：
- 切状态
- 在切状态时通知正确的模块开始工作
- 统一收口赢/输结算

这是这份代码里最值得保留的设计点：流程控制和具体玩法实现是分开的。以后你要加“暂停”“二次画线”“技能释放”，第一落点仍然应该是这里，而不是直接改 Dog 或 Bee。

> 代码片段来自 `Assets/Scripts/LevelInstaller.cs`

```csharp
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
```

> 代码片段来自 `Assets/Scripts/LevelConfig.cs`

```csharp
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
```

`LevelInstaller + LevelConfig` 这组类体现的是“装配层”和“玩法层”分离。

重点不是它们代码多复杂，而是职责边界很干净：
- `LevelInstaller` 是一个薄入口，负责把关卡交给运行时
- `LevelConfig` 持有一关真正需要的参数和关键对象，并暴露 `Initialize()` / `BeginSurvival()`

这是一种很实用的做法：`GameFlowController` 不需要知道 Dog、蜂巢、线长、存活时间的细节，只需要和 `LevelConfig` 对话。这样关卡差异就被收进了配置对象，而不是散落在流程代码里。

## 画线如何变成刚体
这是整个项目最核心的实现。它不是“画一条 UI 线”，而是“先收集轨迹点，再把点集实体化成物理对象”。

> 代码片段来自 `Assets/Scripts/LineDrawController.cs`

```csharp
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
```

> 代码片段来自 `Assets/Scripts/LineDrawController.cs`

```csharp
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
```

`LineDrawController` 里真正重要的是 4 个阶段。

1. `BeginDraw()`
- 开始新一轮绘制
- 创建预览线 `PreviewLine`
- 把屏幕坐标转成世界坐标，写入 `worldPoints`

2. `AppendPoint()`
- 只在点与点距离足够大时才采样，避免点太密
- 每次追加点时都扣减剩余线长
- 如果本次拖动会超限，就把终点裁到刚好用完预算的位置

3. `CommitLine()`
- 点数不足 2，视为无效绘制
- 点数足够时，调用 `CreateSolidLine()`
- 一旦提交，`hasCommitted = true`，本局不能再画第二条线

4. `CreateSolidLine()`
- 创建 `DrawnLine` 对象
- 挂上 `Rigidbody2D`
- 挂上 `EdgeCollider2D`
- 挂上正式 `LineRenderer`

这里最关键的技术点有三个：

第一，预览对象和最终物理对象是分开的。
这避免了“画线途中就开始碰撞”的混乱状态。玩家拖动时看到的是临时预览；松手后才创建真正进入物理世界的线。

第二，线长限制是在采样时实时执行的。
不是最后再判超长，而是在 `AppendPoint()` 里一边收集点，一边裁剪距离。这种实现的手感通常更稳定，因为最终成品和玩家拖动的最后一段是一致的。

第三，最终碰撞体选的是 `EdgeCollider2D`。
这是正确的，因为玩家画出来的是一条开放折线，不是封闭面。代码把 `worldPoints` 直接喂给 `EdgeCollider2D.points`，于是输入轨迹立刻变成了真实碰撞边界。

从类设计角度看，`LineDrawController` 其实做的是“输入轨迹实体化器”。如果你以后想把线改成桥、绳子、冻结墙、可燃物，主改点都在这里。

## 生存阶段的规则是怎么拆的
提交线之后，游戏的重点就从“收集输入”切换成“规则执行”。

> 代码片段来自 `Assets/Scripts/DogController.cs`

```csharp
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
```

> 代码片段来自 `Assets/Scripts/BeeHiveController.cs`

```csharp
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
```

> 代码片段来自 `Assets/Scripts/BeeController.cs`

```csharp
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
```

这里的类拆分是合理的，而且很适合教学：

`DogController`
- 不负责流程推进
- 只负责自己的生存状态和死亡判定
- 一旦死亡，调用 `gameFlow?.Lose(reason)` 把结果上报

`BeeHiveController`
- 只负责“什么时候生成几只蜜蜂”
- 不负责蜜蜂怎么移动

`BeeController`
- 只负责单只蜜蜂的追击与碰撞反应
- 在 `FixedUpdate()` 里用刚体力追 Dog
- 撞到 Dog 就淘汰 Dog
- 撞到 `DrawnLine` 或 `PlatformMarker` 就短暂后退

这个拆分背后的模式可以概括成一句话：

“生成”和“行为”分开，“局级流程”和“个体规则”分开。

这是很值得学的。很多原型项目后面难维护，就是因为刷怪、追击、判输赢、UI 更新全部塞进一个大脚本。这个仓库虽然不大，但至少把这几个责任拆开了。

## 类设计与设计模式
如果只挑有技术含量的部分，这个项目主要有 4 个值得记的设计点。

> 代码片段来自 `Assets/Scripts/DrawnLine.cs`

```csharp
using UnityEngine;

namespace SaveTheDoge
{
    public sealed class DrawnLine : MonoBehaviour
    {
    }
```

> 代码片段来自 `Assets/Scripts/PlatformMarker.cs`

```csharp
using UnityEngine;

namespace SaveTheDoge
{
    public sealed class PlatformMarker : MonoBehaviour
    {
    }
```

> 代码片段来自 `Assets/Scripts/IGameOverTarget.cs`

```csharp
namespace SaveTheDoge
{
    public interface IGameOverTarget
    {
        bool IsEliminated { get; }
        void Eliminate(string reason);
    }
```

> 代码片段来自 `Assets/Scripts/IDamageSource.cs`

```csharp
namespace SaveTheDoge
{
    public interface IDamageSource
    {
        string DamageId { get; }
    }
```

### 1. 状态机模式
`GameState + GameFlowController` 是一个很清晰的显式状态机。优点不是“高级”，而是它把一局游戏的阶段边界说清楚了。代码可读性和可扩展性都比“靠很多布尔值拼逻辑”强。

### 2. 协调器模式
`GameFlowController` 是协调器，不是万事通。它不直接处理画线点集，也不直接写蜜蜂移动，只负责通知谁在什么时候开始工作。这种控制反转让模块之间耦合更低。

### 3. 配置对象 / 装配模式
`LevelConfig` 把“这关是什么”收拢成一个对象。这个模式的价值在于：流程代码不用知道每关的细节，只消费一个统一接口。

### 4. 标记组件模式
`DrawnLine` 和 `PlatformMarker` 都是空类，但它们非常有用。蜜蜂碰撞时不需要靠 tag 字符串判断，而是直接通过 `GetComponentInParen t<T>()` 判断语义对象类型。这是 Unity 里很常见、也很稳的写法。

接口这块目前只做了一半：
- `IGameOverTarget` 已经有实际用途，表达“这个对象可以被淘汰”
- `IDamageSource` 现在更像扩展位，说明作者有把伤害来源抽象出来的意图，但目前还没完全长成

## 最值得你真的记住的几点
- 这个项目的真正核心不是场景，而是 `GameFlowController -> LineDrawController -> Dog/Bee` 这条运行链。
- 画线系统的关键不是 `LineRenderer`，而是“点集 + `EdgeCollider2D` + `Rigidbody2D`”的实体化。
- 代码结构上最好的地方，是流程、配置、输入实体化、角色规则被拆开了。
- 如果以后你要自己改玩法，优先先判断变化属于哪一层：状态机、关卡配置、画线实体化、角色行为。先分层，再下手改。

