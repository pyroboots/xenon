using System.Text;
using Lua;

namespace xenon.Core;

public class XenonStringClass : XenonClass<XenonStringClass>
{
    public override async ValueTask<LuaValue> Constructor(LuaTable args) => args[0].ToString();
    
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
        LuaTable arr = args[2].Read<LuaTable>();
        
        StringBuilder str = new();
        for (int i = 0; i < arr.ArrayLength + 1; i++)
        {
            str.Append(arr[i].ToString());
            str.Append(sep);
        }
        str.Remove(str.Length - sep.Length, sep.Length);

        return str.ToString();
    }
    public static async ValueTask<LuaValue> Measure(LuaTable args) 
        => args[1].Read<string>().Length;
    public static async ValueTask<LuaValue> Substring(LuaTable args) 
        => args[1].Read<string>().Substring(args[2].Read<int>(), args[3].Read<int>());
    public static async ValueTask<LuaValue> StartsWith(LuaTable args) 
        => args[1].Read<string>().StartsWith(args[2].Read<string>());
    public static async ValueTask<LuaValue> EndsWith(LuaTable args) 
        => args[1].Read<string>().StartsWith(args[2].Read<string>());
    public static async ValueTask<LuaValue> CharAt(LuaTable args) 
        => args[1].Read<string>()[args[2].Read<int>()].ToString();
    public static async ValueTask<LuaValue> Upper(LuaTable args) 
        => args[1].Read<string>().ToUpper();
    public static async ValueTask<LuaValue> Lower(LuaTable args) 
        => args[1].Read<string>().ToLower();

    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["split"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
                [2] = ("delimiter", XenonRT.T_STRING),
            },
            Method = Split,
        },
        ["trim"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
            },
            Method = Trim,
        },
        ["contains"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
                [2] = ("substring", XenonRT.T_STRING), 
            },
            Method = Contains,
        },
        ["replace"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
                [2] = ("pattern", XenonRT.T_STRING), 
                [3] = ("replacement", XenonRT.T_STRING), 
            },
            Method = Replace,
        },
        ["join"] = new()
        {
            Arguments = new()
            {
                [1] = ("separator", XenonRT.T_STRING), 
                [2] = ("array", XenonRT.T_ARRAY), 
            },
            Method = Join,
        },
        ["measure"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
            },
            Method = Measure,
        },
        ["substring"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
                [2] = ("start", XenonRT.T_NUMBER), 
                [3] = ("end", XenonRT.T_NUMBER), 
            },
            Method = Substring,
        },
        ["startsWith"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
                [2] = ("term", XenonRT.T_STRING), 
            },
            Method = StartsWith,
        },
        ["endsWith"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
                [2] = ("term", XenonRT.T_STRING), 
            },
            Method = EndsWith,
        },
        ["charAt"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
                [2] = ("index", XenonRT.T_NUMBER), 
            },
            Method = CharAt,
        },
        ["upper"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
            },
            Method = Upper,
        },
        ["lower"] = new()
        {
            Arguments = new()
            {
                [1] = ("string", XenonRT.T_STRING), 
            },
            Method = Lower,
        },
    };
    public override string Name => "string";
}