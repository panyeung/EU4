using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;
using UnityEngine;

namespace SengokuChronicle.Presentation
{
    /// <summary>Positions and the node size used by one deterministic map layout pass.</summary>
    public readonly struct JapanMapLayoutResult
    {
        public JapanMapLayoutResult(Vector2[] positions, Vector2 nodeSize)
        {
            Positions = positions;
            NodeSize = nodeSize;
        }

        public Vector2[] Positions { get; }
        public Vector2 NodeSize { get; }
    }

    /// <summary>One resolved grid: cell counts plus the node-size cap for those cells.</summary>
    internal readonly struct JapanMapGrid
    {
        public JapanMapGrid(int columns, int rows, float nodeWidth, float nodeHeight)
        {
            Columns = columns;
            Rows = rows;
            NodeWidth = nodeWidth;
            NodeHeight = nodeHeight;
        }

        public int Columns { get; }
        public int Rows { get; }
        public float NodeWidth { get; }
        public float NodeHeight { get; }
    }

    /// <summary>
    /// Deterministic, collision-free placement for the discrete map nodes. The
    /// normalized hub coordinates choose a preferred grid cell; a nearest-free
    /// search resolves the dense Kinai/Tokai clusters without changing hub IDs or
    /// route endpoints. The grid grows or shrinks with the map area so a narrow
    /// canvas or runtime resize cannot make two 74x28 nodes overlap.
    /// <para>
    /// <see cref="JapanMapHub.MapX"/>/<see cref="JapanMapHub.MapY"/> remain the
    /// compatibility coordinates of this layout. The geographic layout in
    /// <see cref="JapanMapGeoLayout"/> reuses the same grid, node sizes and
    /// collision rules but seeds each preferred cell from a GeoNames anchor.
    /// </para>
    /// </summary>
    public static class JapanMapLayout
    {
        public const float NodeWidth = 74f;
        public const float NodeHeight = 28f;
        public const int Columns = 10;
        public const int Rows = 10;

        /// <summary>
        /// Map areas at or below 1x1 are degenerate and return zero positions.
        /// Above this size the fallback grid always has at least one distinct cell
        /// per hub, even when the nodes must shrink below 74x28.
        /// </summary>
        public const float MinSupportedSize = 1f;

        /// <summary>
        /// Places every hub inside the given map area. At 864x324 (1280x720) the
        /// preferred 10x10 grid is used; at 648x432 (720x720) it becomes 8x10 so
        /// each node keeps its full 74x28 click target.
        /// </summary>
        public static JapanMapLayoutResult ComputeLayout(IReadOnlyList<JapanMapHub> hubs,
            Vector2 viewport)
        {
            if (hubs == null || hubs.Count == 0)
                return new JapanMapLayoutResult(Array.Empty<Vector2>(),
                    new Vector2(NodeWidth, NodeHeight));

            float width = Mathf.Max(0f, viewport.x);
            float height = Mathf.Max(0f, viewport.y);
            if (width <= MinSupportedSize || height <= MinSupportedSize)
                return new JapanMapLayoutResult(new Vector2[hubs.Count],
                    new Vector2(NodeWidth, NodeHeight));

            int count = hubs.Count;
            var preferred = new Vector2[count];
            for (int i = 0; i < count; i++)
                preferred[i] = new Vector2(hubs[i].MapX, hubs[i].MapY);

            JapanMapGrid grid = ResolveGrid(count, width, height);
            return new JapanMapLayoutResult(
                PlaceNodes(grid, width, height, preferred, IdentityOrder(count)),
                ResolveNodeSize(grid, width, height));
        }

        /// <summary>
        /// Chooses the grid for the given area. A 1x1 or smaller area never reaches
        /// this method; callers return the documented degenerate result first.
        /// </summary>
        internal static JapanMapGrid ResolveGrid(int count, float width, float height)
        {
            int maxColumns = Mathf.Max(1, Mathf.FloorToInt((width + 0.01f) / NodeWidth));
            int maxRows = Mathf.Max(1, Mathf.FloorToInt((height + 0.01f) / NodeHeight));
            int columns = Mathf.Min(Columns, maxColumns);
            int rows = Mathf.Min(Rows, maxRows);
            float nodeWidth = NodeWidth;
            float nodeHeight = NodeHeight;

            if (columns * rows < count)
            {
                // Prefer more cells over shrinking a click target.
                columns = Mathf.Min(maxColumns,
                    Mathf.Max(columns, Mathf.CeilToInt(count / (float)rows)));
                rows = Mathf.Min(maxRows,
                    Mathf.Max(rows, Mathf.CeilToInt(count / (float)columns)));
            }

            if (columns * rows < count)
            {
                bool fits = false;
                for (int step = 1; step <= 13; step++)
                {
                    float scale = 1f - step * 0.05f;
                    float candidateWidth = NodeWidth * scale;
                    float candidateHeight = NodeHeight * scale;
                    int candidateColumns = Mathf.Max(1,
                        Mathf.FloorToInt((width + 0.01f) / candidateWidth));
                    int candidateRows = Mathf.Max(1,
                        Mathf.FloorToInt((height + 0.01f) / candidateHeight));
                    if (candidateColumns * candidateRows < count)
                        continue;
                    nodeWidth = candidateWidth;
                    nodeHeight = candidateHeight;
                    columns = candidateColumns;
                    rows = candidateRows;
                    fits = true;
                    break;
                }

                if (!fits)
                {
                    // The preferred 74x28 target cannot fit. Choose a grid whose
                    // aspect matches the viewport and whose capacity covers every
                    // hub, then let the nodes shrink into those cells. This keeps
                    // small/portrait/ultra-wide windows collision-free instead of
                    // silently reusing a cell when the grid is too small.
                    columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(
                        count * width / Mathf.Max(height, 0.01f))), 1, count);
                    rows = Mathf.CeilToInt(count / (float)columns);
                    if (columns * rows < count)
                        rows++;
                    nodeWidth = Mathf.Min(NodeWidth, width / columns);
                    nodeHeight = Mathf.Min(NodeHeight, height / rows);
                }
            }

            if (columns * rows < count)
                throw new InvalidOperationException(
                    "地图布局容量不足：需要 " + count + " 个位置，但只有 " +
                    columns * rows + " 个网格单元。");

            return new JapanMapGrid(columns, rows, nodeWidth, nodeHeight);
        }

        /// <summary>Node size cap: never larger than a cell, never larger than 74x28.</summary>
        internal static Vector2 ResolveNodeSize(JapanMapGrid grid, float width, float height) =>
            new Vector2(
                Mathf.Min(grid.NodeWidth, width / grid.Columns),
                Mathf.Min(grid.NodeHeight, height / grid.Rows));

        internal static int[] IdentityOrder(int count)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            return order;
        }

        /// <summary>
        /// Places one node per grid cell. <paramref name="preferred"/> holds
        /// normalized preferred cells with y growing south; <paramref name="order"/>
        /// is the claim order, so callers can make the result independent of the
        /// hub list order. Every index gets its own in-bounds cell center.
        /// </summary>
        internal static Vector2[] PlaceNodes(JapanMapGrid grid, float width, float height,
            Vector2[] preferred, int[] order)
        {
            int columns = grid.Columns;
            int rows = grid.Rows;
            float cellWidth = width / columns;
            float cellHeight = height / rows;
            var positions = new Vector2[preferred.Length];
            var occupied = new bool[columns, rows];

            for (int step = 0; step < order.Length; step++)
            {
                int index = order[step];
                int preferredColumn = Mathf.Clamp(
                    Mathf.RoundToInt(preferred[index].x * (columns - 1)), 0, columns - 1);
                int preferredRow = Mathf.Clamp(
                    Mathf.RoundToInt(preferred[index].y * (rows - 1)), 0, rows - 1);

                int bestColumn = -1;
                int bestRow = -1;
                int bestDistance = int.MaxValue;
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        if (occupied[column, row])
                            continue;
                        int deltaColumn = column - preferredColumn;
                        int deltaRow = row - preferredRow;
                        int distance = deltaColumn * deltaColumn + deltaRow * deltaRow;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestColumn = column;
                            bestRow = row;
                        }
                    }
                }

                // Capacity is guaranteed above, so every hub gets its own cell.
                occupied[bestColumn, bestRow] = true;

                positions[index] = new Vector2(
                    -width * 0.5f + (bestColumn + 0.5f) * cellWidth,
                    height * 0.5f - (bestRow + 0.5f) * cellHeight);
            }

            return positions;
        }

        /// <summary>Position-only compatibility wrapper for existing callers.</summary>
        public static Vector2[] Compute(IReadOnlyList<JapanMapHub> hubs, Vector2 viewport) =>
            ComputeLayout(hubs, viewport).Positions;
    }
}
