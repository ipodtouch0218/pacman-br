#if QUANTUM_ENABLE_MIGRATION
namespace Quantum.Editor {
  using System.Linq;
  using UnityEditor;
  using UnityEngine;

#if !DISABLE_QUANTUM_ASSET_INSPECTOR && !QUANTUM_DISABLE_ASSET_EDITORS
  [CustomEditor(typeof(Quantum.LazyEntityPrototype), true)]
#endif
  [CanEditMultipleObjects]
  public class QuantumEntityPrototypeAssetObjectEditor : QuantumAssetObjectEditor {

    private bool IsNestedInPrefab(Quantum.EntityPrototype prototype) {
      var assetPath = AssetDatabase.GetAssetPath(prototype);
      if (string.IsNullOrEmpty(assetPath)) {
        return false;
      }
      
      return AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(GameObject);
    }
    
    public override void OnInspectorGUI() {
      base.OnInspectorGUI();
      
      bool anyUpgradable = false;
      foreach (Quantum.EntityPrototype prototype in targets) {
        if (IsNestedInPrefab(prototype)) {
          anyUpgradable = true;
          break;
        }
      }

      if (anyUpgradable) {
        EditorGUILayout.Space();

        if (GUILayout.Button("Upgrade to standalone assets")) {

          var info = targets.Cast<Quantum.EntityPrototype>()
           .Where(prototype => IsNestedInPrefab(prototype))
           .Select(prototype => new {
              prototype,
              prefabPath = AssetDatabaseUtils.GetAssetPathOrThrow(prototype),
              assetGuid = prototype.Guid,
            })
           .ToList();
          
          // now that the info is saved, destroy the prototypes and force an update - it should spawn
          // a nested asset, if it needed
          foreach (var item in info) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath);
            DestroyImmediate(item.prototype, true);
            EditorUtility.SetDirty(prefab);
          }

          AssetDatabase.SaveAssets();
          
          // now that the prototypes are destroyed, set guid overridesB
          foreach (var item in info) {
            var standaloneAsset = QuantumEntityPrototypeAssetObjectImporterEditor.LoadPrototypeAssetForPrefab(item.prefabPath);
            Debug.Assert(standaloneAsset);
            
            QuantumUnityDBUtilities.SetAssetGuidOverride(standaloneAsset, item.assetGuid);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(standaloneAsset));
          }
          
          QuantumUnityDBUtilities.RefreshGlobalDB();
        }
        
        EditorGUILayout.HelpBox($"Starting with Quantum 3.0, prototype assets are stored outside of the prefab. " +
          $"This is done to speed up asset loading during the simulation and avoid deadlocks on some platforms. " +
          $"Upgrading to standalone assets will destroy this asset and create a new one, next to the prefab. " +
          $"If you want the standalone asset to keep the same GUID, make sure Guid Override is enabled prior to the upgrade.", MessageType.Info);
      }
    }
  }
}
#endif