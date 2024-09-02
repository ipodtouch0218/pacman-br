#if QUANTUM_ENABLE_MIGRATION

using System;
using System.Collections.Generic;
using Quantum;
#if (QUANTUM_ADDRESSABLES || QUANTUM_ENABLE_ADDRESSABLES) && !QUANTUM_DISABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

[Obsolete("Use " + nameof(QuantumUnityDB) + " instead")]
public static partial class UnityDB {
  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.Global) + " instead")]
  public static QuantumUnityDB ResourceManager => QuantumUnityDB.Global;

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.Global) + " instead")]
  public static QuantumUnityDB DefaultResourceManager => QuantumUnityDB.Global;

#if (QUANTUM_ADDRESSABLES || QUANTUM_ENABLE_ADDRESSABLES) && !QUANTUM_DISABLE_ADDRESSABLES
  public static List<(AssetRef, AssetReference)> CollectAddressableAssets() {
    var result = new List<(AssetRef, AssetReference)>();
    CollectAddressableAssets(result);
    return result;
  }

  public static void CollectAddressableAssets(List<(AssetRef, AssetReference)> entries) {
    foreach (var info in QuantumUnityDB.Global.Entries) {
      if (info.Source is QuantumAssetObjectSourceAddressable addressable) {
        entries.Add((new AssetRef(info.Guid), addressable.Address));
      }
    }
  }
#endif

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.UnloadGlobal) + " instead")]
  public static void Dispose() {
    QuantumUnityDB.UnloadGlobal();
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.UpdateGlobal) + " instead")]
  public static void Update() {
    QuantumUnityDB.UpdateGlobal();
  }
  
#if UNITY_EDITOR
  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetAssetEditorInstance) + " instead")]
  public static Quantum.AssetObject FindAssetForInspector(AssetGuid assetGuid) {
    return QuantumUnityDB.GetGlobalAssetEditorInstance(assetGuid);
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.TryGetGlobalAssetEditorInstance) + " instead")]
  public static bool TryFindAssetObjectForInspector<AssetRefT, T>(AssetRefT assetRef, out T asset)
    where AssetRefT : unmanaged, IAssetRef<T>
    where T : AssetObject {
    unsafe {
      var assetRef_ = *(AssetRef*)(&assetRef);
      if (QuantumUnityDB.TryGetGlobalAssetEditorInstance(assetRef_, out asset)) {
        return true;
      }

      asset = null;
      return false;
    }
  }
#endif

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAssetGuid) + " instead")]
  public static AssetGuid GetAssetGuid(String path) => QuantumUnityDB.GetGlobalAssetGuid(path);

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAsset) + " instead")]
  public static AssetBase FindAsset(string path) {
    var assetGuid = QuantumUnityDB.GetGlobalAssetGuid(path);
    if (!assetGuid.IsValid) {
      return null;
    }

    return FindAsset(assetGuid);
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAsset) + " instead")]
  public static AssetBase FindAsset(AssetGuid guid) {
    //return (AssetBase)QuantumUnityDB.GetGlobalAsset(guid);
    throw new NotImplementedException();
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAsset) + " instead")]
  public static T FindAsset<T>(AssetObject asset) where T : AssetBase {
    return asset == null ? default : FindAsset<T>(asset.Guid);
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAsset) + " instead")]
  public static T FindAsset<T>(String path) {
    if (IsType<T, AssetBase>()) {
      return (T)(object)FindAsset(path);  
    }
    if (IsType<T, AssetObject>()) {
      return (T)(object)QuantumUnityDB.GetGlobalAsset(path);
    }
    throw new ArgumentException($"Invalid type {typeof(T)}");
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAsset) + " instead")]
  public static T FindAsset<T>(AssetGuid guid) {
    if (IsType<T, AssetBase>()) {
      return (T)(object)FindAsset(guid);  
    }
    if (IsType<T, AssetObject>()) {
      return (T)(object)QuantumUnityDB.GetGlobalAsset(guid);
    }
    throw new ArgumentException($"Invalid type {typeof(T)}");
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAsset) + " instead")]
  public static T GetGlobalUnityAsset<T>(AssetGuid guid) where T : AssetObject {
    return QuantumUnityDB.GetGlobalAsset(new AssetRef<T>(guid));
  }

  [Obsolete("Use " + nameof(QuantumUnityDB) + "." + nameof(QuantumUnityDB.GetGlobalAsset) + " instead")]
  public static T GetGlobalUnityAsset<T>(AssetObject obj) where T : AssetObject {
    return obj as T;
  }

  private static bool IsType<T, TOther>() => typeof(T) == typeof(TOther) || typeof(T).IsSubclassOf(typeof(TOther));
}

#endif