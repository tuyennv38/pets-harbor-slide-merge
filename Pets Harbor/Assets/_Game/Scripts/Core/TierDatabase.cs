using System;
using UnityEngine;

namespace PetsHarbor.SlideMerge
{
    public enum EmojiFaceType
    {
        T01_AngryFrown = 1,
        T02_HappySmile = 2,
        T03_WinkCat = 3,
        T04_CalmSmile = 4,
        T05_WorriedSweat = 5,
        T06_SquintCat = 6,
        T07_GrumpyZigzag = 7,
        T08_SurprisedO = 8,
        T09_AngryShout = 9,
        T10_SadPuppy = 10,
        T11_LaughSquint = 11,
        T12_CuteWinkGoal = 12
    }

    [Serializable]
    public struct TierConfigData
    {
        public int tier;
        public string idName;
        public Color bodyColor;
        public Color rimHighlightColor;
        public int gridWidth;
        public int gridHeight;
        public EmojiFaceType emojiFace;
        public int mergeValue;

        public TierConfigData(
            int tier,
            string idName,
            string hexColor,
            int gridWidth,
            int gridHeight,
            EmojiFaceType emojiFace)
        {
            this.tier = tier;
            this.idName = idName;
            ColorUtility.TryParseHtmlString(hexColor, out this.bodyColor);
            this.rimHighlightColor = Color.Lerp(this.bodyColor, Color.white, 0.38f);
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            this.emojiFace = emojiFace;
            this.mergeValue = 1 << (tier - 1);
        }
    }

    [Serializable]
    public struct BlockSpawnInfo
    {
        public int tier;
        public int col;
        public int row;

        public BlockSpawnInfo(int tier, int col, int row)
        {
            this.tier = tier;
            this.col = col;
            this.row = row;
        }
    }

    public static class TierDatabase
    {
        public const int BoardCols = 7;
        public const int BoardRows = 8;
        public const int MaxTier = 12;

        public static readonly TierConfigData[] Tiers = new TierConfigData[]
        {
            new TierConfigData(1,  "T01_Orange",     "#EB6B18", 1, 1, EmojiFaceType.T01_AngryFrown),
            new TierConfigData(2,  "T02_Peach",      "#E5AA84", 1, 1, EmojiFaceType.T02_HappySmile),
            new TierConfigData(3,  "T03_Yellow",     "#F7C816", 1, 1, EmojiFaceType.T03_WinkCat),
            new TierConfigData(4,  "T04_Brown",      "#8E5A3B", 2, 1, EmojiFaceType.T04_CalmSmile),
            new TierConfigData(5,  "T05_Green",      "#67E348", 2, 1, EmojiFaceType.T05_WorriedSweat),
            new TierConfigData(6,  "T06_Red",        "#E81E25", 2, 2, EmojiFaceType.T06_SquintCat),
            new TierConfigData(7,  "T07_Magenta",    "#EB087E", 2, 2, EmojiFaceType.T07_GrumpyZigzag),
            new TierConfigData(8,  "T08_SkyBlue",    "#8BD4EA", 2, 2, EmojiFaceType.T08_SurprisedO),
            new TierConfigData(9,  "T09_GoldenTan",  "#E8BF6A", 2, 2, EmojiFaceType.T09_AngryShout),
            new TierConfigData(10, "T10_MintCyan",   "#8AE6C7", 3, 2, EmojiFaceType.T10_SadPuppy),
            new TierConfigData(11, "T11_LimeGreen",  "#AAE849", 3, 2, EmojiFaceType.T11_LaughSquint),
            new TierConfigData(12, "T12_PurpleGoal", "#A626EB", 3, 3, EmojiFaceType.T12_CuteWinkGoal),
        };

        /// <summary>
        /// Exact 27 blocks from frame 00:00 of the YouTube Short creative.
        /// Total merge value = 4*(1) + 6*(2) + 8*(4) + 2*(8) + 2*(16) + 1*(32) + 1*(128) + 1*(256) + 1*(512) + 1*(1024) = 2048 = 1 x Tier 12.
        /// </summary>
        public static readonly BlockSpawnInfo[] InitialLevelLayout = new BlockSpawnInfo[]
        {
            // --- Rows 0..1 ---
            new BlockSpawnInfo(8,  0, 0), // SkyBlue (2x2) at [0..1, 0..1]
            new BlockSpawnInfo(1,  2, 0), // Orange  (1x1)
            new BlockSpawnInfo(3,  3, 0), // Yellow  (1x1)
            new BlockSpawnInfo(2,  4, 0), // Peach   (1x1)
            new BlockSpawnInfo(9,  5, 0), // Tan     (2x2) at [5..6, 0..1]
            new BlockSpawnInfo(4,  2, 1), // Brown   (2x1) at [2..3, 1]
            new BlockSpawnInfo(3,  4, 1), // Yellow  (1x1)

            // --- Row 2 ---
            new BlockSpawnInfo(1,  0, 2), // Orange  (1x1)
            new BlockSpawnInfo(2,  1, 2), // Peach   (1x1)
            new BlockSpawnInfo(3,  2, 2), // Yellow  (1x1)
            new BlockSpawnInfo(5,  3, 2), // Green   (2x1) at [3..4, 2]
            new BlockSpawnInfo(3,  5, 2), // Yellow  (1x1)
            new BlockSpawnInfo(2,  6, 2), // Peach   (1x1)

            // --- Rows 3..4 ---
            new BlockSpawnInfo(11, 0, 3), // Lime    (3x2) at [0..2, 3..4]
            new BlockSpawnInfo(2,  3, 3), // Peach   (1x1)
            new BlockSpawnInfo(1,  3, 4), // Orange  (1x1)
            new BlockSpawnInfo(10, 4, 3), // Cyan    (3x2) at [4..6, 3..4]

            // --- Row 5 ---
            new BlockSpawnInfo(2,  0, 5), // Peach   (1x1)
            new BlockSpawnInfo(3,  1, 5), // Yellow  (1x1)
            new BlockSpawnInfo(2,  2, 5), // Peach   (1x1)
            new BlockSpawnInfo(4,  3, 5), // Brown   (2x1) at [3..4, 5]
            new BlockSpawnInfo(3,  5, 5), // Yellow  (1x1)
            new BlockSpawnInfo(1,  6, 5), // Orange  (1x1)

            // --- Rows 6..7 ---
            new BlockSpawnInfo(5,  2, 7), // Green   (2x1) at [2..3, 7]
            new BlockSpawnInfo(3,  4, 6), // Yellow  (1x1) at [4, 6]
            new BlockSpawnInfo(6,  5, 6), // Red     (2x2) at [5..6, 6..7]
            new BlockSpawnInfo(3,  4, 7), // Yellow  (1x1) at [4, 7] - First tutorial drag block
        };

        public static TierConfigData GetTier(int tier)
        {
            int idx = Mathf.Clamp(tier - 1, 0, Tiers.Length - 1);
            return Tiers[idx];
        }
    }
}
