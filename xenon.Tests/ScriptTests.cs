using Xunit;

namespace xenon.Tests;

public class ScriptTests
{
    [Fact]
    public async Task ExampleScript_RunsWithoutError()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "test.xnn");
        await XenonScriptRunner.RunFileAsync(path);
    }

    [Fact]
    public async Task NumberLibrary_MethodsWork()
    {
        await XenonScriptRunner.RunAsync("""
            assert{number.isOdd{3}}
            assert{number.isEven{4}}
            assert{number.isPositive{1}}
            assert{number.isNegative{-1}}
            assert{number.isInteger{5}}
            assert{not number.isInteger{5.5}}
            """);
    }

    [Fact]
    public async Task MathLibrary_IsRegistered()
    {
        await XenonScriptRunner.RunAsync("""
            assert{math.absolute{-3} == 3}
            assert{math.maximum{2, 5} == 5}
            assert{math.pi > 3}
            """);
    }
}