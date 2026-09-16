# Evidence on the dev machine — ETABS 22 OAPI (2026-09-16, 21:52)

Read-only probes (registry, file metadata, reflection over the API DLL, CHM extraction). **ETABS was NOT started, no .EDB touched.**
Every E-item cites the command that produced it. Anything not listed here is `[chưa xác minh]`.

## E1 — Install + versions (`Get-ChildItem`, `VersionInfo`)
```
C:\Program Files\Computers and Structures\ETABS 22\
  ETABS.exe            263 744 B  FileVersion 22.7.0.4095  ProductVersion 22.7.0.4095   (native PE, not a managed assembly)
  ETABSv1.dll          335 424 B  FileVersion 2.10.0.0
  CSiAPIv1.dll       1 187 392 B  FileVersion 2.10.0.0
  CSI API ETABS v1.chm 4 069 093 B
  ETABS.chm
  NativeAPI\x64, NativeAPI\x86
Uninstall entry: "ETABS 22" DisplayVersion 22.7.0  InstallLocation=C:\Program Files\Computers and Structures\ETABS 22\
```
- Product is **ETABS 22 (v22.7.0.4095)**, CSI version numbering, not a year. OAPI wrapper version **2.10.0.0**.
- `HKLM\SOFTWARE\Computers and Structures\*` does **not exist** (neither HKLM nor HKCU) → no vendor "install path" registry key like Autodesk ships. Discoverable paths: the Uninstall entry above and the COM `LocalServer32` value (E3).

## E2 — `ETABSv1.dll` is a managed **.NET Standard 2.0** wrapper (`[System.Reflection.Assembly]::LoadFile` in pwsh 7)
```
ImageRuntimeVersion: v4.0.30319
FullName: ETABSv1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=453d728ef24c6f5e
TargetFrameworkAttribute: ".NETStandard,Version=v2.0"
ComVisible(true); assembly Guid 542F7A9D-3A7D-4061-97B3-3A1276FF83BD
References: netstandard 2.0.0.0 ; Microsoft.Win32.Registry 5.0.0.0
Types: 346 (135 interfaces, 154 classes) — 0 types carry [ComImport]
  cHelper   interface [Guid]                       Helper class [Guid, ClassInterface, ComVisible]  ← real managed class
  cOAPI     interface [Guid]                       wOAPI : cWrapper<cOAPI>   (wrapper: field T m_objInner, m_OAPIVersionNumberGUI, CheckVersion*, SafeProp)
  cSapModel interface [Guid]                       wSapModel, wAreaElm, wLineElm, wPointElm, wAnalysisResults … (w* wrappers)
  cAnalyze, cFile, cDatabaseTables, cPluginCallback interfaces [Guid]
  eUnits enum: lb_in_F, lb_ft_F, kip_in_F, kip_ft_F, kN_mm_C, kN_m_C, kgf_mm_C, kgf_m_C, N_mm_C, N_m_C, Ton_mm_C, Ton_m_C, kN_cm_C, kgf_cm_C, N_cm_C, Ton_cm_C
```
`CSiAPIv1.dll` is the same shape (managed, `.NETStandard,Version=v2.0`, namespace `CSiAPIv1.*`, same interfaces `cHelper/cOAPI/cSapModel`).
→ **Consumable from `net10.0` as a plain `<Reference>`** (netstandard2.0 is a supported input of every .NET 5+ TFM). No tlbimp, no `[ComImport]`, no PIA. Consequence for the plan: the COM boundary is *inside* the wrapper, not in our code.

## E3 — Attach mechanism = out-of-process COM, ETABS.exe is the LocalServer (registry + `DllImport` scan)
```
HKLM\SOFTWARE\Classes\CSI.ETABS.API.ETABSObject\CLSID = {e4f6d00f-51a5-4d65-a09a-ba00fcbf1f82}
HKLM\SOFTWARE\Classes\CLSID\{e4f6d00f-…}\LocalServer32 = C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe
HKLM\SOFTWARE\Classes\CLSID\{e4f6d00f-…}\ProgId        = CSI.ETABS.API.ETABSObject
HKLM\SOFTWARE\Classes\ETABSv1.Helper\CLSID = {FB5D97C6-0EEB-42A9-B3B9-3BC2FD19E04F}
HKLM\SOFTWARE\Classes\CLSID\{FB5D97C6-…}\InProcServer32 = …\ETABS 22\NativeAPI\x64\ETABSv1.comhost.dll   ← .NET Core comhost (for VBA/COM clients)
TypeLib {542F7A9D-…} 1.0 = "ETABS Application Programming Interface (API) v1"; {F896D55D-…} 1.0 = "CSi Application Programming Interface (API) v1"
P/Invokes inside ETABSv1.dll (ETABSv1.NativeMethods): ole32!CLSIDFromProgIDEx, ole32!CLSIDFromProgID, oleaut32!GetActiveObject, ole32!GetRunningObjectTable, ole32!CreateItemMoniker
```
- `Helper.GetObject("CSI.ETABS.API.ETABSObject")` resolves the running instance through **oleaut32 `GetActiveObject` / the Running Object Table** by P/Invoke — **not** `Marshal.GetActiveObject` (which .NET Core lacks). So attach works on .NET 5+ without any shim of ours.
- `Helper` surface (reflection): `CreateObject(fullPath)`, `CreateObjectProgID(progID)`, `GetObject(typeName)`, `GetObjectProcess(typeName, pid)`, `GetObjectHost(hostName, progID)`, `GetObjectHostPort(…)`, `CreateObjectHost(…)`, `CreateObjectHostPort(…)`, `CreateObjectProgIDHost(…)`, `CreateObjectProgIDHostPort(…)`, `StartAPIWrapper(fullPath)`, `ShellOut/ShellOutV1/ShellOutV2(fullPath)`, `GetOAPIVersionNumber()`, `GetPath(clsid)`, `GetClassId(progId)`, `GetCaseSensitivePath(path)`.
- `cOAPI` surface: `ApplicationExit(bool FileSave)`, `SapModel` (get), `GetOAPIVersionNumber()`, `Hide()`, `Unhide()`, `Visible()`, `SetAsActiveObject()`, `UnsetAsActiveObject()`, `InternalExec(int)`, `ApplicationStart()`.

## E4 — ETABS 22 itself runs on **.NET 8** (`*.runtimeconfig.json` in the install dir)
```
NativeAPI\x64\ETABSv1.runtimeconfig.json : tfm net8.0, Microsoft.NETCore.App 8.0.0, rollForward LatestMinor
NativeAPI\x64\ETABSv1.deps.json          : runtimeTarget .NETCoreApp,Version=v8.0
NativeAPI\x64\ also: CSiAPIv1.comhost.dll, CSiAPIv1.tlb, ETABSv1.tlb, dscom.exe
install root: ETABS.runtimeconfig.json, ETABS.deps.json, CSI.SAPFire.Driver.runtimeconfig.json, RegisterETABS.*, UnregisterETABS.*
Installed runtimes on this machine: Microsoft.NETCore.App 6.0.36, 8.0.26, 8.0.28, 9.0.17, 10.0.7, 10.0.8
```
→ An **in-process ETABS plugin (`cPluginContract`) for ETABS 22 would have to be a .NET 8 assembly** loaded into ETABS's own .NET 8 host — relevant only if ADR-01 picks a bridge inside ETABS. The out-of-process route needs nothing of this.

## E5 — Runtime state when probed
- `Get-Process ETABS` → **0** instances. Nothing was started.
- No CSI/ETABS package in the NuGet cache (`~/.nuget/packages`, checked by the user before this session).

## E6 — API documentation available offline
`CSI API ETABS v1.chm` extracted (7-Zip, `hh.exe -decompile` produced nothing) to the session scratchpad: 1 707 HTML topics, title index at `<scratchpad>/etabs-chm/index.txt` (`title \t html/<guid>.htm`). Topic titles follow `cSapModel.SetPresentUnits Method`, `cHelper Interface`, `Release Notes`, `Introduction`. Cite as **CHM › `<topic title>`**.

## E7 — Repo seam (grep, 2026-09-16)
```
McpShared/HPRebar.McpBridge.Core/Pipe/IBridgeExecutor.cs:10        public interface IBridgeExecutor
McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:26,31    private readonly IBridgeExecutor _executor; ctor(IBridgeExecutor executor, BridgeSettings settings, string hostVersion, string hostName = "Revit")
McpShared/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs:19,27,35     McpBridgeHost(IBridgeExecutor executor, BridgeSettings settings, …)
```
(Server-side counterpart — `BridgeClient` in `HPRebar.Mcp.Server.Core` — analysed in researcher-02.)

## E8 — CHM quotes that decide ADR-02 (read verbatim from the extracted topics, 2026-09-16)
- **CHM › "cSapModel.SetModelIsLocked Method"** — `int SetModelIsLocked(bool Lockit)`; Remarks: *"With some exceptions, definitions and assignments can not be changed in a model while the model is locked. If an attempt is made to change a definition or assignment while the model is locked and that change is not allowed in a locked model, an error will be returned."* → the CHM does **not** state that unlocking deletes analysis results; that is ETABS product behaviour (user brief + CSI community) → cite as `[chưa xác minh trong CHM]`, verify in the phase-1 spike by `GetCaseStatus` before/after.
- **CHM › "cSapModel.GetModelIsLocked Method"** — `bool GetModelIsLocked()` (returns the bool directly, no `ref`).
- **CHM › "cFile.Save Method"** — `int Save(string FileName = "")`; Remarks: *"If no file name is specified, the file is saved using its current name. If there is no current name for the file (the file has not been saved previously) and this function is called with no file name specified, an error will be returned."* → the CHM does **not** say whether `Save(path)` re-points the model's current name ("save as"). researcher-01 fact #13 marks it Verified — **downgrade to `[chưa xác minh]`**; the phase-1 spike must call `Save(tmp)` then `GetModelFilename()` to settle it before the snapshot design relies on it.
- **CHM › "cAnalyze.RunAnalysis Method"** — `int RunAnalysis()`; Remarks: *"The analysis model is automatically created as part of this function. IMPORTANT NOTE: Your model must have a file path defined before running the analysis … the File.Save function must be called with a file name before running the analysis."* No cancel, no progress, no timeout parameter (absence).
- **CHM › "cAnalyze.DeleteResults Method"** — `int DeleteResults(string Name, bool All = false)`.
- **CHM › "cOAPI.ApplicationExit Method"** — `int ApplicationExit(bool FileSave)`; *"If this item is True the existing model file is saved prior to closing the application … saved with its current name."*
- **No topic** named `*IsLoaded*` / `*ModelIs*` besides the two lock methods → "no model open" has no direct API; detection must be probed in the phase-1 spike (`GetModelFilename()`, `GetModelFilepath()`, `PointObj.Count()` behaviour on the ETABS start screen) `[chưa xác minh]`.
