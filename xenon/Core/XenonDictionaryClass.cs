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
        
            if (keyType != expectedKeyType && expectedKeyType != XenonRT.T_ANY)
                throw ExceptionBuilder.TypeMismatch(expectedKeyType, keyType, "key type");
            if (valType != expectedValType && expectedValType != XenonRT.T_ANY)
                throw ExceptionBuilder.TypeMismatch(expectedValType, valType, "value type");

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
        ctx.Return(XenonDictUtil.Count(ctx.GetArgument<LuaTable>(0)));
        return 1;
    }

    public static LuaTable CreateDict(string keyType, string valType, Dictionary<LuaValue, LuaValue> items)
    {
        LuaTable dict = new();
        dict.Metatable = new()
        {
            ["__type"] = XenonRT.T_DICTIONARY,
            ["__keyType"] = keyType,
            ["__valType"] = valType,
            
            ["__newindex"] = new LuaFunction("__set", Set),
            ["__index"] = new LuaFunction("__get", Get),
            ["__len"] = new LuaFunction("__length", Length),
        };
        
        foreach (var kvp in items)
        {
            if (keyType != XenonRT.T_ANY && XenonRT.GetType(kvp.Key) != keyType)
                throw ExceptionBuilder.TypeMismatch(keyType, XenonRT.GetType(kvp.Key), "key type");
            if (valType != XenonRT.T_ANY && XenonRT.GetType(kvp.Value) != valType)
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
            LuaValue result = (await XenonRT.Runtime.CallAsync(innerFunc, [func, funcArgs]))[0];
            if (result.ToBoolean()) newDict[kvp.Key] = kvp.Value;
        }

        string kType = dict.Metatable!["__keyType"].Read<string>();
        string vType = dict.Metatable!["__valType"].Read<string>();
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
            await XenonRT.Runtime.CallAsync(innerFunc, [func, funcArgs]);
        }
        
        return LuaValue.Nil;
    }
    private static async ValueTask<LuaValue> DictMeasure(LuaTable args)
        => XenonDictUtil.Count(args[1].Read<LuaTable>());
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

    private static async ValueTask<LuaValue> DictKeys(LuaTable args)
    {
        LuaTable dict = args[1].Read<LuaTable>();
        string keyType = dict.Metatable!["__keyType"].Read<string>();
        List<LuaValue> keys = new();
        foreach (var kvp in dict) keys.Add(kvp.Key);
        return XenonArrayClass.CreateArray(keyType, keys.ToArray());
    }

    private static async ValueTask<LuaValue> DictValues(LuaTable args)
    {
        LuaTable dict = args[1].Read<LuaTable>();
        string valType = dict.Metatable!["__valType"].Read<string>();
        List<LuaValue> values = new();
        foreach (var kvp in dict) values.Add(kvp.Value);
        return XenonArrayClass.CreateArray(valType, values.ToArray());
    }

    private static async ValueTask<LuaValue> DictMerge(LuaTable args)
    {
        LuaTable a = args[1].Read<LuaTable>();
        LuaTable b = args[2].Read<LuaTable>();
        string keyType = a.Metatable!["__keyType"].Read<string>();
        string valType = a.Metatable!["__valType"].Read<string>();
        Dictionary<LuaValue, LuaValue> merged = new();
        foreach (var kvp in a) merged[kvp.Key] = kvp.Value;
        foreach (var kvp in b) merged[kvp.Key] = kvp.Value;
        return CreateDict(keyType, valType, merged);
    }

    private static async ValueTask<LuaValue> DictGetOr(LuaTable args)
    {
        LuaTable dict = args[1].Read<LuaTable>();
        LuaValue key = args[2];
        LuaValue fallback = args[3];
        return dict.ContainsKey(key) ? dict[key] : fallback;
    }

    private static async ValueTask<LuaValue> DictUpdate(LuaTable args)
    {
        LuaTable dict = args[1].Read<LuaTable>();
        LuaTable other = args[2].Read<LuaTable>();
        foreach (var kvp in other)
            dict[kvp.Key] = kvp.Value;
        return dict;
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
        },
        ["keys"] = new()
        {
            Arguments = new() { [1] = ("dictionary", XenonRT.T_DICTIONARY) },
            Method = DictKeys,
        },
        ["values"] = new()
        {
            Arguments = new() { [1] = ("dictionary", XenonRT.T_DICTIONARY) },
            Method = DictValues,
        },
        ["merge"] = new()
        {
            Arguments = new() { [1] = ("a", XenonRT.T_DICTIONARY), [2] = ("b", XenonRT.T_DICTIONARY) },
            Method = DictMerge,
        },
        ["getOr"] = new()
        {
            Arguments = new() { [1] = ("dictionary", XenonRT.T_DICTIONARY), [2] = ("key", XenonRT.T_ANY), [3] = ("fallback", XenonRT.T_ANY) },
            Method = DictGetOr,
        },
        ["update"] = new()
        {
            Arguments = new() { [1] = ("dictionary", XenonRT.T_DICTIONARY), [2] = ("other", XenonRT.T_DICTIONARY) },
            Method = DictUpdate,
        },
    };
    public override string Name => "dict";
}