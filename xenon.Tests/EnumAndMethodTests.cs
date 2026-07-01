using Xunit;

namespace xenon.Tests;

public class EnumAndMethodTests
{
    [Fact]
    public async Task EnumsAndMethodsScript_RunsWithoutError()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Scripts", "enums-and-methods.xnn");
        await XenonScriptRunner.RunFileAsync(path);
    }

    [Fact]
    public async Task Enum_AutoNumbersFromZero()
    {
        await XenonScriptRunner.RunAsync("""
            Status = enum {[[Status]], [[Idle]], [[Running]]}
            assert{typeof{Status.Idle} == t_Status}
            assert{typeof{Status.Running} == t_Status}
            """);
    }

    [Fact]
    public async Task Enum_ExplicitValue()
    {
        await XenonScriptRunner.RunAsync("""
            Priority = enum {[[Priority]], [[Low]], High = 10, [[Critical]]}
            assert{typeof{Priority.High} == t_Priority}
            assert{typeof{Priority.Critical} == t_Priority}
            """);
    }

    [Fact]
    public async Task TypeMethod_CanMutateThis()
    {
        await XenonScriptRunner.RunAsync("""
            Counter = type {[[Counter]],
                value = t_num,
                inc = func {{}, [[
                    this.value = this.value + 1
                ]], t_void},
            }
            c = Counter { value = 0 }
            c.inc{}
            c.inc{}
            assert{c.value == 2}
            """);
    }

    [Fact]
    public async Task Bootstrap_RegistersEnumKeyword()
    {
        XenonRT.Reset();
        XenonRT.Bootstrap();
        Assert.NotEqual(Lua.LuaValue.Nil, XenonRT.GetGlobal("enum"));
    }
}