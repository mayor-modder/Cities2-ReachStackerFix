# Validation record

Reach Stacker Fix 0.1.0 was built on 2026-09-12 and installed on 2026-09-13. Its first airport regression scenario passed based on the tester's observations and the runtime activation log; broader gameplay checks remain pending.

| Check | Result |
| --- | --- |
| Installed game API and original query inspection | Game 1.6.0f1; expected field, lifecycle methods and query access verified from installed assemblies |
| `dotnet run --project tests/ReachStackerFix.Tests.csproj -c Release` | 20/20 passed using real Unity query descriptors |
| Missing-exclusion mutation in a separate test copy | 3 failures; tests detect removal of the freight-transfer guard |
| `dotnet build reach-stacker-fix.csproj -c Release` | Exit 0; 0 warnings and 0 errors |
| Official mod postprocessor | Entities, Jobs, Burst and Colossal versioning completed; Windows, macOS and Linux companions generated |
| Independent implementation review | No blocking code defects; descriptor policy, Unity query ownership/dependencies and managed scheduling hooks reviewed |
| Assembly identity | `ReachStackerFix`, version `0.1.0.0`, .NET Framework 4.8 |
| Package contents | Mod DLL/PDB, Harmony DLL, generated native companions; no game assemblies or test outputs |
| Live installation | Installed 2026-09-13 with the game closed; all seven installed files match the verified package hashes |
| Runtime guard activation | Log confirmed `Freight-path guard active` in the Game world at 15:25 on 2026-09-13, with no mod warnings or errors in the inspected log |
| Terminal-to-airport regression | Tester removed 16 existing reach stackers; eight semi trucks/delivery vans replaced them, and no new stackers returned during the reported observation period |
| Legitimate port work and extractor production | Pending local playtest |
| Save/reload and uninstall smoke checks | Pending local playtest |

The local static analyzer found the expected metadata, references, patch/unpatch symmetry and build structure. Its settings/localization checks reported missing registration because this mod intentionally has no settings or UI; those checks are not applicable. No settings or localization code was added solely to satisfy them.

Reproducible build/test commands and gameplay success/failure signals are in the [README](../README.md). Detailed local outputs are in `artifacts/build.log`, `artifacts/tests.log`, `artifacts/mutation-test.log`, `artifacts/static-analysis.json` and `artifacts/playtest-manifest.json`. The manifest records file lengths and SHA-256 hashes for the staged package. These generated artifacts are ignored by Git.
