# VNConvert — vendored third-party code

**Internal use only. Never distribute this folder, or a build that contains `VNConvert.dll`, outside the company.**

| | |
|---|---|
| Upstream | https://github.com/AutoLISP-Just-Simple/VNConvert |
| Commit | `205e569` ("Update README with AJS - lisp.vn note"), copied 2026-10-14 |
| Authors | Pham Kim Long (UniKey Project, original C++ `vnconv`); AJS — lisp.vn (C# port, "code lại bằng Google Gemini") |
| Licence | **None published upstream** (no LICENSE file = all rights reserved). The algorithm and tables come from UniKey's `vnconv`, which UniKey distributes under the GPL. Accepted by the project owner on 2026-10-14 for internal use only. |
| Purpose | TCVN3 / VNI-Win → Unicode conversion behind the HPAutoCad MCP seeds `detect_vn_legacy_text` and `convert_vn_text_encoding` |

## Audit before copying (2026-10-14)

Read in full, never executed from the clone. No network, process, registry, reflection, Base64 or P/Invoke in the
managed code; no file that targets an AI agent (CLAUDE.md, AGENTS.md, prompts). Findings that decided what was copied:

- `NativeExports.cs` — C-ABI exports, `VnConvErrMsg` returns a pointer taken inside a `fixed` block (dangling after
  return). Not needed, **not copied**.
- `src/VNConvert.Cli`, `tests/`, the C++ `vnconv/` tree and its `.tlog` build logs (which leak the author's OneDrive
  paths) — **not copied**. The C++ `vnconv/data.cpp` tables are used only off-repo, to generate the golden test data
  (`HPAutoCad/tools/vnconvert/generate-golden-from-unikey-data.py`, sha256 pinned there).
- The code is machine-written and was not reviewed upstream: `HPAutoCad.Aec.Tests/VnConvert/` checks it against the
  original UniKey tables before anything uses it.

## Files (sha256 of the content with CR removed)

| File | sha256 |
|---|---|
| `Core/ByteStreams.cs` | `029cacfd9ea96db27750daa90a6532d57739be0391ca7153561c314f5a3eec01` |
| `Core/DoubleByteEngine.cs` | `04a2d6961ca6592b10821df35a3bf6d3e8b0a565fc84b200892ca12ecf178160` |
| `Core/ICharsetEngine.cs` | `a6c4ebec96f36a48769bb7f82251200605e747112444b2124cfbad5d5e4e5378` |
| `Core/PatternMatcher.cs` | `82ed539940a55f2eace4a94938c7c6343777336da45f86608c6fb352e81afb6e` |
| `Core/SingleByteEngine.cs` | `78f024cd109bd475aacdd5879af7509260f7b38a50eba7abf5da1ed2c99b5b96` |
| `Core/StdVnChar.cs` | `f3fc28880757d6f35878a04ba327b06a6b1098dc4730da70eeb106013dfa873c` |
| `Core/UnicodeCStringEngine.cs` | `1af235b270508de3b1d0dffdbb8d8d7dffd920ab82863a677b15c434edee00ef` |
| `Core/UnicodeCompEngine.cs` | `3617dcffabeea9b8dc3718b81818f49d51fb4691a51210c3b0723e95d0ae31ed` |
| `Core/UnicodeEngine.cs` | `b2a703248522e9147738be43942e86827f586cb3b7b01d23913f3e6868f0c2b7` |
| `Core/UnicodeRefEngine.cs` | `7065804ebc0a8405f80c34ab52036a967f2214881263a9fd1c4dc40aa53a8355` |
| `Core/Utf8Engine.cs` | `f33058b4f2eb263d47bae85ab5555d3d4aca0f79ea964dfdd21eaedd4b1b1c4c` |
| `Core/Utf8ViqrEngine.cs` | `2913523ed8b9477cb05a0ff4129887c67d4d431211ec9c9c42da90919bdceaf0` |
| `Core/ViqrEngine.cs` | `046b998fb7badd71b4326b8bc31d404f891ef5e9ecfbe57825ecb1c471df195a` |
| `Core/VnCharsetRegistry.cs` | `1edac8718903eda901318cf064e587ac60d24fc7ce14c1eeb9b34cb6d0f7946b` |
| `Core/WinCp1258Engine.cs` | `f55f667e616ec66dcecfbbba1186941ba92dd03ee3d8608ada98a83a1216c8b3` |
| `VnCharset.cs` | `25ee98012b8129f1d92c7ee2661e9b3ef835c7026c71fb15c0d8cbcf16609ae9` |
| `VnConvOptions.cs` | `67b4ae145ad5d5cb07a485cd7aaed55f880ed914cba25a3fc420fa5b6beb587c` |
| `VnConverter.cs` | `dcb6f10cd3d6fe80c81305fc9193cf29ace001aa5c0268715990b4677893b0a4` |
| `VnTables.g.cs` | `e5cff27741acb291d76ccca028cb086d969499d76e0364a202bff77fb4bd1230` |

## Local edits

None. (Add one row per edit: file, reason, date — and update the hash above.)

## Usage rules for HP code

- Call only `VnConverter.Convert(ReadOnlySpan<byte>, VnCharsetId, VnCharsetId, VnConvOptions?)` with an explicit
  options value. Never read or write `VnConverter.GlobalOptions` — it is a mutable static shared by every caller.
- Wrap it in `HPAutoCad.Aec/Text/`; nothing else in HPAutoCad references `VNConvert` directly.
