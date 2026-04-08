using Lua;

namespace xenon.Core;

public class XenonArrayClass : XenonClass<XenonArrayClass>
{
    private static async ValueTask<int> Set(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        LuaValue val = ctx.GetArgument(2);
        string expectedType = arr.Metatable!["__arrayType"].Read<string>();
        string type = XenonRT.GetType(val);
        
        if (expectedType != type) throw ExceptionBuilder.TypeMismatch(expectedType, type, "array type");
        if (key.TryRead(out int idx) == false)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(key), "array index");
        if (Int32.IsNegative(idx))
            throw new IndexOutOfRangeException($"index {idx} out of bounds of array");

        arr[idx] = val;
        return 0;
    }
    
    private static async ValueTask<int> Get(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        int length = arr.ArrayLength;

        if (key.TryRead(out int idx) == false)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(key), "array index");
        if (idx > length || Int32.IsNegative(idx))
            throw new IndexOutOfRangeException($"index {idx} out of bounds of array");
        
        ctx.Return(arr[idx]);
        return 1;
    }
    
    private static async ValueTask<int> Length(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        
        ctx.Return(arr.ArrayLength + 1);
        return 1;
    }
    
    public static LuaTable CreateArray(string type, params LuaValue[] items)
    {
        LuaTable array = new();
        LuaTable meta = new()
        {
            ["__type"] = XenonRT.T_ARRAY,
            ["__arrayType"] = type,
            
            ["__newindex"] = new LuaFunction("__set", Set),
            ["__index"] = new LuaFunction("__get", Get),
            ["__len"] = new LuaFunction("__length", Length),
        };
        array.Metatable = meta;

        for (int i = 0; i < items.Length - 1; i++)
        {
            if (XenonRT.GetType(items[i]) != type)
                throw ExceptionBuilder.TypeMismatch(type, XenonRT.GetType(items[i]), "array type");
            array[i] = items[i];
        }
        return array;
    }
    
    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        // array {{t_str}, "hello, ", "world!"}
        string type = args[1].Read<string>();
        List<LuaValue> items = new();
        foreach (var kvp in args.Skip(1))
            items.Add(kvp.Value);
        
        return CreateArray(type, items.ToArray());
    }
    
    private static async ValueTask<LuaValue> ArrayTransform(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (func.Metatable!["__returnType"].Read<string>() != type)
            throw ExceptionBuilder.TypeMismatch(type,
                func.Metatable!["__returnType"].Read<string>(), "transform return");
        
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        List<LuaValue> newArray = new();
        foreach (var kvp in array)
        {
            LuaTable funcArgs = new()
            {
                ["item"] = kvp.Value,
                ["index"] = kvp.Key
            };
            LuaValue result = (await XenonRT.Runtime.CallAsync(innerFunc, [array, funcArgs]))[0];
            newArray.Add(result);
        }
        
        return CreateArray(type, newArray.ToArray());
    }
    private static async ValueTask<LuaValue> ArrayFilter(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (func.Metatable!["__returnType"].Read<string>() != XenonRT.T_BOOLEAN)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_BOOLEAN, func.Metatable!["__returnType"].Read<string>(),
                "filter return");
        
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        List<LuaValue> newArray = new();
        foreach (var kvp in array)
        {
            LuaTable funcArgs = new()
            {
                ["item"] = kvp.Value,
                ["index"] = kvp.Key
            };
            LuaValue result = (await XenonRT.Runtime.CallAsync(innerFunc, [array, funcArgs]))[0];
            if (result.ToBoolean()) newArray.Add(kvp.Value);
        }
        
        return CreateArray(type, newArray.ToArray());
    }
    private static async ValueTask<LuaValue> ArraySlice(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        int length = array.ArrayLength + 1;
        string type = array.Metatable!["__arrayType"].Read<string>();

        int startIdx = args[2].Read<int>();
        int endIdx = args[3].Read<int>();
        if (startIdx > length || Int32.IsNegative(startIdx))
            throw new IndexOutOfRangeException($"start index {startIdx} out of bounds of array");
        if (endIdx > length || Int32.IsNegative(endIdx))
            throw new IndexOutOfRangeException($"end index {endIdx} out of bounds of array");
        if (startIdx > endIdx)
            throw new IndexOutOfRangeException($"end index ({endIdx}) cannot be smaller than start index ({startIdx})");
        List<LuaValue> newArray = new();

        for (int i = startIdx; i < endIdx; i++) 
            if (array.ContainsKey(i))
                newArray.Add(array[i]);
        
        return CreateArray(type, newArray.ToArray());
    }
    private static async ValueTask<LuaValue> ArrayEnumerate(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        for (int i = 0; i < array.ArrayLength + 1; i++)
        {
            LuaTable funcArgs = new()
            {
                ["item"] = array[i],
                ["index"] = i,
            };
            await XenonRT.Runtime.CallAsync(innerFunc, [array, funcArgs]);
        }
        
        return LuaValue.Nil;
    }
    private static async ValueTask<LuaValue> ArrayMeasure(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        return array.ArrayLength + 1;
    }
    private static async ValueTask<LuaValue> ArrayAppend(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(array.Metatable!["__arrayType"].Read<string>(), 
                XenonRT.GetType(item), "array type");

        array[array.ArrayLength + 1] = item;
        return array;
    }
    private static async ValueTask<LuaValue> ArrayPrepend(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(array.Metatable!["__arrayType"].Read<string>(), 
                XenonRT.GetType(item), "array type");
        
        LuaTable result = new();
        result[0] = item;
        
        foreach (var kvp in array)
            result[kvp.Key.Read<int>() + 1] = kvp.Value;
        
        result.Metatable = array.Metatable;
        return result;
    }
    private static async ValueTask<LuaValue> ArrayInsert(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(array.Metatable!["__arrayType"].Read<string>(), 
                XenonRT.GetType(item), "array type");

        int idx = args[3].Read<int>();
        if (idx > array.ArrayLength || Int32.IsNegative(idx))
            throw new IndexOutOfRangeException($"index {idx} out of bounds of array");
        
        LuaTable result = new();
        foreach (var kvp in array)
            if (kvp.Key.Read<int>() >= idx)
                result[kvp.Key.Read<int>() + 1] = kvp.Value;
            else
                result[kvp.Key.Read<int>()] = kvp.Value;
        result[idx] = item;
        
        result.Metatable = array.Metatable;
        return result;
    }
    private static async ValueTask<LuaValue> ArrayContains(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(array.Metatable!["__arrayType"].Read<string>(), 
                XenonRT.GetType(item), "array type");

        foreach (var kvp in array)
            if (kvp.Value == item) return true;
        return false;
    }
    private static async ValueTask<LuaValue> ArrayFind(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(array.Metatable!["__arrayType"].Read<string>(), 
                XenonRT.GetType(item), "array type");

        foreach (var kvp in array)
            if (kvp.Value == item) return kvp.Key;
        return LuaValue.Nil;
    }
    
    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["transform"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("transformer", XenonRT.T_FUNCTION),
            },
            Method = ArrayTransform,
        },
        ["filter"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("predicate", XenonRT.T_FUNCTION),
            },
            Method = ArrayFilter,
        },
        ["slice"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("start",  XenonRT.T_NUMBER),
                [3] = ("end", XenonRT.T_NUMBER),
            },
            Method = ArraySlice,
        },
        ["enumerate"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("enumerator", XenonRT.T_FUNCTION),
            },
            Method = ArrayEnumerate,
        },
        ["measure"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY)
            },
            Method = ArrayMeasure,
        },
        ["append"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("item", XenonRT.T_ANY),
            },
            Method = ArrayAppend,
        },
        ["prepend"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("item", XenonRT.T_ANY)
            },
            Method = ArrayPrepend,
        },
        ["insert"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("item", XenonRT.T_ANY),
                [3] = ("index", XenonRT.T_NUMBER),
            },
            Method = ArrayInsert,
        },
        ["contains"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("item", XenonRT.T_ANY),
            },
            Method = ArrayContains,
        },
        ["find"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
                [2] = ("item", XenonRT.T_ANY)
            },
            Method = ArrayFind,
        },
    };
    public override string Name => "array";
}