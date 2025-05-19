using UnityEngine;

public static class GeometryUtils
{
    
    public static Vector3 TripleProd(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(Vector3.Cross(a, b), c);
    }
    
    
    public static bool Case2D(SupportFunction2D a,SupportFunction2D b,int iterStep = 20)
    {
        Vector2 centroidA = a.centroid;
        Vector2 centroidB = b.centroid;

        //初始的查询向量用两个质心的连线向量
        Vector2 initialNormal = (centroidA - centroidB).normalized;
        
        return GJK_2D(a, b, initialNormal, out Simplex2D finalSimplex2D,iterStep);
        
        
    }
    
    private static bool GJK_2D<TA,TB>(TA a,TB b, Vector2 normal , out Simplex2D finalSimplex2D,int iterStep)
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
                finalSimplex2D = simplex2D;
                return false;
            }

            bool hasZero = CheckSimplex2D(ref simplex2D, ref normal);
            if (hasZero)
            {
                finalSimplex2D = simplex2D;
                return true;
            }
            
            if(i == iterStep - 1)
            {
            }
        }

        return false;
    }

    private static bool CheckSimplex2D(ref Simplex2D simplex2D, ref Vector2 normal)
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
    
    
    public static Vector2 EPA2D(Simplex2D simplex, SupportFunction2D shapeA, SupportFunction2D shapeB, int maxIterations = 20, float tolerance = 0.0001f)
    {
        // simplex 参数是 GJK 算法结束时包含原点的单纯形（通常是三角形）
        // 确保 simplex.curStatus 设置为 CollisionStatus.EPA; (或者在调用EPA前设置)
        simplex.curStatus = CollisionStatus.EPA;
        for (int iter = 0; iter < maxIterations; iter++)
        {
            // 1. 找到当前单纯形上离原点最近的边，以及原点到该边所在直线的投影点 (minQ) 和距离
            float distToOrigin = GetClosestEdgeToOrigin(simplex, out int index1, out int index2, out Vector2 minQ);
    
            // minQ 是从原点指向最近边的法向量 (penetration normal candidate)
            // distToOrigin 是它的长度
    
            if (distToOrigin < float.Epsilon) // 如果距离非常接近0，可能出现问题或已接触
            {
                // 这意味着 minQ 非常接近 (0,0)。
                // 可能是GJK的单纯形某个顶点/边恰好在原点。
                // 如果 simplex 是一个有效的多边形且包含原点，其边到原点的距离应 > 0。
                // 如果发生这种情况，可能需要返回一个零向量或一个微小的默认穿透。
                Debug.LogWarning("EPA: minQ is very close to origin. Potential issue.");
                return minQ; // 或者 Vector2.zero
            }
    
            Vector2 searchDirection = minQ.normalized; // EPA扩展的方向应该是归一化的
    
            // 2. 在此方向上找到闵可夫斯基差的最远点
            Vector2 pA = shapeA.Support(searchDirection);
            Vector2 pB = shapeB.Support(-searchDirection);
            Vector2 newSupportPoint = pA - pB;
    
            // 3. 检查收敛性：新支撑点在 searchDirection 上的投影与当前最近距离比较
            float projection = Vector2.Dot(newSupportPoint, searchDirection);
    
            // 如果新点在法线方向的投影“深入”程度与当前最近距离几乎相同，则收敛
            if (projection - distToOrigin < tolerance)
            {
                // 收敛，minQ (其长度为 distToOrigin) 就是穿透向量
                Debug.Log("EPA converged in " + iter + " iterations. Penetration vector: " + minQ);
                Gizmos.DrawLine(Vector2.zero, minQ); // 在 OnDrawGizmos 中绘制
                return minQ;
            }
    
            // 4. 未收敛，将新支撑点插入到单纯形中以扩展它
            // 插入到构成最近边的两个顶点之间，保持顶点顺序 (通常是逆时针)
            // GetClosestEdgeToOrigin返回的 index1 是 preIndex, index2 是 curIndex
            // 我们应该插入在 index2 的位置，以替换掉原来的 index2，并将后续元素后移
            simplex.AppendPointEPA(index2, newSupportPoint);
        }
    
        //达到最大迭代次数仍未收敛
        Debug.LogWarning("EPA: Reached max iterations ("+ maxIterations +"). Returning current best estimate.");
        // 尝试返回当前最好的minQ，尽管可能不完全精确
        GetClosestEdgeToOrigin(simplex, out _, out _, out Vector2 finalMinQ); // 获取最后一次的minQ
        return finalMinQ;
    }

    public static float GetClosestEdgeToOrigin(Simplex2D simplex2D, out int index1, out int index2,out Vector2 minQ)
    {
        int preIndex = simplex2D.nodes.Count - 1;
        float minDist = float.MaxValue;
        int minIndex1 = 0;
        int minIndex2 = 0;
        minQ = Vector2.zero;
        for (int curIndex = 0; curIndex < simplex2D.nodes.Count; curIndex++)
        {
            Vector2 v = simplex2D.nodes[curIndex] - simplex2D.nodes[preIndex];
            Vector2 w = Vector2.zero - simplex2D.nodes[preIndex];
            float t = Vector2.Dot(v, w) / Vector2.Dot(v, v);
            Vector2 q = simplex2D.nodes[preIndex] + v * t;
            
            float dist = Vector2.Distance(Vector2.zero, q);
            Debug.Log("index1: "+preIndex+" index2: "+curIndex+" dist: "+dist);
            Debug.Log("pos1: "+simplex2D.nodes[preIndex].ToString()+" pos2: "+simplex2D.nodes[curIndex].ToString());
            if (dist <= minDist)
            {
                minDist = dist;
                minIndex1 = preIndex;
                minIndex2 = curIndex;
                minQ = q;
            }
            preIndex = curIndex;
        }

        index1 = minIndex1;
        index2 = minIndex2;
        return minDist;
    }

}
