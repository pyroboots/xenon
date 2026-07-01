using Lua;
using xenon.Core;

namespace xenon.Libs;

public class XenonFsLibrary : XenonStaticClass<XenonFsLibrary>
{
    public static async ValueTask<LuaValue> Read(LuaTable args)
        => await File.ReadAllTextAsync(args[1].Read<string>());

    public static async ValueTask<LuaValue> Write(LuaTable args)
    {
        await File.WriteAllTextAsync(args[1].Read<string>(), args[2].Read<string>());
        return LuaValue.Nil;
    }

    public static async ValueTask<LuaValue> Exists(LuaTable args)
        => File.Exists(args[1].Read<string>());

    public static async ValueTask<LuaValue> List(LuaTable args)
    {
        string[] entries = Directory.GetFileSystemEntries(args[1].Read<string>());
        return XenonArrayClass.CreateArray(XenonRT.T_STRING, entries.Select(e => (LuaValue)e).ToArray());
    }

    public static async ValueTask<LuaValue> Join(LuaTable args)
        => Path.Combine(args[1].Read<string>(), args[2].Read<string>());

    public override Dictionary<string, XenonClass<XenonFsLibrary>.XenonClassMethod> Methods => new()
    {
        ["read"] = new() { Arguments = new() { [1] = ("path", XenonRT.T_STRING) }, Method = Read },
        ["write"] = new() { Arguments = new() { [1] = ("path", XenonRT.T_STRING), [2] = ("content", XenonRT.T_STRING) }, Method = Write },
        ["exists"] = new() { Arguments = new() { [1] = ("path", XenonRT.T_STRING) }, Method = Exists },
        ["list"] = new() { Arguments = new() { [1] = ("path", XenonRT.T_STRING) }, Method = List },
        ["join"] = new() { Arguments = new() { [1] = ("a", XenonRT.T_STRING), [2] = ("b", XenonRT.T_STRING) }, Method = Join },
    };

    public override Dictionary<string, LuaValue> Properties => new();
    public override string Name => "fs";
}