using System.Text.Json;
using Lua;
using xenon.Core;

namespace xenon.Libs;

public class XenonJsonLibrary : XenonStaticClass<XenonJsonLibrary>
{
    public static async ValueTask<LuaValue> Parse(LuaTable args)
    {
        using JsonDocument doc = JsonDocument.Parse(args[1].Read<string>());
        return JsonToLua(doc.RootElement);
    }

    public static async ValueTask<LuaValue> Stringify(LuaTable args)
        => JsonSerializer.Serialize(LuaToJson(args[1]));

    private static LuaValue JsonToLua(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => JsonObjectToDict(el),
        JsonValueKind.Array => JsonArrayToArray(el),
        JsonValueKind.String => el.GetString()!,
        JsonValueKind.Number => el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => LuaValue.Nil,
    };

    private static LuaTable JsonObjectToDict(JsonElement el)
    {
        Dictionary<LuaValue, LuaValue> items = new();
        foreach (var prop in el.EnumerateObject())
            items[prop.Name] = JsonToLua(prop.Value);
        return XenonDictionaryClass.CreateDict(XenonRT.T_STRING, XenonRT.T_ANY, items);
    }

    private static LuaTable JsonArrayToArray(JsonElement el)
    {
        List<LuaValue> items = new();
        foreach (var item in el.EnumerateArray())
            items.Add(JsonToLua(item));
        return XenonArrayClass.CreateArray(XenonRT.T_ANY, items.ToArray());
    }

    private static object? LuaToJson(LuaValue value) => value.Type switch
    {
        LuaValueType.Nil => null,
        LuaValueType.Boolean => value.ToBoolean(),
        LuaValueType.Number => value.Read<double>(),
        LuaValueType.String => value.Read<string>(),
        LuaValueType.Table => LuaTableToJson(value.Read<LuaTable>()),
        _ => value.ToString(),
    };

    private static object LuaTableToJson(LuaTable table)
    {
        if (table.Metatable != null
            && table.Metatable!.ContainsKey("__type")
            && table.Metatable!["__type"].Type == LuaValueType.String
            && table.Metatable!["__type"].Read<string>() == XenonRT.T_ARRAY)
        {
            List<object?> list = new();
            foreach (LuaValue item in XenonArrayUtil.Values(table))
                list.Add(LuaToJson(item));
            return list;
        }

        Dictionary<string, object?> dict = new();
        foreach (var kvp in table)
            dict[kvp.Key.ToString()] = LuaToJson(kvp.Value);
        return dict;
    }

    public override Dictionary<string, XenonClass<XenonJsonLibrary>.XenonClassMethod> Methods => new()
    {
        ["parse"] = new() { Arguments = new() { [1] = ("json", XenonRT.T_STRING) }, Method = Parse },
        ["stringify"] = new() { Arguments = new() { [1] = ("value", XenonRT.T_ANY) }, Method = Stringify },
    };

    public override Dictionary<string, LuaValue> Properties => new();
    public override string Name => "json";
}