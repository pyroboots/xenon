using Lua;
using xenon.Core;

namespace xenon.Libs;

public class XenonEnumLibrary : XenonStaticClass<XenonEnumLibrary>
{
    private const string ENUM_VALUE_KEY = "__enumValue";

    public static async ValueTask<LuaValue> ToInt(LuaTable args)
    {
        LuaTable val = args[1].Read<LuaTable>();
        return val.Metatable![ENUM_VALUE_KEY].Read<int>();
    }

    public static async ValueTask<LuaValue> FromInt(LuaTable args)
    {
        LuaTable enumTable = args[1].Read<LuaTable>();
        int target = args[2].Read<int>();
        string enumName = enumTable.Metatable!["__type"].Read<string>();

        foreach (var kvp in enumTable)
        {
            if (kvp.Value.Type != LuaValueType.Table)
                continue;
            LuaTable member = kvp.Value.Read<LuaTable>();
            if (member.Metatable![ENUM_VALUE_KEY].Read<int>() == target)
                return member;
        }

        throw new XenonRuntimeException($"no member with value {target} in enum {enumName}");
    }

    public static async ValueTask<LuaValue> Members(LuaTable args)
    {
        LuaTable enumTable = args[1].Read<LuaTable>();
        List<LuaValue> names = new();
        foreach (var kvp in enumTable)
        {
            if (kvp.Value.Type == LuaValueType.Table)
                names.Add(kvp.Key.Read<string>());
        }

        return XenonArrayClass.CreateArray(XenonRT.T_STRING, names.ToArray());
    }

    public override Dictionary<string, XenonClass<XenonEnumLibrary>.XenonClassMethod> Methods => new()
    {
        ["toInt"] = new() { Arguments = new() { [1] = ("value", XenonRT.T_ANY) }, Method = ToInt },
        ["fromInt"] = new() { Arguments = new() { [1] = ("enum", XenonRT.T_ANY), [2] = ("value", XenonRT.T_NUMBER) }, Method = FromInt },
        ["members"] = new() { Arguments = new() { [1] = ("enum", XenonRT.T_ANY) }, Method = Members },
    };

    public override Dictionary<string, LuaValue> Properties => new();
    public override string Name => "enumlib";
}