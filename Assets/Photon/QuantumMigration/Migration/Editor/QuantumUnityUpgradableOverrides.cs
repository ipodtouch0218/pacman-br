#if QUANTUM_ENABLE_MIGRATION_CODE_UPGRADE
namespace Quantum.Editor {
  using System;
  using System.Reflection;
  using UnityEditor;

  public static class QuantumUnityUpgradableOverrides {
    private const string QuantumOverridesContent = @"|Quantum\.[^\\]+(\.ref)?\.dll$";
    
    [MenuItem("Quantum/Migration/Set SourceUpdater filters", priority = 6000)]
    [InitializeOnLoadMethod]
    static void SetSourceUpdaterFiltersMenuItem() {
      if (SetSourceUpdateFilters(true, out var filter)) {
        QuantumEditorLog.LogImport($"SourceUpdater filters set to: {filter}");
      } else {
        QuantumEditorLog.TraceImport($"SourceUpdater filters already set ({filter})");
      }
    }

    public static bool SetSourceUpdateFilters(bool enable, out string filter) {
    
      var type = Type.GetType("UnityEditorInternal.APIUpdating.APIUpdaterManager, UnityEditor", true);
      var property = type.GetProperty("ConfigurationSourcesFilter", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) ?? throw new ArgumentOutOfRangeException("Property ConfigurationSourcesFilter not found on APIUpdaterManager");
      filter = (string)property.GetValue(null);

      if (filter.Contains(QuantumOverridesContent)) {
        if (enable) {
          return false;
        } else {
          filter = filter.Replace(QuantumOverridesContent, string.Empty);
          property.SetValue(null, filter);
          return true;
        }
      } else {
        if (enable) {
          filter += QuantumOverridesContent;
          property.SetValue(null, filter);
          return true;
        } else {
          return false;
        }
      }
    }
  }
}
#endif