using Xunit;

namespace xenon.Tests;

public class IterationTests
{
    [Fact]
    public async Task Array_MeasureAndLength_Agree()
    {
        await XenonScriptRunner.RunAsync("""
            arr = array {{t_num}, 1, 2, 3}
            assert{array.measure{arr} == 3}
            assert{#arr == 3}
            """);
    }

    [Fact]
    public async Task Array_EmptyConstructor_Works()
    {
        await XenonScriptRunner.RunAsync("""
            arr = array {{t_num}}
            assert{array.measure{arr} == 0}
            assert{#arr == 0}
            """);
    }

    [Fact]
    public async Task Dict_MeasureAndLength_Agree()
    {
        await XenonScriptRunner.RunAsync("""
            d = dict {{t_str, t_str}, ["a"] = "1", ["b"] = "2"}
            assert{dict.measure{d} == 2}
            assert{#d == 2}
            """);
    }

    [Fact]
    public async Task Dict_Measure_AfterRemove_ExcludesRemoved()
    {
        await XenonScriptRunner.RunAsync("""
            d = dict {{t_str, t_str}, ["a"] = "1", ["b"] = "2"}
            dict.remove{d, "a"}
            assert{dict.measure{d} == 1}
            assert{#d == 1}
            """);
    }

    [Fact]
    public async Task Bool_AnyAll_UseFullArray()
    {
        await XenonScriptRunner.RunAsync("""
            arr = array {{t_bool}, true, false, true}
            assert{bool.any{arr}}
            assert{not bool.all{arr}}
            """);
    }
}