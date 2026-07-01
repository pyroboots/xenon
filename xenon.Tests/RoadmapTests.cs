using Xunit;

namespace xenon.Tests;

public class RoadmapTests
{
    [Fact]
    public async Task TypeInstances_AreDistinct()
    {
        await XenonScriptRunner.RunAsync("""
            Person = type {[[Person]], name = t_str}
            a = Person { name = "Alice" }
            b = Person { name = "Bob" }
            assert{a.name == "Alice"}
            assert{b.name == "Bob"}
            """);
    }

    [Fact]
    public async Task Cast_EnumToNumber()
    {
        await XenonScriptRunner.RunAsync("""
            Color = enum {[[Color]], [[Red]], [[Green]]}
            assert{cast{Color.Green, t_num} == 1}
            """);
    }

    [Fact]
    public async Task Try_CatchesErrors()
    {
        await XenonScriptRunner.RunAsync("""
            bad = func {{}, [[ error{"boom"} ]], t_void}
            result = try{bad, {}}
            assert{not result.ok}
            assert{result.error == "boom"}
            """);
    }

    [Fact]
    public async Task Fs_WriteAndRead()
    {
        string path = Path.Combine(Path.GetTempPath(), $"xenon-test-{Guid.NewGuid()}.txt");
        string escaped = path.Replace("\\", "/");
        await XenonScriptRunner.RunAsync(
            "fs.write{\"" + escaped + "\", \"hello xenon\"}\n" +
            "content = fs.read{\"" + escaped + "\"}\n" +
            "assert{content == \"hello xenon\"}");
        File.Delete(path);
    }

    [Fact]
    public async Task Json_RoundTrip()
    {
        await XenonScriptRunner.RunAsync("""
            d = dict {{t_str, t_str}, ["a"] = "1"}
            s = json.stringify{d}
            parsed = json.parse{s}
            assert{parsed["a"] == "1"}
            """);
    }

    [Fact]
    public async Task Func_ReturnsValue()
    {
        await XenonScriptRunner.RunAsync("""
            add = func {{acc = t_num, item = t_num}, [[return a.acc + a.item]], t_num}
            assert{add{acc = 1, item = 2} == 3}
            """);
    }

    [Fact]
    public async Task Array_Reduce()
    {
        await XenonScriptRunner.RunAsync("""
            arr = array {{t_num}, 1, 2, 3}
            add = func {{acc = t_num, item = t_num}, [[return a.acc + a.item]], t_num}
            assert{add{acc = 10, item = 1} == 11}
            sum = array.reduce{arr, add, 10}
            assert{sum == 16}
            """);
    }

    [Fact]
    public async Task Array_Reverse()
    {
        await XenonScriptRunner.RunAsync("""
            arr = array {{t_num}, 1, 2, 3}
            rev = array.reverse{arr}
            assert{rev[0] == 3}
            """);
    }

    [Fact]
    public async Task ForEach_Iterates()
    {
        await XenonScriptRunner.RunAsync("""
            arr = array {{t_num}, 1, 2, 3}
            forEach {arr, "n", [[ -- iterate ]]}
            """);
    }

    [Fact]
    public async Task OptionalField_Works()
    {
        await XenonScriptRunner.RunAsync("""
            Config = type {[[Config]],
                name = t_str,
                verbose = {type = t_bool, optional = true},
            }
            c = Config { name = "app" }
            assert{c.name == "app"}
            """);
    }

    [Fact]
    public async Task EnumLib_ToIntFromInt()
    {
        await XenonScriptRunner.RunAsync("""
            Status = enum {[[Status]], [[Idle]], [[Running]]}
            assert{enumlib.toInt{Status.Running} == 1}
            assert{enumlib.fromInt{Status, 0} == Status.Idle}
            """);
    }
}