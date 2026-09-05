# CouchCoop embedded ShaderGraph 17.4.0

Lean override of Unity 6000.4.2f1 builtin ShaderGraph to clear first-compile **CS0246** on `GUID` (`BuiltInCanvasSubTarget`, `TargetSetupContext`).

- Same 17.4.0 sources as editor builtin PackageCache fingerprint era, minus Samples~/Documentation~/Tests.
- GUID type usages that lack `using UnityEngine;` are qualified as `UnityEngine.GUID`.
- Remove this folder and the `file:com.unity.shadergraph` manifest entry after upgrading the editor to **6000.4.8f1+** (upstream fix).
