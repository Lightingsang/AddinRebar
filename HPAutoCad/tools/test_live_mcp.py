import sys, os, json

sys.path.insert(0, r"F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\McpShared\tools")
from harness_common import Server

exe = r"F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe"

def run_tests():
    server = Server(exe)
    server.initialize()

    print("\n--- Test: Invoke HPGeoInfoCommand.Run via Reflection ---")
    code_invoke = """
    var asmList = System.AppDomain.CurrentDomain.GetAssemblies();
    var hpGeoAsm = asmList.FirstOrDefault(a => a.GetName().Name == "HPAutoCad");
    if (hpGeoAsm == null) return new { error = "HPAutoCad assembly not loaded" };

    var cmdType = hpGeoAsm.GetType("HPAutoCad.HPGeoLink.Commands.HPGeoInfoCommand");
    if (cmdType == null) return new { error = "HPGeoInfoCommand not found" };

    var runMethod = cmdType.GetMethod("Run", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    if (runMethod == null) return new { error = "Run method not found" };

    try
    {
        runMethod.Invoke(null, null);
        return new { success = true, message = "HPGeoInfoCommand.Run() executed successfully" };
    }
    catch (System.Exception ex)
    {
        return new { success = false, error = ex.ToString() };
    }
    """
    res = server.tool("execute_autocad_code", {"code": code_invoke, "transaction": "none"})
    print("HPGeoInfo Result:", json.dumps(res, indent=2, ensure_ascii=False))

    server.close()

if __name__ == "__main__":
    run_tests()
