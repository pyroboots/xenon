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
        string expectedKeyType = dict.Metatable!["__keyType"].Read<string>();
        string keyType = XenonRT.GetType(key);
        
        if (expectedKeyType != keyType) 
            throw ExceptionBuilder.TypeMismatch(expectedKeyType, keyType, "dict index");
        
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

    public static LuaTable CreateDict(string keyType, string valType, Dictionary<LuaValue, LuaValue> items)
    {
        LuaTable dict = new();
        dict.Metatable = new()
        {
            ["__keyType"] = keyType,
            ["__valType"] = valType,
            
            ["__newindex"] = new LuaFunction("__set", Set),
            ["__index"] = new LuaFunction("__get", Get),
            ["__len"] = new LuaFunction("__length", Length),
        };
        
        foreach (var kvp in items)
        {
            if (keyType != XenonRT.GetType(kvp.Key))
                throw ExceptionBuilder.TypeMismatch(keyType, XenonRT.GetType(kvp.Key), "key type");
            if (valType != XenonRT.GetType(kvp.Value))
                throw ExceptionBuilder.TypeMismatch(valType, XenonRT.GetType(kvp.Value), "value type");

            dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }
    
    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        LuaTable typePair = args[1].Read<LuaTable>();
        string expectedKeyType = typePair[1].Read<string>();
        string expectedValType = typePair[2].Read<string>();

        Dictionary<LuaValue, LuaValue> items = new();
        // skip 1 for typePair
        foreach (var kvp in args.Skip(1))
            items.Add(kvp.Key, kvp.Value);

        return CreateDict(expectedKeyType, expectedValType, items);
    }
    
    private static async ValueTask<LuaValue> DictFilter(LuaTable args)
    {
        LuaTable dict = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        if (func.Metatable!["__returnType"].Read<string>() != XenonRT.T_BOOLEAN)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_BOOLEAN, func.Metatable!["__returnType"].Read<string>(),
                "filter return");
        
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        Dictionary<LuaValue, LuaValue> newDict = new();
        foreach (var kvp in dict)
        {
            LuaTable funcArgs = new()
            {
                ["key"] = kvp.Key,
                ["value"] = kvp.Value
            };
            LuaValue result = (await XenonRT.Runtime.CallAsync(innerFunc, [dict, funcArgs]))[0];
            if (result.ToBoolean()) newDict[kvp.Key] = kvp.Value;
        }

        string kType = dict.Metatable!["__keyType"].Read<string>();
        string vType = dict.Metatable!["__keyType"].Read<string>();
        return CreateDict(kType, vType, newDict);
    }
    private static async ValueTask<LuaValue> DictEnumerate(LuaTable args)
    {   
        LuaTable dict = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        foreach (var kvp in dict)
        {
            LuaTable funcArgs = new()
            {
                ["key"] = kvp.Key,
                ["value"] = kvp.Value,
            };
            await XenonRT.Runtime.CallAsync(innerFunc, [dict, funcArgs]);
        }
        
        return LuaValue.Nil;
    }
    private static async ValueTask<LuaValue> DictMeasure(LuaTable args)
    {   
        LuaTable dict = args[1].Read<LuaTable>();
        return dict.ArrayLength + 1;
    }
    private static async ValueTask<LuaValue> DictAdd(LuaTable args)
    {   
        LuaTable dict = args[1].Read<LuaTable>();
        LuaValue k = args[2];
        if (XenonRT.GetType(k) != dict.Metatable!["__keyType"].Read<string>())
            throw ExceptionBuilder.TypeMismatch(dict.Metatable!["__keyType"].Read<string>(), 
                XenonRT.GetType(k), 
                "key type");
        LuaValue v = args[3];
        if (XenonRT.GetType(v) != dict.Metatable!["__valType"].Read<string>())
            throw ExceptionBuilder.TypeMismatch(dict.Metatable!["__valType"].Read<string>(), 
                XenonRT.GetType(v), 
                "val type");

        dict[k] = v;
        return dict;
    }
    private static async ValueTask<LuaValue> DictRemove(LuaTable args)
    {   
        LuaTable dict = args[1].Read<LuaTable>();
        LuaValue k = args[2];
        if (XenonRT.GetType(k) != dict.Metatable!["__keyType"].Read<string>())
            throw ExceptionBuilder.TypeMismatch(dict.Metatable!["__keyType"].Read<string>(), 
                XenonRT.GetType(k), 
                "key type");

        dict[k] = LuaValue.Nil;
        return dict;
    }
    private static async ValueTask<LuaValue> DictContains(LuaTable args)
    {   
        LuaTable dict = args[1].Read<LuaTable>();
        LuaValue k = args[2];
        if (XenonRT.GetType(k) != dict.Metatable!["__keyType"].Read<string>())
            throw ExceptionBuilder.TypeMismatch(dict.Metatable!["__keyType"].Read<string>(), 
                XenonRT.GetType(k), 
                "key type");
        
        return dict.ContainsKey(k);
    }
    private static async ValueTask<LuaValue> DictFind(LuaTable args)
    {   
        LuaTable dict = args[1].Read<LuaTable>();
        LuaValue v = args[2];
        if (XenonRT.GetType(v) != dict.Metatable!["__valType"].Read<string>())
            throw ExceptionBuilder.TypeMismatch(dict.Metatable!["__valType"].Read<string>(), 
                XenonRT.GetType(v), 
                "value type");

        foreach (var kvp in dict)
            if (kvp.Value == v)
                return kvp.Key;
        
        return LuaValue.Nil;
    }

    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["filter"] = new()
        {
            Arguments = new()
            {
                [1] = ("dictionary", XenonRT.T_DICTIONARY),
                [2] = ("predicate", XenonRT.T_FUNCTION)
            },
            Method = DictFilter,
        },
        ["enumerate"] = new()
        {
            Arguments = new()
            {
                [1] = ("dictionary", XenonRT.T_DICTIONARY),
                [2] = ("enumerator", XenonRT.T_FUNCTION)
            },
            Method = DictEnumerate,
        },
        ["measure"] = new()
        {
            Arguments = new()
            {
                [1] = ("dictionary", XenonRT.T_DICTIONARY)
            },
            Method = DictMeasure,
        },
        ["add"] = new()
        {
            Arguments = new()
            {
                [1] = ("dictionary", XenonRT.T_DICTIONARY),
                [2] = ("key", XenonRT.T_ANY),
                [3] = ("value", XenonRT.T_ANY),
            },
            Method = DictAdd,
        },
        ["remove"] = new()
        {
            Arguments = new()
            {
                [1] = ("dictionary", XenonRT.T_DICTIONARY),
                [2] = ("key", XenonRT.T_ANY),
            },
            Method = DictRemove,
        },
        ["contains"] = new()
        {
            Arguments = new()
            {
                [1] = ("dictionary", XenonRT.T_DICTIONARY),
                [2] = ("key", XenonRT.T_ANY),
            },
            Method = DictContains,
        },
        ["find"] = new()
        {
            Arguments = new()
            {
                [1] = ("dictionary", XenonRT.T_DICTIONARY),
                [2] = ("value", XenonRT.T_ANY),
            },
            Method = DictFind,
        }
    };
    public override string Name => "dict";
}