using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PetsHarbor.SlideMerge
{
    public class BoardGridManager : MonoBehaviour
    {
        public static BoardGridManager Instance { get; private set; }

        [Header("Board Dimensions")]
        public int cols = TierDatabase.BoardCols; // 7
        public int rows = TierDatabase.BoardRows; // 8
        public float CellSize = 1.0f;

        public readonly List<JellyBlock> ActiveBlocks = new List<JellyBlock>();
        public event Action<int> OnTierUnlocked;
        public event Action<JellyBlock> OnGoalTierCreated;

        private Transform _blocksContainer;
        private Transform _trayContainer;
        private int _highestUnlockedTier = 11;

        private void Awake()
        {
            Instance = this;
        }

        public void InitializeBoard()
        {
            BuildWoodenTray3D();
            SpawnInitialLevel();
        }

        public void BuildWoodenTray3D()
        {
            Transform existingTray = transform.Find("WoodenTray3D");
            if (existingTray != null)
            {
                existingTray.name = "WoodenTray3D_Old";
                existingTray.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(existingTray.gameObject);
                else DestroyImmediate(existingTray.gameObject);
            }

            _trayContainer = new GameObject("WoodenTray3D").transform;
            _trayContainer.SetParent(transform, false);
            _trayContainer.localPosition = Vector3.zero;

            Shader litShader = ProceduralMeshGenerator.FindBestLitShader();
            Texture2D floorTex = ProceduralMeshGenerator.GetOrCreateWoodTexture(false);
            Texture2D rimTex = ProceduralMeshGenerator.GetOrCreateWoodTexture(true);

            Material floorMat = new Material(litShader) { name = "Mat_WoodTrayFloor" };
            if (floorMat.HasProperty("_BaseMap"))
            {
                floorMat.SetTexture("_BaseMap", floorTex);
                floorMat.SetTextureScale("_BaseMap", new Vector2(1.5f, 1.5f));
            }
            if (floorMat.HasProperty("_MainTex"))
            {
                floorMat.SetTexture("_MainTex", floorTex);
                floorMat.SetTextureScale("_MainTex", new Vector2(1.5f, 1.5f));
            }
            if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", new Color(0.95f, 0.88f, 0.80f));
            if (floorMat.HasProperty("_Color")) floorMat.SetColor("_Color", new Color(0.95f, 0.88f, 0.80f));
            if (floorMat.HasProperty("_Smoothness")) floorMat.SetFloat("_Smoothness", 0.32f);
            if (floorMat.HasProperty("_Glossiness")) floorMat.SetFloat("_Glossiness", 0.32f);

            Material rimMat = new Material(litShader) { name = "Mat_WoodTrayRim" };
            if (rimMat.HasProperty("_BaseMap")) rimMat.SetTexture("_BaseMap", rimTex);
            if (rimMat.HasProperty("_MainTex")) rimMat.SetTexture("_MainTex", rimTex);
            if (rimMat.HasProperty("_BaseColor")) rimMat.SetColor("_BaseColor", new Color(0.92f, 0.82f, 0.72f));
            if (rimMat.HasProperty("_Color")) rimMat.SetColor("_Color", new Color(0.92f, 0.82f, 0.72f));
            if (rimMat.HasProperty("_Smoothness")) rimMat.SetFloat("_Smoothness", 0.38f);
            if (rimMat.HasProperty("_Glossiness")) rimMat.SetFloat("_Glossiness", 0.38f);

            Material tableMat = new Material(litShader) { name = "Mat_WoodTable" };
            if (tableMat.HasProperty("_BaseMap"))
            {
                tableMat.SetTexture("_BaseMap", floorTex);
                tableMat.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
            }
            if (tableMat.HasProperty("_MainTex"))
            {
                tableMat.SetTexture("_MainTex", floorTex);
                tableMat.SetTextureScale("_MainTex", new Vector2(3f, 3f));
            }
            if (tableMat.HasProperty("_BaseColor")) tableMat.SetColor("_BaseColor", new Color(1.0f, 0.94f, 0.86f));
            if (tableMat.HasProperty("_Color")) tableMat.SetColor("_Color", new Color(1.0f, 0.94f, 0.86f));
            if (tableMat.HasProperty("_Smoothness")) tableMat.SetFloat("_Smoothness", 0.25f);

            float boardW = cols * CellSize;
            float boardD = rows * CellSize;
            float rimThickness = 0.42f;
            float rimHeight = 0.72f;

            // 1. Outer Wooden Table Background
            CreateBoxPiece("OuterWoodTable", _trayContainer, new Vector3(0f, -0.36f, 1.5f), new Vector3(40f, 0.3f, 36f), tableMat);

            // 2. Inner Tray Floor
            CreateBoxPiece("TrayFloor", _trayContainer, new Vector3(0f, -0.12f, 0f), new Vector3(boardW + 0.1f, 0.24f, boardD + 0.1f), floorMat);

            // 3. Four Beveled Wooden Rims
            CreateBoxPiece("Rim_Top", _trayContainer,
                new Vector3(0f, rimHeight * 0.4f, boardD * 0.5f + rimThickness * 0.5f),
                new Vector3(boardW + rimThickness * 2f, rimHeight, rimThickness), rimMat);

            CreateBoxPiece("Rim_Bottom", _trayContainer,
                new Vector3(0f, rimHeight * 0.4f, -boardD * 0.5f - rimThickness * 0.5f),
                new Vector3(boardW + rimThickness * 2f, rimHeight, rimThickness), rimMat);

            CreateBoxPiece("Rim_Left", _trayContainer,
                new Vector3(-boardW * 0.5f - rimThickness * 0.5f, rimHeight * 0.4f, 0f),
                new Vector3(rimThickness, rimHeight, boardD), rimMat);

            CreateBoxPiece("Rim_Right", _trayContainer,
                new Vector3(boardW * 0.5f + rimThickness * 0.5f, rimHeight * 0.4f, 0f),
                new Vector3(rimThickness, rimHeight, boardD), rimMat);
        }

        private void CreateBoxPiece(string pieceName, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(go.GetComponent<Collider>());
            go.name = pieceName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        public void SpawnInitialLevel()
        {
            ClearBlocks();

            Transform existingContainer = transform.Find("BlocksContainer");
            if (existingContainer != null)
            {
                existingContainer.name = "BlocksContainer_Old";
                existingContainer.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(existingContainer.gameObject);
                else DestroyImmediate(existingContainer.gameObject);
            }

            _blocksContainer = new GameObject("BlocksContainer").transform;
            _blocksContainer.SetParent(transform, false);
            _blocksContainer.localPosition = Vector3.zero;

            _highestUnlockedTier = 11;
            foreach (BlockSpawnInfo info in TierDatabase.InitialLevelLayout)
            {
                SpawnBlock(info.tier, info.col, info.row, false);
            }

            Physics.SyncTransforms();
        }

        public void ClearBlocks()
        {
            for (int i = ActiveBlocks.Count - 1; i >= 0; i--)
            {
                if (ActiveBlocks[i] != null)
                {
                    if (Application.isPlaying) Destroy(ActiveBlocks[i].gameObject);
                    else DestroyImmediate(ActiveBlocks[i].gameObject);
                }
            }
            ActiveBlocks.Clear();
        }

        public JellyBlock SpawnBlock(int tier, float gridX, float gridY, bool playBounce = true)
        {
            if (_blocksContainer == null)
            {
                Transform found = transform.Find("BlocksContainer");
                _blocksContainer = found != null ? found : new GameObject("BlocksContainer").transform;
                _blocksContainer.SetParent(transform, false);
            }

            GameObject go = new GameObject($"JellyBlock_T{tier:D2}");
            go.transform.SetParent(_blocksContainer, false);

            JellyBlock block = go.AddComponent<JellyBlock>();
            block.Initialize(this, tier, gridX, gridY, playBounce);
            ActiveBlocks.Add(block);

            if (tier > _highestUnlockedTier)
            {
                _highestUnlockedTier = tier;
                OnTierUnlocked?.Invoke(tier);
            }

            return block;
        }

        public Vector3 GridToWorldPosition(float gridX, float gridY, int width, int height)
        {
            float wx = (gridX + width * 0.5f - cols * 0.5f) * CellSize;
            float wz = (rows * 0.5f - (gridY + height * 0.5f)) * CellSize;
            return transform.TransformPoint(new Vector3(wx, 0f, wz));
        }

        public Vector2 WorldToGridPoint(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            float gx = local.x / CellSize + cols * 0.5f;
            float gy = rows * 0.5f - local.z / CellSize;
            return new Vector2(gx, gy);
        }

        public Vector2 WorldDeltaToGridDelta(Vector3 worldDelta)
        {
            Vector3 localDelta = transform.InverseTransformVector(worldDelta);
            return new Vector2(localDelta.x / CellSize, -localDelta.z / CellSize);
        }

        public JellyBlock FindBlockAtWorldPoint(Vector3 worldPoint)
        {
            Vector2 gp = WorldToGridPoint(worldPoint);
            JellyBlock best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock b = ActiveBlocks[i];
                if (b == null || b.IsMerging) continue;

                if (gp.x >= b.GridX - 0.15f && gp.x <= b.GridX + b.GridWidth + 0.15f &&
                    gp.y >= b.GridY - 0.15f && gp.y <= b.GridY + b.GridHeight + 0.15f)
                {
                    Vector2 center = new Vector2(b.GridX + b.GridWidth * 0.5f, b.GridY + b.GridHeight * 0.5f);
                    float d = Vector2.Distance(gp, center);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = b;
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// Slides the dragged block toward targetGridPos using Axis-Separated Swept AABB with
        /// Magnetic Grid-Lane Centering and wide Corner Chamfer Assist so movement feels effortless and slippery.
        /// </summary>
        public bool SlideBlockWithCollision(
            JellyBlock block,
            Vector2 targetGridPos,
            bool allowMerge,
            out JellyBlock mergedBlock)
        {
            mergedBlock = null;
            if (block == null || block.IsMerging) return false;

            GameFeelConfig feel = GameFeelConfig.Instance;
            Vector2 current = new Vector2(block.GridX, block.GridY);
            Vector2 desired = new Vector2(
                Mathf.Clamp(targetGridPos.x, 0f, cols - block.GridWidth),
                Mathf.Clamp(targetGridPos.y, 0f, rows - block.GridHeight));

            Vector2 totalDelta = desired - current;
            float dist = totalDelta.magnitude;

            // Magnetic Grid-Lane Centering: while sliding primarily along one axis, softly align the perpendicular axis to the grid lane
            float absDx = Mathf.Abs(totalDelta.x);
            float absDy = Mathf.Abs(totalDelta.y);
            float laneStrength = feel.laneCenteringStrength;
            if (laneStrength > 0.01f)
            {
                if (absDx > absDy * 1.15f && absDx > 0.005f)
                {
                    float snappedY = Mathf.Clamp(Mathf.Round(current.y), 0f, rows - block.GridHeight);
                    float laneDiffY = snappedY - current.y;
                    if (Mathf.Abs(laneDiffY) <= 0.48f)
                    {
                        float pullY = current.y + laneDiffY * Mathf.Clamp01(absDx * 2.5f * laneStrength);
                        if (!OverlapsSolidObstacle(block, current.x, pullY, allowMerge))
                        {
                            current.y = pullY;
                            desired.y = Mathf.Lerp(desired.y, snappedY, laneStrength);
                        }
                    }
                }
                else if (absDy > absDx * 1.15f && absDy > 0.005f)
                {
                    float snappedX = Mathf.Clamp(Mathf.Round(current.x), 0f, cols - block.GridWidth);
                    float laneDiffX = snappedX - current.x;
                    if (Mathf.Abs(laneDiffX) <= 0.48f)
                    {
                        float pullX = current.x + laneDiffX * Mathf.Clamp01(absDy * 2.5f * laneStrength);
                        if (!OverlapsSolidObstacle(block, pullX, current.y, allowMerge))
                        {
                            current.x = pullX;
                            desired.x = Mathf.Lerp(desired.x, snappedX, laneStrength);
                        }
                    }
                }
            }

            totalDelta = desired - current;
            dist = totalDelta.magnitude;

            if (dist < 0.0005f)
            {
                if (EvaluateSameTierPenetration(block, current, Vector2.zero, allowMerge, out JellyBlock stationaryTarget))
                {
                    mergedBlock = ExecuteMerge(block, stationaryTarget, Vector2.zero);
                    return true;
                }
                return false;
            }

            float maxStep = Mathf.Max(0.5f, feel.maxSlideStepPerFrame);
            if (dist > maxStep)
            {
                totalDelta = totalDelta.normalized * maxStep;
                dist = maxStep;
            }

            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 0.065f));
            Vector2 stepDelta = totalDelta / steps;
            float chamferRange = feel.cornerChamferRange;
            float chamferSpeed = feel.cornerChamferSpeed;

            for (int s = 0; s < steps; s++)
            {
                // 1. Move along X toward desired.x
                if (Mathf.Abs(stepDelta.x) > 0.0001f)
                {
                    float nextX = Mathf.Clamp(current.x + stepDelta.x, 0f, cols - block.GridWidth);
                    if (!OverlapsSolidObstacle(block, nextX, current.y, allowMerge))
                    {
                        current.x = nextX;
                    }
                    else
                    {
                        // Wide Corner Chamfer Assist for X: if Y misalignment is blocking X, slide Y into lane
                        float roundY = Mathf.Clamp(Mathf.Round(current.y), 0f, rows - block.GridHeight);
                        float misalignY = roundY - current.y;
                        bool cornerAssisted = false;
                        if (Mathf.Abs(misalignY) > 0.005f && Mathf.Abs(misalignY) <= chamferRange)
                        {
                            float maxNudge = Mathf.Max(0.08f, stepDelta.magnitude * chamferSpeed);
                            float nudgeY = current.y + Mathf.Sign(misalignY) * Mathf.Min(Mathf.Abs(misalignY), maxNudge);
                            if (!OverlapsSolidObstacle(block, current.x, nudgeY, allowMerge))
                            {
                                current.y = nudgeY;
                                if (!OverlapsSolidObstacle(block, nextX, current.y, allowMerge))
                                {
                                    current.x = nextX;
                                    cornerAssisted = true;
                                }
                                else if (!OverlapsSolidObstacle(block, nextX, roundY, allowMerge) &&
                                         !OverlapsSolidObstacle(block, current.x, roundY, allowMerge))
                                {
                                    current.y = roundY;
                                    current.x = nextX;
                                    cornerAssisted = true;
                                }
                            }
                        }

                        if (!cornerAssisted)
                        {
                            float low = current.x;
                            float high = nextX;
                            for (int b = 0; b < 4; b++)
                            {
                                float mid = (low + high) * 0.5f;
                                if (!OverlapsSolidObstacle(block, mid, current.y, allowMerge)) low = mid;
                                else high = mid;
                            }
                            current.x = low;
                        }
                    }
                }

                // 2. Move along Y toward desired.y
                if (Mathf.Abs(stepDelta.y) > 0.0001f)
                {
                    float nextY = Mathf.Clamp(current.y + stepDelta.y, 0f, rows - block.GridHeight);
                    if (!OverlapsSolidObstacle(block, current.x, nextY, allowMerge))
                    {
                        current.y = nextY;
                    }
                    else
                    {
                        // Wide Corner Chamfer Assist for Y: if X misalignment is blocking Y, slide X into lane
                        float roundX = Mathf.Clamp(Mathf.Round(current.x), 0f, cols - block.GridWidth);
                        float misalignX = roundX - current.x;
                        bool cornerAssisted = false;
                        if (Mathf.Abs(misalignX) > 0.005f && Mathf.Abs(misalignX) <= chamferRange)
                        {
                            float maxNudge = Mathf.Max(0.08f, stepDelta.magnitude * chamferSpeed);
                            float nudgeX = current.x + Mathf.Sign(misalignX) * Mathf.Min(Mathf.Abs(misalignX), maxNudge);
                            if (!OverlapsSolidObstacle(block, nudgeX, current.y, allowMerge))
                            {
                                current.x = nudgeX;
                                if (!OverlapsSolidObstacle(block, current.x, nextY, allowMerge))
                                {
                                    current.y = nextY;
                                    cornerAssisted = true;
                                }
                                else if (!OverlapsSolidObstacle(block, roundX, nextY, allowMerge) &&
                                         !OverlapsSolidObstacle(block, roundX, current.y, allowMerge))
                                {
                                    current.x = roundX;
                                    current.y = nextY;
                                    cornerAssisted = true;
                                }
                            }
                        }

                        if (!cornerAssisted)
                        {
                            float low = current.y;
                            float high = nextY;
                            for (int b = 0; b < 4; b++)
                            {
                                float mid = (low + high) * 0.5f;
                                if (!OverlapsSolidObstacle(block, current.x, mid, allowMerge)) low = mid;
                                else high = mid;
                            }
                            current.y = low;
                        }
                    }
                }

                // 3. Check if the block has touched/entered a same-tier neighbor enough to magnetically merge
                if (EvaluateSameTierPenetration(block, current, totalDelta, allowMerge, out JellyBlock mergeTarget))
                {
                    block.SetGridPositionFromDrag(current.x, current.y);
                    mergedBlock = ExecuteMerge(block, mergeTarget, totalDelta);
                    return true;
                }
            }

            block.SetGridPositionFromDrag(current.x, current.y);
            return false;
        }

        /// <summary>
        /// Evaluates contact/penetration between the moving block and any same-tier block.
        /// Triggers a magnetic merge as soon as the dragged block touches/overlaps a well-aligned same-tier neighbor.
        /// </summary>
        private bool EvaluateSameTierPenetration(
            JellyBlock block,
            Vector2 testPos,
            Vector2 dragIntent,
            bool allowTriggerMerge,
            out JellyBlock mergeTarget,
            float thresholdMultiplier = 1.0f)
        {
            mergeTarget = null;
            GameFeelConfig feel = GameFeelConfig.Instance;
            float ax0 = testPos.x;
            float ax1 = testPos.x + block.GridWidth;
            float ay0 = testPos.y;
            float ay1 = testPos.y + block.GridHeight;

            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock other = ActiveBlocks[i];
                if (other == null || other == block || other.IsMerging || other.Tier != block.Tier) continue;

                float bx0 = other.GridX;
                float bx1 = other.GridX + other.GridWidth;
                float by0 = other.GridY;
                float by1 = other.GridY + other.GridHeight;

                float overlapX = Mathf.Min(ax1, bx1) - Mathf.Max(ax0, bx0);
                float overlapY = Mathf.Min(ay1, by1) - Mathf.Max(ay0, by0);

                if (overlapX <= 0.005f || overlapY <= 0.005f) continue;

                float ratioX = overlapX / block.GridWidth;
                float ratioY = overlapY / block.GridHeight;

                bool horizontalCollision = ratioY >= ratioX;
                float alignRatio = horizontalCollision ? ratioY : ratioX;
                float penetrationCells = horizontalCollision ? overlapX : overlapY;

                if (alignRatio < feel.mergeRequiredAlignment) continue;

                float blockSpanAlongCollision = horizontalCollision ? block.GridWidth : block.GridHeight;
                float baseThresh = feel.mergePenetrationThreshold;
                float requiredPenetration = Mathf.Lerp(baseThresh, baseThresh * 1.35f, Mathf.Clamp01((blockSpanAlongCollision - 1f) / 2f)) * thresholdMultiplier;

                float squishProgress = Mathf.Clamp01(penetrationCells / Mathf.Max(0.03f, requiredPenetration));
                Vector2 squishVec = horizontalCollision
                    ? new Vector2(squishProgress * 0.16f, 0f)
                    : new Vector2(0f, squishProgress * 0.16f);

                block.SetContactSquish(squishVec);
                other.SetContactSquish(squishVec);

                if (allowTriggerMerge && penetrationCells >= requiredPenetration)
                {
                    mergeTarget = other;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if placing 'moving' at (testX, testY) overlaps any impassable obstacle.
        /// </summary>
        private bool OverlapsSolidObstacle(JellyBlock moving, float testX, float testY, bool allowSameTierPenetration)
        {
            GameFeelConfig feel = GameFeelConfig.Instance;
            float obstaclePad = feel.obstacleClearancePad;
            float ox0 = testX + obstaclePad;
            float ox1 = testX + moving.GridWidth - obstaclePad;
            float oy0 = testY + obstaclePad;
            float oy1 = testY + moving.GridHeight - obstaclePad;

            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock other = ActiveBlocks[i];
                if (other == null || other == moving) continue;

                float bx0 = other.GridX + obstaclePad;
                float bx1 = other.GridX + other.GridWidth - obstaclePad;
                float by0 = other.GridY + obstaclePad;
                float by1 = other.GridY + other.GridHeight - obstaclePad;

                if (ox0 < bx1 && ox1 > bx0 && oy0 < by1 && oy1 > by0)
                {
                    if (allowSameTierPenetration && !other.IsMerging && other.Tier == moving.Tier)
                    {
                        float rawOverlapX = Mathf.Min(testX + moving.GridWidth, other.GridX + other.GridWidth) - Mathf.Max(testX, other.GridX);
                        float rawOverlapY = Mathf.Min(testY + moving.GridHeight, other.GridY + other.GridHeight) - Mathf.Max(testY, other.GridY);
                        float alignRatio = Mathf.Max(rawOverlapX / moving.GridWidth, rawOverlapY / moving.GridHeight);
                        if (alignRatio >= feel.mergeRequiredAlignment)
                        {
                            continue;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public void SnapBlockToValidCell(JellyBlock block)
        {
            if (block == null || block.IsMerging) return;

            GameFeelConfig feel = GameFeelConfig.Instance;

            // Project forward slightly using release flick velocity so quick flicks glide into the target cell or merge
            Vector2 currentPos = new Vector2(block.GridX, block.GridY);
            Vector2 flickOffset = Vector2.ClampMagnitude(block.GridVelocity * feel.releaseFlickMomentum, feel.maxFlickDistanceCells);
            if (flickOffset.magnitude > 0.04f)
            {
                if (SlideBlockWithCollision(block, currentPos + flickOffset, true, out _))
                {
                    return;
                }
                currentPos = new Vector2(block.GridX, block.GridY);
            }

            if (EvaluateSameTierPenetration(block, currentPos, Vector2.zero, true, out JellyBlock touchTarget, 0.55f))
            {
                ExecuteMerge(block, touchTarget, Vector2.zero);
                return;
            }

            block.ClearFusionVisualState();

            int bestCol = Mathf.Clamp(Mathf.RoundToInt(block.GridX), 0, cols - block.GridWidth);
            int bestRow = Mathf.Clamp(Mathf.RoundToInt(block.GridY), 0, rows - block.GridHeight);

            if (OverlapsSolidObstacle(block, bestCol, bestRow, false))
            {
                float minDist = float.MaxValue;
                for (int r = 0; r <= rows - block.GridHeight; r++)
                {
                    for (int c = 0; c <= cols - block.GridWidth; c++)
                    {
                        if (!OverlapsSolidObstacle(block, c, r, false))
                        {
                            float d = Vector2.SqrMagnitude(new Vector2(c - block.GridX, r - block.GridY));
                            if (d < minDist)
                            {
                                minDist = d;
                                bestCol = c;
                                bestRow = r;
                            }
                        }
                    }
                }
            }

            block.SetGridPositionSmooth(bestCol, bestRow, feel.releaseSnapDuration);
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Merges movingBlock and targetBlock of Tier k into a single Tier k+1 block using a juicy 2-stage
        /// animation: Stage 1 (magnetic suck-in & squish together) -> Stage 2 (evolution pop & splash).
        /// </summary>
        public JellyBlock ExecuteMerge(JellyBlock movingBlock, JellyBlock targetBlock, Vector2 dragDir)
        {
            if (movingBlock == null || targetBlock == null) return null;
            if (movingBlock.IsMerging || targetBlock.IsMerging) return null;

            int currentTier = targetBlock.Tier;
            if (currentTier >= TierDatabase.MaxTier) return targetBlock;

            movingBlock.IsMerging = true;
            movingBlock.IsBeingDragged = false;
            targetBlock.IsMerging = true;
            targetBlock.IsBeingDragged = false;

            int nextTier = currentTier + 1;
            TierConfigData nextCfg = TierDatabase.GetTier(nextTier);

            ActiveBlocks.Remove(movingBlock);

            FindBestPlacementForMergedBlock(movingBlock, targetBlock, nextCfg.gridWidth, nextCfg.gridHeight, out int bestCol, out int bestRow);

            if (Application.isPlaying)
            {
                StartCoroutine(AnimateMergeFusionRoutine(movingBlock, targetBlock, nextTier, nextCfg, bestCol, bestRow));
            }
            else
            {
                CompleteMergeImmediate(movingBlock, targetBlock, nextTier, nextCfg, bestCol, bestRow);
            }

            return targetBlock;
        }

        private IEnumerator AnimateMergeFusionRoutine(
            JellyBlock movingBlock,
            JellyBlock targetBlock,
            int nextTier,
            TierConfigData nextCfg,
            int bestCol,
            int bestRow)
        {
            GameFeelConfig feel = GameFeelConfig.Instance;
            Vector3 startWorldA = movingBlock != null ? movingBlock.transform.position : Vector3.zero;
            Vector3 startWorldB = targetBlock != null ? targetBlock.transform.position : Vector3.zero;
            Vector3 fusionCenter = GridToWorldPosition(bestCol, bestRow, nextCfg.gridWidth, nextCfg.gridHeight);

            bool horizCollision = Mathf.Abs(startWorldA.x - startWorldB.x) >= Mathf.Abs(startWorldA.z - startWorldB.z);
            float fusionDuration = Mathf.Max(0.02f, feel.mergeSuckInDuration);
            float elapsed = 0f;

            // Stage 1: Magnetic Suck-In & Squish Together
            while (elapsed < fusionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fusionDuration);
                float pullT = t * t * (2.1f - 1.1f * t);

                float compress = 1f - 0.28f * Mathf.Sin(t * Mathf.PI * 0.85f);
                float bulge = 1f + 0.18f * Mathf.Sin(t * Mathf.PI * 0.75f);
                float shrinkCore = Mathf.Lerp(1f, 0.76f, t * t);

                Vector3 fuseScale = horizCollision
                    ? new Vector3(compress * shrinkCore, bulge, bulge * shrinkCore)
                    : new Vector3(bulge * shrinkCore, bulge, compress * shrinkCore);

                if (movingBlock != null)
                {
                    movingBlock.SetFusionVisualState(Vector3.Lerp(startWorldA, fusionCenter, pullT), fuseScale);
                }
                if (targetBlock != null)
                {
                    targetBlock.SetFusionVisualState(Vector3.Lerp(startWorldB, fusionCenter, pullT), fuseScale);
                }

                yield return null;
            }

            if (movingBlock != null)
            {
                Destroy(movingBlock.gameObject);
            }

            if (targetBlock == null) yield break;

            // Stage 2: Transform into Next Tier & Play Pronounced Jelly Evolution Pop
            targetBlock.ClearFusionVisualState();
            targetBlock.ApplyTierConfig(nextTier);
            targetBlock.SetGridPositionImmediate(bestCol, bestRow);
            targetBlock.TriggerMergeEvolutionPop(feel.jellyMergePopDuration);

            ResolveOverlapsByPushingNeighbors(targetBlock);
            Physics.SyncTransforms();

            if (MergeVFXManager.Instance != null)
            {
                float vfxScale = Mathf.Lerp(0.85f, 1.85f, (nextTier - 1f) / 11f);
                MergeVFXManager.Instance.PlayMergeSplash(targetBlock.transform.position, nextCfg.bodyColor, vfxScale);
            }

            if (nextTier > _highestUnlockedTier)
            {
                _highestUnlockedTier = nextTier;
            }
            OnTierUnlocked?.Invoke(nextTier);

            if (nextTier >= TierDatabase.MaxTier)
            {
                targetBlock.IsMerging = false;
                if (MergeVFXManager.Instance != null)
                {
                    Vector3 checkOffset = new Vector3(-targetBlock.GridWidth * 0.46f, 0.85f, -targetBlock.GridHeight * 0.38f);
                    MergeVFXManager.Instance.SpawnVictoryCheckmark(targetBlock.transform, checkOffset);
                }

                OnGoalTierCreated?.Invoke(targetBlock);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.NotifyVictory(targetBlock);
                }
                yield break;
            }

            // Release IsMerging immediately so continuous dragging feels weightless and responsive
            targetBlock.IsMerging = false;
        }

        private void CompleteMergeImmediate(
            JellyBlock movingBlock,
            JellyBlock targetBlock,
            int nextTier,
            TierConfigData nextCfg,
            int bestCol,
            int bestRow)
        {
            if (movingBlock != null) DestroyImmediate(movingBlock.gameObject);
            if (targetBlock == null) return;

            targetBlock.ClearFusionVisualState();
            targetBlock.ApplyTierConfig(nextTier);
            targetBlock.SetGridPositionImmediate(bestCol, bestRow);
            targetBlock.IsMerging = false;
            ResolveOverlapsByPushingNeighbors(targetBlock);
            Physics.SyncTransforms();
        }

        private void FindBestPlacementForMergedBlock(
            JellyBlock moving,
            JellyBlock target,
            int newW,
            int newH,
            out int bestCol,
            out int bestRow)
        {
            Vector2 targetCenter = new Vector2(target.GridX + target.GridWidth * 0.5f, target.GridY + target.GridHeight * 0.5f);
            Vector2 midCenter = new Vector2(
                (moving.GridX + moving.GridWidth * 0.5f + targetCenter.x) * 0.5f,
                (moving.GridY + moving.GridHeight * 0.5f + targetCenter.y) * 0.5f);

            bestCol = Mathf.Clamp(Mathf.RoundToInt(targetCenter.x - newW * 0.5f), 0, cols - newW);
            bestRow = Mathf.Clamp(Mathf.RoundToInt(targetCenter.y - newH * 0.5f), 0, rows - newH);

            float bestScore = float.MinValue;

            for (int r = 0; r <= rows - newH; r++)
            {
                for (int c = 0; c <= cols - newW; c++)
                {
                    Rect candRect = new Rect(c + 0.05f, r + 0.05f, newW - 0.1f, newH - 0.1f);

                    float obstacleArea = 0f;
                    int obstacleCount = 0;
                    for (int i = 0; i < ActiveBlocks.Count; i++)
                    {
                        JellyBlock other = ActiveBlocks[i];
                        if (other == null || other == target || other == moving) continue;

                        Rect otherRect = new Rect(other.GridX + 0.05f, other.GridY + 0.05f, other.GridWidth - 0.1f, other.GridHeight - 0.1f);
                        if (candRect.Overlaps(otherRect))
                        {
                            float ox = Mathf.Min(candRect.xMax, otherRect.xMax) - Mathf.Max(candRect.xMin, otherRect.xMin);
                            float oy = Mathf.Min(candRect.yMax, otherRect.yMax) - Mathf.Max(candRect.yMin, otherRect.yMin);
                            obstacleArea += ox * oy * (other.GridWidth * other.GridHeight);
                            obstacleCount++;
                        }
                    }

                    Vector2 candCenter = new Vector2(c + newW * 0.5f, r + newH * 0.5f);
                    float distToTarget = Vector2.Distance(candCenter, targetCenter);
                    float distToMid = Vector2.Distance(candCenter, midCenter);

                    float score = -obstacleArea * 120f - obstacleCount * 45f - distToTarget * 6.5f - distToMid * 3.5f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCol = c;
                        bestRow = r;
                    }
                }
            }
        }

        private void ResolveOverlapsByPushingNeighbors(JellyBlock mergedBlock)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                bool anyOverlap = false;
                for (int i = 0; i < ActiveBlocks.Count; i++)
                {
                    JellyBlock other = ActiveBlocks[i];
                    if (other == null || other == mergedBlock) continue;

                    if (RectsOverlap(mergedBlock.GridX, mergedBlock.GridY, mergedBlock.GridWidth, mergedBlock.GridHeight,
                            other.GridX, other.GridY, other.GridWidth, other.GridHeight))
                    {
                        anyOverlap = true;
                        MoveBlockToNearestFreeCell(other);
                    }
                }
                if (!anyOverlap) break;
            }
        }

        private void MoveBlockToNearestFreeCell(JellyBlock block)
        {
            float bestDist = float.MaxValue;
            int bestC = Mathf.Clamp(Mathf.RoundToInt(block.GridX), 0, cols - block.GridWidth);
            int bestR = Mathf.Clamp(Mathf.RoundToInt(block.GridY), 0, rows - block.GridHeight);

            for (int r = 0; r <= rows - block.GridHeight; r++)
            {
                for (int c = 0; c <= cols - block.GridWidth; c++)
                {
                    if (!OverlapsSolidObstacle(block, c, r, false))
                    {
                        float d = Vector2.SqrMagnitude(new Vector2(c - block.GridX, r - block.GridY));
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestC = c;
                            bestR = r;
                        }
                    }
                }
            }

            block.SetGridPositionSmooth(bestC, bestR, 0.12f);
        }

        private static bool RectsOverlap(float ax, float ay, int aw, int ah, float bx, float by, int bw, int bh)
        {
            const float pad = 0.06f;
            return (ax + pad) < (bx + bw - pad) &&
                   (ax + aw - pad) > (bx + pad) &&
                   (ay + pad) < (by + bh - pad) &&
                   (ay + ah - pad) > (by + pad);
        }

        public bool TryFindHintPair(out JellyBlock fromBlock, out JellyBlock toBlock)
        {
            fromBlock = null;
            toBlock = null;

            JellyBlock bottomYellow = null;
            JellyBlock targetYellow = null;
            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock b = ActiveBlocks[i];
                if (b == null) continue;
                if (b.Tier == 3 && b.SnappedCol == 4 && b.SnappedRow == 7) bottomYellow = b;
                if (b.Tier == 3 && b.SnappedCol == 4 && b.SnappedRow == 6) targetYellow = b;
            }

            if (bottomYellow != null && targetYellow != null)
            {
                fromBlock = bottomYellow;
                toBlock = targetYellow;
                return true;
            }

            float bestScore = float.MaxValue;
            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock a = ActiveBlocks[i];
                if (a == null) continue;

                for (int j = i + 1; j < ActiveBlocks.Count; j++)
                {
                    JellyBlock b = ActiveBlocks[j];
                    if (b == null || b.Tier != a.Tier) continue;

                    float dist = Vector2.Distance(new Vector2(a.GridX, a.GridY), new Vector2(b.GridX, b.GridY));
                    float score = dist - (a.GridY + b.GridY) * 0.25f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        fromBlock = a.GridY >= b.GridY ? a : b;
                        toBlock = a.GridY >= b.GridY ? b : a;
                    }
                }
            }

            return fromBlock != null && toBlock != null;
        }
    }
}
