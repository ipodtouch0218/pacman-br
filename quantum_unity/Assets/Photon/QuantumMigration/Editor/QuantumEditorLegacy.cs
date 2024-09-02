#if QUANTUM_ENABLE_MIGRATION

namespace Quantum.Editor {
  using System;

  [Obsolete("Renamed to QuantumEditorCustomPluginMenu")]
  public abstract class CustomPluginMenu : QuantumEditorMenuCustomPlugin { }

  [Obsolete("Renamed to QuantumEditorShortcutsWindow")]
  public abstract class QuantumShortcuts : QuantumEditorShortcutsWindow { }

  [Obsolete("Renamed to QuantumEditorAutoBaker")]
  public abstract class QuantumAutoBaker : QuantumEditorAutoBaker { }

  [Obsolete("Renamed to QuantumEditorMenuLookUpTables")]
  public abstract class GenerateLookUpTables : QuantumEditorMenuLookUpTables { }

  [Obsolete("Renamed to QuantumEditorFrameDifferWindow")]
  public abstract class QuantumFrameDifferWindow : QuantumEditorFrameDifferWindow { }
}

#endif