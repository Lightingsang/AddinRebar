#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/ModelContextProtocol.Core.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/ModelContextProtocol.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/HPRebar.Mcp.Server.Core.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/HPRebar.Mcp.Contracts.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.Hosting.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.Hosting.Abstractions.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.DependencyInjection.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.DependencyInjection.Abstractions.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.Configuration.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.Configuration.Abstractions.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.Logging.dll"
#r "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPDynamo/HPDynamo.E2E.Tests/bin/Debug/net10.0/Microsoft.Extensions.Logging.Abstractions.dll"

open System
open System.Reflection
open Microsoft.Extensions.DependencyInjection
open HPRebar.Mcp.Server.Bootstrap
open HPRebar.Mcp.Server.Hosts

let profile = HostProfile.Dynamo.WithHostAssembly(typeof<HostProfile>.Assembly)
// Let's create a builder with a dummy assembly to avoid ArgumentException
let dummyProfile = HostProfile.Dynamo.WithHostAssembly(Assembly.GetExecutingAssembly())
let builder = McpServerHost.CreateBuilder([||], dummyProfile)

printfn "Total ServiceDescriptors: %d" builder.Services.Count

let toolDescriptors =
    builder.Services
    |> Seq.filter (fun sd -> sd.ServiceType.Name.Contains("Tool"))
    |> Seq.toList

for sd in toolDescriptors do
    printfn "ServiceType: %s, ImplType: %A, ImplInstance: %A, Lifetime: %A" 
        sd.ServiceType.FullName 
        (if isNull sd.ImplementationType then "null" else sd.ImplementationType.FullName)
        (if isNull sd.ImplementationInstance then "null" else sd.ImplementationInstance.GetType().FullName)
        sd.Lifetime

let mutable removed = 0
for i = builder.Services.Count - 1 downto 0 do
    let sd = builder.Services.[i]
    if sd.ServiceType = typeof<ModelContextProtocol.Server.McpServerTool> && not (isNull sd.ImplementationFactory) then
        let target = sd.ImplementationFactory.Target
        if not (isNull target) then
            let field = target.GetType().GetField("toolMethod", BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance)
            if not (isNull field) then
                match field.GetValue(target) with
                | :? MethodInfo as mi when mi.DeclaringType.Name = "InspectTypeTool" ->
                    builder.Services.RemoveAt(i)
                    removed <- removed + 1
                | _ -> ()

printfn "Removed %d descriptors" removed

let host = builder.Build()
let tools = host.Services.GetServices<ModelContextProtocol.Server.McpServerTool>()
printfn "Remaining tools count: %d" (Seq.length tools)
for t in tools do
    printfn "Tool: %s" t.ProtocolTool.Name
