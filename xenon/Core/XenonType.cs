using System.Data;
using Lua;

namespace xenon;

public class XenonType
{
    private const string PROPERTY_TYPE_KEY = "__xenon_typemap";
    private const string PROPERTY_READONLY_KEY = "__xenon_readonlymap";
    private const string VALUE_PROXY_KEY = "__xenon_vals";
    private const string TYPE_TYPE_KEY = "__type";
    
    private static async ValueTask<int> Constructor(LuaFunctionExecutionContext ctx, CancellationToken ct)
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
    
    private static async ValueTask<int> Get(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        LuaTable valProxy = tbl[VALUE_PROXY_KEY].Read<LuaTable>();
        LuaValue key = ctx.GetArgument(1);

        if (valProxy[key].Type == LuaValueType.Nil)
            throw ExceptionBuilder.MissingProperty(key.Read<string>(), tbl[TYPE_TYPE_KEY].Read<string>());
        else ctx.Return(valProxy[key]);
        return 1;
    }
    
    private static async ValueTask<int> Set(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        LuaTable readonlyMap = tbl[PROPERTY_READONLY_KEY].Read<LuaTable>();
        LuaTable valProxy = tbl[VALUE_PROXY_KEY].Read<LuaTable>();
        LuaTable typeMap = tbl[PROPERTY_TYPE_KEY].Read<LuaTable>();
        LuaValue key = ctx.GetArgument(1);
        LuaValue val = ctx.GetArgument(2);

        if (readonlyMap[key].ToBoolean())
            throw ExceptionBuilder.ReadOnlyProperty(key.Read<string>(), tbl[TYPE_TYPE_KEY].Read<string>());
        if (typeMap.ContainsKey(key) == false)
            throw ExceptionBuilder.MissingProperty(key.Read<string>(), tbl[TYPE_TYPE_KEY].Read<string>());
        valProxy[key] = val;
        return 0;
    }
    
    private static async ValueTask<int> Length(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        LuaTable types = tbl[PROPERTY_TYPE_KEY].Read<LuaTable>();

        ctx.Return(types.ArrayLength);
        return 1;
    }
    
    private static async ValueTask<int> ToString(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);

        ctx.Return(tbl[TYPE_TYPE_KEY].Read<string>());
        return 1;
    }
    
    public static LuaTable CreateType(LuaTable t)
    {
        LuaTable type = new();
        
        string name = t[1].Read<string>();
        type[TYPE_TYPE_KEY] = name;
        XenonRT.RegisterType(name);
        
        LuaTable meta = new()
        {
            ["__call"] = new LuaFunction($"{name}__ctor", Constructor),
            ["__index"] = new LuaFunction($"{name}__get", Get),
            ["__newindex"] = new LuaFunction($"{name}__set", Set),
            ["__len"] = new LuaFunction($"{name}__length", Length),
            ["__tostring"] = new LuaFunction($"{name}__string", ToString),
        };
        LuaTable propertyTypeMap = new();
        LuaTable propertyReadonlyMap = new();
        
        foreach (var kvp in t.Skip(1))
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
                // TODO
            }
        }

        type.Metatable = meta;
        type[PROPERTY_TYPE_KEY] = propertyTypeMap;
        type[PROPERTY_READONLY_KEY] = propertyReadonlyMap;
        return type;
    }

    public static LuaFunction TypeCtor = new(async (ctx, ct) =>
    {
        LuaTable arguments = ctx.Arguments[0].Read<LuaTable>();
        return ctx.Return(XenonType.CreateType(arguments));
    });
}