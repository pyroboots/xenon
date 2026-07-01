using Lua;
using Xunit;

namespace xenon.Tests;

public class BootstrapTests
{
    [Fact]
    public void Bootstrap_RegistersCoreKeywords()
    {
        XenonRT.Reset();
        XenonRT.Bootstrap();

        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("func"));
        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("type"));
        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("array"));
        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("str"));
        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("bool"));
        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("dict"));
    }

    [Fact]
    public void Bootstrap_RegistersStaticLibraries()
    {
        XenonRT.Reset();
        XenonRT.Bootstrap();

        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("math"));
        Assert.NotEqual(LuaValue.Nil, XenonRT.GetGlobal("number"));
    }
}