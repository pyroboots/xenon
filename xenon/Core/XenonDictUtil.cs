using Lua;

namespace xenon.Core;

public static class XenonDictUtil
{
    public static int Count(LuaTable dict)
    {
        int count = 0;
        foreach (var kvp in dict)
        {
            if (kvp.Value.Type != LuaValueType.Nil)
                count++;
        }

        return count;
    }
}