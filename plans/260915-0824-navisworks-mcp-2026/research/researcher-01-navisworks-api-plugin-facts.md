# Navisworks Manage 2026 MCP Bridge Plugin Development Research

**Researcher:** Claude Code (researcher subagent) · **Date:** 2026-09-15 · **Scope:** Autodesk Navisworks Manage 2026 (.NET API 23.0, .NET Framework 4.8) plugin development facts for MCP bridge architecture.

> Editor's note (planner, 2026-09-15): the original 488-line report was lost to a bad `git mv -k` + `rm -rf`; this is the condensed rewrite from the captured content, every source kept. §4 "Rollback" and §10 are superseded by on-machine evidence — see `research/evidence-on-machine-2026-09-15.md` and `adr/adr-01-*.md`, `adr/adr-02-*.md`. Where this report says `[chưa xác minh]` and the evidence file says verified, the evidence file wins.

---

## 1. Plugin Deployment Rules

### Folder & Assembly Naming

**Claim:** Folder name and assembly base name MUST match exactly.

**Source:** [Navisworks .NET: Deploy Addins (Plugins) - RevitNetAddinWizard & NavisworksNetAddinWizard](https://spiderinnet.typepad.com/blog/2012/10/navisworks-net-deploy-addins-plugins.html)
> "The subfolder name and the file base name must match each other; otherwise, Navisworks (either Manager or Simulate) will not find and load the plugin."

Example: folder `Abc.Def\` must contain `Abc.Def.dll`, full path `C:\Program Files\Autodesk\Navisworks Manage XXXX\Plugins\Abc.Def\Abc.Def.dll`.
> "If the assembly file name is 'NNAW.NavisworksNetAddin1.dll', its full path should be C:\Program Files\Autodesk\Navisworks Simulate 2012\Plugins\NNAW.NavisworksNetAddin1\NNAW.NavisworksNetAddin1.dll."

### Deployment Paths (Machine-wide vs Per-user)

- **Per-user (PRIMARY):** `%APPDATA%\Autodesk\Navisworks Manage 2026\Plugins\<FolderName>\<Assembly>.dll` — verified locally: `...\Plugins\NavisworksMCPPlugin\NavisworksMCPPlugin.dll` exists (third-party plugin).
- **Install dir (legacy):** `<install>\Plugins\<FolderName>\<Assembly>.dll` — [Side Loading - Manually Installing a Navisworks Plugin | House of BIM](https://www.houseofbim.com/posts/side-loadingmanually-installing-a-navisworks-plugin/): "Create a sub folder under the 'Plugins' folder of the Navisworks product installation path and put the .NET assembly into the sub folder."

### ApplicationPlugins Bundle Support

**Claim:** Navisworks supports `%APPDATA%\Autodesk\ApplicationPlugins\<name>.bundle\` with `PackageContents.xml`.
**Status:** [chưa xác minh] for 2026 on this machine. Source: [Navisworks publisher guidelines | APS](https://aps.autodesk.com/marketplace/publisher-center/navisworks-publisher-guidelines): "Navisworks apps live in a .bundle folder containing a PackageContents.xml configuration file and a Contents subfolder with deliverables. Apps are installed to %APPDATA%\Autodesk\ApplicationPlugins\." Bundle layout: `ADSK.MyApp.bundle\PackageContents.xml`, `Contents\v23\ADSK.MyPlugin.dll` (v23 = Navisworks 2026). One community comment suggests bundle loading is unreliable for Navisworks.

### Dependency Resolution
**Claim:** DLL dependencies beside the plugin resolve through the load-from context. **Status:** [chưa xác minh] — no explicit doc; the evidence file shows an `AssemblyResolve` handler is needed for version mismatches regardless.

### Security/Signing
**Claim:** No SECURELOAD equivalent; no prompt for unsigned plugins. **Status:** [chưa xác minh] — nothing found parallel to AutoCAD SECURELOAD / Revit "publisher could not be verified"; verify by loading an unsigned test plugin.

### Plugin Uninstall
Delete `%APPDATA%\Autodesk\Navisworks Manage 2026\Plugins\<PluginFolder>\`. No registry entries. [chưa xác minh] whether Options has a Plugins page that disables plugins.

---

## 2. Naming Conventions

- **PluginAttribute:** `[Plugin(name, developerId)]`, DeveloperId = 4-char ADN code or GUID (Api.xml).
- **ExecuteAddInPlugin(string pluginId):** format [chưa xác minh] — likely `"Name.DeveloperId"`.
- **RibbonLayout XAML:** language subfolder beside the DLL: `Plugins\MyPlugin\MyPlugin.dll`, `Plugins\MyPlugin\en-US\MyPluginRibbon.xaml` (+ optional `.name` strings file). Source: [Navisworks .NET: Create Small Ribbon Buttons…](https://spiderinnet.typepad.com/blog/2013/11/navisworks-net-create-small-ribbon-buttons-using-commandhandlerplugincommandattributeribbontabattributeribbonlayoutattri.html): "Plugin DLLs in folders like C:\Program Files\Autodesk\Navisworks Manage [year]\Plugins\[PluginName], with XAML and .TXT (name) files deployed in language-specific subfolders such as en-US." [Autodesk Developer Blog: Add Custom Panel and Button to Built-in Tab](https://blog.autodesk.io/add-custom-panel-and-button-to-built-in-tab-of-navisworks-ribbon/): "The strings defined in the XAML layout file override strings defined in the name file."

---

## 3. Threading Model

- **Main-thread-only API.** Source: [Thread and memory issues of NAVISWORKS API - Autodesk Community](https://forums.autodesk.com/t5/navisworks-api/thread-and-memory-issues-of-navisworks-api/td-p/13364942): "The AutoCAD APIs do not support multithreading, and you should only call the API functions from the main thread." (Autodesk staff answer in the Navisworks API forum; wording borrowed from AutoCAD guidance.)
- **Marshal:** same thread: "If you are in a different thread, you have to marshal the call to the main thread. The simplest way to achieve that is creating a System.Windows.Forms.Control object on the main thread and call its Invoke() function…"
- **Application.Idle** exists (Api.xml). Fires during modal dialogs / clash run / append? [chưa xác minh].

---

## 4. Transactions & Undo

- `Document.BeginTransaction(string)` → `Transaction`; `Commit()`. Api.xml: "Currently it is a requirement that you must call Commit… In the future rollback may be supported." `Document.Rollback()`: "Rolls back (undoes) the last completed transaction. It will not be available for Redo." (see evidence file for the reconciled reading: no in-flight rollback; post-commit undo exists).
- Undo grouping: [Transaction/Undo/Redo - Autodesk Community](https://forums.autodesk.com/t5/navisworks-api-forum/transaction-undo-redo/td-p/12050582): "Navisworks transactions allow you to enclose multiple actions modifying the document, providing both a speedup and allowing users to undo all of them at once."
- Non-undoable: AppendFile, SaveFile, Export (forums). Clash run undoable? [chưa xác minh]. "Undo NOT available after deleting appended files; must re-append" — [ApiDocs.co Document.AppendFile](https://apidocs.co/apps/navisworks/2018/M_Autodesk_Navisworks_Api_Document_AppendFile_1_bb3a7a4f.htm) + forum.
- Nested transactions: [chưa xác minh].

---

## 5. Writable Areas via .NET API

1. Selection sets — `DocumentSelectionSets.AddCopy(SelectionSet)` — [forum](https://forums.autodesk.com/t5/navisworks-api-forum/how-to-add-current-selection-to-selection-set-by-navisworks-api/m-p/5702964).
2. Saved viewpoints — `SavedViewpoints.AddCopy(SavedViewpoint)` — [AEC DevBlog 2012](https://adndevblog.typepad.com/aec/2012/06/navisworks-net-api-2013-new-feature-saved-viewpoint.html).
3. Clash tests — `DocumentClash.TestsData.TestsAddCopy` / `TestsRunTest` — [xiaodongliang/Forge-Navisworks-ClashTest](https://github.com/xiaodongliang/Forge-Navisworks-ClashTest/blob/master/Navisworks%20Plugin/Class1.cs). Sync/async, undo participation: [chưa xác minh].
4. Comments — `AddComment(SavedItem, Comment)` — [TwentyTwo Viewpoint Part-1](https://twentytwo.space/2020/12/06/navisworks-api-viewpoint-part-1/).
5. Appearance — `DocumentModels.OverridePermanentColor/OverridePermanentTransparency` — [TwentyTwo](https://twentytwo.space/2020/08/01/navisworks-api-find-intersect-and-override-color-transparency-v2/).
6. Custom user properties — COM only (`InwGUIPropertyNode2.SetUserDefined`) [chưa xác minh in 2026].
7. Append/Merge — `Document.AppendFile` — [Navisworks Help](https://help.autodesk.com/view/NAV/2024/ENU/?guid=GUID-B2CCBB7B-B876-4BBC-8CF7-F062DA3FEDD7).
8. Export — `ExportToNwd`, `PublishFile`, `GenerateImage` (Api.xml).
9. Redline markup — no .NET method found [chưa xác minh]; likely ComApi/UI only.
10. TimeLiner — `DocumentTimeliner.TaskAddCopy` (reflection confirms, see evidence file).
11. Hide/Require/Freeze — `DocumentModels.SetHidden/SetRequired/SetFrozen` (reflection confirms).

**Not exposed publicly:** clash report export — [How can I export clash report using api? - Autodesk Community](https://forums.autodesk.com/t5/navisworks-api/how-can-i-export-clash-report-using-api/td-p/11900777): no public API; `Autodesk.Navisworks.Api.Interop.LcClClashReport.WriteReport` is internal interop.

---

## 6. Units
- Per-model default unit vs one scene unit; API geometry already in `Document.Units`. Source: [Setting "Units and Transform" values via Navisworks API](https://blog.autodesk.io/setting-units-and-transform-values-via-navisworks-api/): "Each model file has its own default scale unit, while Navisworks has a single scene unit set from the global options editor. Each file is scaled appropriately according to the scene's units…"
- Mixed units: `DocumentModels.SetModelUnitsAndTransform(model, units, Transform3D, bool)`.
- `UnitConversion.ScaleFactor(Units, Units)` exists (Api.xml).

---

## 7. License Tiers
- Freedom: no API, no plugins — [forum](https://forums.autodesk.com/t5/navisworks-api-forum/navisworks-plugins-for-freedom/td-p/5291331): "Programmers will need Navisworks Simulate or Manage to be installed since no form of the API (plug-in or controls) will be supported in Freedom." + [Mastering Autodesk Navisworks 2013](https://www.oreilly.com/library/view/mastering-autodesk-navisworks/9781118330708/c12_level1_5.xhtml).
- Simulate: API + plugins, no Clash. Manage: + Clash Detective — [novatr guide](https://www.novatr.com/blog/navisworks-guide).

---

## 8. Sample Models
`C:\Program Files\Autodesk\Navisworks Manage 2026\Samples\`: bathcity, Eircom_Park, gatehouse, ice stadium, snowmobile, enviro-dome, Getting Started (Architecture/MEP/Structure .nwc), Quantification\Autodesk_Hospital_*.nwc (30 MB). Source: [Navisworks Help | Samples](https://help.autodesk.com/view/NAV/2024/ENU/?guid=GUID-C3AF0000-2810-47FB-BF21-95181A36DCD8). Sizes/entity counts for test selection: [chưa xác minh] (file sizes measured locally in the evidence file).

---

## 9. Open-Source Navisworks MCP Projects
1. [Aitology/Navisworks_MCP](https://github.com/Aitology/Navisworks_MCP) — C# plugin + named-pipe server, 30+ ops.
2. [meococ/navis-mcp](https://github.com/meococ/navis-mcp) — NavisMcp.Server (.NET 8) + NavisMcp.Plugin (NW 2026 UI thread), named pipe + session token.
3. [livpasdiora/Navis-MCP-ClashDetective](https://github.com/livpasdiora/Navis-MCP-ClashDetective) — clash grouping with Claude.
4. [kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026](https://github.com/kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026) — PoC.
5. [T8899J/navisworks-MCP](https://github.com/T8899J/navisworks-MCP).
6. [Thestreetarckitect/navisworks-mcp](https://github.com/Thestreetarckitect/navisworks-mcp) — NW 2027, C# add-in + Python server, 35 routes.

Common pattern: .NET 8+ stdio server ⇄ named pipe ⇄ .NET Framework 4.8 in-process plugin; all expose fixed tools (no dynamic C# scripting, no self-extending registry).

---

## 10. Roslyn Scripting in .NET Framework 4.8
- Roslyn scripting targets netstandard2.0 → runs on .NET Framework 4.6.1+ ([NuGet 4.8.0](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/4.8.0)).
- Known binding issues: System.Collections.Immutable, System.Reflection.Metadata, System.Runtime.CompilerServices.Unsafe, System.Memory — [dotnet/roslyn#26359](https://github.com/dotnet/roslyn/issues/26359): "A dynamic binding redirect can fail when an old version of System.Immutable is in the GAC." Workarounds: binding redirects (not possible in a host we do not own) or `AppDomain.AssemblyResolve`.
- InteractiveAssemblyLoader on desktop: [chưa xác minh] here — **verified in the evidence file (probe ran on .NET Framework 4.8.9181)**.

---

## Unresolved (as of the web research; several closed by the evidence file)
1. Rollback semantics → closed (evidence file). 2. `TestsRunTest` blocking/undo → open. 3. `Application.Idle` during modal/append/clash → open. 4. Redline API → open (assume ComApi/UI only). 5. AppendFile undo → open (assume not undoable). 6. Security prompt on load → open. 7. Freedom behaviour → not in scope (Manage only). 8. Pipe ACL → closed by design (bridge creates the pipe with a current-user ACL; server connects as the same user). 9. ComApi-only features → custom properties; others open. 10. Clash report export public API → none; plan uses `TestsImageForResult`/result serialization; `LcClClashReport` is internal. 11. Sample sizes → measured locally. 12. `IHostProfile` reuse → yes (ADR-01).

**Status:** DONE
