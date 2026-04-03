using Lua;

namespace xenon;

public class XenonArray
{
    public static async ValueTask<int> Set(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        LuaValue val = ctx.GetArgument(2);
        string expectedType = arr["__arrayType"].Read<string>();
        string type = XenonRT.GetType(val);
        int capacity = arr["__arrayCapacity"].Read<int>();
        
        if (expectedType != type) throw ExceptionBuilder.TypeMismatch(expectedType, type, "array type");
        if (key.TryRead(out int idx) == false)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(key), "array index");
        if (idx > capacity - 1 || Int32.IsNegative(idx))
            throw new IndexOutOfRangeException($"index {idx} out of bounds of array");

        arr[idx] = val;
        return 0;
    }
    
    public static async ValueTask<int> Get(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        int capacity = arr["__arrayCapacity"].Read<int>();

        if (key.TryRead(out int idx) == false)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(key), "array index");
        if (idx > capacity - 1 || Int32.IsNegative(idx))
            throw new IndexOutOfRangeException($"index {idx} out of bounds of array");
        
        ctx.Return(arr[idx]);
        return 1;
    }
    
    public static async ValueTask<int> Length(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        
        ctx.Return(arr.ArrayLength - 2);
        return 1;
    }
    
    public static LuaFunction ArrayCtor = new(async (ctx, ct) =>
    {
        // array {{t_str}, "hello, ", "world!"}
        
        LuaTable arguments = ctx.Arguments[0].Read<LuaTable>();
        string enforcedType = arguments[1].Read<string>();

        LuaTable array = new()
        {
            ["__type"] = XenonRT.T_ARRAY,
            ["__arrayType"] = enforcedType,
        };
        LuaTable meta = new()
        {
            ["__newindex"] = new LuaFunction("__set", Set),
            ["__index"] = new LuaFunction("__get", Get),
            ["__len"] = new LuaFunction("__length", Length),
        };
        array.Metatable = meta;
        
        int len = 0;
        foreach (var kvp in arguments.Skip(1))
        {
            if (kvp.Key.TryRead(out int idx) == false)
                throw ExceptionBuilder.TypeMismatch(XenonRT.T_NUMBER, XenonRT.GetType(kvp.Key), "array index");
            if (XenonRT.GetType(kvp.Value) != enforcedType)
                throw ExceptionBuilder.TypeMismatch(enforcedType, XenonRT.GetType(kvp.Value), "array type");
            
            // -1 for the type initializer, -1 for luas starting idx
            array[idx - 2] = kvp.Value;
            len++;
        }
        
        array["__arrayCapacity"] = len;
        ctx.Return(array);
        return 1;
    });
}