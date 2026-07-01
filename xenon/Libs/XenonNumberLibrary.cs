using Lua;

namespace xenon.Libs;

public class XenonNumberLibrary : XenonStaticClass<XenonNumberLibrary>
{
    public static async ValueTask<LuaValue> IsOdd(LuaTable args)
        => float.IsOddInteger(args[1].Read<float>());
    public static async ValueTask<LuaValue> IsEven(LuaTable args)
        => float.IsEvenInteger(args[1].Read<float>());
    public static async ValueTask<LuaValue> IsPositive(LuaTable args)
        => float.IsPositive(args[1].Read<float>());
    public static async ValueTask<LuaValue> IsNegative(LuaTable args)
        => float.IsNegative(args[1].Read<float>());
    public static async ValueTask<LuaValue> IsInteger(LuaTable args)
        => float.IsInteger(args[1].Read<float>());
    public static async ValueTask<LuaValue> Clamp(LuaTable args)
        => Math.Clamp(args[1].Read<float>(), args[2].Read<float>(), args[3].Read<float>());
    public static async ValueTask<LuaValue> Round(LuaTable args)
        => MathF.Round(args[1].Read<float>());
    public static async ValueTask<LuaValue> Parse(LuaTable args)
    {
        if (float.TryParse(args[1].Read<string>(), out float result))
            return result;
        throw new XenonRuntimeException($"could not parse number from '{args[1].Read<string>()}'");
    }
    public static async ValueTask<LuaValue> Between(LuaTable args)
    {
        float val = args[1].Read<float>();
        float min = args[2].Read<float>();
        float max = args[3].Read<float>();
        return val >= min && val <= max;
    }

    public override Dictionary<string, XenonClass<XenonNumberLibrary>.XenonClassMethod> Methods => new()
    {
        ["isOdd"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) }, Method = IsOdd },
        ["isEven"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) }, Method = IsEven },
        ["isPositive"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) }, Method = IsPositive },
        ["isNegative"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) }, Method = IsNegative },
        ["isInteger"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) }, Method = IsInteger },
        ["clamp"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER), [2] = ("min", XenonRT.T_NUMBER), [3] = ("max", XenonRT.T_NUMBER) }, Method = Clamp },
        ["round"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) }, Method = Round },
        ["parse"] = new() { Arguments = new() { [1] = ("text", XenonRT.T_STRING) }, Method = Parse },
        ["between"] = new() { Arguments = new() { [1] = ("x", XenonRT.T_NUMBER), [2] = ("min", XenonRT.T_NUMBER), [3] = ("max", XenonRT.T_NUMBER) }, Method = Between },
    };

    public override Dictionary<string, LuaValue> Properties => new();
    public override string Name => "number";
}