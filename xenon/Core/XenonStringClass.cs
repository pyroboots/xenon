using System.Text;
using System.Text.RegularExpressions;
using Lua;

namespace xenon.Core;

public class XenonStringClass : XenonClass<XenonStringClass>
{
    public override async ValueTask<LuaValue> Constructor(LuaTable args) => args[0].ToString();

    public static async ValueTask<LuaValue> Split(LuaTable args)
    {
        string str = args[1].Read<string>();
        string delim = args[2].Read<string>();
        LuaTable arr = new() { [1] = new LuaTable { [1] = XenonRT.T_STRING } };
        string[] result = delim == "" ? str.Split() : str.Split(delim);
        for (int i = 0; i < result.Length; i++)
            arr[i + 2] = result[i];
        return await new XenonArrayClass().Constructor(arr);
    }

    public static async ValueTask<LuaValue> Trim(LuaTable args) => args[1].Read<string>().Trim();
    public static async ValueTask<LuaValue> Contains(LuaTable args) => args[1].Read<string>().Contains(args[2].Read<string>());
    public static async ValueTask<LuaValue> Replace(LuaTable args) => args[1].Read<string>().Replace(args[2].Read<string>(), args[3].Read<string>());
    public static async ValueTask<LuaValue> ReplaceAll(LuaTable args) => args[1].Read<string>().Replace(args[2].Read<string>(), args[3].Read<string>());

    public static async ValueTask<LuaValue> Join(LuaTable args)
    {
        string sep = args[1].Read<string>();
        LuaTable arr = args[2].Read<LuaTable>();
        StringBuilder str = new();
        bool first = true;
        foreach (LuaValue item in XenonArrayUtil.Values(arr))
        {
            if (!first) str.Append(sep);
            str.Append(item.ToString());
            first = false;
        }
        return str.ToString();
    }

    public static async ValueTask<LuaValue> Measure(LuaTable args) => args[1].Read<string>().Length;
    public static async ValueTask<LuaValue> Substring(LuaTable args) => args[1].Read<string>().Substring(args[2].Read<int>(), args[3].Read<int>());
    public static async ValueTask<LuaValue> StartsWith(LuaTable args) => args[1].Read<string>().StartsWith(args[2].Read<string>());
    public static async ValueTask<LuaValue> EndsWith(LuaTable args) => args[1].Read<string>().EndsWith(args[2].Read<string>());
    public static async ValueTask<LuaValue> CharAt(LuaTable args) => args[1].Read<string>()[args[2].Read<int>()].ToString();
    public static async ValueTask<LuaValue> Upper(LuaTable args) => args[1].Read<string>().ToUpper();
    public static async ValueTask<LuaValue> Lower(LuaTable args) => args[1].Read<string>().ToLower();
    public static async ValueTask<LuaValue> IsEmpty(LuaTable args) => args[1].Read<string>().Length == 0;
    public static async ValueTask<LuaValue> IsBlank(LuaTable args) => string.IsNullOrWhiteSpace(args[1].Read<string>());

    public static async ValueTask<LuaValue> Repeat(LuaTable args)
    {
        string s = args[1].Read<string>();
        int count = args[2].Read<int>();
        return string.Concat(Enumerable.Repeat(s, count));
    }

    public static async ValueTask<LuaValue> Match(LuaTable args)
    {
        string input = args[1].Read<string>();
        string pattern = args[2].Read<string>();
        Match match = Regex.Match(input, pattern);
        if (!match.Success)
            return LuaValue.Nil;
        return match.Groups.Count > 1 && match.Groups[1].Success
            ? match.Groups[1].Value
            : match.Value;
    }

    public static async ValueTask<LuaValue> Test(LuaTable args)
        => Regex.IsMatch(args[1].Read<string>(), args[2].Read<string>());

    public static async ValueTask<LuaValue> Format(LuaTable args)
    {
        string template = args[1].Read<string>();
        if (args.ContainsKey(2) && args[2].Type == LuaValueType.Table)
        {
            foreach (var kvp in args[2].Read<LuaTable>())
                template = template.Replace($"{{{kvp.Key.Read<string>()}}}", kvp.Value.ToString());
        }
        return template;
    }

    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["split"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("delimiter", XenonRT.T_STRING) }, Method = Split },
        ["trim"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING) }, Method = Trim },
        ["contains"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("substring", XenonRT.T_STRING) }, Method = Contains },
        ["replace"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("pattern", XenonRT.T_STRING), [3] = ("replacement", XenonRT.T_STRING) }, Method = Replace },
        ["replaceAll"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("pattern", XenonRT.T_STRING), [3] = ("replacement", XenonRT.T_STRING) }, Method = ReplaceAll },
        ["join"] = new() { Arguments = new() { [1] = ("separator", XenonRT.T_STRING), [2] = ("array", XenonRT.T_ARRAY) }, Method = Join },
        ["measure"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING) }, Method = Measure },
        ["substring"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("start", XenonRT.T_NUMBER), [3] = ("end", XenonRT.T_NUMBER) }, Method = Substring },
        ["startsWith"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("term", XenonRT.T_STRING) }, Method = StartsWith },
        ["endsWith"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("term", XenonRT.T_STRING) }, Method = EndsWith },
        ["charAt"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("index", XenonRT.T_NUMBER) }, Method = CharAt },
        ["upper"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING) }, Method = Upper },
        ["lower"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING) }, Method = Lower },
        ["isEmpty"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING) }, Method = IsEmpty },
        ["isBlank"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING) }, Method = IsBlank },
        ["repeat"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("count", XenonRT.T_NUMBER) }, Method = Repeat },
        ["format"] = new() { Arguments = new() { [1] = ("template", XenonRT.T_STRING), [2] = ("vars", XenonRT.T_ANY) }, Method = Format },
        ["match"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("pattern", XenonRT.T_STRING) }, Method = Match },
        ["test"] = new() { Arguments = new() { [1] = ("string", XenonRT.T_STRING), [2] = ("pattern", XenonRT.T_STRING) }, Method = Test },
    };
    public override string Name => "str";
}