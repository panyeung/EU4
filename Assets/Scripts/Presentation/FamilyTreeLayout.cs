using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>Plain 2D point used by the presenter's layout result.</summary>
    public readonly struct FamilyTreePoint
    {
        public FamilyTreePoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }
    }

    /// <summary>Plain 2D size used by the presenter's layout result.</summary>
    public readonly struct FamilyTreeSize
    {
        public FamilyTreeSize(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; }
        public float Height { get; }
    }

    public enum FamilyTreeLayoutSection
    {
        Generation = 0,
        UnconnectedKin = 1,
        Household = 2
    }

    /// <summary>One person card position inside the scroll content (top-left origin, y grows downward).</summary>
    public readonly struct FamilyTreeNodePlacement
    {
        public FamilyTreeNodePlacement(PersonId personId, FamilyTreePoint position,
            FamilyTreeSize size, int generation, FamilyTreeLayoutSection section)
        {
            PersonId = personId;
            Position = position;
            Size = size;
            Generation = generation;
            Section = section;
        }

        public PersonId PersonId { get; }
        public FamilyTreePoint Position { get; }
        public FamilyTreeSize Size { get; }
        /// <summary>1-based generation for connected kin, 0 for the separate sections.</summary>
        public int Generation { get; }
        public FamilyTreeLayoutSection Section { get; }
    }

    /// <summary>Section header position and title inside the scroll content.</summary>
    public readonly struct FamilyTreeSectionPlacement
    {
        public FamilyTreeSectionPlacement(FamilyTreeLayoutSection section, string title,
            FamilyTreePoint position, FamilyTreeSize size)
        {
            Section = section;
            Title = title ?? string.Empty;
            Position = position;
            Size = size;
        }

        public FamilyTreeLayoutSection Section { get; }
        public string Title { get; }
        public FamilyTreePoint Position { get; }
        public FamilyTreeSize Size { get; }
    }

    /// <summary>Deterministic positions plus the total scrollable content size.</summary>
    public sealed class FamilyTreeLayoutResult
    {
        private readonly List<FamilyTreeNodePlacement> _nodes;
        private readonly List<FamilyTreeSectionPlacement> _sections;
        private readonly Dictionary<string, int> _index =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public FamilyTreeLayoutResult(IEnumerable<FamilyTreeNodePlacement> nodes,
            IEnumerable<FamilyTreeSectionPlacement> sections, FamilyTreeSize contentSize)
        {
            _nodes = nodes != null ? new List<FamilyTreeNodePlacement>(nodes)
                : new List<FamilyTreeNodePlacement>();
            _sections = sections != null
                ? new List<FamilyTreeSectionPlacement>(sections)
                : new List<FamilyTreeSectionPlacement>();
            for (int i = 0; i < _nodes.Count; i++)
            {
                string key = _nodes[i].PersonId.Value ?? string.Empty;
                if (!_index.ContainsKey(key))
                    _index.Add(key, i);
            }
            ContentSize = contentSize;
        }

        public IReadOnlyList<FamilyTreeNodePlacement> Nodes => _nodes.AsReadOnly();
        public IReadOnlyList<FamilyTreeSectionPlacement> Sections => _sections.AsReadOnly();
        public FamilyTreeSize ContentSize { get; }
        public int Count => _nodes.Count;

        public bool TryGetNode(PersonId personId, out FamilyTreeNodePlacement placement)
        {
            if (personId.Value != null && _index.TryGetValue(personId.Value, out int index))
            {
                placement = _nodes[index];
                return true;
            }
            placement = default;
            return false;
        }

        public bool HasSection(FamilyTreeLayoutSection section)
        {
            for (int i = 0; i < _sections.Count; i++)
                if (_sections[i].Section == section)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Deterministic graph layout. Generations are derived from structural parent/child
    /// and adoption facts (spouses and siblings share a generation), never from names,
    /// SC-013 IDs or a fixed member count. Connected kin are laid out in generation
    /// rows; unconnected kin and household members get their own labelled bands below.
    /// </summary>
    public static class FamilyTreeLayout
    {
        public const float NodeWidth = 196f;
        public const float NodeHeight = 88f;
        public const float HorizontalGap = 36f;
        public const float VerticalGap = 56f;
        public const float SectionGap = 44f;
        public const float Margin = 24f;
        public const float SectionHeaderHeight = 28f;
        public const float MinimumContentWidth = 720f;

        private const string KinshipTitle = "家族亲缘";
        private const string UnconnectedTitle = "未连接亲族";
        private const string HouseholdTitle = "家臣・弟子・客将";

        public static FamilyTreeLayoutResult Calculate(FamilyTreeGraphSnapshot graph)
        {
            var nodes = new List<FamilyTreeNodePlacement>();
            var sections = new List<FamilyTreeSectionPlacement>();
            if (graph == null || graph.Count == 0)
                return new FamilyTreeLayoutResult(nodes, sections,
                    new FamilyTreeSize(MinimumContentWidth, 0f));

            var kin = new List<FamilyTreePersonNode>();
            for (int i = 0; i < graph.KinshipMembers.Count; i++)
                kin.Add(graph.KinshipMembers[i]);
            kin.Sort(ComparePersons);

            Dictionary<PersonId, int> depths = CalculateDepths(kin, graph.Relations);
            var rows = new List<List<FamilyTreePersonNode>>();
            for (int i = 0; i < kin.Count; i++)
            {
                int depth = depths[kin[i].PersonId];
                while (rows.Count <= depth)
                    rows.Add(new List<FamilyTreePersonNode>());
                rows[depth].Add(kin[i]);
            }

            var orderInDepth = new Dictionary<PersonId, int>();
            for (int depth = 0; depth < rows.Count; depth++)
            {
                rows[depth].Sort((a, b) => CompareRowOrder(a, b, depth, orderInDepth, graph));
                for (int i = 0; i < rows[depth].Count; i++)
                    orderInDepth[rows[depth][i].PersonId] = i;
            }

            float contentWidth = MinimumContentWidth;
            int widestRow = 0;
            for (int i = 0; i < rows.Count; i++)
                widestRow = Math.Max(widestRow, rows[i].Count);
            if (widestRow > 0)
                contentWidth = Math.Max(contentWidth,
                    Margin * 2f + widestRow * NodeWidth + (widestRow - 1) * HorizontalGap);

            float cursorY = 0f;
            if (kin.Count > 0)
            {
                sections.Add(new FamilyTreeSectionPlacement(FamilyTreeLayoutSection.Generation,
                    KinshipTitle, new FamilyTreePoint(Margin, cursorY),
                    new FamilyTreeSize(contentWidth - Margin * 2f, SectionHeaderHeight)));
                cursorY += SectionHeaderHeight;
                for (int depth = 0; depth < rows.Count; depth++)
                {
                    for (int column = 0; column < rows[depth].Count; column++)
                    {
                        FamilyTreePersonNode person = rows[depth][column];
                        nodes.Add(new FamilyTreeNodePlacement(person.PersonId,
                            new FamilyTreePoint(Margin + column * (NodeWidth + HorizontalGap),
                                cursorY + depth * (NodeHeight + VerticalGap)),
                            new FamilyTreeSize(NodeWidth, NodeHeight), depth + 1,
                            FamilyTreeLayoutSection.Generation));
                    }
                }
                cursorY += rows.Count * NodeHeight + Math.Max(0, rows.Count - 1) * VerticalGap;
            }

            int columns = Math.Max(1, (int)((contentWidth - Margin * 2f + HorizontalGap) /
                                            (NodeWidth + HorizontalGap)));
            cursorY = AppendSection(graph.UnconnectedKin, FamilyTreeLayoutSection.UnconnectedKin,
                UnconnectedTitle, nodes, sections, columns, contentWidth, cursorY);
            cursorY = AppendSection(graph.HouseholdMembers, FamilyTreeLayoutSection.Household,
                HouseholdTitle, nodes, sections, columns, contentWidth, cursorY);

            float contentHeight = cursorY > 0f ? cursorY + Margin : 0f;
            return new FamilyTreeLayoutResult(nodes, sections,
                new FamilyTreeSize(contentWidth, contentHeight));
        }

        private static Dictionary<PersonId, int> CalculateDepths(
            IReadOnlyList<FamilyTreePersonNode> kin,
            IReadOnlyList<FamilyTreeRelationEdge> relations)
        {
            var depths = new Dictionary<PersonId, int>();
            for (int i = 0; i < kin.Count; i++)
                depths[kin[i].PersonId] = 0;
            if (kin.Count == 0)
                return depths;

            var edges = new List<FamilyTreeRelationEdge>();
            for (int i = 0; i < relations.Count; i++)
                if (depths.ContainsKey(relations[i].From) && depths.ContainsKey(relations[i].To))
                    edges.Add(relations[i]);
            edges.Sort((a, b) =>
            {
                int type = ((int)a.Type).CompareTo((int)b.Type);
                if (type != 0)
                    return type;
                int from = string.Compare(a.From.Value ?? string.Empty, b.From.Value ?? string.Empty,
                    StringComparison.Ordinal);
                return from != 0
                    ? from
                    : string.Compare(a.To.Value ?? string.Empty, b.To.Value ?? string.Empty,
                        StringComparison.Ordinal);
            });

            int limit = kin.Count * 4 + 4;
            for (int iteration = 0; iteration < limit; iteration++)
            {
                bool changed = false;
                for (int i = 0; i < edges.Count; i++)
                {
                    FamilyTreeRelationEdge edge = edges[i];
                    int from = depths[edge.From];
                    int to = depths[edge.To];
                    if (edge.Type == KinshipType.ParentChild || edge.Type == KinshipType.Adoption)
                    {
                        if (to < from + 1 && from < limit)
                        {
                            depths[edge.To] = from + 1;
                            changed = true;
                        }
                    }
                    else if (from != to)
                    {
                        int shared = Math.Max(from, to);
                        depths[edge.From] = shared;
                        depths[edge.To] = shared;
                        changed = true;
                    }
                }
                if (!changed)
                    break;
            }

            int minimum = int.MaxValue;
            for (int i = 0; i < kin.Count; i++)
                minimum = Math.Min(minimum, depths[kin[i].PersonId]);
            if (minimum > 0 && minimum != int.MaxValue)
                for (int i = 0; i < kin.Count; i++)
                    depths[kin[i].PersonId] = depths[kin[i].PersonId] - minimum;
            return depths;
        }

        private static int CompareRowOrder(FamilyTreePersonNode a, FamilyTreePersonNode b,
            int depth, Dictionary<PersonId, int> orderInDepth, FamilyTreeGraphSnapshot graph)
        {
            // The current head is always the first card of the first generation. This
            // is a domain flag, not an SC-013 name or ID, and keeps the reading order
            // stable for every family.
            int headA = depth == 0 && a.IsHead ? 0 : 1;
            int headB = depth == 0 && b.IsHead ? 0 : 1;
            if (headA != headB)
                return headA.CompareTo(headB);

            int parentA = ParentOrder(a.PersonId, orderInDepth, graph.Relations);
            int parentB = ParentOrder(b.PersonId, orderInDepth, graph.Relations);
            if (parentA != parentB)
                return parentA.CompareTo(parentB);
            return string.Compare(a.PersonId.Value ?? string.Empty, b.PersonId.Value ?? string.Empty,
                StringComparison.Ordinal);
        }

        private static int ParentOrder(PersonId personId, Dictionary<PersonId, int> orderInDepth,
            IReadOnlyList<FamilyTreeRelationEdge> relations)
        {
            int best = int.MaxValue;
            for (int i = 0; i < relations.Count; i++)
            {
                FamilyTreeRelationEdge edge = relations[i];
                if (edge.To != personId)
                    continue;
                if (edge.Type != KinshipType.ParentChild && edge.Type != KinshipType.Adoption)
                    continue;
                if (orderInDepth.TryGetValue(edge.From, out int order) && order < best)
                    best = order;
            }
            return best;
        }

        private static float AppendSection(IReadOnlyList<FamilyTreePersonNode> people,
            FamilyTreeLayoutSection section, string title,
            List<FamilyTreeNodePlacement> nodes, List<FamilyTreeSectionPlacement> sections,
            int columns, float contentWidth, float startY)
        {
            if (people == null || people.Count == 0)
                return startY;

            var ordered = new List<FamilyTreePersonNode>(people);
            ordered.Sort(ComparePersons);
            if (startY > 0f)
                startY += SectionGap;
            sections.Add(new FamilyTreeSectionPlacement(section, title,
                new FamilyTreePoint(Margin, startY),
                new FamilyTreeSize(contentWidth - Margin * 2f, SectionHeaderHeight)));
            float gridTop = startY + SectionHeaderHeight;
            for (int i = 0; i < ordered.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                nodes.Add(new FamilyTreeNodePlacement(ordered[i].PersonId,
                    new FamilyTreePoint(Margin + column * (NodeWidth + HorizontalGap),
                        gridTop + row * (NodeHeight + VerticalGap)),
                    new FamilyTreeSize(NodeWidth, NodeHeight), 0, section));
            }
            int rowCount = (ordered.Count + columns - 1) / columns;
            return gridTop + rowCount * NodeHeight + Math.Max(0, rowCount - 1) * VerticalGap;
        }

        private static int ComparePersons(FamilyTreePersonNode a, FamilyTreePersonNode b)
        {
            return string.Compare(a.PersonId.Value ?? string.Empty,
                b.PersonId.Value ?? string.Empty, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Pure card-text formatting shared by the roster renderer and its tests. A known
    /// life record is displayed even when no age can be computed; an absent life record
    /// never claims that a person is dead.
    /// </summary>
    public static class FamilyTreeCardText
    {
        public static string Format(FamilyTreePersonNode person,
            FamilyTreeNodePlacement placement)
        {
            string marker = person.IsHead
                ? "【家主】"
                : person.IsDesignatedHeir
                    ? person.IsDesignatedHeirValid ? "【指定继承人】" : "【指定继承人·已失效】"
                    : string.Empty;
            string age = person.HasAge ? person.Age + "岁" : "年龄不详";
            string life = !person.HasLife
                ? string.Empty
                : !person.IsAlive
                    ? " · 已故"
                    : person.IsRetired ? " · 已隐居" : string.Empty;
            string group;
            if (placement.Section == FamilyTreeLayoutSection.Generation)
                group = "第" + placement.Generation + "代";
            else if (placement.Section == FamilyTreeLayoutSection.UnconnectedKin)
                group = "未连接亲族";
            else
                group = "附属成员";
            return person.DisplayName + marker + "\n" + person.RoleName + " · " + age + life +
                   "\n" + group;
        }
    }

    /// <summary>
    /// Deterministic relation-line routing. Spouse links use a direct side-to-side line
    /// only when it is clear. When another person card lies on that line the connector
    /// first tries the six deterministic lanes around the two endpoint rows, then falls
    /// back to a clearance-aware rectilinear visibility grid whose outer margin lies
    /// beyond every card. If even that grid cannot connect the endpoints, routing fails
    /// explicitly with an empty path so the renderer draws nothing through a card.
    /// Other relation kinds keep their existing shapes.
    /// </summary>
    public static class FamilyTreeRouting
    {
        public const float CardClearance = 12f;

        public static IReadOnlyList<FamilyTreePoint> BuildPath(KinshipType type,
            FamilyTreeNodePlacement from, FamilyTreeNodePlacement to,
            IReadOnlyList<FamilyTreeNodePlacement> placements)
        {
            return TryBuildPath(type, from, to, placements,
                out IReadOnlyList<FamilyTreePoint> path)
                ? path
                : Array.Empty<FamilyTreePoint>();
        }

        /// <summary>
        /// Plans one relation line. A false result (and an empty path) is an explicit
        /// route failure; callers must not draw a connector in that case.
        /// </summary>
        public static bool TryBuildPath(KinshipType type,
            FamilyTreeNodePlacement from, FamilyTreeNodePlacement to,
            IReadOnlyList<FamilyTreeNodePlacement> placements,
            out IReadOnlyList<FamilyTreePoint> path)
        {
            switch (type)
            {
                case KinshipType.ParentChild:
                case KinshipType.Adoption:
                    path = ParentChildPath(from, to);
                    break;
                case KinshipType.Spouse:
                    path = SpousePath(from, to, placements);
                    break;
                case KinshipType.Sibling:
                    path = SiblingPath(from, to);
                    break;
                default:
                    path = new List<FamilyTreePoint>
                    {
                        Center(from),
                        Center(to)
                    };
                    break;
            }
            return path != null && path.Count >= 2;
        }

        /// <summary>
        /// True when a segment enters the interior of a person card. Card boundaries are
        /// inset slightly so a connector that merely touches a card edge is not treated as
        /// crossing it.
        /// </summary>
        public static bool SegmentIntersectsCard(FamilyTreePoint start, FamilyTreePoint end,
            FamilyTreeNodePlacement card)
        {
            return SegmentIntersectsRect(start, end, card.Position.X, card.Position.Y,
                card.Position.X + card.Size.Width, card.Position.Y + card.Size.Height);
        }

        private static bool SegmentIntersectsRect(FamilyTreePoint start, FamilyTreePoint end,
            float left, float top, float right, float bottom)
        {
            const float inset = 0.5f;
            left += inset;
            right -= inset;
            top += inset;
            bottom -= inset;
            if (right <= left || bottom <= top)
                return false;

            float t0 = 0f;
            float t1 = 1f;
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            if (!Clip(-dx, start.X - left, ref t0, ref t1)) return false;
            if (!Clip(dx, right - start.X, ref t0, ref t1)) return false;
            if (!Clip(-dy, start.Y - top, ref t0, ref t1)) return false;
            if (!Clip(dy, bottom - start.Y, ref t0, ref t1)) return false;
            return t0 < t1;
        }

        private static IReadOnlyList<FamilyTreePoint> ParentChildPath(
            FamilyTreeNodePlacement from, FamilyTreeNodePlacement to)
        {
            var start = new FamilyTreePoint(CenterX(from), from.Position.Y + from.Size.Height);
            var end = new FamilyTreePoint(CenterX(to), to.Position.Y);
            var path = new List<FamilyTreePoint>(4);
            if (end.Y <= start.Y)
            {
                path.Add(start);
                path.Add(end);
                return path;
            }

            float middle = (start.Y + end.Y) / 2f;
            path.Add(start);
            path.Add(new FamilyTreePoint(start.X, middle));
            path.Add(new FamilyTreePoint(end.X, middle));
            path.Add(end);
            return path;
        }

        private static IReadOnlyList<FamilyTreePoint> SiblingPath(
            FamilyTreeNodePlacement from, FamilyTreeNodePlacement to)
        {
            var start = new FamilyTreePoint(CenterX(from), from.Position.Y + from.Size.Height);
            var end = new FamilyTreePoint(CenterX(to), to.Position.Y + to.Size.Height);
            float detour = Math.Max(start.Y, end.Y) + 14f;
            return new List<FamilyTreePoint>
            {
                start,
                new FamilyTreePoint(start.X, detour),
                new FamilyTreePoint(end.X, detour),
                end
            };
        }

        private static IReadOnlyList<FamilyTreePoint> SpousePath(
            FamilyTreeNodePlacement from, FamilyTreeNodePlacement to,
            IReadOnlyList<FamilyTreeNodePlacement> placements)
        {
            bool fromIsLeft = CenterX(from) <= CenterX(to);
            var start = new FamilyTreePoint(
                fromIsLeft ? from.Position.X + from.Size.Width : from.Position.X,
                CenterY(from));
            var end = new FamilyTreePoint(
                fromIsLeft ? to.Position.X : to.Position.X + to.Size.Width,
                CenterY(to));
            var direct = new List<FamilyTreePoint>(2) { start, end };
            if (!IntersectsAnyCard(direct, placements, from.PersonId, to.PersonId))
                return direct;

            // Prefer a lane just below the cards, then above them. Further lanes cover
            // unusual layouts where the first gap already contains another card.
            float below = Math.Max(Bottom(from), Bottom(to)) + CardClearance;
            float above = Math.Min(Top(from), Top(to)) - CardClearance;
            float step = FamilyTreeLayout.NodeHeight + FamilyTreeLayout.VerticalGap;
            var lanes = new List<float>(6)
            {
                below, above,
                below + step, above - step,
                below + step * 2f, above - step * 2f
            };

            for (int i = 0; i < lanes.Count; i++)
            {
                List<FamilyTreePoint> path = SpouseDetour(from, to, start, end, lanes[i],
                    fromIsLeft);
                if (CountCardIntersections(path, placements, from.PersonId, to.PersonId) == 0)
                    return path;
            }

            // Every fixed lane is blocked. Build a route on a clearance-aware
            // visibility grid instead of accepting the least-crossing path. The grid
            // always includes one coordinate line beyond the complete card bounds, so
            // a clear outer corridor exists whenever the endpoint ports themselves are
            // reachable. No least-crossing connector is ever drawn through a card.
            if (TryBuildVisibilityRoute(from, to, start, end, placements,
                    out List<FamilyTreePoint> guaranteed))
                return guaranteed;
            return Array.Empty<FamilyTreePoint>();
        }

        private readonly struct RouteRect
        {
            public RouteRect(float left, float top, float right, float bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public float Left { get; }
            public float Top { get; }
            public float Right { get; }
            public float Bottom { get; }
        }

        /// <summary>
        /// Rectilinear visibility-grid route. Non-endpoint cards are expanded by
        /// <see cref="CardClearance"/>; the endpoint cards stay at their real bounds so
        /// the connector still starts and ends exactly on their side boundaries. Grid
        /// lines sit on every expanded edge plus one outer margin beyond all cards, and
        /// only axis-aligned edges that cross no expanded rectangle are usable.
        /// </summary>
        private static bool TryBuildVisibilityRoute(FamilyTreeNodePlacement from,
            FamilyTreeNodePlacement to, FamilyTreePoint start, FamilyTreePoint end,
            IReadOnlyList<FamilyTreeNodePlacement> placements,
            out List<FamilyTreePoint> path)
        {
            path = null;
            if (placements == null || placements.Count == 0)
                return false;

            var obstacles = new List<RouteRect>(placements.Count);
            var xs = new List<float>(placements.Count * 4 + 6);
            var ys = new List<float>(placements.Count * 4 + 6);
            for (int i = 0; i < placements.Count; i++)
            {
                FamilyTreeNodePlacement card = placements[i];
                float left = card.Position.X;
                float top = card.Position.Y;
                float right = left + card.Size.Width;
                float bottom = top + card.Size.Height;
                bool isEndpoint = card.PersonId == from.PersonId ||
                                  card.PersonId == to.PersonId;
                if (!isEndpoint)
                {
                    left -= CardClearance;
                    top -= CardClearance;
                    right += CardClearance;
                    bottom += CardClearance;
                }
                obstacles.Add(new RouteRect(left, top, right, bottom));
                AddCoordinate(xs, left);
                AddCoordinate(xs, right);
                AddCoordinate(ys, top);
                AddCoordinate(ys, bottom);
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < obstacles.Count; i++)
            {
                RouteRect rect = obstacles[i];
                minX = Math.Min(minX, rect.Left);
                maxX = Math.Max(maxX, rect.Right);
                minY = Math.Min(minY, rect.Top);
                maxY = Math.Max(maxY, rect.Bottom);
            }
            AddCoordinate(xs, minX - CardClearance);
            AddCoordinate(xs, maxX + CardClearance);
            AddCoordinate(ys, minY - CardClearance);
            AddCoordinate(ys, maxY + CardClearance);
            AddCoordinate(xs, start.X);
            AddCoordinate(xs, end.X);
            AddCoordinate(ys, start.Y);
            AddCoordinate(ys, end.Y);
            xs.Sort();
            ys.Sort();

            int startX = CoordinateIndex(xs, start.X);
            int startY = CoordinateIndex(ys, start.Y);
            int endX = CoordinateIndex(xs, end.X);
            int endY = CoordinateIndex(ys, end.Y);
            if (startX < 0 || startY < 0 || endX < 0 || endY < 0)
                return false;

            int width = xs.Count;
            int height = ys.Count;
            int startIndex = startY * width + startX;
            int endIndex = endY * width + endX;
            var visited = new bool[width * height];
            var parent = new int[width * height];
            for (int i = 0; i < parent.Length; i++)
                parent[i] = -1;
            if (PointInsideAnyObstacle(start, obstacles) ||
                PointInsideAnyObstacle(end, obstacles))
                return false;

            var queue = new Queue<int>();
            visited[startIndex] = true;
            queue.Enqueue(startIndex);
            while (queue.Count > 0 && !visited[endIndex])
            {
                int current = queue.Dequeue();
                int ix = current % width;
                int iy = current / width;
                TryVisitRouteNode(ix - 1, iy, current);
                TryVisitRouteNode(ix + 1, iy, current);
                TryVisitRouteNode(ix, iy - 1, current);
                TryVisitRouteNode(ix, iy + 1, current);
            }

            if (!visited[endIndex])
                return false;

            var points = new List<FamilyTreePoint>();
            for (int at = endIndex; at >= 0; at = parent[at])
                points.Add(new FamilyTreePoint(xs[at % width], ys[at / width]));
            points.Reverse();
            points = SimplifyPath(points);
            if (points.Count < 2)
                return false;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                for (int card = 0; card < placements.Count; card++)
                {
                    if (SegmentIntersectsCard(points[i], points[i + 1], placements[card]))
                        return false;
                }
            }
            path = points;
            return true;

            void TryVisitRouteNode(int ix, int iy, int current)
            {
                if (ix < 0 || iy < 0 || ix >= width || iy >= height)
                    return;
                int next = iy * width + ix;
                if (visited[next])
                    return;
                var candidate = new FamilyTreePoint(xs[ix], ys[iy]);
                if (PointInsideAnyObstacle(candidate, obstacles))
                    return;
                var fromPoint = new FamilyTreePoint(xs[current % width], ys[current / width]);
                for (int i = 0; i < obstacles.Count; i++)
                {
                    RouteRect rect = obstacles[i];
                    if (SegmentIntersectsRect(fromPoint, candidate, rect.Left, rect.Top,
                            rect.Right, rect.Bottom))
                        return;
                }
                visited[next] = true;
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        private static bool PointInsideAnyObstacle(FamilyTreePoint point,
            List<RouteRect> obstacles)
        {
            const float inset = 0.5f;
            for (int i = 0; i < obstacles.Count; i++)
            {
                RouteRect rect = obstacles[i];
                if (point.X > rect.Left + inset && point.X < rect.Right - inset &&
                    point.Y > rect.Top + inset && point.Y < rect.Bottom - inset)
                    return true;
            }
            return false;
        }

        private static void AddCoordinate(List<float> values, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return;
            for (int i = 0; i < values.Count; i++)
                if (Math.Abs(values[i] - value) < 0.0001f)
                    return;
            values.Add(value);
        }

        private static int CoordinateIndex(List<float> values, float value)
        {
            for (int i = 0; i < values.Count; i++)
                if (Math.Abs(values[i] - value) < 0.0001f)
                    return i;
            return -1;
        }

        private static List<FamilyTreePoint> SimplifyPath(List<FamilyTreePoint> path)
        {
            List<FamilyTreePoint> result = RemoveDuplicatePoints(path);
            int index = 1;
            while (index + 1 < result.Count)
            {
                FamilyTreePoint previous = result[index - 1];
                FamilyTreePoint current = result[index];
                FamilyTreePoint next = result[index + 1];
                bool vertical = Math.Abs(previous.X - current.X) < 0.0001f &&
                                Math.Abs(current.X - next.X) < 0.0001f;
                bool horizontal = Math.Abs(previous.Y - current.Y) < 0.0001f &&
                                  Math.Abs(current.Y - next.Y) < 0.0001f;
                if (vertical || horizontal)
                    result.RemoveAt(index);
                else
                    index++;
            }
            return result;
        }

        private static List<FamilyTreePoint> SpouseDetour(FamilyTreeNodePlacement from,
            FamilyTreeNodePlacement to, FamilyTreePoint start, FamilyTreePoint end, float lane,
            bool fromIsLeft)
        {
            float stub = CardClearance / 2f;
            float fromLaneX = fromIsLeft
                ? from.Position.X + from.Size.Width + stub
                : from.Position.X - stub;
            float toLaneX = fromIsLeft
                ? to.Position.X - stub
                : to.Position.X + to.Size.Width + stub;
            return RemoveDuplicatePoints(new List<FamilyTreePoint>(6)
            {
                start,
                new FamilyTreePoint(fromLaneX, start.Y),
                new FamilyTreePoint(fromLaneX, lane),
                new FamilyTreePoint(toLaneX, lane),
                new FamilyTreePoint(toLaneX, end.Y),
                end
            });
        }

        private static List<FamilyTreePoint> RemoveDuplicatePoints(List<FamilyTreePoint> path)
        {
            var result = new List<FamilyTreePoint>(path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                if (result.Count == 0 ||
                    Math.Abs(result[result.Count - 1].X - path[i].X) > 0.0001f ||
                    Math.Abs(result[result.Count - 1].Y - path[i].Y) > 0.0001f)
                    result.Add(path[i]);
            }
            return result;
        }

        private static bool IntersectsAnyCard(IReadOnlyList<FamilyTreePoint> path,
            IReadOnlyList<FamilyTreeNodePlacement> placements, PersonId from, PersonId to)
        {
            return CountCardIntersections(path, placements, from, to) > 0;
        }

        private static int CountCardIntersections(IReadOnlyList<FamilyTreePoint> path,
            IReadOnlyList<FamilyTreeNodePlacement> placements, PersonId from, PersonId to)
        {
            if (placements == null || placements.Count == 0)
                return 0;
            int count = 0;
            for (int i = 0; i < placements.Count; i++)
            {
                FamilyTreeNodePlacement card = placements[i];
                if (card.PersonId == from || card.PersonId == to)
                    continue;
                for (int segment = 0; segment + 1 < path.Count; segment++)
                {
                    if (!SegmentIntersectsCard(path[segment], path[segment + 1], card))
                        continue;
                    count++;
                    break;
                }
            }
            return count;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Math.Abs(p) < 0.000001f)
                return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }

        private static FamilyTreePoint Center(FamilyTreeNodePlacement placement) =>
            new FamilyTreePoint(CenterX(placement), CenterY(placement));

        private static float CenterX(FamilyTreeNodePlacement placement) =>
            placement.Position.X + placement.Size.Width / 2f;

        private static float CenterY(FamilyTreeNodePlacement placement) =>
            placement.Position.Y + placement.Size.Height / 2f;

        private static float Top(FamilyTreeNodePlacement placement) => placement.Position.Y;

        private static float Bottom(FamilyTreeNodePlacement placement) =>
            placement.Position.Y + placement.Size.Height;
    }
}
