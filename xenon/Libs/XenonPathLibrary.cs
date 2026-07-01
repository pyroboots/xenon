using Lua;

namespace xenon.Libs;

public class XenonPathLibrary : XenonStaticClass<XenonPathLibrary>
{
    public static async ValueTask<LuaValue> Join(LuaTable args)
        => Path.Combine(args[1].Read<string>(), args[2].Read<string>());

    public static async ValueTask<LuaValue> BaseName(LuaTable args)
        => Path.GetFileName(args[1].Read<string>());

    public static async ValueTask<LuaValue> DirName(LuaTable args)
        => Path.GetDirectoryName(args[1].Read<string>()) ?? "";

    public static async ValueTask<LuaValue> Extension(LuaTable args)
        => Path.GetExtension(args[1].Read<string>());

    public override Dictionary<string, XenonClass<XenonPathLibrary>.XenonClassMethod> Methods => new()
    {
        ["join"] = new() { Arguments = new() { [1] = ("a", XenonRT.T_STRING), [2] = ("b", XenonRT.T_STRING) }, Method = Join },
        ["baseName"] = new() { Arguments = new() { [1] = ("path", XenonRT.T_STRING) }, Method = BaseName },
        ["dirName"] = new() { Arguments = new() { [1] = ("path", XenonRT.T_STRING) }, Method = DirName },
        ["extension"] = new() { Arguments = new() { [1] = ("path", XenonRT.T_STRING) }, Method = Extension },
    };

    public override Dictionary<string, LuaValue> Properties => new();
    public override string Name => "path";
}