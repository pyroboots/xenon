using Lua;
using xenon.Core;
using Xunit;

namespace xenon.Tests;

public class ArrayUtilTests
{
    [Fact]
    public void CreateArray_StoresAllIndices()
    {
        LuaTable arr = XenonArrayClass.CreateArray("t_num", 1, 2, 3);

        Assert.Equal(3, XenonArrayUtil.Count(arr));
        Assert.Equal(1, arr[0].Read<int>());
        Assert.Equal(2, arr[1].Read<int>());
        Assert.Equal(3, arr[2].Read<int>());

        var values = XenonArrayUtil.Values(arr).ToList();
        Assert.Equal([1, 2, 3], values.Select(v => v.Read<int>()).ToList());
    }
}