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
        /// Slides the dragged block toward targetGridPos smoothly along Orthogonal Grid Rails (1:1 along Row or Column)
        /// with L-Turn Corner Assist and strict Head-On Orthogonal Merge (never merging on diagonal/corner contact).
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

            // Ensure starting position never has both X and Y off-rail simultaneously
            float startRoundX = Mathf.Clamp(Mathf.Round(current.x), 0f, cols - block.GridWidth);
            float startRoundY = Mathf.Clamp(Mathf.Round(current.y), 0f, rows - block.GridHeight);
            float startOffX = Mathf.Abs(current.x - startRoundX);
            float startOffY = Mathf.Abs(current.y - startRoundY);
            if (startOffX > 0.002f && startOffY > 0.002f)
            {
                if (startOffX <= startOffY && !OverlapsSolidObstacle(block, startRoundX, current.y, false))
                {
                    current.x = startRoundX;
                }
                else if (!OverlapsSolidObstacle(block, current.x, startRoundY, false))
                {
                    current.y = startRoundY;
                }
            }

            float maxStep = Mathf.Max(0.8f, feel.MaxSlideStepPerFrame * 1.5f);
            float chamferWindow = Mathf.Clamp(feel.CornerChamferRange, 0.12f, 0.34f);

            int maxIterations = 8;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                float roundX = Mathf.Clamp(Mathf.Round(current.x), 0f, cols - block.GridWidth);
                float roundY = Mathf.Clamp(Mathf.Round(current.y), 0f, rows - block.GridHeight);
                float offX = Mathf.Abs(current.x - roundX);
                float offY = Mathf.Abs(current.y - roundY);

                float remX = Mathf.Clamp(desired.x - current.x, -maxStep, maxStep);
                float remY = Mathf.Clamp(desired.y - current.y, -maxStep, maxStep);

                if (Mathf.Abs(remX) < 0.0005f && Mathf.Abs(remY) < 0.0005f)
                {
                    break;
                }

                // STATE 1: Currently sliding on Horizontal Rail (offX > 0.005f, Y is locked to roundY)
                if (offX > 0.005f && offY <= 0.005f)
                {
                    current.y = roundY;
                    int turnCol = Mathf.RoundToInt(roundX);

                    // Check if user wants to make an L-turn into vertical column 'turnCol'
                    if ( offX <= chamferWindow &&
                         Mathf.Abs(desired.y - roundY) > 0.16f &&
                         Mathf.Abs(desired.y - roundY) > Mathf.Abs(desired.x - turnCol) * 1.1f &&
                         CanEnterVerticalRailAt(block, turnCol, Mathf.RoundToInt(roundY), desired.y > roundY ? 1 : -1))
                    {
                        current.x = turnCol;
                        if (MoveAlongVerticalRail(block, ref current, desired.y - current.y, desired.y, allowMerge, out mergedBlock, out _))
                            return true;
                        break;
                    }

                    // Otherwise continue smoothly along Horizontal Rail X
                    if (Mathf.Abs(remX) > 0.0005f)
                    {
                        float prevX = current.x;
                        if (MoveAlongHorizontalRail(block, ref current, remX, desired.x, allowMerge, out mergedBlock, out bool hitIntX))
                            return true;

                        // If we just landed on an integer column during this horizontal slide and have remaining Y movement, loop once more to turn the L-corner!
                        if (hitIntX && Mathf.Abs(current.x - prevX) > 0.001f && Mathf.Abs(desired.y - current.y) > 0.12f)
                        {
                            continue;
                        }
                    }
                    break;
                }

                // STATE 2: Currently sliding on Vertical Rail (offY > 0.005f, X is locked to roundX)
                if (offY > 0.005f && offX <= 0.005f)
                {
                    current.x = roundX;
                    int turnRow = Mathf.RoundToInt(roundY);

                    // Check if user wants to make an L-turn into horizontal row 'turnRow'
                    if ( offY <= chamferWindow &&
                         Mathf.Abs(desired.x - roundX) > 0.16f &&
                         Mathf.Abs(desired.x - roundX) > Mathf.Abs(desired.y - turnRow) * 1.1f &&
                         CanEnterHorizontalRailAt(block, Mathf.RoundToInt(roundX), turnRow, desired.x > roundX ? 1 : -1))
                    {
                        current.y = turnRow;
                        if (MoveAlongHorizontalRail(block, ref current, desired.x - current.x, desired.x, allowMerge, out mergedBlock, out _))
                            return true;
                        break;
                    }

                    // Otherwise continue smoothly along Vertical Rail Y
                    if (Mathf.Abs(remY) > 0.0005f)
                    {
                        float prevY = current.y;
                        if (MoveAlongVerticalRail(block, ref current, remY, desired.y, allowMerge, out mergedBlock, out bool hitIntY))
                            return true;

                        if (hitIntY && Mathf.Abs(current.y - prevY) > 0.001f && Mathf.Abs(desired.x - current.x) > 0.12f)
                        {
                            continue;
                        }
                    }
                    break;
                }

                // STATE 3: At an Integer Grid Intersection (offX <= 0.005f && offY <= 0.005f)
                current.x = roundX;
                current.y = roundY;

                bool preferX = Mathf.Abs(remX) >= Mathf.Abs(remY);
                if (preferX)
                {
                    float beforeX = current.x;
                    if (Mathf.Abs(remX) > 0.0005f)
                    {
                        if (MoveAlongHorizontalRail(block, ref current, remX, desired.x, allowMerge, out mergedBlock, out _))
                            return true;
                    }
                    // If X was completely blocked at this intersection, allow sliding along Y immediately!
                    if (Mathf.Abs(current.x - beforeX) < 0.001f && Mathf.Abs(remY) > 0.0005f)
                    {
                        if (MoveAlongVerticalRail(block, ref current, remY, desired.y, allowMerge, out mergedBlock, out _))
                            return true;
                    }
                }
                else
                {
                    float beforeY = current.y;
                    if (Mathf.Abs(remY) > 0.0005f)
                    {
                        if (MoveAlongVerticalRail(block, ref current, remY, desired.y, allowMerge, out mergedBlock, out _))
                            return true;
                    }
                    // If Y was completely blocked at this intersection, allow sliding along X immediately!
                    if (Mathf.Abs(current.y - beforeY) < 0.001f && Mathf.Abs(remX) > 0.0005f)
                    {
                        if (MoveAlongHorizontalRail(block, ref current, remX, desired.x, allowMerge, out mergedBlock, out _))
                            return true;
                    }
                }
                break;
            }

            block.SetGridPositionFromDrag(current.x, current.y);
            return false;
        }

        private bool CanEnterVerticalRailAt(JellyBlock block, int col, int row, int signY)
        {
            if (OverlapsSolidObstacle(block, col, row, false)) return false;
            float testY = Mathf.Clamp(row + signY * 0.15f, 0f, rows - block.GridHeight);
            if (Mathf.Abs(testY - row) > 0.01f && !OverlapsSolidObstacle(block, col, testY, false))
            {
                return true;
            }
            return TryFindAdjacentMergeTarget(block, col, row, new Vector2Int(0, signY), false, out _);
        }

        private bool CanEnterHorizontalRailAt(JellyBlock block, int col, int row, int signX)
        {
            if (OverlapsSolidObstacle(block, col, row, false)) return false;
            float testX = Mathf.Clamp(col + signX * 0.15f, 0f, cols - block.GridWidth);
            if (Mathf.Abs(testX - col) > 0.01f && !OverlapsSolidObstacle(block, testX, row, false))
            {
                return true;
            }
            return TryFindAdjacentMergeTarget(block, col, row, new Vector2Int(signX, 0), false, out _);
        }

        private bool MoveAlongHorizontalRail(
            JellyBlock block,
            ref Vector2 current,
            float deltaX,
            float desiredX,
            bool allowMerge,
            out JellyBlock mergedBlock,
            out bool stoppedAtInteger)
        {
            mergedBlock = null;
            stoppedAtInteger = false;
            if (Mathf.Abs(deltaX) <= 0.0001f) return false;

            float roundY = Mathf.Clamp(Mathf.Round(current.y), 0f, rows - block.GridHeight);
            current.y = roundY;

            int signX = deltaX > 0f ? 1 : -1;
            float targetX = Mathf.Clamp(current.x + deltaX, 0f, cols - block.GridWidth);

            // Step in small increments along X so we never tunnel through narrow 1x1 blocks
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(targetX - current.x) / 0.12f));
            float stepX = (targetX - current.x) / steps;

            int startIntX = Mathf.RoundToInt(current.x);
            for (int s = 0; s < steps; s++)
            {
                float nextX = Mathf.Clamp(current.x + stepX, 0f, cols - block.GridWidth);
                if (!OverlapsSolidObstacle(block, nextX, roundY, false))
                {
                    current.x = nextX;

                    // SMART PARTNER ALIGNMENT CATCH WHILE DRAGGING:
                    int crossedCol = Mathf.RoundToInt(current.x);
                    if (GameFeelConfig.Instance.enableSmartPartnerAlignmentStop &&
                        crossedCol != startIntX &&
                        Mathf.Abs(current.x - crossedCol) < 0.22f &&
                        HasPerpendicularMergeOpportunity(block, crossedCol, Mathf.RoundToInt(roundY), true, out _, out _))
                    {
                        float breakoutThreshold = 0.85f * Mathf.Max(0.5f, GameFeelConfig.Instance.partnerAlignmentMagnetStrength);
                        if (Mathf.Abs(desiredX - crossedCol) <= breakoutThreshold)
                        {
                            current.x = crossedCol;
                            stoppedAtInteger = true;
                            return false;
                        }
                    }
                }
                else
                {
                    // Blocked on X! Stop at the exact integer column right before the obstacle
                    int stopCol = signX > 0
                        ? Mathf.FloorToInt(current.x + 0.02f)
                        : Mathf.CeilToInt(current.x - 0.02f);
                    stopCol = Mathf.Clamp(stopCol, 0, cols - block.GridWidth);

                    if (!OverlapsSolidObstacle(block, stopCol, roundY, false))
                    {
                        current.x = stopCol;
                    }
                    stoppedAtInteger = true;

                    // Head-On Orthogonal Merge along X: ONLY if pushing into the adjacent block along the SAME row!
                    if (Mathf.Abs(desiredX - stopCol) >= 0.12f &&
                        TryFindAdjacentMergeTarget(block, stopCol, Mathf.RoundToInt(roundY), new Vector2Int(signX, 0), allowMerge, out JellyBlock hitTarget))
                    {
                        block.SetGridPositionFromDrag(stopCol, roundY);
                        mergedBlock = ExecuteMerge(block, hitTarget, new Vector2(signX, 0f));
                        return true;
                    }
                    return false;
                }
            }

            if (Mathf.Abs(current.x - Mathf.Round(current.x)) < 0.01f)
            {
                current.x = Mathf.Round(current.x);
                stoppedAtInteger = true;
            }
            return false;
        }

        private bool MoveAlongVerticalRail(
            JellyBlock block,
            ref Vector2 current,
            float deltaY,
            float desiredY,
            bool allowMerge,
            out JellyBlock mergedBlock,
            out bool stoppedAtInteger)
        {
            mergedBlock = null;
            stoppedAtInteger = false;
            if (Mathf.Abs(deltaY) <= 0.0001f) return false;

            float roundX = Mathf.Clamp(Mathf.Round(current.x), 0f, cols - block.GridWidth);
            current.x = roundX;

            int signY = deltaY > 0f ? 1 : -1;
            float targetY = Mathf.Clamp(current.y + deltaY, 0f, rows - block.GridHeight);

            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(targetY - current.y) / 0.12f));
            float stepY = (targetY - current.y) / steps;

            int startIntY = Mathf.RoundToInt(current.y);
            for (int s = 0; s < steps; s++)
            {
                float nextY = Mathf.Clamp(current.y + stepY, 0f, rows - block.GridHeight);
                if (!OverlapsSolidObstacle(block, roundX, nextY, false))
                {
                    current.y = nextY;

                    // SMART PARTNER ALIGNMENT CATCH WHILE DRAGGING:
                    int crossedRow = Mathf.RoundToInt(current.y);
                    if (GameFeelConfig.Instance.enableSmartPartnerAlignmentStop &&
                        crossedRow != startIntY &&
                        Mathf.Abs(current.y - crossedRow) < 0.22f &&
                        HasPerpendicularMergeOpportunity(block, Mathf.RoundToInt(roundX), crossedRow, false, out _, out _))
                    {
                        float breakoutThreshold = 0.85f * Mathf.Max(0.5f, GameFeelConfig.Instance.partnerAlignmentMagnetStrength);
                        if (Mathf.Abs(desiredY - crossedRow) <= breakoutThreshold)
                        {
                            current.y = crossedRow;
                            stoppedAtInteger = true;
                            return false;
                        }
                    }
                }
                else
                {
                    // Blocked on Y! Stop at the exact integer row right before the obstacle
                    int stopRow = signY > 0
                        ? Mathf.FloorToInt(current.y + 0.02f)
                        : Mathf.CeilToInt(current.y - 0.02f);
                    stopRow = Mathf.Clamp(stopRow, 0, rows - block.GridHeight);

                    if (!OverlapsSolidObstacle(block, roundX, stopRow, false))
                    {
                        current.y = stopRow;
                    }
                    stoppedAtInteger = true;

                    // Head-On Orthogonal Merge along Y: ONLY if pushing into the adjacent block along the SAME column!
                    if (Mathf.Abs(desiredY - stopRow) >= 0.12f &&
                        TryFindAdjacentMergeTarget(block, Mathf.RoundToInt(roundX), stopRow, new Vector2Int(0, signY), allowMerge, out JellyBlock hitTarget))
                    {
                        block.SetGridPositionFromDrag(roundX, stopRow);
                        mergedBlock = ExecuteMerge(block, hitTarget, new Vector2(0f, signY));
                        return true;
                    }
                    return false;
                }
            }

            if (Mathf.Abs(current.y - Mathf.Round(current.y)) < 0.01f)
            {
                current.y = Mathf.Round(current.y);
                stoppedAtInteger = true;
            }
            return false;
        }

        /// <summary>
        /// Checks if 'block' standing at integer cell (col, row) and pushing head-on in 'stepDir' ((±1,0) or (0,±1))
        /// directly collides face-to-face with a same-tier neighbor on the SAME grid lane (never on corner/diagonal touch).
        /// </summary>
        private bool TryFindAdjacentMergeTarget(
            JellyBlock block,
            int col,
            int row,
            Vector2Int stepDir,
            bool allowTriggerMerge,
            out JellyBlock mergeTarget)
        {
            mergeTarget = null;
            if (stepDir.x == 0 && stepDir.y == 0) return false;

            float testX = col + stepDir.x * 0.25f;
            float testY = row + stepDir.y * 0.25f;
            float ax0 = testX;
            float ax1 = testX + block.GridWidth;
            float ay0 = testY;
            float ay1 = testY + block.GridHeight;

            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock other = ActiveBlocks[i];
                if (other == null || other == block || other.IsMerging || other.Tier != block.Tier) continue;

                float bx0 = Mathf.Round(other.GridX);
                float bx1 = bx0 + other.GridWidth;
                float by0 = Mathf.Round(other.GridY);
                float by1 = by0 + other.GridHeight;

                float overlapX = Mathf.Min(ax1, bx1) - Mathf.Max(ax0, bx0);
                float overlapY = Mathf.Min(ay1, by1) - Mathf.Max(ay0, by0);
                if (overlapX <= 0.01f || overlapY <= 0.01f) continue;

                // Require strict orthogonal lane alignment (>= 92%) so blocks NEVER merge on corner/diagonal contact
                bool horizPush = Mathf.Abs(stepDir.x) > 0;
                float alignRatio = horizPush
                    ? (overlapY / block.GridHeight)
                    : (overlapX / block.GridWidth);

                if (alignRatio < 0.92f) continue;

                Vector2 squishVec = horizPush
                    ? new Vector2(0.14f, 0f)
                    : new Vector2(0f, 0.14f);
                block.SetContactSquish(squishVec);
                other.SetContactSquish(squishVec);

                if (allowTriggerMerge)
                {
                    mergeTarget = other;
                    return true;
                }
            }

            return false;
        }

        private bool EvaluateSameTierPenetration(
            JellyBlock block,
            Vector2 testPos,
            Vector2 dragIntent,
            bool allowTriggerMerge,
            out JellyBlock mergeTarget,
            float thresholdMultiplier = 1.0f)
        {
            mergeTarget = null;
            if (!allowTriggerMerge) return false;

            // Only allow merge along the active orthogonal push direction when the block is aligned on the rail
            int col = Mathf.Clamp(Mathf.RoundToInt(testPos.x), 0, cols - block.GridWidth);
            int row = Mathf.Clamp(Mathf.RoundToInt(testPos.y), 0, rows - block.GridHeight);

            if (Mathf.Abs(testPos.x - col) > 0.12f || Mathf.Abs(testPos.y - row) > 0.12f)
            {
                return false;
            }

            if (Mathf.Abs(dragIntent.x) > Mathf.Abs(dragIntent.y) && Mathf.Abs(dragIntent.x) > 0.1f)
            {
                Vector2Int dirX = new Vector2Int(dragIntent.x > 0f ? 1 : -1, 0);
                if (TryFindAdjacentMergeTarget(block, col, row, dirX, true, out mergeTarget))
                    return true;
            }
            else if (Mathf.Abs(dragIntent.y) > 0.1f)
            {
                Vector2Int dirY = new Vector2Int(0, dragIntent.y > 0f ? 1 : -1);
                if (TryFindAdjacentMergeTarget(block, col, row, dirY, true, out mergeTarget))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if placing 'block' at integer cell (col, row) perfectly aligns with a same-tier merge partner
        /// along the perpendicular axis, with an unobstructed path between them.
        /// </summary>
        public bool HasPerpendicularMergeOpportunity(
            JellyBlock block,
            int col,
            int row,
            bool movingHorizontalRail,
            out int perpSign,
            out JellyBlock targetPartner)
        {
            perpSign = 0;
            targetPartner = null;
            if (block == null || block.IsMerging) return false;

            int w = block.GridWidth;
            int h = block.GridHeight;

            // 1. Moving along Horizontal rail -> Check Perpendicular Vertical lanes (Up: -1, Down: +1)
            if (movingHorizontalRail)
            {
                // Check UP (signY = -1, lower row indices)
                for (int r = row - 1; r >= 0; r--)
                {
                    JellyBlock obstacle = FindBlockCoveringArea(col, r, w, 1, block);
                    if (obstacle != null)
                    {
                        if (obstacle.Tier == block.Tier && !obstacle.IsMerging)
                        {
                            // Strict head-on alignment check: must align in X (>= 85%)
                            float overlapX = Mathf.Min(col + w, obstacle.GridX + obstacle.GridWidth) - Mathf.Max(col, obstacle.GridX);
                            float maxW = Mathf.Max(w, obstacle.GridWidth);
                            if (overlapX / maxW >= 0.85f)
                            {
                                perpSign = -1;
                                targetPartner = obstacle;
                                return true;
                            }
                        }
                        // Blocked by a different tier or misaligned block -> path is not clear
                        break;
                    }
                }

                // Check DOWN (signY = +1, higher row indices)
                for (int r = row + h; r < rows; r++)
                {
                    JellyBlock obstacle = FindBlockCoveringArea(col, r, w, 1, block);
                    if (obstacle != null)
                    {
                        if (obstacle.Tier == block.Tier && !obstacle.IsMerging)
                        {
                            float overlapX = Mathf.Min(col + w, obstacle.GridX + obstacle.GridWidth) - Mathf.Max(col, obstacle.GridX);
                            float maxW = Mathf.Max(w, obstacle.GridWidth);
                            if (overlapX / maxW >= 0.85f)
                            {
                                perpSign = 1;
                                targetPartner = obstacle;
                                return true;
                            }
                        }
                        break;
                    }
                }
            }
            // 2. Moving along Vertical rail -> Check Perpendicular Horizontal lanes (Left: -1, Right: +1)
            else
            {
                // Check LEFT (signX = -1, lower col indices)
                for (int c = col - 1; c >= 0; c--)
                {
                    JellyBlock obstacle = FindBlockCoveringArea(c, row, 1, h, block);
                    if (obstacle != null)
                    {
                        if (obstacle.Tier == block.Tier && !obstacle.IsMerging)
                        {
                            float overlapY = Mathf.Min(row + h, obstacle.GridY + obstacle.GridHeight) - Mathf.Max(row, obstacle.GridY);
                            float maxH = Mathf.Max(h, obstacle.GridHeight);
                            if (overlapY / maxH >= 0.85f)
                            {
                                perpSign = -1;
                                targetPartner = obstacle;
                                return true;
                            }
                        }
                        break;
                    }
                }

                // Check RIGHT (signX = +1, higher col indices)
                for (int c = col + w; c < cols; c++)
                {
                    JellyBlock obstacle = FindBlockCoveringArea(c, row, 1, h, block);
                    if (obstacle != null)
                    {
                        if (obstacle.Tier == block.Tier && !obstacle.IsMerging)
                        {
                            float overlapY = Mathf.Min(row + h, obstacle.GridY + obstacle.GridHeight) - Mathf.Max(row, obstacle.GridY);
                            float maxH = Mathf.Max(h, obstacle.GridHeight);
                            if (overlapY / maxH >= 0.85f)
                            {
                                perpSign = 1;
                                targetPartner = obstacle;
                                return true;
                            }
                        }
                        break;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Scans forward along the active rail in 'directionSign' (+1 or -1) to find the nearest integer cell
        /// that has an unobstructed perpendicular merge opportunity with a same-tier partner.
        /// </summary>
        public bool TryFindNearestAlignmentTargetAlongRail(
            JellyBlock block,
            float currentCoord,
            int fixedOtherCoord,
            bool isHorizontalRail,
            int directionSign,
            out int targetCoord,
            out int perpSign,
            out JellyBlock partnerBlock)
        {
            targetCoord = -1;
            perpSign = 0;
            partnerBlock = null;

            if (block == null || block.IsMerging || directionSign == 0) return false;

            if (isHorizontalRail)
            {
                int fixedRow = fixedOtherCoord;
                if (directionSign > 0)
                {
                    int minCol = Mathf.FloorToInt(currentCoord + 0.05f) + 1;
                    int maxCol = cols - block.GridWidth;
                    for (int c = minCol; c <= maxCol; c++)
                    {
                        if (OverlapsSolidObstacle(block, c, fixedRow, false))
                        {
                            break;
                        }
                        if (HasPerpendicularMergeOpportunity(block, c, fixedRow, true, out perpSign, out partnerBlock))
                        {
                            targetCoord = c;
                            return true;
                        }
                    }
                }
                else
                {
                    int maxCol = Mathf.CeilToInt(currentCoord - 0.05f) - 1;
                    for (int c = maxCol; c >= 0; c--)
                    {
                        if (OverlapsSolidObstacle(block, c, fixedRow, false))
                        {
                            break;
                        }
                        if (HasPerpendicularMergeOpportunity(block, c, fixedRow, true, out perpSign, out partnerBlock))
                        {
                            targetCoord = c;
                            return true;
                        }
                    }
                }
            }
            else
            {
                int fixedCol = fixedOtherCoord;
                if (directionSign > 0)
                {
                    int minRow = Mathf.FloorToInt(currentCoord + 0.05f) + 1;
                    int maxRow = rows - block.GridHeight;
                    for (int r = minRow; r <= maxRow; r++)
                    {
                        if (OverlapsSolidObstacle(block, fixedCol, r, false))
                        {
                            break;
                        }
                        if (HasPerpendicularMergeOpportunity(block, fixedCol, r, false, out perpSign, out partnerBlock))
                        {
                            targetCoord = r;
                            return true;
                        }
                    }
                }
                else
                {
                    int maxRow = Mathf.CeilToInt(currentCoord - 0.05f) - 1;
                    for (int r = maxRow; r >= 0; r--)
                    {
                        if (OverlapsSolidObstacle(block, fixedCol, r, false))
                        {
                            break;
                        }
                        if (HasPerpendicularMergeOpportunity(block, fixedCol, r, false, out perpSign, out partnerBlock))
                        {
                            targetCoord = r;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private JellyBlock FindBlockCoveringArea(float areaX, float areaY, float areaW, float areaH, JellyBlock ignore)
        {
            const float pad = 0.04f;
            float x0 = areaX + pad;
            float x1 = areaX + areaW - pad;
            float y0 = areaY + pad;
            float y1 = areaY + areaH - pad;

            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock b = ActiveBlocks[i];
                if (b == null || b == ignore || b.IsMerging) continue;

                float bx0 = b.GridX + pad;
                float bx1 = b.GridX + b.GridWidth - pad;
                float by0 = b.GridY + pad;
                float by1 = b.GridY + b.GridHeight - pad;

                if (x0 < bx1 && x1 > bx0 && y0 < by1 && y1 > by0)
                {
                    return b;
                }
            }
            return null;
        }

        public bool IsCellPassableForBlock(JellyBlock block, int col, int row)
        {
            if (block == null) return false;
            if (col < 0 || col > cols - block.GridWidth || row < 0 || row > rows - block.GridHeight) return false;
            return !OverlapsSolidObstacle(block, col, row, false);
        }

        /// <summary>
        /// Checks if placing 'moving' at (testX, testY) overlaps any other block on the board.
        /// All blocks act as solid boundaries so no block can ever sit overlapping or crooked inside another block.
        /// </summary>
        private bool OverlapsSolidObstacle(JellyBlock moving, float testX, float testY, bool allowSameTierPenetration)
        {
            const float pad = 0.02f;
            float ox0 = testX + pad;
            float ox1 = testX + moving.GridWidth - pad;
            float oy0 = testY + pad;
            float oy1 = testY + moving.GridHeight - pad;

            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock other = ActiveBlocks[i];
                if (other == null || other == moving) continue;

                float bx0 = other.GridX + pad;
                float bx1 = other.GridX + other.GridWidth - pad;
                float by0 = other.GridY + pad;
                float by1 = other.GridY + other.GridHeight - pad;

                if (ox0 < bx1 && ox1 > bx0 && oy0 < by1 && oy1 > by0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Launches the block into a continuous momentum-driven coasting slide based on the player's push force.
        /// If an unobstructed perpendicular alignment with a same-tier partner is ahead, calculates the exact
        /// slide distance to land squarely at that intersection so the player can immediately match with zero overshoot.
        /// </summary>
        public void LaunchBlockCoasting(JellyBlock block, Vector2 initialVelocity)
        {
            if (block == null || block.IsMerging) return;

            if (!Application.isPlaying)
            {
                SnapBlockToValidCell(block);
                return;
            }

            // PRE-CALCULATE EXACT GLIDE DISTANCE TO PARTNER ALIGNMENT INTERSECTION:
            if (GameFeelConfig.Instance.enableSmartPartnerAlignmentStop)
            {
                bool isHoriz = Mathf.Abs(initialVelocity.x) >= Mathf.Abs(initialVelocity.y);
                if (isHoriz && Mathf.Abs(initialVelocity.x) > 0.15f)
                {
                    int signX = initialVelocity.x > 0 ? 1 : -1;
                    int fixedRow = Mathf.RoundToInt(block.GridY);
                    if (TryFindNearestAlignmentTargetAlongRail(block, block.GridX, fixedRow, true, signX, out int targetCol, out int perpSign, out JellyBlock partner))
                    {
                        StartCoroutine(GlideDirectlyToAlignmentIntersection(block, new Vector2(targetCol, fixedRow), perpSign, true, partner));
                        return;
                    }
                }
                else if (!isHoriz && Mathf.Abs(initialVelocity.y) > 0.15f)
                {
                    int signY = initialVelocity.y > 0 ? 1 : -1;
                    int fixedCol = Mathf.RoundToInt(block.GridX);
                    if (TryFindNearestAlignmentTargetAlongRail(block, block.GridY, fixedCol, false, signY, out int targetRow, out int perpSign, out JellyBlock partner))
                    {
                        StartCoroutine(GlideDirectlyToAlignmentIntersection(block, new Vector2(fixedCol, targetRow), perpSign, false, partner));
                        return;
                    }
                }
            }

            StartCoroutine(CoastingGlideRoutine(block, initialVelocity));
        }

        private IEnumerator GlideDirectlyToAlignmentIntersection(
            JellyBlock block,
            Vector2 targetGridPos,
            int perpSign,
            bool isHorizontal,
            JellyBlock partner)
        {
            block.IsCoasting = true;
            Vector2 startPos = new Vector2(block.GridX, block.GridY);
            float totalDist = Vector2.Distance(startPos, targetGridPos);

            if (totalDist <= 0.01f)
            {
                block.SetGridPositionFromDrag(targetGridPos.x, targetGridPos.y);
                block.IsCoasting = false;
                block.TriggerJellyBounce(0.18f);
                SnapBlockToValidCell(block, isHorizontal ? new Vector2(0f, perpSign) : new Vector2(perpSign, 0f));
                yield break;
            }

            // Snappy duration: 0.14s for 1 cell, 0.20s for 2 cells, capped at 0.32s
            float duration = Mathf.Clamp(0.10f + totalDist * 0.055f, 0.14f, 0.32f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (block == null || !block.IsCoasting || block.IsBeingDragged || block.IsMerging)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);

                // Smooth cubic ease-out deceleration: rapid launch, crisp arrival squarely at the intersection
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                Vector2 curPos = Vector2.Lerp(startPos, targetGridPos, ease);

                if (OverlapsSolidObstacle(block, curPos.x, curPos.y, false))
                {
                    break;
                }

                block.SetGridPositionFromDrag(curPos.x, curPos.y);
                Vector2 stepVel = (targetGridPos - startPos) * (3f * (1f - p) * (1f - p) / duration);
                block.SetDragVelocity(stepVel);

                yield return null;
            }

            if (block != null && !block.IsBeingDragged && !block.IsMerging)
            {
                block.IsCoasting = false;
                block.SetGridPositionFromDrag(targetGridPos.x, targetGridPos.y);
                block.TriggerJellyBounce(0.18f);
                SnapBlockToValidCell(block, isHorizontal ? new Vector2(0f, perpSign) : new Vector2(perpSign, 0f));
            }
        }

        private IEnumerator CoastingGlideRoutine(JellyBlock block, Vector2 velocity)
        {
            block.IsCoasting = true;
            block.SetDragVelocity(velocity);

            Vector2 lastMoveDir = velocity.sqrMagnitude > 0.001f ? velocity.normalized : Vector2.zero;
            int startCol = Mathf.RoundToInt(block.GridX);
            int startRow = Mathf.RoundToInt(block.GridY);

            while (block != null && block.IsCoasting && !block.IsBeingDragged && !block.IsMerging)
            {
                float dt = Time.deltaTime;
                if (dt <= 0.0001f)
                {
                    yield return null;
                    continue;
                }

                if (velocity.sqrMagnitude > 0.01f)
                {
                    lastMoveDir = velocity.normalized;
                }

                Vector2 prevPos = new Vector2(block.GridX, block.GridY);
                Vector2 targetPos = prevPos + velocity * dt;

                // SMART ALIGNMENT STOP DURING COASTING / FLICKING:
                // If the block glides past an integer cell that perfectly aligns with a same-tier partner,
                // automatically brake and snap right at that alignment cell!
                if (GameFeelConfig.Instance.enableSmartPartnerAlignmentStop)
                {
                    bool isHoriz = Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y);
                    if (isHoriz && Mathf.Abs(velocity.x) > 0.15f)
                    {
                        int signX = velocity.x > 0 ? 1 : -1;
                        float minX = Mathf.Min(prevPos.x, targetPos.x);
                        float maxX = Mathf.Max(prevPos.x, targetPos.x);
                        int checkCol = signX > 0 ? Mathf.FloorToInt(targetPos.x) : Mathf.CeilToInt(targetPos.x);

                        if (checkCol != startCol && checkCol >= minX - 0.05f && checkCol <= maxX + 0.05f)
                        {
                            if (HasPerpendicularMergeOpportunity(block, checkCol, Mathf.RoundToInt(prevPos.y), true, out int perpSign, out _))
                            {
                                block.IsCoasting = false;
                                block.SetGridPositionFromDrag(checkCol, prevPos.y);
                                block.TriggerJellyBounce(0.18f);
                                SnapBlockToValidCell(block, new Vector2(0f, perpSign));
                                yield break;
                            }
                        }
                    }
                    else if (!isHoriz && Mathf.Abs(velocity.y) > 0.15f)
                    {
                        int signY = velocity.y > 0 ? 1 : -1;
                        float minY = Mathf.Min(prevPos.y, targetPos.y);
                        float maxY = Mathf.Max(prevPos.y, targetPos.y);
                        int checkRow = signY > 0 ? Mathf.FloorToInt(targetPos.y) : Mathf.CeilToInt(targetPos.y);

                        if (checkRow != startRow && checkRow >= minY - 0.05f && checkRow <= maxY + 0.05f)
                        {
                            if (HasPerpendicularMergeOpportunity(block, Mathf.RoundToInt(prevPos.x), checkRow, false, out int perpSign, out _))
                            {
                                block.IsCoasting = false;
                                block.SetGridPositionFromDrag(prevPos.x, checkRow);
                                block.TriggerJellyBounce(0.18f);
                                SnapBlockToValidCell(block, new Vector2(perpSign, 0f));
                                yield break;
                            }
                        }
                    }
                }

                if (SlideBlockWithCollision(block, targetPos, true, out _))
                {
                    // Merged during coasting glide!
                    if (block != null) block.IsCoasting = false;
                    yield break;
                }

                if (block == null || !block.IsCoasting || block.IsBeingDragged || block.IsMerging)
                {
                    yield break;
                }

                Vector2 actualDelta = new Vector2(block.GridX, block.GridY) - prevPos;

                // Detect wall/obstacle impact along X or Y and trigger jelly impact squash
                if (Mathf.Abs(velocity.x) > 0.4f && Mathf.Abs(actualDelta.x) < 0.001f)
                {
                    block.TriggerDirectionalImpact(Vector2.right, Mathf.Abs(velocity.x));
                    velocity.x = 0f;
                }
                if (Mathf.Abs(velocity.y) > 0.4f && Mathf.Abs(actualDelta.y) < 0.001f)
                {
                    block.TriggerDirectionalImpact(Vector2.up, Mathf.Abs(velocity.y));
                    velocity.y = 0f;
                }

                GameFeelConfig feel = GameFeelConfig.Instance;
                float friction = Mathf.Max(1.5f, feel.slideFriction);

                // Smooth physical deceleration (linear + exponential drag)
                velocity = Vector2.MoveTowards(velocity, Vector2.zero, friction * 1.9f * dt);
                velocity *= Mathf.Exp(-friction * 0.32f * dt);
                block.SetDragVelocity(velocity);

                if (velocity.magnitude < 1.35f)
                {
                    break;
                }

                yield return null;
            }

            if (block != null && !block.IsBeingDragged && !block.IsMerging)
            {
                block.IsCoasting = false;
                SnapBlockToValidCell(block, lastMoveDir);
            }
        }

        public void SnapBlockToValidCell(JellyBlock block, Vector2 intentDir = default)
        {
            if (block == null || block.IsMerging) return;

            block.IsCoasting = false;
            GameFeelConfig feel = GameFeelConfig.Instance;
            Vector2 currentPos = new Vector2(block.GridX, block.GridY);

            if (EvaluateSameTierPenetration(block, currentPos, intentDir, true, out JellyBlock touchTarget, 0.55f))
            {
                ExecuteMerge(block, touchTarget, intentDir);
                return;
            }

            block.ClearFusionVisualState();

            // Directional Intent Bias:
            // If the player nudged/slid the block in a direction (even by just 7% of a cell),
            // snap FORWARD to the next grid cell in that direction instead of snapping back to the origin cell!
            const float intentBias = 0.43f; // 0.50 - 0.43 = only 0.07 cells required to advance to the next cell
            float biasedX = block.GridX;
            float biasedY = block.GridY;

            if (Mathf.Abs(intentDir.x) > 0.15f)
            {
                biasedX += Mathf.Sign(intentDir.x) * intentBias;
            }
            if (Mathf.Abs(intentDir.y) > 0.15f)
            {
                biasedY += Mathf.Sign(intentDir.y) * intentBias;
            }

            // Smart Partner Alignment Snap on Release:
            // If the block is within range of an alignment opportunity cell, snap directly into alignment!
            if (GameFeelConfig.Instance.enableSmartPartnerAlignmentStop)
            {
                int curRoundCol = Mathf.Clamp(Mathf.RoundToInt(block.GridX), 0, cols - block.GridWidth);
                int curRoundRow = Mathf.Clamp(Mathf.RoundToInt(block.GridY), 0, rows - block.GridHeight);

                // Priority 1: Current integer cell has opportunity
                if (!OverlapsSolidObstacle(block, curRoundCol, curRoundRow, false) &&
                    HasPerpendicularMergeOpportunity(block, curRoundCol, curRoundRow, Mathf.Abs(intentDir.x) >= Mathf.Abs(intentDir.y), out _, out _))
                {
                    biasedX = curRoundCol;
                    biasedY = curRoundRow;
                }
                // Priority 2: In direction of intent, find alignment opportunity along the corridor
                else if (Mathf.Abs(intentDir.x) > 0.15f)
                {
                    int signX = intentDir.x > 0 ? 1 : -1;
                    if (TryFindNearestAlignmentTargetAlongRail(block, block.GridX, curRoundRow, true, signX, out int targetCol, out _, out _))
                    {
                        if (Mathf.Abs(targetCol - block.GridX) <= 2.5f)
                        {
                            biasedX = targetCol;
                            biasedY = curRoundRow;
                        }
                    }
                }
                else if (Mathf.Abs(intentDir.y) > 0.15f)
                {
                    int signY = intentDir.y > 0 ? 1 : -1;
                    if (TryFindNearestAlignmentTargetAlongRail(block, block.GridY, curRoundCol, false, signY, out int targetRow, out _, out _))
                    {
                        if (Mathf.Abs(targetRow - block.GridY) <= 2.5f)
                        {
                            biasedX = curRoundCol;
                            biasedY = targetRow;
                        }
                    }
                }
            }

            int bestCol = Mathf.Clamp(Mathf.RoundToInt(biasedX), 0, cols - block.GridWidth);
            int bestRow = Mathf.Clamp(Mathf.RoundToInt(biasedY), 0, rows - block.GridHeight);

            // If biased forward cell overlaps a solid obstacle, fall back to nearest unblocked cell
            if (OverlapsSolidObstacle(block, bestCol, bestRow, false))
            {
                bestCol = Mathf.Clamp(Mathf.RoundToInt(block.GridX), 0, cols - block.GridWidth);
                bestRow = Mathf.Clamp(Mathf.RoundToInt(block.GridY), 0, rows - block.GridHeight);
            }

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

            // Also check if snapping into (bestCol, bestRow) brings it into merge contact with a same-tier neighbor
            if (EvaluateSameTierPenetration(block, new Vector2(bestCol, bestRow), intentDir, true, out JellyBlock snapMergeTarget, 0.55f))
            {
                ExecuteMerge(block, snapMergeTarget, intentDir);
                return;
            }

            block.SetGridPositionSmooth(bestCol, bestRow, feel.ReleaseSnapDuration);
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
            float fusionDuration = Mathf.Max(0.02f, feel.MergeSuckInDuration);
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
            targetBlock.TriggerMergeEvolutionPop(feel.JellyMergePopDuration);

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

            bestCol = Mathf.Clamp(Mathf.RoundToInt(target.GridX), 0, cols - newW);
            bestRow = Mathf.Clamp(Mathf.RoundToInt(target.GridY), 0, rows - newH);

            Rect unionFootprint = new Rect(
                Mathf.Min(moving.GridX, target.GridX) - 0.1f,
                Mathf.Min(moving.GridY, target.GridY) - 0.1f,
                Mathf.Max(moving.GridX + moving.GridWidth, target.GridX + target.GridWidth) - Mathf.Min(moving.GridX, target.GridX) + 0.2f,
                Mathf.Max(moving.GridY + moving.GridHeight, target.GridY + target.GridHeight) - Mathf.Min(moving.GridY, target.GridY) + 0.2f);

            float bestScore = float.MinValue;

            for (int r = 0; r <= rows - newH; r++)
            {
                for (int c = 0; c <= cols - newW; c++)
                {
                    Rect candRect = new Rect(c + 0.05f, r + 0.05f, newW - 0.1f, newH - 0.1f);

                    // Merged block MUST overlap the combined footprint of the two merging blocks so it never teleports across the grid
                    if (!candRect.Overlaps(unionFootprint)) continue;

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

                    float score = -obstacleArea * 250f - obstacleCount * 80f - distToTarget * 8.5f - distToMid * 4.5f;
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

        /// <summary>
        /// Uses Breadth-First Search (BFS) on the 7x8 grid to find if 'fromBlock' has an unobstructed
        /// orthogonal sliding path to reach an adjacent face-to-face merge position next to 'toBlock'.
        /// Returns false if obstacles block all corridors between the two blocks.
        /// </summary>
        public bool TryFindReachableMergePath(JellyBlock fromBlock, JellyBlock toBlock, out List<Vector2Int> cellPath)
        {
            cellPath = null;
            if (fromBlock == null || toBlock == null || fromBlock == toBlock) return false;
            if (fromBlock.IsMerging || toBlock.IsMerging || fromBlock.Tier != toBlock.Tier) return false;

            int w = fromBlock.GridWidth;
            int h = fromBlock.GridHeight;
            int maxC = cols - w;
            int maxR = rows - h;

            int startC = Mathf.Clamp(fromBlock.SnappedCol, 0, maxC);
            int startR = Mathf.Clamp(fromBlock.SnappedRow, 0, maxR);
            int toC = toBlock.SnappedCol;
            int toR = toBlock.SnappedRow;
            int toW = toBlock.GridWidth;
            int toH = toBlock.GridHeight;

            float reqAlign = GameFeelConfig.Instance.MergeRequiredAlignment;

            bool[,] visited = new bool[cols + 1, rows + 1];
            Vector2Int[,] parent = new Vector2Int[cols + 1, rows + 1];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            Vector2Int startNode = new Vector2Int(startC, startR);
            visited[startC, startR] = true;
            parent[startC, startR] = new Vector2Int(-1, -1);
            queue.Enqueue(startNode);

            Vector2Int[] dirs =
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();

                // Check if standing at 'cur', 'fromBlock' touches 'toBlock' face-to-face along one of the 4 orthogonal sides
                bool touchesHoriz = (cur.x + w == toC || cur.x == toC + toW);
                bool touchesVert = (cur.y + h == toR || cur.y == toR + toH);

                if (touchesHoriz)
                {
                    float overlapY = Mathf.Min(cur.y + h, toR + toH) - Mathf.Max(cur.y, toR);
                    if (overlapY / h >= reqAlign)
                    {
                        cellPath = ReconstructGridPath(parent, startNode, cur);
                        return true;
                    }
                }
                if (touchesVert)
                {
                    float overlapX = Mathf.Min(cur.x + w, toC + toW) - Mathf.Max(cur.x, toC);
                    if (overlapX / w >= reqAlign)
                    {
                        cellPath = ReconstructGridPath(parent, startNode, cur);
                        return true;
                    }
                }

                // Expand 4 orthogonal grid steps
                for (int d = 0; d < dirs.Length; d++)
                {
                    int nc = cur.x + dirs[d].x;
                    int nr = cur.y + dirs[d].y;
                    if (nc < 0 || nc > maxC || nr < 0 || nr > maxR) continue;
                    if (visited[nc, nr]) continue;

                    if (!OverlapsSolidObstacle(fromBlock, nc, nr, false))
                    {
                        visited[nc, nr] = true;
                        parent[nc, nr] = cur;
                        queue.Enqueue(new Vector2Int(nc, nr));
                    }
                }
            }

            return false;
        }

        private static List<Vector2Int> ReconstructGridPath(Vector2Int[,] parent, Vector2Int start, Vector2Int goal)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int cur = goal;
            int safety = 0;
            while (cur.x >= 0 && cur.y >= 0 && safety++ < 128)
            {
                path.Add(cur);
                if (cur == start) break;
                cur = parent[cur.x, cur.y];
            }
            path.Reverse();
            return path;
        }

        public bool TryFindHintPair(out JellyBlock fromBlock, out JellyBlock toBlock)
        {
            return TryFindHintPairWithPath(out fromBlock, out toBlock, out _);
        }

        public bool TryFindHintPairWithPath(out JellyBlock fromBlock, out JellyBlock toBlock, out List<Vector2Int> bestPath)
        {
            fromBlock = null;
            toBlock = null;
            bestPath = null;

            JellyBlock bottomYellow = null;
            JellyBlock targetYellow = null;
            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock b = ActiveBlocks[i];
                if (b == null) continue;
                if (b.Tier == 3 && b.SnappedCol == 3 && b.SnappedRow == 7) bottomYellow = b;
                if (b.Tier == 3 && b.SnappedCol == 4 && b.SnappedRow == 7) bottomYellow = b;
                if (b.Tier == 3 && b.SnappedCol == 4 && b.SnappedRow == 6) targetYellow = b;
            }

            if (bottomYellow != null && targetYellow != null &&
                TryFindReachableMergePath(bottomYellow, targetYellow, out List<Vector2Int> yellowPath))
            {
                fromBlock = bottomYellow;
                toBlock = targetYellow;
                bestPath = yellowPath;
                return true;
            }

            float bestScore = float.MaxValue;
            for (int i = 0; i < ActiveBlocks.Count; i++)
            {
                JellyBlock a = ActiveBlocks[i];
                if (a == null || a.IsMerging) continue;

                for (int j = 0; j < ActiveBlocks.Count; j++)
                {
                    if (i == j) continue;
                    JellyBlock b = ActiveBlocks[j];
                    if (b == null || b.IsMerging || b.Tier != a.Tier) continue;

                    // ONLY allow pairs where 'a' has a real unobstructed sliding path on the grid to reach 'b'
                    if (!TryFindReachableMergePath(a, b, out List<Vector2Int> candidatePath)) continue;

                    float pathSteps = candidatePath.Count;
                    float score = pathSteps * 1.5f + a.Tier * 0.4f - (a.GridY + b.GridY) * 0.25f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        fromBlock = a;
                        toBlock = b;
                        bestPath = candidatePath;
                    }
                }
            }

            return fromBlock != null && toBlock != null && bestPath != null;
        }
    }
}
