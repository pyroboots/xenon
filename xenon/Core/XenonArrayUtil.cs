using Lua;

namespace xenon.Core;

public static class XenonArrayUtil
{
    public static int Count(LuaTable arr)
    {
        int max = -1;
        foreach (var kvp in arr)
        {
            if (kvp.Key.TryRead(out int idx) && idx > max)
                max = idx;
        }

        return max + 1;
    }

    public static IEnumerable<LuaValue> Values(LuaTable arr)
    {
        int count = Count(arr);
        for (int i = 0; i < count; i++)
            yield return arr[i];
    }
}