using Xunit;

namespace xenon.Tests;

public class FutureFeatureTests
{
    [Fact]
    public async Task Type_Extends_InheritsFieldsAndMethods()
    {
        await XenonScriptRunner.RunAsync("""
            Animal = type {[[Animal]],
                name = t_str,
                speak = func {{}, [[return "..." ]], t_str},
            }
            Dog = type {[[Dog]],
                extends = Animal,
                breed = t_str,
            }
            spot = Dog { name = "Spot", breed = "Corgi" }
            assert{spot.name == "Spot"}
            assert{spot.breed == "Corgi"}
            assert{spot.speak{} == "..."}
            """);
    }

    [Fact]
    public async Task Str_MatchAndTest()
    {
        await XenonScriptRunner.RunAsync("""
            assert{str.test{"hello123", "\\d+"}}
            assert{str.match{"hello123", "(\\d+)"} == "123"}
            assert{str.match{"nope", "\\d+"} == nil}
            """);
    }

    [Fact]
    public async Task RuntimeError_IncludesChunkName()
    {
        var ex = await Assert.ThrowsAsync<XenonRuntimeException>(() =>
            XenonScriptRunner.RunAsync("error{\"boom\"}", "my-chunk"));
        Assert.Contains("my-chunk", ex.Message);
        Assert.Contains("boom", ex.Message);
    }
}