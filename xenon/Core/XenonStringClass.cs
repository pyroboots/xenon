using System.Text;
using Lua;

namespace xenon.Core;

public class XenonStringClass : XenonClass<XenonStringClass>
{
    public override async ValueTask<LuaValue> Constructor(LuaTable args)
    {
        StringBuilder str = new();

        foreach (var kvp in args)
            str.Append(kvp.Value.ToString());

        return str.ToString();
    }
    
    public static async ValueTask<LuaValue> Split(LuaTable args)
    {   
        string str = args[1].Read<string>();
        string delim = args[2].Read<string>();

        LuaTable arr = new()
        {
            [1] = new LuaTable { [1] = XenonRT.T_STRING }
        };

        string[] result;
        if (delim == "") result = str.Split();
        else result = str.Split(delim);

        for (int i = 0; i < result.Length; i++)
            arr[i + 2] = result[i];

        return await new XenonArrayClass().Constructor(arr);
    }
    
    public static async ValueTask<LuaValue> Trim(LuaTable args) 
        => args[1].Read<string>().Trim();
    public static async ValueTask<LuaValue> Contains(LuaTable args) 
        => args[1].Read<string>().Contains(args[2].Read<string>());
    public static async ValueTask<LuaValue> Replace(LuaTable args) 
        => args[1].Read<string>().Replace(args[2].Read<string>(), args[3].Read<string>());

    public static async ValueTask<LuaValue> Join(LuaTable args)
    {
        string sep = args[1].Read<string>();
        LuaTable arr = args[1].Read<LuaTable>();
        if (XenonRT.GetType(arr) != XenonRT.T_ARRAY)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_ARRAY, XenonRT.GetType(arr), "argument 2");
        
        StringBuilder str = new();
        foreach (var kvp in arr)
        {
            str.Append(kvp.Value.ToString());
            str.Append(sep);
        }
        str.Remove(str.Length - sep.Length, sep.Length);

        return str.ToString();
    }

    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["split"] = new()
        {
            Arguments = new()
            {
                [1] = "string",
                [2] = "delimiter"
            },
            Method = Split,
        },
        ["trim"] = new()
        {
            Arguments = new()
            {
                [1] = "string",
            },
            Method = Trim,
        },
        ["contains"] = new()
        {
            Arguments = new()
            {
                [1] = "string",
                [2] = "substring"
            },
            Method = Contains,
        },
        ["replace"] = new()
        {
            Arguments = new()
            {
                [1] = "string",
                [2] = "pattern",
                [3] = "replacement"
            },
            Method = Replace,
        },
        ["join"] = new()
        {
            Arguments = new()
            {
                [1] = "separator",
                [2] = "array"
            },
            Method = Contains,
        },
    };
    public override string Name => "string";
}