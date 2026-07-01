using Lua;
using xenon.Core;

namespace xenon.Libs;

public static class XenonCast
{
    private const string ENUM_VALUE_KEY = "__enumValue";

    public static LuaValue Cast(LuaValue value, string targetType)
    {
        string sourceType = XenonRT.GetType(value);
        if (sourceType == targetType)
            return value;

        if (targetType == XenonRT.T_NUMBER)
        {
            if (value.Type == LuaValueType.Number)
                return value;
            if (value.Type == LuaValueType.String && float.TryParse(value.Read<string>(), out float f))
                return f;
            if (value.Type == LuaValueType.Boolean)
                return value.ToBoolean() ? 1 : 0;
            if (value.Type == LuaValueType.Table)
            {
                LuaTable tbl = value.Read<LuaTable>();
                if (tbl.Metatable != null && tbl.Metatable!.ContainsKey(ENUM_VALUE_KEY))
                    return tbl.Metatable![ENUM_VALUE_KEY].Read<double>();
            }
        }

        if (targetType == XenonRT.T_STRING)
        {
            if (value.Type == LuaValueType.String)
                return value;
            if (value.Type == LuaValueType.Number)
                return value.Read<double>().ToString();
            if (value.Type == LuaValueType.Boolean)
                return value.ToBoolean() ? "true" : "false";
        }

        if (targetType == XenonRT.T_BOOLEAN)
        {
            if (value.Type == LuaValueType.Boolean)
                return value;
            if (value.Type == LuaValueType.Number)
                return value.Read<double>() != 0;
            if (value.Type == LuaValueType.String)
                return bool.TryParse(value.Read<string>(), out bool b) && b;
        }

        throw ExceptionBuilder.TypeMismatch(targetType, sourceType, "cast");
    }
}