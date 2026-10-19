namespace HPGrasshopper.McpBridge.Tests.Fakes;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;

/// <summary>
///     DispatchProxy implementation for IGH_Param test doubles.
/// </summary>
public class TestParamProxy : DispatchProxy
{
    public string NickName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid InstanceGuid { get; set; } = Guid.NewGuid();
    public Guid ComponentGuid { get; set; } = Guid.NewGuid();
    public string TypeName { get; set; } = "Number";
    public string Category { get; set; } = "Params";
    public string SubCategory { get; set; } = "Primitive";
    public bool Optional { get; set; }
    public int? VolatileDataCountOverride { get; set; }
    public List<IGH_Goo> VolatileDataAdded { get; } = new();
    public List<IGH_Param> Sources { get; } = new();
    public List<IGH_Param> Recipients { get; } = new();
    public IGH_Attributes? Attributes { get; set; }
    public bool AddVolatileDataReturnValue { get; set; } = true;

    public int VolatileDataCount => VolatileDataCountOverride ?? VolatileDataAdded.Count;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) return null;
        var name = targetMethod.Name;

        if (name == "ClearData")
        {
            VolatileDataAdded.Clear();
            return null;
        }

        if (name == "AddVolatileData")
        {
            if (AddVolatileDataReturnValue && args != null && args.Length >= 3 && args[2] is IGH_Goo goo)
            {
                VolatileDataAdded.Add(goo);
            }
            return AddVolatileDataReturnValue;
        }

        if (TryHandleDataOrSource(name, args)) return true;
        if (TryGetProperty(name, out var propVal)) return propVal;

        if (name == "get_VolatileData")
        {
            var tree = new GH_Structure<IGH_Goo>();
            foreach (var item in VolatileDataAdded) tree.Append(item);
            return tree;
        }

        if (name == "Write")
        {
            if (args != null && args.Length >= 1 && args[0] is GH_IWriter writer)
            {
                writer.SetString("Name", Name);
                writer.SetGuid("InstanceGuid", InstanceGuid);
            }
            return true;
        }

        if (name == "Read") return true;

        if (targetMethod.ReturnType == typeof(void)) return null;

        return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
    }

    private bool TryHandleDataOrSource(string name, object?[]? args)
    {
        if (name == "AddSource")
        {
            if (args != null && args.Length >= 1 && args[0] is IGH_Param src && !Sources.Contains(src))
            {
                Sources.Add(src);
            }
            return true;
        }

        if (name == "RemoveSource")
        {
            if (args != null && args.Length >= 1 && args[0] is IGH_Param rmSrc)
            {
                Sources.Remove(rmSrc);
            }
            return true;
        }

        return false;
    }

    private bool TryGetProperty(string name, out object? value)
    {
        switch (name)
        {
            case "get_NickName": value = NickName; return true;
            case "get_Name": value = Name; return true;
            case "get_InstanceGuid": value = InstanceGuid; return true;
            case "get_ComponentGuid": value = ComponentGuid; return true;
            case "get_TypeName": value = TypeName; return true;
            case "get_Category": value = Category; return true;
            case "get_SubCategory": value = SubCategory; return true;
            case "get_Optional": value = Optional; return true;
            case "get_VolatileDataCount": value = VolatileDataCount; return true;
            case "get_Sources": value = Sources; return true;
            case "get_Recipients": value = Recipients; return true;
            case "get_Attributes": value = Attributes; return true;
            default: value = null; return false;
        }
    }
}

/// <summary>
///     DispatchProxy implementation for IGH_Component test doubles.
/// </summary>
public class TestComponentProxy : DispatchProxy
{
    public string NickName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid InstanceGuid { get; set; } = Guid.NewGuid();
    public Guid ComponentGuid { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = "Math";
    public string SubCategory { get; set; } = "Operators";
    public GH_SolutionPhase Phase { get; set; } = GH_SolutionPhase.Blank;
    public List<IGH_Param> Inputs { get; } = new();
    public List<IGH_Param> Outputs { get; } = new();
    public List<string> ErrorMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public IGH_Attributes? Attributes { get; set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) return null;
        var name = targetMethod.Name;

        if (name == "get_NickName") return NickName;
        if (name == "get_Name") return Name;
        if (name == "get_InstanceGuid") return InstanceGuid;
        if (name == "get_ComponentGuid") return ComponentGuid;
        if (name == "get_Category") return Category;
        if (name == "get_SubCategory") return SubCategory;
        if (name == "get_Phase") return Phase;
        if (name == "get_Attributes") return Attributes;

        if (name == "RuntimeMessages" && args != null && args.Length >= 1 && args[0] is GH_RuntimeMessageLevel level)
        {
            if (level == GH_RuntimeMessageLevel.Error) return ErrorMessages;
            if (level == GH_RuntimeMessageLevel.Warning) return WarningMessages;
            return new List<string>();
        }

        if (name == "get_Params")
        {
            return TestProxyFactory.CreateParamServer(Inputs, Outputs);
        }

        return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
    }
}

/// <summary>
///     Factory for creating Grasshopper test double proxies.
/// </summary>
public static class TestProxyFactory
{
    private static readonly ConditionalWeakTable<object, object> Proxies = new();

    public static IGH_Param CreateParam(
        string nickName,
        string name = "TestParam",
        int? volatileDataCount = null,
        Guid? instanceGuid = null)
    {
        var param = DispatchProxy.Create<IGH_Param, TestParamProxy>();
        var proxy = (TestParamProxy)(object)param;
        proxy.NickName = nickName;
        proxy.Name = name;
        proxy.VolatileDataCountOverride = volatileDataCount;
        if (instanceGuid.HasValue) proxy.InstanceGuid = instanceGuid.Value;

        Proxies.Add(param, proxy);
        return param;
    }

    public static IGH_Component CreateComponent(
        string name = "Addition",
        string nickName = "+",
        string category = "Math",
        string subCategory = "Operators")
    {
        var comp = DispatchProxy.Create<IGH_Component, TestComponentProxy>();
        var proxy = (TestComponentProxy)(object)comp;
        proxy.Name = name;
        proxy.NickName = nickName;
        proxy.Category = category;
        proxy.SubCategory = subCategory;

        Proxies.Add(comp, proxy);
        return comp;
    }

    public static TProxy GetProxy<TProxy>(object instance) where TProxy : class
    {
        if (Proxies.TryGetValue(instance, out var proxy) && proxy is TProxy typed) return typed;
        return (TProxy)instance;
    }

    internal static GH_ComponentParamServer CreateParamServer(List<IGH_Param> inputs, List<IGH_Param> outputs)
    {
        var type = typeof(GH_ComponentParamServer);
        var server = (GH_ComponentParamServer)RuntimeHelpers.GetUninitializedObject(type);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var listFields = new List<FieldInfo>();
        foreach (var f in fields)
        {
            if (f.FieldType == typeof(List<IGH_Param>)) listFields.Add(f);
        }
        if (listFields.Count >= 2)
        {
            listFields[0].SetValue(server, inputs);
            listFields[1].SetValue(server, outputs);
        }
        return server;
    }
}
