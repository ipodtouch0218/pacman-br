#if QUANTUM_ENABLE_MIGRATION

using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

[Serializable]
[Obsolete]
public struct MapNavMeshTriangle {
  public String   Id;
  public String[] VertexIds;
  public Int32[]  VertexIds2;
  public Int32    Area;
  public String   RegionId;
  public FP       Cost;
}

[Serializable]
[Obsolete]
public struct MapNavMeshVertex {
  public String      Id;
  public Vector3     Position;
  public List<Int32> Neighbors;
  public List<Int32> Triangles;
}

[Serializable]
[Obsolete]
public struct MapNavMeshVertexFP {
  public String      Id;
  public FPVector3   Position;
  public List<Int32> Neighbors;
  public List<Int32> Triangles;
}

[Serializable]
[Obsolete]
public struct MapNavMeshLink {
  public FPVector3 StartFP;
  public FPVector3 EndFP;
  public int       StartTriangle;
  public int       EndTriangle;
  public bool      Bidirectional;
  public FP        CostOverride;
  public String    RegionId;
  public String    Name;

  [Obsolete]
  public Vector3 Start {
    set { StartFP = value.ToFPVector3(); }
    get { return StartFP.ToUnityVector3(); }
  }

  [Obsolete]
  public Vector3 End {
    set { EndFP = value.ToFPVector3(); }
    get { return EndFP.ToUnityVector3(); }
  }
}

[Obsolete("Use " + nameof(QuantumNavMesh) + " instead")]
[LastSupportedVersion("3.0")]
public abstract class MapNavMesh : QuantumNavMesh {
  [Obsolete]
  public static NavMeshBakeDataFindClosestTriangle Convert(FindClosestTriangleCalculation value) => (NavMeshBakeDataFindClosestTriangle)value;
  
  [Obsolete]
  public static NavMeshBakeData Convert(BakeData value) {
    return new NavMeshBakeData {
      AgentRadius = value.AgentRadius,
      ClosestTriangleCalculation = Convert(value.ClosestTriangleCalculation),
      ClosestTriangleCalculationDepth = value.ClosestTriangleCalculationDepth,
      EnableQuantum_XY = value.EnableQuantum_XY,
      Name = value.Name,
      Position = value.PositionFP,
      Regions = value.Regions.ToArray(),
      Links = null,
      Triangles = null,
      Vertices = null
    };
  }

  [Obsolete]
  public static NavMeshBakeDataLink Convert(MapNavMeshLink value) {
    return new NavMeshBakeDataLink() {
      Start = value.StartFP,
      End = value.EndFP,
      StartTriangle = value.StartTriangle,
      EndTriangle = value.EndTriangle,
      Bidirectional = value.Bidirectional,
      CostOverride = value.CostOverride,
      RegionId = value.RegionId,
      Name = value.Name
    };
  }

  [Obsolete]
  public static NavMeshBakeDataVertex Convert(MapNavMeshVertexFP value) {
    return new NavMeshBakeDataVertex {
      Position = value.Position
    };
  }

  [Obsolete]
  public static NavMeshBakeDataVertex Convert(MapNavMeshVertex value) {
    return new NavMeshBakeDataVertex {
      Position = value.Position.ToFPVector3()
    };
  }

  [Obsolete]
  public static NavMeshBakeDataTriangle Convert(MapNavMeshTriangle value) {
    return new NavMeshBakeDataTriangle {
      Cost = value.Cost,
      RegionId = value.RegionId,
      VertexIds = value.VertexIds2
    };
  }


  [Serializable]
  [Obsolete("Use Quantum.NavMeshBakeDataFindClosestTriangle")]
  public enum FindClosestTriangleCalculation {
    BruteForce,
    SpiralOut
  }

  [Serializable]
  [Obsolete("Use Quantum.NavMeshBakeData")]
  public class BakeData {
    public string                         Name;
    public FPVector3                      PositionFP;
    public FP                             AgentRadius;
    public List<string>                   Regions;
    public MapNavMeshVertexFP[]           Vertices;
    public MapNavMeshTriangle[]           Triangles;
    public MapNavMeshLink[]               Links;
    public FindClosestTriangleCalculation ClosestTriangleCalculation;
    public int                            ClosestTriangleCalculationDepth;
    public bool                           EnableQuantum_XY;

    [Obsolete("Not used anymore, link error correction now happens during the Unity navmesh import.")]
    public float LinkErrorCorrection;

    [Obsolete("Use PositionFP instead")]
    public Vector3 Position {
      set { PositionFP = value.ToFPVector3(); }
      get { return PositionFP.ToUnityVector3(); }
    }

    // The editor code works with floating points and BakeData will be initialized with determinisic FP values.
    public static MapNavMeshVertexFP[] ConvertVertices(MapNavMeshVertex[] editorVertices) {
      return editorVertices.Select(v => new MapNavMeshVertexFP {
        Id = v.Id,
        Position = v.Position.ToFPVector3(),
        Neighbors = v.Neighbors.ToList(),
        Triangles = v.Triangles.ToList(),
      }).ToArray();
    }

    public static MapNavMeshVertex[] ConvertVertices(MapNavMeshVertexFP[] editorVertices) {
      return editorVertices.Select(v => new MapNavMeshVertex {
        Id = v.Id,
        Position = v.Position.ToUnityVector3(),
        Neighbors = v.Neighbors.ToList(),
        Triangles = v.Triangles.ToList(),
      }).ToArray();
    }
  }
}

#endif