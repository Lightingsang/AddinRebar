# HPAutoCad MCP — tool catalog: blocks, annotations, measure, geometry issues, CAD standards, audit, issue markup

Generated from `tools/list` of `HPAutoCad.Mcp.Server.exe` (64 tools in all) on an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-autocad__<name>` in Claude Code. `REQ` = required; every length is millimetres, points are `{x, y}` objects in mm; handles are hex strings. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Block

### `insert_block` — Insert a block reference

*destructiveHint.* Inserts a reference to an existing block definition at a point in millimetres with uniform scale and rotation in degrees, fills its attributes from a {tag: value} object, optionally on an existing layer. Fails with a clear message when the block is not defined (list_block_definitions shows the names).

| arg | type | default | description |
|---|---|---|---|
| `blockName` REQ | string |  | Name of an existing block definition |
| `position` REQ | object |  | Insertion point in millimetres |
| `scale` | number | 1 | Uniform scale factor (> 0) |
| `rotationDeg` | number | 0 | Rotation in degrees, counter-clockwise |
| `layer` | string |  | Existing layer name; default: the current layer |
| `attributes` | object |  | Attribute values by tag, e.g. {"TAG": "D01"}; tags not listed keep their default |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `list_block_definitions` — List block definitions

*readOnlyHint.* Lists the block definitions of the drawing (not layouts, not xref-dependent blocks) with entity count, number of references, whether they carry attributes, and whether they are xrefs. Wildcards * and ? in pattern. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `pattern` | string | "*" | Name pattern with * and ? wildcards, case-insensitive |
| `includeAnonymous` | boolean | false | Include anonymous blocks (*U…, *D…, hatch/dimension blocks) |

### `manage_blocks_attributes` — Manage blocks, attributes and dynamic properties

*destructiveHint.* One tool for block work, chosen by op. Read ops return the analysis envelope {success, summary, items, count, offset, truncated, warnings, errors}: listDefinitions (namePattern wildcard, includeAnonymous; name, isXref, isDynamic, attributeTags, referenceCount…), findReferences (filter {blockNames, layers, space, textContains…} or handles; position/rotation/scale/attributes per reference), readAttributes (handles → {tag: value}), inspectDynamic (handles → dynamic properties with value, unitsType, allowedValues, readOnly). Write ops return the edit envelope {success, createdCount, modifiedCount, affectedHandles, items, warnings, errors}: insert (insert {blockName, position, scale?, rotationDeg?, layer?, attributes{tag: value}?}), writeAttributes (handles + attributes {tag: value}), batchUpdateAttributes (items [{handle, attributes}], or handles + attributes, or filter + attributes for every matching reference, max 500), setDynamic (handles[0] + properties {name: value} — distances in mm, angles in degrees). Unknown tags are warnings (insert too); a block without any of the tags, or an attribute whose own layer is locked/frozen, refuses that item. readAttributes reads up to 100 handles and inspectDynamic 20 per call (truncated + warning beyond); attributes per item are cut at 8 values / 120 chars (attributesTruncated says how many more). setDynamic validates every property first (exists, writable) and a value the property rejects aborts the run — a block is never half-changed. Millimetres at the boundary. Side effects: modifies the current drawing inside one undo entry 'MCP: <label>' (U reverts it); dryRun on the request rolls everything back and still reports what would change. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `op` REQ | string listDefinitions|findReferences|insert|readAttributes|writeAttributes|batchUpdateAttributes|inspectDynamic|setDynamic |  |  |
| `namePattern` | string |  | listDefinitions: wildcard on the block name (* any run, ? one char) |
| `includeAnonymous` | boolean | false | listDefinitions: also list anonymous *U/*D blocks |
| `filter` | object |  | findReferences / batchUpdateAttributes: which block references (wildcards allowed) |
| `handles` | array<string> |  | Entity handles (hex, as every AEC tool reports them) |
| `items` | array<object> |  | batchUpdateAttributes: per-reference attribute values |
| `attributes` | object |  | writeAttributes / batchUpdateAttributes: {TAG: value} applied to every selected reference |
| `properties` | object |  | setDynamic: {propertyName: value}; distances in mm, angles in degrees, lists by their allowed value |
| `insert` | object |  | insert: the reference to create |
| `space` | string | "current" | Where new entities go: current (default), model, or a layout name |
| `atomic` | boolean | true | true (default): every item is validated first (layers, styles, keys per entity type, attribute tags) and one invalid item refuses the whole batch — success false, zero counts, every refusal in items[i].error, nothing written and nothing opened for write; a failure while writing aborts the transaction. false: invalid items are listed per item and the rest is written; an item that fails midway reports what it had changed |
| `limit` | integer | 100 | listDefinitions: definitions per page (max 200); findReferences: references per page (max 100 — a reference with 8 attributes is ~500 B) |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 |  |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## Annotation

### `add_linear_dimension` — Add a linear dimension

*destructiveHint.* Adds a dimension between two points given in millimetres, with the dimension line through dimLinePoint: rotated (horizontal by default, or at rotationDeg) or aligned to the two points. Uses the current dimension style unless dimStyle names another. Returns handle and the measured length in mm.

| arg | type | default | description |
|---|---|---|---|
| `p1` REQ | object |  | First extension-line origin in millimetres |
| `p2` REQ | object |  | Second extension-line origin in millimetres |
| `dimLinePoint` REQ | object |  | A point the dimension line passes through, in millimetres |
| `aligned` | boolean | false | Aligned dimension (parallel to p1–p2) instead of a rotated one |
| `rotationDeg` | number | 0 | Rotated dimensions only: angle of the dimension line in degrees (0 = horizontal, 90 = vertical) |
| `dimStyle` | string |  | Existing dimension style name; default: the current style |
| `layer` | string |  | Existing layer name; default: the current layer |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `add_text` — Add text

*destructiveHint.* Places single-line text (DBText) or multi-line text (MText) at a position given in millimetres, with height in mm and rotation in degrees, optionally on an existing layer. Returns handle and the entity type created.

| arg | type | default | description |
|---|---|---|---|
| `text` REQ | string |  | The text; MText accepts \P for a line break |
| `position` REQ | object |  | Insertion point in millimetres (bottom-left for DBText, top-left for MText) |
| `heightMm` REQ | number |  | Text height in millimetres (> 0) |
| `rotationDeg` | number | 0 | Rotation in degrees, counter-clockwise |
| `layer` | string |  | Existing layer name; default: the current layer |
| `mtext` | boolean | false | Create MText instead of single-line DBText |
| `widthMm` | number |  | MText only: wrapping width in millimetres (0 = no wrap) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `convert_vn_text_encoding` — Convert TCVN3 / VNI text to Unicode

*destructiveHint.* Converts the pre-Unicode Vietnamese text detect_vn_legacy_text lists (same arguments, same plan) to Unicode and switches the legacy text styles it used to Unicode TrueType faces (remapStyles, default true): VNI-Times / .VnTime → Times New Roman, VNI-Helve-Condense / .VnArial Narrow → Arial Narrow, VNI-Helve / VNI-Avo / VNI-Arial / .VnArial → Arial; bold/italic kept; .VnTimeH-style capitals-only fonts become real capitals. MText format codes stay byte for byte (\P, \H…;, \U+00B0, {}), an inline \f legacy font is rewritten to its Unicode face with its flags, Symbol and Unicode-font runs are left alone. Looks at DBText, MText, attributes, attribute definitions (value + prompt), dimension text overrides, multileaders and table cells, in model space, every layout and (includeBlocks) every block definition — never xrefs, nor the *D / *T blocks AutoCAD regenerates; text with a field, multiline attributes and entities already converted (XData HP_VNCONVERT) are skipped. The rule that keeps the screen readable: with remapStyles (default) a legacy style is switched to its Unicode face only when every legacy text under it is converted in the same run — when some stay (outside the selection, in an excluded block or table, on a locked layer, carrying a field) the style is kept and its selected texts are held too. Every converted entity gets the XData flag HP_VNCONVERT, so a later run (for example the style switch after a change set) never converts it twice. Text inside block definitions is edited once in the definition (every reference shows it); attributes per reference; dimension overrides are recomputed. A legacy font with no Unicode face (any SHX font, an unlisted VNI-* face) refuses the whole call with NO_FONT_MAPPING until fontMap names one. sourceCharset tcvn3 | vni (text in fonts the rules do not know) needs a handles / layers filter. atomic (default): an entity on a locked/frozen layer (LAYER_LOCKED / LAYER_FROZEN) or a table with a content-locked cell (CELL_LOCKED) refuses the call, nothing written; atomic false: those are listed and the rest converted (their styles kept). Returns the edit envelope {success, modifiedCount (entities + styles), affectedHandles (500 at most), items[{index, ok, handle, type, changed [contents|text|prompt|dimensionText|mtext|cell]}] (errors first, 100 listed), summary {examined, converted, failed, held, byType, blockDefinitions, stylesRemapped[{style, from, to, file}], stylesKept[{style, font, legacyLeft}], remapStyles, skipped, listed, truncated}, warnings, errors}. Side effects: rewrites text values and text-style fonts inside one undo entry 'MCP: <label>'; dryRun on the request rolls it back — preview with detect_vn_legacy_text or dryRun first. With changeSetId the call is recorded into that change set instead of applied; a change set holds only remapStyles: false (styles are table records a rollback cannot restore) — switch the styles afterwards with a direct call.

| arg | type | default | description |
|---|---|---|---|
| `handles` | array<string> |  | Only these entities (an INSERT selects its attributes); empty = all |
| `layers` | array<string> |  | Only entities on these layers (AutoCAD wildcards); empty = all. With a filter only the styles those entities use are remapped |
| `includeBlocks` | boolean | true | Also convert text inside block definitions (title blocks, symbols); xrefs and *D/*T blocks never |
| `includeTables` | boolean | true | Also convert table cells |
| `sourceCharset` | string auto|tcvn3|vni | "auto" | auto: only text in fonts the rules know as legacy; tcvn3 / vni: also text in unknown fonts |
| `fontMap` | object |  | Legacy face or SHX file (wildcards) → Unicode TrueType face, e.g. {"VNI-Univer": "Arial", "vn_vni.shx": "Arial"} |
| `remapStyles` | boolean | true | Also switch the legacy text styles to their Unicode faces (after every text succeeded) |
| `atomic` | boolean | true | true: one entity that cannot be edited refuses the call; false: it is listed and the rest converted |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it; requires remapStyles false |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `create_issue_markup` — Mark issues up in the drawing

*destructiveHint.* Draws one marker per issue — style circle (default), rectangle or revcloud (a closed polyline of outward arcs) — at the issue's locationMm with radiusMm, or, when the issue has handles but no location, around the handles' extents (grown by 25 %) — plus a multileader reading '<issueId>: <description>' (withLeader), on a markup layer (default HP-MCP-ISSUES, created only when something is drawn; a locked/frozen layer is refused with LAYER_LOCKED / LAYER_FROZEN, an off layer warned), coloured by severity (critical red 1, warning yellow 2, info cyan 4; magenta when unknown) unless colorBySeverity is false. Each issue is drawn in the space its entities live in (space auto, default) — a paper-space text finding is clouded on its layout; an explicit space draws everything there and warns per issue whose entities live elsewhere. Pass the issue objects audit_aec_drawing / cad_standards_check / detect_geometry_issues / the structural checks returned (issueId, severity, locationMm, handles, description); up to 100 per call. A table-level finding (no location and no handles, e.g. layer_naming) is skipped with a warning, never a refusal; an issue whose handles all fail to resolve is an error (atomic: refuses the whole call, nothing drawn; atomic false: listed per item). radiusMm / textHeightMm are millimetres of the target space (sheet millimetres on a layout). The original geometry is only read for its extents — never modified. Returns the edit envelope {success, createdCount, affectedHandles, items[{index, ok, handle (marker), type, changed [leader:<handle>, space:<name>] | [skipped: …]}], summary {marked, skipped, spaces, markups[{issueId, markerHandle, leaderHandle, centerMm, radiusMm, space, sizedByHandles}]}, warnings, errors}. Side effects: adds entities (and possibly a layer) inside one undo entry 'MCP: <label>'; dryRun on the request rolls it back. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `issues` REQ | array<object> |  | Issue objects as returned by the audit tools (locationMm and/or handles required) |
| `style` | string circle|rectangle|revcloud | "circle" |  |
| `layer` | string | "HP-MCP-ISSUES" | Markup layer; created when missing |
| `radiusMm` | number | 500 | Marker radius in mm of the target space (the minimum when the marker is sized by handles) |
| `textHeightMm` | number | 150 |  |
| `withLeader` | boolean | true |  |
| `colorBySeverity` | boolean | true |  |
| `space` | string | "auto" | auto (default): each issue in its entities' space; or current, model, a layout name |
| `atomic` | boolean | true | true: an issue whose handles cannot be resolved refuses the whole call, nothing drawn; false: it is listed per item and the rest is drawn. Table-level findings without a location are skipped either way |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `detect_vn_legacy_text` — Find pre-Unicode Vietnamese text (TCVN3 / VNI)

*readOnlyHint.* Read-only. Shows what convert_vn_text_encoding would do with the same arguments: the text an old Vietnamese drawing holds in a legacy encoding — TCVN3/ABC (.VnTime, .VnArial, .VnTimeH capitals-only fonts, uv-abc.shx) or VNI-Win (VNI-Times, VNI-Avo, VNI-Helve…, vn_vni.shx) — with a Unicode preview of each, the legacy styles that would switch to a Unicode face, the ones that would be kept, and the entities that would fail (LAYER_LOCKED / LAYER_FROZEN / CELL_LOCKED) or refuse the call (NO_FONT_MAPPING). The charset comes from the font in force: the style's, or an inline \f font inside MText (Symbol runs, e.g. f = φ, and runs in Unicode fonts such as Arial are never touched). Looks at DBText, MText, attributes, attribute definitions (value + prompt), dimension text overrides, multileaders and table cells, in model space, every layout and (includeBlocks) every block definition — never xrefs, nor the *D / *T blocks AutoCAD regenerates; text with a field, multiline attributes and entities already converted (XData HP_VNCONVERT) are skipped. The rule that keeps the screen readable: with remapStyles (default) a legacy style is switched to its Unicode face only when every legacy text under it is converted in the same run — when some stay (outside the selection, in an excluded block or table, on a locked layer, carrying a field) the style is kept and its selected texts are held too. Filter with handles (an INSERT's handle selects its attributes) and/or layers (wildcards); no filter = the whole drawing. sourceCharset tcvn3 | vni forces a charset on text whose font the rules do not know (VNI bytes left under an Arial style) and needs a handles / layers filter; auto (default) never guesses. fontMap {"<legacy face or SHX file, wildcards ok>": "<Unicode TrueType face>"} chooses the face a legacy font becomes (every SHX font and VNI-* faces other than Times/Helve/Avo/Arial need one). Returns the analysis envelope {success (false when NO_FONT_MAPPING would refuse), summary {examined, convert, held, failed, byType, inBlockDefinitions, stylesRemapped[{name, font, charset, upperCaseOnly, targetFace}], stylesKept, unicodeStyles, skipped, sourceCharset}, items[{handle, type, owner (layout or block name), inBlockDefinition, layer, style, fields, field, text, preview}] (the texts to convert, first changed value, 80 characters each), count, offset, truncated, warnings, errors}; page with limit (max 40) and offset.

| arg | type | default | description |
|---|---|---|---|
| `handles` | array<string> |  | Only these entities (an INSERT selects its attributes); empty = all |
| `layers` | array<string> |  | Only entities on these layers (AutoCAD wildcards); empty = all |
| `includeBlocks` | boolean | true | Also look inside block definitions (title blocks, symbols); xrefs and *D/*T blocks never |
| `includeTables` | boolean | true | Also look at table cells |
| `sourceCharset` | string auto|tcvn3|vni | "auto" | auto: only text in fonts the rules know as legacy; tcvn3 / vni: also text in unknown fonts |
| `fontMap` | object |  | Legacy face or SHX file (wildcards) → Unicode TrueType face, e.g. {"VNI-Univer": "Arial", "vn_vni.shx": "Arial"} |
| `remapStyles` | boolean | true | Plan as convert_vn_text_encoding would with the same value |
| `limit` | integer | 40 |  |
| `offset` | integer | 0 |  |

### `manage_annotations` — Manage annotations

*destructiveHint.* Text, mtext, dimensions and multileaders, chosen by op. create (annotation {type text|mtext|dimension|mleader, …}): text/mtext {text, position, heightMm, widthMm?, rotationDeg?, style?}; dimension kind linear|aligned {p1, p2, dimLinePoint, rotationDeg?}, angular {vertex, p1, p2, arcPoint}, radial {center, chordPoint, leaderLengthMm?}, diameter {chordPoint, farChordPoint, leaderLengthMm?}, all with dimStyle? and textOverride?; mleader {text, arrowPoint, landingPoint, heightMm?}; plus layer/colorIndex/color/linetype/lineweight. update (handles[0] + set — same keys as update_entities_batch: text, heightMm, rotationDeg, style, dimStyle, textOverride, geometry.position, move…), batchUpdate (items [{handle, set}] or handles + set), delete (handles — annotation entities in model/paper space only: TEXT, MTEXT, DIMENSION, LEADER, MULTILEADER; geometry, attribute definitions and anything inside a block definition are refused with UNSUPPORTED_ENTITY). create refuses structurally (success false + LAYER_LOCKED / INVALID_ARGUMENT) when the layer or a property is wrong — nothing created. Millimetres and degrees. Returns the edit envelope {success, createdCount, modifiedCount, deletedCount, affectedHandles, items[{index, ok, handle, type, changed, error}], warnings, errors[{code, message, handle}]}. create's summary carries the dimension's measurement. Side effects: modifies the current drawing inside one undo entry 'MCP: <label>' (U reverts it); dryRun on the request rolls everything back and still reports what would change. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `op` REQ | string create|update|delete|batchUpdate |  |  |
| `annotation` | object |  | create: the annotation to add |
| `handles` | array<string> |  | Entity handles (hex, as every AEC tool reports them) |
| `items` | array<object> |  | batchUpdate: per-annotation changes |
| `set` | object |  | update / batchUpdate: the change (text, heightMm, rotationDeg, style, dimStyle, textOverride, layer, color…, geometry {position}, move {dx, dy}) |
| `space` | string | "current" | Where new entities go: current (default), model, or a layout name |
| `atomic` | boolean | true | true (default): every item is validated first (layers, styles, keys per entity type, attribute tags) and one invalid item refuses the whole batch — success false, zero counts, every refusal in items[i].error, nothing written and nothing opened for write; a failure while writing aborts the transaction. false: invalid items are listed per item and the rest is written; an item that fails midway reports what it had changed |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## Geometry

### `measure_geometry` — Measure geometry

*readOnlyHint.* Measures entities and points in millimetres: length / totalLength / perimeter of curves, area (mm² and m²) of closed shapes and hatches, centroid, bounding box (per entity and overall), direction angle of lines and the angle between two, distance between two points / two entities / a point and an entity, the closest point on an entity to a point, and the intersection points of two entities (exact AutoCAD curve maths where available). Use this when the user asks how long, how big, how far, at what angle, or where two things meet. Give handles[] and/or points[] ({x, y, z} in mm). Read-only.

| arg | type | default | description |
|---|---|---|---|
| `measure` REQ | string length|totalLength|area|perimeter|centroid|boundingBox|angle|distance|closestPoint|intersections |  | What to measure |
| `handles` | array<string> |  | Entity handles to measure (two for distance/intersections/angle-between) |
| `points` | array<object> |  | Points in mm (two for distance/angle, one for closestPoint, three+ for a polygon centroid) |
| `tolerance` | object |  | Overrides of the geometry tolerances in millimetres (angles in degrees); omitted members keep the defaults shown. |

## Audit

### `audit_aec_drawing` — Audit the drawing

*readOnlyHint.* Read-only. One report over several audit sections: geometry (duplicate, near_duplicate, overlapping_segments, tiny_segment, zero_length, open_polyline, endpoint_gap, self_intersection, invalid_geometry — every space checked on its own, so a title block repeated on two layouts is not a duplicate; capped at 2 000 findings with truncated + a warning) and standards (as cad_standards_check with ruleSet). Empty filter = every space; filter {space} alone narrows it. Issues keep their own ids (GEO-nnnn, STD-nnnn) and come back in one stable order — critical → warning → info, then category, type, handle — so paging with offset never reorders; minSeverity drops the rest (summary.belowMinSeverity counts them). summary: sections run, examined, bySeverity {critical, warning, info}, byCategory, byType, checkedStandards, tolerance. tolerance {pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap} in mm/degrees overrides the defaults. Returns the analysis envelope {success, summary, items[issue], count, offset, truncated, warnings, errors}; an issue is {issueId, category, type, severity critical|warning|info, handles[], locationMm, valueMm, description, suggestedAction, layer, rule} — pass items straight to create_issue_markup.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to examine (wildcards); empty = the whole drawing, every space |
| `sections` | array<string> |  | Which sections to run; empty = all |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (any member) |
| `ruleSet` | string |  | Standards rule set: default \| user \| <file name> |
| `minSeverity` | string critical|warning|info | "info" | Report only issues at least this severe |
| `limit` | integer | 100 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 |  |

### `cad_standards_check` — Check CAD standards

*readOnlyHint.* Read-only. Checks the drawing against a CAD standards rule set (JSON, data): layer_naming (regex with exemptions), entity_layer (text/dimension/hatch on the expected layers), layer_zero (non-block entities on layer 0), color_override / linetype_override / lineweight_override (anything but ByLayer, hatches and blocks exempt), text_style / dim_style (allowed names), text_height (allowed heights in mm per space), block_naming, unused_layer (only when the whole drawing is examined — no filter; a layer used only inside a block definition counts as used; hidden system layers and xref-dependent layers/blocks are never reported). Rules: ruleSet default (embedded, AIA/NCS-shaped and permissive) or user (%AppData%\HPAutoCad\McpServer\rules\cad-standards.json) or a plain file name there; checks[] narrows to some types. Issues are STD-nnnn in a stable order (critical → warning → info, then type, then handle text). filter {space} alone narrows the space (wholeDrawing false). A requested check the rule set does not enable is warned about. The summary lists the allowed styles/heights and the entity-layer rules so a finding's action can be resolved. Returns the analysis envelope {success, summary, items[issue], count, offset, truncated, warnings, errors}; an issue is {issueId, category, type, severity critical|warning|info, handles[], locationMm, valueMm, description, suggestedAction, layer, rule} — pass items straight to create_issue_markup. Millimetres at the boundary.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to examine (wildcards); empty = the whole drawing, every space |
| `handles` | array<string> |  | Check exactly these entities (table checks still run) |
| `ruleSet` | string |  | default \| user \| <file name in the rules folder>; omitted = user file when it exists, else default |
| `checks` | array<string> |  | Only these checks; empty = every check the rule set enables |
| `limit` | integer | 100 | Issues per page (max 100 — an issue is up to ~500 B) |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 |  |

### `detect_geometry_issues` — Detect geometry issues

*readOnlyHint.* Finds drafting defects in the selected entities and returns them as located, measured issues: exact duplicates, near duplicates, overlapping collinear segments between entities, tiny and zero-length segments, polylines that are almost closed, gaps between endpoints that were meant to meet, self-intersecting polylines, and entities whose geometry cannot be read. Each issue has an id (GEO-0001…), type, severity, the handles involved, a location in mm, the measured value, the tolerance applied and a suggested action. Use this for drawing clean-up, QA before issue, 'find double lines', 'find gaps in the walls'. Give a layer/type filter or handles; tolerances in mm can be overridden. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to consider. Types, layers and block names accept AutoCAD wildcards (* and ?) and comma lists; handles short-circuit everything else. |
| `handles` | array<string> |  | Check exactly these entities instead of a filter |
| `issueTypes` | array<string> |  | Issue kinds to look for; omit for all |
| `tolerance` | object |  | Overrides of the geometry tolerances in millimetres (angles in degrees); omitted members keep the defaults shown. |
| `limit` | integer | 100 | Maximum issues returned per call (count tells the total; about 350 bytes per issue, so 100 keeps the answer well under the 64 KB cap) |
| `maxCandidates` | integer | 5000 | Cap on entities examined |

