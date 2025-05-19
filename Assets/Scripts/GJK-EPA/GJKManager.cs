using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class GJKManager : MonoBehaviour
{
    public Color colorA;
    public Color colorB;
    public bool drawShape;
    [Range(0, 20)]
    public int iterStep;
    [Header("2D Case")]
    [SerializeField]
    private Triangle2D triangleA;
    [SerializeField]
    private Triangle2D triangleB;
    [SerializeField]
    private Quad2D quadA;
    [SerializeField]
    private Quad2D quadB;
    public bool drawSimplex2D;
    
    private void OnDrawGizmos()
    {
        //Debug.Log(GeometryUtils.Case2D(quadA, quadB, iterStep));//结果正确
        Case2D(triangleA, triangleB);
    }

    private void Case2D(SupportFunction2D a, SupportFunction2D b)
    {
        if (a is Triangle2D && b is Triangle2D)
        {
            CaseTriangle2D(a as Triangle2D, b as Triangle2D);
        }
    }

    private void CaseTriangle2D(Triangle2D triangleA,Triangle2D triangleB)
    {
        Vector2 centroidA = triangleA.centroid;
        Vector2 centroidB = triangleB.centroid;

        //初始的查询向量用两个质心的连线向量
        Vector2 initialNormal = (centroidA - centroidB).normalized;

        GizmosDrawer simplexDrawer = new GizmosDrawer(drawSimplex2D);
        bool isCollide = GJK_2D(triangleA, triangleB, initialNormal, out Simplex2D finalSimplex2D, simplexDrawer);
        
        simplexDrawer.DrawTriangle2D(triangleA, isCollide ? Color.red : colorA, true);
        simplexDrawer.DrawTriangle2D(triangleB, isCollide ? Color.blue : colorB, true);

        simplexDrawer.DrawPoint(centroidA, colorA, 0.02f);
        simplexDrawer.DrawPoint(centroidB, colorB, 0.02f);

        DrawShapes(triangleA, triangleB, simplexDrawer);
        
        
    }
    
    private bool GJK_2D<TA,TB>(TA a,TB b, Vector2 normal , out Simplex2D finalSimplex2D , GizmosDrawer drawer)
    where TA : SupportFunction2D where TB : SupportFunction2D
    {
        //初始化状态,找到第一个点
        //首先获取两个凸型的支持点
        //MaX(Support(A - B, d)) = Support(A, d) - Support(B, -d)
        Vector2 pointOnA = a.Support(normal);
        Vector2 pointOnB = b.Support(-normal);
        //计算闵差
        Vector2 mSub = pointOnA - pointOnB;
        Simplex2D simplex2D = new Simplex2D();
        simplex2D.AppendPoint(mSub);
        normal = Vector2.zero - mSub;
        finalSimplex2D = null;
        for (int i = 0; i < iterStep; ++i)
        {
            pointOnA = a.Support(normal);
            pointOnB = b.Support(-normal);
            mSub = pointOnA - pointOnB;
            simplex2D.AppendPoint(mSub);
            
            //大于零说明,新的计算出的单纯型的极点，它和原来的那个单纯性极点，分别在原来的normal的垂线的两侧
            //这个点不在原点与法线构成的正空间
            if (Vector2.Dot(mSub, normal) < 0)
            {
                drawer.DrawSimplex2D(simplex2D,Color.yellow);
                finalSimplex2D = simplex2D;
                return false;
            }

            bool hasZero = CheckSimplex2D(ref simplex2D, ref normal);
            if (hasZero)
            {
                drawer.DrawSimplex2D(simplex2D, Color.magenta);
                finalSimplex2D = simplex2D;
                
                GeometryUtils.EPA2D(simplex2D,a,b);
                return true;
            }
            
            if(i == iterStep - 1)
            {
                drawer.DrawSimplex2D(simplex2D, Color.yellow);
            }
        }

        return false;
    }

    private bool CheckSimplex2D(ref Simplex2D simplex2D, ref Vector2 normal)
    {
        if (simplex2D.type == Simplex2DType.Line)
        {
            Vector2 ab = simplex2D.b - simplex2D.newPoint;
            Vector2 ao = Vector2.zero - simplex2D.newPoint;
            normal = GeometryUtils.TripleProd(ab,ao,ab);
            return false;
        }
        else if (simplex2D.type == Simplex2DType.Triangle)
        {
            // //判断原点是否在三角形内部
            // Vector2 ab =  simplex2D.newPoint - simplex2D.b;
            // Vector2 ao =  simplex2D.newPoint - Vector2.zero;
            // Vector2 bc =  simplex2D.b - simplex2D.c;
            // Vector2 bo =  simplex2D.b - Vector2.zero;
            // Vector2 ca =  simplex2D.c - simplex2D.newPoint;
            // Vector2 co =  simplex2D.c - Vector2.zero;
            //
            // float aCheck = Vector2.Dot(ab, ao);
            // float bCheck = Vector2.Dot(bc, bo);
            // float cCheck = Vector2.Dot(ca, co);
            //
            // bool originInTriangle = (aCheck >= 0 && bCheck >= 0 && cCheck >= 0) || (aCheck <= 0 && bCheck <= 0 && cCheck <= 0);
            // if (originInTriangle)
            // {
            //     return true;
            // }
            // //如果在外部，那么就判断离哪条边比较近
            // else
            // {
            //     float db = GetPoint2EdgeDistance(Vector2.zero,simplex2D.newPoint,simplex2D.b,out Vector2 Q1);
            //     float dc = GetPoint2EdgeDistance(Vector2.zero, simplex2D.newPoint, simplex2D.c, out Vector2 Q2);
            //     if (db < dc)
            //     {
            //         simplex2D.RemoveC();
            //         normal = Vector2.zero - Q1;
            //         return false;
            //     }
            //     else
            //     {
            //         simplex2D.RemoveB();
            //         normal = Vector2.zero - Q2;
            //         return false;
            //     }
            //     
            // }
            
            //单纯形是一个三角形，判断原点落在哪一个区域。
            var ab = simplex2D.b - simplex2D.newPoint;
            var ac = simplex2D.c - simplex2D.newPoint;
            var ao = Vector2.zero -simplex2D.newPoint;
            var normal_ab = GeometryUtils.TripleProd(ac, ab, ab);
            var normal_ac =  GeometryUtils.TripleProd(ab, ac, ac);

            if (Vector2.Dot(normal_ab, ao) > 0) //region ab
            {
                simplex2D.RemoveC();
                normal = normal_ab;
                return false;
            }
            else if (Vector2.Dot(normal_ac, ao) > 0) //region ac
            {
                simplex2D.RemoveB();
                normal = normal_ac;
                return false;
            }
            return true;
        }
        return false;
    }

    private float GetPoint2EdgeDistance(Vector2 point, Vector2 vec1, Vector2 vec2 ,out Vector2 Q)
    {
        Vector2 v1p = vec1 - point;
        Vector2 v1v2 = vec1 - vec2;
        float t = Vector2.Dot(v1p, v1v2);
        if (t < 0)
        {
            Q = vec1;
            return 0;
        }
        else if (t > 1)
        {
            Q = vec2;
            return 0;
        }
        Q = vec1 + v1v2 * t;
        return Vector2.Distance(point, Q);
    }
    private void DrawShapes<TA, TB>(TA a, TB b, GizmosDrawer? drawer)
        where TA : SupportFunction2D
        where TB : SupportFunction2D
    {
        var subs = new List<Vector2>();
        for (int i = 0; i < 12; i++)
        {
            var rot = Quaternion.Euler(0, 0, 30.0f * i);
            var normal = rot * Vector2.up;
            var supportA = a.Support(normal);
            var supportB = b.Support(-normal);
            var sub = supportA - supportB;
            subs.Add(sub);
        }

        for (var i = 0; i + 1 < subs.Count; i++)
        {
            drawer?.DrawDottedLine(subs[i], subs[i + 1], Color.black);
        }
        drawer?.DrawDottedLine(subs[0], subs[subs.Count - 1], Color.black);
        drawer?.DrawPoint(Vector2.zero, Color.green,radius:0.1f,label:"Origin");
    } 
}
