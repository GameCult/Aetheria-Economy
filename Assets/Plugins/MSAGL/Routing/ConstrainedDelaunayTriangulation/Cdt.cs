/*
Following "Sweep-line algorithm for constrained Delaunay triangulation", by Domiter and Zalik
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;

namespace Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation {
    ///<summary>
    ///triangulates the space between point, line segment and polygons in the Delaunay fashion
    ///</summary>
    public class Cdt : AlgorithmBase {
         readonly IEnumerable<Tuple<Point, object>> isolatedSitesWithObject; 
        readonly IEnumerable<Point> isolatedSites;
        readonly IEnumerable<Polyline> obstacles;
        readonly List<SymmetricSegment> isolatedSegments;
        CdtSite P1;
        CdtSite P2;
        CdtSweeper sweeper;
        internal readonly Dictionary<Point, CdtSite> PointsToSites = new Dictionary<Point, CdtSite>();
        List<CdtSite> allInputSites;

        ///<summary>
        ///constructor
        ///</summary>
        ///<param name="isolatedSites"></param>
        ///<param name="obstacles"></param>
        ///<param name="isolatedSegments"></param>
        public Cdt(IEnumerable<Point> isolatedSites, IEnumerable<Polyline> obstacles, List<SymmetricSegment> isolatedSegments) {
            this.isolatedSites = isolatedSites;
            this.obstacles = obstacles;
            this.isolatedSegments = isolatedSegments;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="isolatedSites"></param>
        public Cdt(IEnumerable<Tuple<Point, object>> isolatedSites) {
            isolatedSitesWithObject = isolatedSites;
        }

        void FillAllInputSites() {
            //for now suppose that the data is correct: no isolatedSites coincide with obstacles or isolatedSegments, obstacles are mutually disjoint, etc

            if (isolatedSitesWithObject != null)
                foreach (var tuple in isolatedSitesWithObject)
                    AddSite(tuple.Item1, tuple.Item2);

            if (isolatedSites != null)
                foreach (var isolatedSite in isolatedSites)
                    AddSite(isolatedSite, null);

            if (obstacles != null)
                foreach (var poly in obstacles)
                    AddPolylineToAllInputSites(poly);

            if (isolatedSegments != null)
                foreach (var isolatedSegment in isolatedSegments)
                    AddConstrainedEdge(isolatedSegment.A, isolatedSegment.B, null);

            AddP1AndP2();

            allInputSites = new List<CdtSite>(PointsToSites.Values);
        }

        CdtSite AddSite(Point point, object relatedObject) {
            CdtSite site;
            if (PointsToSites.TryGetValue(point, out site)) {
                site.Owner = relatedObject;//set the owner anyway
                return site;
            }
            PointsToSites[point] = site = new CdtSite(point) { Owner = relatedObject };
            return site;
        }

        void AddP1AndP2() {
            var box = Rectangle.CreateAnEmptyBox();
            foreach (var site in PointsToSites.Keys)
                box.Add(site);

            var delx = box.Width / 3;
            var dely = box.Height / 3;
            P1 = new CdtSite(box.LeftBottom + new Point(-delx, -dely));
            P2 = new CdtSite(box.RightBottom + new Point(delx, -dely));
        }

        void AddPolylineToAllInputSites(Polyline poly) {
            for (var pp = poly.StartPoint; pp.Next != null; pp = pp.Next)
                AddConstrainedEdge(pp.Point, pp.Next.Point, poly);
            if (poly.Closed)
                AddConstrainedEdge(poly.EndPoint.Point, poly.StartPoint.Point, poly);
        }


        void AddConstrainedEdge(Point a, Point b, Polyline poly) {
            var ab = Above(a, b);
            Debug.Assert(ab != 0);
            CdtSite upperPoint;
            CdtSite lowerPoint;
            if (ab > 0) {//a is above b
                upperPoint = AddSite(a, poly);
                lowerPoint = AddSite(b, poly);
            }
            else {
                Debug.Assert(ab < 0);
                upperPoint = AddSite(b, poly);
                lowerPoint = AddSite(a, poly);
            }
            var edge = CreateEdgeOnOrderedCouple(upperPoint, lowerPoint);
            edge.Constrained = true;
        }


        static internal CdtEdge GetOrCreateEdge(CdtSite a, CdtSite b) {
            if (Above(a.Point, b.Point) == 1) {
                var e = a.EdgeBetweenUpperSiteAndLowerSite(b);
                if (e != null)
                    return e;
                return CreateEdgeOnOrderedCouple(a, b);
            }
            else {
                var e = b.EdgeBetweenUpperSiteAndLowerSite(a);
                if (e != null)
                    return e;
                return CreateEdgeOnOrderedCouple(b, a);
            }
        }

        static CdtEdge CreateEdgeOnOrderedCouple(CdtSite upperPoint, CdtSite lowerPoint) {
            Debug.Assert(Above(upperPoint.Point, lowerPoint.Point) == 1);
            return new CdtEdge(upperPoint, lowerPoint);
        }


        ///<summary>
        ///</summary>
        ///<returns></returns>
        public Set<CdtTriangle> GetTriangles() {
            return sweeper.Triangles;
        }

        /// <summary>
        /// Executes the actual algorithm.
        /// </summary>
        protected override void RunInternal() {
            Initialization();
            SweepAndFinalize();
        }


        void SweepAndFinalize() {
            sweeper = new CdtSweeper(allInputSites, P1, P2, GetOrCreateEdge);
            sweeper.Run();
        }


        void Initialization() {
            FillAllInputSites();
            allInputSites.Sort(OnComparison);
        }

        static int OnComparison(CdtSite a, CdtSite b) {
            return Above(a.Point, b.Point);
        }
        /// <summary>
        /// compare first y then -x coordinates
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>1 if a is above b, 0 if points are the same and -1 if a is below b</returns>
        static public int Above(Point a, Point b) {
            var del = a.Y - b.Y;
            if (del > 0)
                return 1;
            if (del < 0)
                return -1;
            del = a.X - b.X;
            return del > 0 ? -1 : (del < 0 ? 1 : 0); //for a horizontal edge the point with the smaller X is the upper point
        }

        /// <summary>
        /// compare first y then -x coordinates
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>1 if a is above b, 0 if points are the same and -1 if a is below b</returns>
        static internal int Above(CdtSite a, CdtSite b) {
            var del = a.Point.Y - b.Point.Y;
            if (del > 0)
                return 1;
            if (del < 0)
                return -1;
            del = a.Point.X - b.Point.X;
            return del > 0 ? -1 : (del < 0 ? 1 : 0); //for a horizontal edge the point with the smaller X is the upper point
        }

        internal void RestoreEdgeCapacities() {
            foreach (var site in allInputSites)
                foreach (var e in site.Edges)
                    if (!e.Constrained) //do not care of constrained edges
                        e.ResidualCapacity = e.Capacity;
        }

        ///<summary>
        ///</summary>
        public void SetInEdges() {
            foreach (var site in PointsToSites.Values) {
                var edges = site.Edges;
                for (int i = edges.Count - 1; i >= 0; i--) {
                    var e = edges[i];
                    var oSite = e.lowerSite;
                    Debug.Assert(oSite != site);
                    oSite.AddInEdge(e);
                }
            }
        }

        ///<summary>
        ///</summary>
        ///<param name="point"></param>
        ///<returns></returns>
        public CdtSite FindSite(Point point) {
            return PointsToSites[point];
        }

        //        /// <summary>
        //        /// returns CdtEdges crossed by the segment a.Point, b.Point
        //        /// </summary>
        //        /// <param name="prevA">if prevA is not a null, that means the path is passing through prevA and might need to include
        //        /// the edge containing a.Point if such exists</param>
        //        /// <param name="a"></param>
        //        /// <param name="b"></param>
        //        /// <returns></returns>
        //        internal IEnumerable<CdtEdge> GetCdtEdgesCrossedBySegment(PolylinePoint prevA, PolylinePoint a, PolylinePoint b) {
        //            count++;
        //            if (dd) {
        //                var l = new List<DebugCurve> {
        //                                                 new DebugCurve("red", new Ellipse(5, 5, a.Point)),
        //                                                 new DebugCurve("blue", new Ellipse(5, 5, b.Point)),
        //                                                 new DebugCurve("blue", new LineSegment(a.Point, b.Point))
        //                                             };
        //
        //                l.AddRange(
        //                    GetTriangles().Select(
        //                        tr => new DebugCurve(100, 1, "green", new Polyline(tr.Sites.Select(v => v.Point)) {Closed = true})));
        //                LayoutAlgorithmSettings.ShowDebugCurvesEnumeration(l);
        //
        //            }
        //            var ret = new List<CdtEdge>();
        //            CdtEdge piercedEdge;
        //            CdtTriangle t = GetFirstTriangleAndPiercedEdge(a, b, out piercedEdge);
        //            if (ProperCrossing(a, b, piercedEdge))
        //                ret.Add(piercedEdge);
        //
        //            ret.AddRange(ContinueThreadingThroughTriangles(a,b,t, piercedEdge));
        //
        //            return ret;
        //        }

        /*
                static bool ProperCrossing(Point a, Point b, CdtEdge cdtEdge) {
                    return cdtEdge != null && cdtEdge.upperSite.Owner != cdtEdge.lowerSite.Owner &&
                           CrossEdgeInterior(cdtEdge, a, b);
                }
        */
        //        static int count;
        //        static bool db { get { return count == 125; }}

        /*
                static CdtEdge GetPiercedEdge(Point a, Point b, CdtTriangle triangle) {
                    Debug.Assert(!triangle.Sites.Any(s=>ApproximateComparer.Close(a, s.Point)));
                    var a0 = Point.GetTriangleOrientation(a, triangle.Sites[0].Point, triangle.Sites[1].Point);
                    if (a0 == TriangleOrientation.Clockwise)return null;

                    var a1 = Point.GetTriangleOrientation(a,triangle.Sites[1].Point, triangle.Sites[2].Point);
                    if (a1 == TriangleOrientation.Clockwise)return null;

                    var a2 = Point.GetTriangleOrientation(a,triangle.Sites[2].Point, triangle.Sites[3].Point);
                    if (a2 == TriangleOrientation.Clockwise)return null;
      
                    if (a0 == TriangleOrientation.Counterclockwise &&
                        Point.GetTriangleOrientation(b, triangle.Sites[0].Point, triangle.Sites[1].Point) == TriangleOrientation.Clockwise)
                        return triangle.Edges[0];
                    if (a1 == TriangleOrientation.Counterclockwise &&
                        Point.GetTriangleOrientation(b, triangle.Sites[1].Point, triangle.Sites[2].Point) == TriangleOrientation.Clockwise)
                        return triangle.Edges[1];
                    if (a2 == TriangleOrientation.Counterclockwise &&
                        Point.GetTriangleOrientation(b, triangle.Sites[2].Point, triangle.Sites[3].Point) == TriangleOrientation.Clockwise)
                        return triangle.Edges[2];

                    return null;
                }
        */

        /*
                static bool CheckIntersectionStartingFromSide(int i, CdtTriangle triangle, Point a, Point b) {
                    var edgeDir = triangle.Sites[i + 1].Point - triangle.Sites[i].Point;
                    var edgePerp = edgeDir.Rotate90Ccw();
                    return ((b - a)*edgePerp)*((triangle.Sites[i + 2].Point - a)*edgePerp) > 0;
                }
        */

        internal static bool PointIsInsideOfTriangle(Point point, CdtTriangle t) {
            for (int i = 0; i < 3; i++) {
                var a = t.Sites[i].Point;
                var b = t.Sites[i + 1].Point;
                if (Point.SignedDoubledTriangleArea(point, a, b) < -ApproximateComparer.DistanceEpsilon)
                    return false;
            }
            return true;
        }

        RectangleNode<CdtTriangle> cdtTree = null;

        internal RectangleNode<CdtTriangle> GetCdtTree() {
            if (cdtTree == null) {
                cdtTree = RectangleNode<CdtTriangle>.CreateRectangleNodeOnEnumeration(GetTriangles().Select(t => new RectangleNode<CdtTriangle>(t, t.BoundingBox())));
            }

            return cdtTree;
        }
    }
}
