namespace HPBim.Cli;

using System;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parserResult = CliParser.Parse(args);
        var formatter = new ConsoleFormatter();

        try
        {
            return parserResult.Command switch
            {
                "validate" => await new ValidateCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate" => await new GenerateCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "verify" => await new VerifyCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "estimate" => await new EstimateCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "extract-cad" => await new ExtractCadCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-cad" => await new QueryCadCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-footings" => await new QueryCadFootingsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-elevations" => await new QueryElevationsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-foundation" => await new GenerateFoundationCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-piles" => await new GeneratePilesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-revit-grids" => await new QueryRevitGridsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "cross-verify-grids" => await new CrossVerifyGridsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "check-grid-spans" => await new CheckGridSpansCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-building-levels" => await new QueryBuildingLevelsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "match-floors" => await new QueryStoryElevationsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-levels" => await new GenerateBuildingLevelsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "extend-grids" => await new ExtendGridsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "extend-levels" => await new ExtendLevelsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "configure-elevation-bubbles" => await new ConfigureElevationBubblesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-revit" => await new InspectRevitModelCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-piles" => await new InspectPilesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "convert-piles-to-square" => await new ConvertPilesToSquareCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "extract-footings" => await new ExtractFootingsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "rename-level-types" => await new RenameLevelTypesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "rename-symbols" => await new RenameSymbolsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "rename-grid-types" => await new RenameGridTypesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-floor-types" => await new InspectFoundationSlabsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "analyze-footings" => await new AnalyzeFootingsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-slabs" => await new GenerateFoundationSlabsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "verify-slabs" => await new VerifyFoundationSlabsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-footing-details" => await new QueryFootingDetailsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-sections-detailed" => await new QueryFootingSectionsDetailedCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "update-footing-elevations" => await new UpdateFootingElevationsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "extract-grade-beams" => await new ExtractGradeBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "extract-grade-beam-lines" => await new ExtractGradeBeamLinesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "analyze-grade-beams" => await new AnalyzeGradeBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "build-grade-beam-network" => await new BuildGradeBeamNetworkCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-beam-types" => await new InspectBeamTypesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-footing-dims" => await new InspectFootingDimensionsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-layers-footings" => await new InspectCadLayersAndFootingsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-true-footings" => await new QueryTrueFootingPolylinesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-pile-area" => await new InspectPileAreaFootingsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-foundation-grids" => await new InspectFoundationPlanCoordinatesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "extract-true-foundation" => await new ExtractTrueFoundationElementsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-beam-details" => await new InspectGradeBeamDetailsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "catalog-sheets" => await new InspectCadDrawingSheetsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-plana-footings" => await new InspectFootingLayersOnPlanACommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-plana-stats" => await new InspectPlanAAllEntitiesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-bao-dk-block" => await new InspectBaoDkBlockCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "scan-blocks" => await new ScanAllBlockDefinitionsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-plana-lines" => await new QueryPlanALinesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-sections-geo" => await new InspectFootingSectionsGeoCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-grade-beams" => await new GenerateGradeBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "verify-grade-beams" => await new VerifyGradeBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "audit-foundation" => await new AuditFoundationCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "remediate-foundation" => await new RemediateFoundationCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-cad-columns" => await new InspectCadColumnsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-column-types" => await new InspectColumnTypesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "prepare-columns" => await new PrepareColumnsModelCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-columns" => await new GenerateColumnsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "audit-columns" => await new AuditColumnsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-cad-elevator" => await new InspectCadElevatorCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-wall-types" => await new InspectWallTypesCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "prepare-elevator-walls" => await new PrepareElevatorWallsModelCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-elevator-walls" => await new GenerateElevatorWallsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "audit-elevator-walls" => await new AuditElevatorWallsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-story2-framing" => await new InspectCadStory2FramingCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-story2-beam-details" => await new InspectCadStory2BeamDetailsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "realign-elevations" => await new RealignElevationsAndElementsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "audit-levels-and-elements" => await new AuditLevelsAndElementsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-beam-params" => await new InspectBeamParametersCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "inspect-da-kieng-sheet" => await new InspectStory1DaKiengSheetCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "survey-da-kieng-plan" => await new SurveyDaKiengFloorPlanCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "detect-cad-beams" => await new Story1CadBeamDetector(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "audit-spans" => await new Story1SpanAuditService(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "assemble-complete-beams" => await new AssembleCompleteStory1BeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "prepare-story1-grade-beams" => await new PrepareStory1GradeBeamsModelCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "generate-story1-grade-beams" => await new GenerateStory1GradeBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "audit-story1-grade-beams" => await new AuditStory1GradeBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "purge-story1-beams" => await new PurgeStory1BeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "export-da-kieng-dwg" => await new ExportDaKiengDwgCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "link-da-kieng-cad" => await new LinkDaKiengCadCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "query-imported-cad" => await new QueryImportedCadBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "cancel-revit" => await new CancelRevitExecutionCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "coord" => await new CoordinationCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "simulate-beams" => await new SimulateBeamsCommand(formatter).ExecuteAsync(parserResult).ConfigureAwait(false),
                "help" or "--help" or "-h" or "" => PrintHelp(formatter),
                _ => PrintUnknown(formatter, parserResult.Command)
            };
        }
        catch (Exception ex)
        {
            formatter.WriteError($"Lỗi thực thi nghiêm trọng: {ex.Message}");
            return 99;
        }
    }

    private static int PrintHelp(ConsoleFormatter formatter)
    {
        formatter.WriteBanner("Help & Usage", "Công cụ dòng lệnh BIM tự động cho Revit 2026");
        Console.WriteLine("""
        Sử dụng: hpbim <command> [options]

        Các lệnh hỗ trợ:
          extract-cad [--output <path>]          Trích xuất 100% C# tọa độ tim cọc & lưới trục từ AutoCAD qua Named Pipe.
          generate-foundation [--dry-run]        Tạo 27 Lưới Trục (1..13, A..N) & Level Móng trong Revit 2026 qua Named Pipe.
          query-revit-grids [--output <path>]    Truy vấn tọa độ lưới trục & cao độ từ Revit 2026 qua Named Pipe.
          cross-verify-grids [--report <path>]   Đối soát chéo 27 trục & cao độ móng giữa CAD và Revit (dung sai <= 1mm).
          validate  --input <path>               Soát lỗi quy chuẩn TCVN, khoảng hở và va chạm offline.
          generate  --input <path> [--dry-run]   Dispatch tạo dựng mô hình 3D bản địa vào Revit 2026 qua Named Pipe.
          verify    --input <path>               Đối soát 1:1 số lượng phần tử trong Revit so với schema thiết kế.
          estimate  --input <path> [--output <path>]  Bóc tách dự toán khối lượng (BOQ) 4 phần theo chuẩn TCVN.
          coord [init|status|check|reconcile]    Quản trị Sổ cái điều phối, FailFastGate và tự động đối soát phục hồi.

        Tùy chọn chung:
          --pipe <name>                          Tên Windows Named Pipe (mặc định: hpautocad-mcp-2026 cho CAD, hprebar-mcp-r2026 cho Revit).
          --help, -h                             Hiển thị bảng trợ giúp này.
        """);
        return 0;
    }

    private static int PrintUnknown(ConsoleFormatter formatter, string command)
    {
        formatter.WriteError($"Lệnh '{command}' không được nhận diện.");
        PrintHelp(formatter);
        return 1;
    }
}
