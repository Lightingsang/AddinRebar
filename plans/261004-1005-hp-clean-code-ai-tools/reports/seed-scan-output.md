| Host | code.cs | R-B1 | R-B2 | R-B3 | R-W1 | R-W2 | R-W3 | R-W4 | R-W5 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| HPRebar | 21 | 0 | 0 | 0 | 3 | 1 | 1 | 2 | 0 |
| HPAutoCad | 50 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| HPCivil3d | 12 | 0 | 0 | 0 | 0 | 12 | 0 | 0 | 14 |
| HPNavis | 12 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 |
| HPEtabs | 12 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 |
| HPSap2000 | 12 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 |
| HPRobot | 12 | 0 | 0 | 0 | 0 | 2 | 7 | 0 | 0 |
| HPExcel | 12 | 0 | 2 | 0 | 0 | 2 | 2 | 0 | 0 |
| HPPowerBi | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| HPTekla | 12 | 0 | 0 | 0 | 0 | 1 | 3 | 0 | 0 |
| **Total** | 155 | 0 | 2 | 0 | 3 | 18 | 18 | 2 | 14 |

### Files with blocking hits

| File | Rule | Lines |
|---|---|---|
| HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/read_table/code.cs | R-B2 | 13, 25 |

### Top 10 largest code.cs

| Lines | File |
|---:|---|
| 137 | HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Generic/create_line_based_element/code.cs |
| 102 | HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Data/ai_element_filter/code.cs |
| 101 | HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Architecture/create_point_based_element/code.cs |
| 99 | HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Annotation/create_dimensions/code.cs |
| 93 | HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Architecture/create_surface_based_element/code.cs |
| 88 | HPSap2000/HPSap2000.Mcp.Server/Registry/SeedLibrary/Geometry/get_structural_objects/code.cs |
| 88 | HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/View/operate_element/code.cs |
| 86 | HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/read_table/code.cs |
| 77 | HPEtabs/HPEtabs.Mcp.Server/Registry/SeedLibrary/Geometry/get_structural_objects/code.cs |
| 76 | HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Pipe/list_pipe_networks/code.cs |

### All hits

- R-W4 HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Architecture/create_level/code.cs: 28
- R-W2 HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Architecture/create_point_based_element/code.cs: 70
- R-W4 HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Data/ai_element_filter/code.cs: 39
- R-W3 HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Data/get_material_quantities/code.cs: 16
- R-W1 HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/Generic/create_line_based_element/code.cs: 62, 46
- R-W1 HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/View/operate_element/code.cs: 25
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Alignment/get_alignment_geometry/code.cs: 50
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Alignment/list_alignments/code.cs: 36
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Corridor/list_corridors/code.cs: 29
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Document/get_civil_document_info/code.cs: 15, 15
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Parcel/list_parcels/code.cs: 31
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Pipe/list_pipe_networks/code.cs: 48, 61, 67, 73
- R-W2 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Pipe/list_pipe_networks/code.cs: 37, 39, 42, 44, 48, 51, 53, 56, 58, 61
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Point/list_cogo_points/code.cs: 47
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Profile/list_profiles/code.cs: 44
- R-W5 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Surface/list_surfaces/code.cs: 27, 37
- R-W2 HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/Surface/list_surfaces/code.cs: 26, 27
- R-W3 HPNavis/HPNavis.Mcp.Server/Registry/SeedLibrary/Report/summarize_by_category/code.cs: 23
- R-W3 HPEtabs/HPEtabs.Mcp.Server/Registry/SeedLibrary/Geometry/draw_frame_by_coords/code.cs: 1, 2
- R-W3 HPSap2000/HPSap2000.Mcp.Server/Registry/SeedLibrary/Geometry/draw_frame_by_coords/code.cs: 1, 2
- R-W3 HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Geometry/draw_bar_by_coords/code.cs: 1, 4
- R-W3 HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Geometry/get_structural_objects/code.cs: 29, 49
- R-W2 HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Load/get_load_definitions/code.cs: 15, 17
- R-W3 HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Property/get_materials_and_sections/code.cs: 12, 32
- R-W3 HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Results/get_bar_forces/code.cs: 11
- R-W3 HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Automation/run_macro/code.cs: 21
- R-B2 HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/read_table/code.cs: 13, 25
- R-W2 HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/write_range/code.cs: 34, 37
- R-W3 HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/write_range/code.cs: 35
- R-W3 HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Geometry/create_beam/code.cs: 1, 2
- R-W3 HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Rebar/create_single_rebar/code.cs: 14
- R-W2 HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Rebar/get_reinforcement_info/code.cs: 29
