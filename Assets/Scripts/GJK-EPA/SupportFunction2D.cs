
using System;
using UnityEngine;
[Serializable]
public abstract class SupportFunction2D
{
     public virtual Vector2 centroid
     {
          get { return Vector2.zero; }
          private set { ; }
     }
     [HideInInspector] public abstract Vector2 Support(Vector2 normal);
     
}
