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
        int capacity = arr.Metatable!["__arrayCapacity"].Read<int>();
        
        if (expectedType != type) throw ExceptionBuilder.TypeMismatch(expectedType, type, "array type");
        if (key.TryRead(out int idx) == false)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(key), "array index");
        if (idx > capacity - 1 || Int32.IsNegative(idx))
            throw new IndexOutOfRangeException($"index {idx} out of bounds of array");

        arr[idx] = val;
        return 0;
    }
    
    private static async ValueTask<int> Get(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        int capacity = arr.Metatable!["__arrayCapacity"].Read<int>();

        if (key.TryRead(out int idx) == false)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(key), "array index");
        if (idx > capacity - 1 || Int32.IsNegative(idx))
            throw new IndexOutOfRangeException($"index {idx} out of bounds of array");
        
        ctx.Return(arr[idx]);
        return 1;
    }
    
    private static async ValueTask<int> Length(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        
        ctx.Return(arr.Metatable!["__arrayCapacity"].Read<int>());
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
        
        int len = 0;
        foreach (var kvp in args.Skip(1))
        {
            if (kvp.Key.TryRead(out int idx) == false)
                throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(kvp.Key), "array index");
            if (XenonRT.GetType(kvp.Value) != enforcedType)
                throw ExceptionBuilder.TypeMismatch(enforcedType, XenonRT.GetType(kvp.Value), "array type");
            
            // -1 for the type initializer, -1 for luas starting idx
            array[idx - 2] = kvp.Value;
            len++;
        }
        
        array.Metatable["__arrayCapacity"] = len;
        return array;
    }
    
    public static async ValueTask<LuaValue> ArrayShrink(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        if (XenonRT.GetType(array) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(array));
        int capacity = array.Metatable!["__arrayCapacity"].Read<int>();
        int decrease = args[2].Read<int>();
        
        array.Metatable!["__arrayCapacity"] = capacity - decrease;
        return capacity - decrease;
    }
    
    public static async ValueTask<LuaValue> ArrayExpand(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        if (XenonRT.GetType(array) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(array));
        int capacity = array.Metatable!["__arrayCapacity"].Read<int>();
        int increase = args[2].Read<int>();
        
        array.Metatable!["__arrayCapacity"] = capacity + increase;
        return capacity + increase;
    }
    
    public static async ValueTask<LuaValue> ArrayTransform(LuaTable args)
    {   
        LuaTable array = args[1].Read<LuaTable>();
        if (XenonRT.GetType(array) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(array));
        
        LuaTable func = args[2].Read<LuaTable>();
        if (XenonRT.GetType(func) != XenonRT.T_FUNCTION)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_FUNCTION, XenonRT.GetType(func));
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
        if (XenonRT.GetType(array) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(array));
        
        LuaTable func = args[2].Read<LuaTable>();
        if (XenonRT.GetType(func) != XenonRT.T_FUNCTION)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_FUNCTION, XenonRT.GetType(func));
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
        if (XenonRT.GetType(array) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(array));
        int capacity = array.Metatable!["__arrayCapacity"].Read<int>();

        int startIdx = args[2].Read<int>();
        int endIdx = args[3].Read<int>();
        if (startIdx > capacity || Int32.IsNegative(startIdx))
            throw new IndexOutOfRangeException($"start index {startIdx} out of bounds of array");
        if (endIdx > capacity || Int32.IsNegative(endIdx))
            throw new IndexOutOfRangeException($"end index {endIdx} out of bounds of array");
        if (startIdx > endIdx)
            throw new IndexOutOfRangeException($"end index ({endIdx}) cannot be smaller than start index ({startIdx})");
        LuaTable newArray = new()
        {
            [1] = new LuaTable { [1] = array.Metatable!["__arrayType"].Read<string>() }
        };

        for (int i = startIdx; i < endIdx; i++) 
            if (array.ContainsKey(i))
                newArray.Insert(newArray.ArrayLength + 1, array[i]);
        
        return await new XenonArrayClass().Constructor(newArray);
    }
    
    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["shrink"] = new()
        {
            Arguments = new()
            {
                [1] = "array",
                [2] = "amount",
            },
            Method = ArrayShrink,
        },
        ["expand"] = new()
        {
            Arguments = new()
            {
                [1] = "array",
                [2] = "amount",
            },
            Method = ArrayExpand,
        },
        ["transform"] = new()
        {
            Arguments = new()
            {
                [1] = "array",
                [2] = "transformer",
            },
            Method = ArrayTransform,
        },
        ["filter"] = new()
        {
            Arguments = new()
            {
                [1] = "array",
                [2] = "predicate",
            },
            Method = ArrayFilter,
        },
    };
    public override string Name => "array";
}