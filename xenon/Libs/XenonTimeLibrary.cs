using Lua;

namespace xenon.Libs;

public class XenonTimeLibrary : XenonStaticClass<XenonTimeLibrary>
{
    public static async ValueTask<LuaValue> Now(LuaTable args)
        => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static async ValueTask<LuaValue> Sleep(LuaTable args)
    {
        await Task.Delay(args[1].Read<int>());
        return LuaValue.Nil;
    }

    public static async ValueTask<LuaValue> Format(LuaTable args)
    {
        long unix = (long)args[1].Read<double>();
        string pattern = args[2].Read<string>();
        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        return dt.ToString(pattern);
    }

    public override Dictionary<string, XenonClass<XenonTimeLibrary>.XenonClassMethod> Methods => new()
    {
        ["now"] = new() { Arguments = new(), Method = Now },
        ["sleep"] = new() { Arguments = new() { [1] = ("ms", XenonRT.T_NUMBER) }, Method = Sleep },
        ["format"] = new() { Arguments = new() { [1] = ("timestamp", XenonRT.T_NUMBER), [2] = ("pattern", XenonRT.T_STRING) }, Method = Format },
    };

    public override Dictionary<string, LuaValue> Properties => new();
    public override string Name => "time";
}