#if QUANTUM_ENABLE_MIGRATION

namespace Quantum {
  using System;
  using System.Collections.Generic;
  using Editor;

  [Obsolete("Use " + nameof(QuantumUnityDBUtilities) + " instead")]
  public static class AssetDBGeneration {
    public static void Generate() {
      QuantumUnityDBUtilities.RefreshGlobalDB();
    }

    public static void Export() {
      QuantumUnityDBUtilities.ExportAsJson("db.json");
    }
    
    public static AssetObject[] GatherQuantumAssets() {
      var assets = new List<AssetObject>();
      foreach (var it in QuantumUnityDBUtilities.IterateAssets()) {
        assets.Add((AssetObject)it.pptrValue);
      }
      return assets.ToArray();
    }
  }
}

#endif