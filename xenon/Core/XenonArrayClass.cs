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
    
    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        // array {{t_str}, "hello, ", "world!"}
        
        if (!args.ContainsKey(1)) 
            throw ExceptionBuilder.SyntaxMissingArg(Name, "array type", "argument 1");
        string enforcedType = args[1].Read<LuaTable>()[1].Read<string>();

        LuaTable array = new();
        LuaTable meta = new()
        {
            ["__type"] = XenonRT.T_ARRAY,
            ["__arrayType"] = enforcedType,
            
            ["__newindex"] = new LuaFunction("__set", Set),
            ["__index"] = new LuaFunction("__get", Get),
            ["__len"] = new LuaFunction("__length", Length),
        };
        array.Metatable = meta;
        
        foreach (var kvp in args.Skip(1))
        {
            if (kvp.Key.TryRead(out int idx) == false)
                throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(kvp.Key), "array index");
            if (XenonRT.GetType(kvp.Value) != enforcedType)
                throw ExceptionBuilder.TypeMismatch(enforcedType, XenonRT.GetType(kvp.Value), "array type");
            
            // -1 for the type initializer, -1 for luas starting idx
            array[idx - 2] = kvp.Value;
        }
        return array;
    }
    
    public static async ValueTask<LuaValue> ArrayTransform(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        if (func.Metatable!["__returnType"].Read<string>() != array.Metatable!["__arrayType"].Read<string>())
            throw ExceptionBuilder.TypeMismatch(array.Metatable!["__arrayType"].Read<string>(),
                func.Metatable!["__returnType"].Read<string>(), "transform return");

        
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        LuaTable newArray = new()
        {
            [1] = new LuaTable { [1] = array.Metatable!["__arrayType"].Read<string>() }
        };
        foreach (var kvp in array)
        {
            LuaTable funcArgs = new()
            {
                ["item"] = kvp.Value,
                ["index"] = kvp.Key
            };
            LuaValue result = (await XenonRT.Runtime.CallAsync(innerFunc, [array, funcArgs]))[0];
            newArray[kvp.Key.Read<int>() + 2] = result;
        }
        
        // we use the array ctor to set all the proper metamethods and values
        return await new XenonArrayClass().Constructor(newArray);
    }
    public static async ValueTask<LuaValue> ArrayFilter(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        if (func.Metatable!["__returnType"].Read<string>() != XenonRT.T_BOOLEAN)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_BOOLEAN, func.Metatable!["__returnType"].Read<string>(),
                "filter return");
        
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        LuaTable newArray = new()
        {
            [1] = new LuaTable { [1] = array.Metatable!["__arrayType"].Read<string>() }
        };
        foreach (var kvp in array)
        {
            LuaTable funcArgs = new()
            {
                ["item"] = kvp.Value,
                ["index"] = kvp.Key
            };
            LuaValue result = (await XenonRT.Runtime.CallAsync(innerFunc, [array, funcArgs]))[0];
            if (result.ToBoolean()) newArray[kvp.Key.Read<int>() + 2] = kvp.Value;
        }
        
        return await new XenonArrayClass().Constructor(newArray);
    }
    public static async ValueTask<LuaValue> ArraySlice(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        int length = array.ArrayLength + 1;

        int startIdx = args[2].Read<int>();
        int endIdx = args[3].Read<int>();
        if (startIdx > length || Int32.IsNegative(startIdx))
            throw new IndexOutOfRangeException($"start index {startIdx} out of bounds of array");
        if (endIdx > length || Int32.IsNegative(endIdx))
            throw new IndexOutOfRangeException($"end index {endIdx} out of bounds of array");
        if (startIdx > endIdx)
            throw new IndexOutOfRangeException($"end index ({endIdx}) cannot be smaller than start index ({startIdx})");
        LuaTable newArray = new()
        {
            [1] = new LuaTable { [1] = array.Metatable!["__arrayType"].Read<string>() }
        };

        for (int i = startIdx; i < endIdx; i++) 
            if (array.ContainsKey(i))
                newArray.Insert(newArray.ArrayLength + 2, array[i]);
        
        return await new XenonArrayClass().Constructor(newArray);
    }
    public static async ValueTask<LuaValue> ArrayEnumerate(LuaTable args)
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
    public static async ValueTask<LuaValue> ArrayMeasure(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        return array.ArrayLength + 1;
    }
    public static async ValueTask<LuaValue> ArrayAppend(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(array.Metatable!["__arrayType"].Read<string>(), 
                XenonRT.GetType(item), "array type");

        array[array.ArrayLength + 1] = item;
        return array;
    }
    public static async ValueTask<LuaValue> ArrayPrepend(LuaTable args)
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
    public static async ValueTask<LuaValue> ArrayInsert(LuaTable args)
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
    public static async ValueTask<LuaValue> ArrayContains(LuaTable args)
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
    public static async ValueTask<LuaValue> ArrayFind(LuaTable args)
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