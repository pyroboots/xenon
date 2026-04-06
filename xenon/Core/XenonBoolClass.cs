using Lua;

namespace xenon.Core;

public class XenonBoolClass : XenonClass<XenonBoolClass>
{
    public async override ValueTask<LuaValue> Constructor(LuaTable args) => args[0].ToBoolean();

    public static async ValueTask<LuaValue> And(LuaTable args)
        => args[1].Read<bool>() && args[2].Read<bool>();
    public static async ValueTask<LuaValue> Or(LuaTable args)
        => args[1].Read<bool>() || args[2].Read<bool>();
    public static async ValueTask<LuaValue> Not(LuaTable args)
        => !args[1].Read<bool>();
    public static async ValueTask<LuaValue> Nand(LuaTable args)
        => !(args[1].Read<bool>() && args[2].Read<bool>());
    public static async ValueTask<LuaValue> Nor(LuaTable args)
        => !(args[1].Read<bool>() || args[2].Read<bool>());
    public static async ValueTask<LuaValue> Xor(LuaTable args)
        => args[1].Read<bool>() != args[2].Read<bool>();
    public static async ValueTask<LuaValue> Xnor(LuaTable args)
        => args[1].Read<bool>() == args[2].Read<bool>();
    
    public static async ValueTask<LuaValue> Ternary(LuaTable args)
        => args[1].Read<bool>() ? args[2] : args[3];
    public static async ValueTask<LuaValue> Whichever(LuaTable args)
    {
        bool condition = args[1].Read<bool>();
        LuaTable trueFunc = args[2].Read<LuaTable>();
        LuaTable falseFunc = args[3].Read<LuaTable>();
        
        if (condition) 
            return (await XenonRT.Runtime.CallAsync(trueFunc.Metatable!["__call"].Read<LuaFunction>(), []))[0];
        else
            return (await XenonRT.Runtime.CallAsync(falseFunc.Metatable!["__call"].Read<LuaFunction>(), []))[0];
    }
    
    public static async ValueTask<LuaValue> FromNum(LuaTable args)
        => args[1].Read<double>() != 0;
    public static async ValueTask<LuaValue> ToNum(LuaTable args)
        => args[1].Read<bool>() ? 1 : 0;

    public static async ValueTask<LuaValue> Any(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        if (XenonRT.GetType(array) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(array));
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.T_BOOLEAN)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_BOOLEAN, array.Metatable!["__arrayType"].Read<string>(), "array type");

        bool result = false;
        foreach (var kvp in array)
            if (kvp.Value.Read<bool>()) result = true;
        
        return result;
    }
    public static async ValueTask<LuaValue> All(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        if (XenonRT.GetType(array) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(array));
        if (array.Metatable!["__arrayType"].Read<string>() != XenonRT.T_BOOLEAN)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_BOOLEAN, array.Metatable!["__arrayType"].Read<string>(), "array type");

        bool result = true;
        foreach (var kvp in array)
            if (kvp.Value.Read<bool>() == false) result = false;
        
        return result;
    }

    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["and"] = new()
        {
            Arguments = new()
            {
                [1] = ("a", XenonRT.T_BOOLEAN),
                [2] = ("b", XenonRT.T_BOOLEAN),
            },
            Method = And,
        },
        ["or"] = new()
        {
            Arguments = new()
            {
                [1] = ("a", XenonRT.T_BOOLEAN),
                [2] = ("b", XenonRT.T_BOOLEAN),
            },
            Method = Or,
        },
        ["not"] = new()
        {
            Arguments = new()
            {
                [1] = ("a", XenonRT.T_BOOLEAN),
            },
            Method = Not,
        },
        ["nand"] = new()
        {
            Arguments = new()
            {
                [1] = ("a", XenonRT.T_BOOLEAN),
                [2] = ("b", XenonRT.T_BOOLEAN),
            },
            Method = Nand,
        },
        ["nor"] = new()
        {
            Arguments = new()
            {
                [1] = ("a", XenonRT.T_BOOLEAN),
                [2] = ("b", XenonRT.T_BOOLEAN),
            },
            Method = Nor,
        },
        ["xnor"] = new()
        {
            Arguments = new()
            {
                [1] = ("a", XenonRT.T_BOOLEAN),
                [2] = ("b", XenonRT.T_BOOLEAN),
            },
            Method = Xnor,
        },
        ["ternary"] = new()
        {
            Arguments = new()
            {
                [1] = ("condition", XenonRT.T_BOOLEAN),
                [2] = ("trueValue", XenonRT.T_ANY),
                [3] = ("falseValue",  XenonRT.T_ANY),
            },
            Method = Ternary,
        },
        ["whichever"] = new()
        {
            Arguments = new()
            {
                [1] = ("condition", XenonRT.T_BOOLEAN),
                [2] = ("trueFunc", XenonRT.T_FUNCTION),
                [3] = ("falseFunc", XenonRT.T_FUNCTION),
            },
            Method = Whichever,
        },
        ["any"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
            },
            Method = Any,
        },
        ["all"] = new()
        {
            Arguments = new()
            {
                [1] = ("array", XenonRT.T_ARRAY),
            },
            Method = All,
        },
        ["asNum"] = new()
        {
            Arguments = new()
            {
                [1] = ("bit", XenonRT.T_BOOLEAN),
            },
            Method = ToNum,
        },
        ["fromNum"] = new()
        {
            Arguments = new()
            {
                [1] = ("bit", XenonRT.T_BOOLEAN),
            },
            Method = FromNum,
        },
    };
    public override string Name => "bool";
}