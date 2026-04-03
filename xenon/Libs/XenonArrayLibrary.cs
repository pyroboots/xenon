using Lua;

namespace xenon.Libs;

[LuaObject]
public partial class XenonArrayLibrary
{
    [LuaIgnoreMember]
    public static void Implement(ref LuaState state) => state.Environment["arr"] = new XenonArrayLibrary();
    
    [LuaMember("expand")]
    public static LuaTable Expand(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array["__type"].Read<string>();
        if (type != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, type);
        int sz = args[2].Read<int>();

        array["__arrayCapacity"] = array["__arrayCapacity"].Read<int>() + sz;
        return array;
    }
    
    [LuaMember("shrink")]
    public static LuaTable Shrink(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array["__type"].Read<string>();
        if (type != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, type);
        int sz = args[2].Read<int>();

        array["__arrayCapacity"] = array["__arrayCapacity"].Read<int>() - sz;
        return array;
    }
    
    [LuaMember("setCapacity")]
    public static LuaTable SetCapacity(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array["__type"].Read<string>();
        if (type != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, type);
        int capacity = args[2].Read<int>();

        array["__arrayCapacity"] = capacity;
        return array;
    }
    
    [LuaMember("enumerate")]
    public static async Task<LuaTable> Enumerate(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array["__type"].Read<string>();
        if (type != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, type);
        LuaFunction enumerator = args[2].Read<LuaFunction>();

        LuaTable accumulator = new();
        foreach (var kvp in array)
        {
            if (kvp.Key.Type != LuaValueType.Number) continue;
            // TODO: this does not work because xenon funcs take a table as args
            LuaValue[] result = await XenonRT.Runtime.CallAsync(enumerator, [kvp.Value]);
            accumulator[kvp.Key] = result[0];
        }

        return accumulator;
    }
    
    [LuaMember("last")]
    public static LuaValue Last(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array["__type"].Read<string>();
        if (type != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, type);
        int capacity = array["__arrayCapacity"].Read<int>();

        LuaValue item = array[capacity - 1];
        return item;
    }
    
    [LuaMember("first")]
    public static LuaValue First(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array["__type"].Read<string>();
        if (type != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, type);

        LuaValue item = array[0];
        return item;
    }
    
    [LuaMember("type")]
    public static LuaValue Type(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array["__type"].Read<string>();
        if (type != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, type);
        
        return array["__arrayType"];
    }
}