using Lua;

namespace xenon.Core;

public class XenonTypeClass : XenonClass<XenonTypeClass>
{
    private const string PROPERTY_TYPE_KEY = "__xenon_typemap";
    private const string PROPERTY_READONLY_KEY = "__xenon_readonlymap";
    private const string VALUE_PROXY_KEY = "__xenon_vals";
    
    private static async ValueTask<int> TypeConstructor(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        LuaTable args = ctx.GetArgument<LuaTable>(1);
        LuaTable typeMap = tbl[PROPERTY_TYPE_KEY].Read<LuaTable>();
        LuaTable valProxy = new();

        foreach (var kvp in args)
        {
            string k = kvp.Key.Read<string>();
            LuaValue v = kvp.Value;
            
            string expectedType = typeMap[k].Read<string>();
            string actualType = XenonRT.GetType(v);
            if (expectedType != actualType)
                throw ExceptionBuilder.TypeMismatch(expectedType, actualType);
            else valProxy[k] = v;
        }

        tbl[VALUE_PROXY_KEY] = valProxy;
        ctx.Return(tbl);
        
        return 1;
    }
    
    private static async ValueTask<int> TypeGet(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        LuaTable valProxy = tbl[VALUE_PROXY_KEY].Read<LuaTable>();
        LuaValue key = ctx.GetArgument(1);

        if (valProxy[key].Type == LuaValueType.Nil)
            throw ExceptionBuilder.MissingMember(key.Read<string>(), tbl.Metatable!["__type"].Read<string>());
        else ctx.Return(valProxy[key]);
        return 1;
    }
    
    private static async ValueTask<int> TypeSet(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        LuaTable readonlyMap = tbl[PROPERTY_READONLY_KEY].Read<LuaTable>();
        LuaTable valProxy = tbl[VALUE_PROXY_KEY].Read<LuaTable>();
        LuaTable typeMap = tbl[PROPERTY_TYPE_KEY].Read<LuaTable>();
        LuaValue key = ctx.GetArgument(1);
        LuaValue val = ctx.GetArgument(2);

        if (readonlyMap[key].ToBoolean())
            throw ExceptionBuilder.ReadOnlyProperty(key.Read<string>(), tbl.Metatable!["__type"].Read<string>());
        if (typeMap.ContainsKey(key) == false)
            throw ExceptionBuilder.MissingMember(key.Read<string>(), tbl.Metatable!["__type"].Read<string>());
        valProxy[key] = val;
        return 0;
    }
    
    private static async ValueTask<int> TypeLength(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);

        ctx.Return(tbl.GetArrayMemory().Length);
        return 1;
    }
    
    private static async ValueTask<int> TypeToString(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);

        ctx.Return(tbl.Metatable!["__type"].Read<string>());
        return 1;
    }
    
    
    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        if (!args.ContainsKey(1)) 
            throw ExceptionBuilder.SyntaxMissingArg(Name, "type name", "argument 1");
        string name = args[1].Read<string>();
        XenonRT.RegisterType(name);
        
        LuaTable type = new();
        LuaTable meta = new()
        {
            ["__call"] = new LuaFunction($"{name}__ctor", TypeConstructor),
            ["__index"] = new LuaFunction($"{name}__get", TypeGet),
            ["__newindex"] = new LuaFunction($"{name}__set", TypeSet),
            ["__len"] = new LuaFunction($"{name}__length", TypeLength),
            ["__tostring"] = new LuaFunction($"{name}__string", TypeToString),
            
            ["__type"] = name,
        };
        LuaTable propertyTypeMap = new();
        LuaTable propertyReadonlyMap = new();
        
        foreach (var kvp in args.Skip(1))
        {
            string propName = kvp.Key.Read<string>();
            LuaValueType valType = kvp.Value.Type;

            if (valType == LuaValueType.Table)
            {
                LuaTable valCfg = kvp.Value.Read<LuaTable>();
                if (valCfg["const"].Type == LuaValueType.Boolean) propertyReadonlyMap[propName] = valCfg["const"];
                if (valCfg["type"].Type == LuaValueType.String) propertyTypeMap[propName] = valCfg["type"];
            }
            else if (valType == LuaValueType.String)
                propertyTypeMap[propName] = kvp.Value.Read<string>();
            else if (valType == LuaValueType.Function)
            {
                // TODO: implement type methods
            }
        }

        type.Metatable = meta;
        type[PROPERTY_TYPE_KEY] = propertyTypeMap;
        type[PROPERTY_READONLY_KEY] = propertyReadonlyMap;
        return type;
    }

    public override Dictionary<string, XenonClassMethod> Methods => new();
    public override string Name => "type";
}