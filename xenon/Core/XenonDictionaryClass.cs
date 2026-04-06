using Lua;

namespace xenon.Core;

public class XenonDictionaryClass : XenonClass<XenonDictionaryClass>
{
    private static async ValueTask<int> Set(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable dict = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        LuaValue val = ctx.GetArgument(2);

        string expectedKeyType = dict.Metatable!["__keyType"].Read<string>();
        string expectedValType = dict.Metatable!["__valType"].Read<string>();
        string keyType = XenonRT.GetType(key);
        string valType = XenonRT.GetType(val);
        
        if (keyType != expectedKeyType)
            throw ExceptionBuilder.TypeMismatch(expectedKeyType, keyType, "key type");
        if (valType != expectedValType)
            throw ExceptionBuilder.TypeMismatch(expectedKeyType, keyType, "value type");

        dict[key] = val;
        return 0;
    }
    
    private static async ValueTask<int> Get(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable dict = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        
        if (dict.ContainsKey(key) == false)
            throw new KeyNotFoundException($"key {key.ToString()} not present in dictionary");
        
        ctx.Return(dict[key]);
        return 1;
    }
    
    private static async ValueTask<int> Length(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable dict = ctx.GetArgument<LuaTable>(0);
        
        ctx.Return(dict.ArrayLength + 1);
        return 1;
    }
    
    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        LuaTable typePair = args[1].Read<LuaTable>();
        string expectedKeyType = typePair[1].Read<string>();
        string expectedValType = typePair[2].Read<string>();

        LuaTable dict = new();
        dict.Metatable = new()
        {
            ["__keyType"] = expectedKeyType,
            ["__valType"] = expectedValType,
            
            ["__newindex"] = new LuaFunction("__set", Set),
            ["__index"] = new LuaFunction("__get", Get),
            ["__len"] = new LuaFunction("__length", Length),
        };

        // skip one here because of typePair
        foreach (var kvp in args.Skip(1))
        {
            string keyType = XenonRT.GetType(kvp.Key);
            string valType = XenonRT.GetType(kvp.Value);

            if (keyType != expectedKeyType)
                throw ExceptionBuilder.TypeMismatch(expectedKeyType, keyType, "key type");
            if (valType != expectedValType)
                throw ExceptionBuilder.TypeMismatch(expectedKeyType, keyType, "value type");

            dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }

    public override Dictionary<string, XenonClassMethod> Methods => new();
    public override string Name => "dict";
}