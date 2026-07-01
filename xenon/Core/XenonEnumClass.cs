using Lua;

namespace xenon.Core;

public class XenonEnumClass : XenonClass<XenonEnumClass>
{
    private const string ENUM_VALUE_KEY = "__enumValue";

    private static async ValueTask<int> EnumCall(LuaFunctionExecutionContext ctx, CancellationToken ct)
        => throw ExceptionBuilder.InvalidKeywordOperation("invoke", "enum");

    private static async ValueTask<int> EnumSet(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        string key = ctx.GetArgument<string>(1);
        string enumName = ctx.GetArgument<LuaTable>(0).Metatable!["__type"].Read<string>();
        throw ExceptionBuilder.ModifyImmutable("enum member", $"{enumName}.{key}");
    }

    private static async ValueTask<int> EnumToString(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        ctx.Return(tbl.Metatable!["__type"].Read<string>());
        return 1;
    }

    private static async ValueTask<int> EnumValueToString(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        ctx.Return(tbl.Metatable!["__enumName"].Read<string>() + "." + tbl.Metatable!["__enumMember"].Read<string>());
        return 1;
    }

    public static LuaTable CreateEnumValue(string enumName, string memberName, int numericValue)
    {
        LuaTable value = new();
        value.Metatable = new()
        {
            ["__type"] = enumName,
            ["__enumName"] = enumName,
            ["__enumMember"] = memberName,
            [ENUM_VALUE_KEY] = numericValue,
            ["__tostring"] = new LuaFunction($"{enumName}.{memberName}__string", EnumValueToString),
        };
        return value;
    }

    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        if (!args.ContainsKey(1))
            throw ExceptionBuilder.SyntaxMissingArg(Name, "enum name", "argument 1");

        string name = args[1].Read<string>();
        XenonRT.RegisterType($"t_{name}");

        LuaTable enumTable = new();
        LuaTable meta = new()
        {
            ["__call"] = new LuaFunction($"{name}__call", EnumCall),
            ["__newindex"] = new LuaFunction($"{name}__set", EnumSet),
            ["__tostring"] = new LuaFunction($"{name}__string", EnumToString),
            ["__type"] = name,
        };
        enumTable.Metatable = meta;

        int autoValue = 0;
        foreach (var kvp in args.Skip(1))
        {
            if (kvp.Value.Type == LuaValueType.String && kvp.Key.TryRead(out int _))
            {
                string memberName = kvp.Value.Read<string>();
                enumTable[memberName] = CreateEnumValue(name, memberName, autoValue++);
            }
            else if (kvp.Value.Type == LuaValueType.Number && kvp.Key.Type == LuaValueType.String)
            {
                string memberName = kvp.Key.Read<string>();
                int value = (int)kvp.Value.Read<double>();
                enumTable[memberName] = CreateEnumValue(name, memberName, value);
                autoValue = value + 1;
            }
            else
                throw ExceptionBuilder.InvalidEnumMember(name, kvp.Key.ToString());
        }

        return enumTable;
    }

    public override Dictionary<string, XenonClassMethod> Methods => new();
    public override string Name => "enum";
}