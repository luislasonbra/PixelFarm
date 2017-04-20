﻿//MIT, 2017, WinterDev
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Typography.Rendering
{

    public struct GlyphPointToBoneLink
    {
        public GlyphPoint glyphPoint;
        public Vector2 bonePoint;
    }


    public class GlyphBoneJoint
    {

        //A GlyphBoneJoint is on a midpoint of two 'inside' adjacent edges.
        //(2 contact edges)
        //of 2 triangles,      
        //(_p_contact_edge, _q_contact_edge)

        public EdgeLine _p_contact_edge;
        public EdgeLine _q_contact_edge;
        GlyphCentroidLine _owner;

#if DEBUG
        public readonly int dbugId = dbugTotalId++;
        public static int dbugTotalId;
#endif
        internal GlyphBoneJoint(GlyphCentroidLine owner,
            EdgeLine p_contact_edge,
            EdgeLine q_contact_edge)
        {
            this._p_contact_edge = p_contact_edge;
            this._q_contact_edge = q_contact_edge;
            this._owner = owner;
        }

        /// <summary>
        /// get position of this bone joint (mid point of the edge)
        /// </summary>
        /// <returns></returns>
        public Vector2 Position
        {
            get
            {
                //mid point of the edge line
                return _p_contact_edge.GetMidPoint();
            }
        }
        internal GlyphCentroidLine OwnerCentroidLine
        {
            get { return _owner; }
        }
        public float GetLeftMostRib()
        {
            //TODO: revisit this again

            return 0;
        }
        /// <summary>
        /// calculate distance^2 from contact point to specific point v
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double CalculateSqrDistance(Vector2 v)
        {

            Vector2 contactPoint = this.Position;
            float xdiff = contactPoint.X - v.X;
            float ydiff = contactPoint.Y - v.Y;

            return (xdiff * xdiff) + (ydiff * ydiff);
        }



        /// <summary>
        /// tip point (mid of tip edge)
        /// </summary>
        Vector2 _tipPoint;

        //one bone joint can have up to 2 tips 

        EdgeLine _selectedTipEdge;
        public List<GlyphBone> _assocBones;
        public List<GlyphPoint> _assocGlyphPoints;


        public void SetTipEdge(EdgeLine tipEdge)
        {
            this._selectedTipEdge = tipEdge;
            this._tipPoint = tipEdge.GetMidPoint();
        } 
        public Vector2 TipPoint { get { return _tipPoint; } } 
        public EdgeLine TipEdge { get { return _selectedTipEdge; } }

        public void AddAssociatedGlyphPoint(GlyphPoint glyphPoint)
        {
            if (_assocGlyphPoints == null) { _assocGlyphPoints = new List<GlyphPoint>(); }
            _assocGlyphPoints.Add(glyphPoint);
        }
        public void AddAssociatedBone(GlyphBone bone)
        {
            if (_assocBones == null) { _assocBones = new List<GlyphBone>(); }
            _assocBones.Add(bone);
        }

#if DEBUG
        public override string ToString()
        {
            return "id:" + dbugId + " " + this.Position.ToString();
        }
#endif

    }


    /// <summary>
    /// link between 2 GlyphBoneJoint or Joint and tipEdge
    /// </summary>
    public class GlyphBone
    {
        public readonly EdgeLine TipEdge;
        public readonly GlyphBoneJoint JointA;
        public readonly GlyphBoneJoint JointB;

        double _len;

        public Vector2 cutPoint_onEdge;
        public bool hasCutPointOnEdge;

        public GlyphBone(GlyphBoneJoint a, GlyphBoneJoint b)
        {
#if DEBUG
            if (a == b)
            {
                throw new NotSupportedException();
            }
#endif
            JointA = a;
            JointB = b;


            Vector2 bpos = b.Position;
            _len = Math.Sqrt(a.CalculateSqrDistance(bpos));
            EvaluteSlope(a.Position, bpos);
            //------  

            //for analysis in later step
            a.AddAssociatedBone(this);
            b.AddAssociatedBone(this);
            //------  

            //find common triangle between  2 joint
            GlyphTriangle commonTri = FindCommonTriangle(a, b);
            if (commonTri != null)
            {
                //found common triangle 
                EdgeLine outsideEdge = GetFirstFoundOutsidEdge(commonTri);
                if (outsideEdge != null)
                {
                    hasCutPointOnEdge = MyMath.FindPerpendicularCutPoint(outsideEdge, GetMidPoint(), out cutPoint_onEdge);
                }
            }
            else
            {
                //not found?=>
            }
        }

        public GlyphBone(GlyphBoneJoint a, EdgeLine tipEdge)
        {
            JointA = a;
            TipEdge = tipEdge;

            var midPoint = tipEdge.GetMidPoint();
            _len = Math.Sqrt(a.CalculateSqrDistance(midPoint));
            EvaluteSlope(a.Position, midPoint);
            //------
            //for analysis in later step
            a.AddAssociatedBone(this);

            //tip bone, no common triangle
            //
            EdgeLine outsideEdge = FindOutsideEdge(a, tipEdge);
            if (outsideEdge != null)
            {
                hasCutPointOnEdge = MyMath.FindPerpendicularCutPoint(outsideEdge, GetMidPoint(), out cutPoint_onEdge);
            }
        }
        static EdgeLine FindOutsideEdge(GlyphBoneJoint a, EdgeLine tipEdge)
        {
            GlyphCentroidLine ownerCentroid_A = a.OwnerCentroidLine;
            if (ContainsEdge(ownerCentroid_A.p, tipEdge))
            {
                return FindAnotherOutsideEdge(ownerCentroid_A.p, tipEdge);
            }
            else if (ContainsEdge(ownerCentroid_A.q, tipEdge))
            {
                return FindAnotherOutsideEdge(ownerCentroid_A.q, tipEdge);
            }
            return null;
        }
        static EdgeLine FindAnotherOutsideEdge(GlyphTriangle tri, EdgeLine knownOutsideEdge)
        {
            if (tri.e0.IsOutside && tri.e0 != knownOutsideEdge) { return tri.e0; }
            if (tri.e1.IsOutside && tri.e1 != knownOutsideEdge) { return tri.e1; }
            if (tri.e2.IsOutside && tri.e2 != knownOutsideEdge) { return tri.e2; }
            return null;
        }
        static bool ContainsEdge(GlyphTriangle tri, EdgeLine edge)
        {
            return tri.e0 == edge || tri.e1 == edge || tri.e2 == edge;
        }
        static GlyphTriangle FindCommonTriangle(GlyphBoneJoint a, GlyphBoneJoint b)
        {
            GlyphCentroidLine ownerCentroid_A = a.OwnerCentroidLine;
            GlyphCentroidLine ownerCentroid_B = b.OwnerCentroidLine;
            if (ownerCentroid_A.p == ownerCentroid_B.p || ownerCentroid_A.p == ownerCentroid_B.q)
            {
                return ownerCentroid_A.p;
            }
            else if (ownerCentroid_A.q == ownerCentroid_B.p || ownerCentroid_A.q == ownerCentroid_B.q)
            {
                return ownerCentroid_A.q;
            }
            else
            {
                return null;
            }
        }
        static EdgeLine GetFirstFoundOutsidEdge(GlyphTriangle tri)
        {
            if (tri.e0.IsOutside) { return tri.e0; }
            if (tri.e1.IsOutside) { return tri.e1; }
            if (tri.e2.IsOutside) { return tri.e2; }
            return null; //not found               
        }
        void EvaluteSlope(Vector2 p, Vector2 q)
        {

            double x0 = p.X;
            double y0 = p.Y;
            //q
            double x1 = q.X;
            double y1 = q.Y;

            if (x1 == x0)
            {
                this.SlopeKind = LineSlopeKind.Vertical;
                SlopeAngle = 1;
            }
            else
            {
                SlopeAngle = Math.Abs(Math.Atan2(Math.Abs(y1 - y0), Math.Abs(x1 - x0)));
                if (SlopeAngle > MyMath._85degreeToRad)
                {
                    SlopeKind = LineSlopeKind.Vertical;
                }
                else if (SlopeAngle < MyMath._03degreeToRad) //_15degreeToRad
                {
                    SlopeKind = LineSlopeKind.Horizontal;
                }
                else
                {
                    SlopeKind = LineSlopeKind.Other;
                }
            }
        }
        internal double SlopeAngle { get; set; }
        public LineSlopeKind SlopeKind { get; set; }
        internal double Length
        {
            get
            {
                return _len;
            }
        }
        public bool IsLongBone { get; internal set; }

        //--------
        public float LeftMostPoint()
        {
            if (JointB != null)
            {
                //compare joint A and B 
                if (JointA.Position.X < JointB.Position.X)
                {
                    return JointA.GetLeftMostRib();
                }
                else
                {
                    return JointB.GetLeftMostRib();
                }
            }
            else
            {
                return JointA.GetLeftMostRib();
            }
        }

        public Vector2 GetMidPoint()
        {
            if (JointB != null)
            {
                return (JointA.Position + JointB.Position) / 2;
            }
            else if (TipEdge != null)
            {
                Vector2 edge = TipEdge.GetMidPoint();
                return (edge + JointA.Position) / 2;
            }
            else
            {
                return Vector2.Zero;
            }
        }

        public Vector2 GetBoneVector()
        {
            if (JointB != null)
            {
                return JointB.Position - JointA.Position;
            }
            else if (TipEdge != null)
            {
                return TipEdge.GetMidPoint() - JointA.Position;
            }
            else
            {
                return Vector2.Zero;
            }
        }
        public List<GlyphPointToBoneLink> _perpendiculatPoints;
        public void AddPerpendicularPoint(GlyphPoint p, Vector2 bonePoint)
        {
            //add a perpendicular glyph point to bones
            if (_perpendiculatPoints == null) { _perpendiculatPoints = new List<GlyphPointToBoneLink>(); }
            GlyphPointToBoneLink pointToBoneLink = new GlyphPointToBoneLink();
            pointToBoneLink.bonePoint = bonePoint;
            pointToBoneLink.glyphPoint = p;
            _perpendiculatPoints.Add(pointToBoneLink);
        }




#if DEBUG
        public override string ToString()
        {
            if (TipEdge != null)
            {
                return JointA.ToString() + "->" + TipEdge.GetMidPoint().ToString();
            }
            else
            {
                return JointA.ToString() + "->" + JointB.ToString();
            }
        }
#endif
    }
}