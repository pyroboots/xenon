using Lua;

namespace xenon.Libs;

public class XenonMathLibrary : XenonStaticClass<XenonMathLibrary>
{
    public static async ValueTask<LuaValue> Absolute(LuaTable args)
        => MathF.Abs(args[1].Read<float>());
    public static async ValueTask<LuaValue> ArcCosine(LuaTable args)
        => MathF.Acos(args[1].Read<float>());
    public static async ValueTask<LuaValue> ArcSine(LuaTable args)
        => MathF.Asin(args[1].Read<float>());
    public static async ValueTask<LuaValue> ArcTangent(LuaTable args)
        => MathF.Atan(args[1].Read<float>());
    public static async ValueTask<LuaValue> Ceiling(LuaTable args)
        => MathF.Ceiling(args[1].Read<float>());
    public static async ValueTask<LuaValue> Cosine(LuaTable args)
        => MathF.Cos(args[1].Read<float>());
    public static async ValueTask<LuaValue> Exponent(LuaTable args)
        => MathF.Exp(args[1].Read<float>());
    public static async ValueTask<LuaValue> Floor(LuaTable args)
        => MathF.Floor(args[1].Read<float>());
    public static async ValueTask<LuaValue> Logarithm(LuaTable args)
        => MathF.Log(args[1].Read<float>());
    public static async ValueTask<LuaValue> Maximum(LuaTable args)
        => MathF.Max(args[1].Read<float>(), args[2].Read<float>());
    public static async ValueTask<LuaValue> Minimum(LuaTable args)
        => MathF.Min(args[1].Read<float>(), args[2].Read<float>());
    public static async ValueTask<LuaValue> Power(LuaTable args)
        => MathF.Pow(args[1].Read<float>(), args[2].Read<float>());
    public static async ValueTask<LuaValue> Sine(LuaTable args)
        => MathF.Sin(args[1].Read<float>());
    public static async ValueTask<LuaValue> SquareRoot(LuaTable args)
        => MathF.Sqrt(args[1].Read<float>());
    public static async ValueTask<LuaValue> Tangent(LuaTable args)
        => MathF.Tan(args[1].Read<float>());
    
    public static async ValueTask<LuaValue> Random(LuaTable args)
        => System.Random.Shared.NextDouble();
    public static async ValueTask<LuaValue> RandomRange(LuaTable args)
        => System.Random.Shared.Next(args[1].Read<int>(), args[2].Read<int>());
    
    public override Dictionary<string, XenonClass<XenonMathLibrary>.XenonClassMethod> Methods => new()
    {
        ["absolute"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Absolute
        },
        ["arcCosine"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = ArcCosine
        },
        ["arcSine"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = ArcSine
        },
        ["arcTangent"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = ArcTangent
        },
        ["ceiling"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Ceiling
        },
        ["cosine"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Cosine
        },
        ["exponent"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Exponent
        },
        ["floor"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Floor
        },
        ["logarithm"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Logarithm
        },
        ["maximum"] = new()
        {
            Arguments = new() 
            { 
                [1] = ("val1", XenonRT.T_NUMBER), 
                [2] = ("val2", XenonRT.T_NUMBER) 
            },
            Method = Maximum
        },
        ["minimum"] = new()
        {
            Arguments = new() 
            { 
                [1] = ("val1", XenonRT.T_NUMBER), 
                [2] = ("val2", XenonRT.T_NUMBER) 
            },
            Method = Minimum
        },
        ["power"] = new()
        {
            Arguments = new() 
            { 
                [1] = ("base", XenonRT.T_NUMBER), 
                [2] = ("exponent", XenonRT.T_NUMBER) 
            },
            Method = Power
        },
        ["sine"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Sine
        },
        ["squareRoot"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = SquareRoot
        },
        ["tangent"] = new()
        {
            Arguments = new() { [1] = ("x", XenonRT.T_NUMBER) },
            Method = Tangent
        },
        ["random"] = new()
        {
            Arguments = new(),
            Method = Random
        },
        ["randomRange"] = new()
        {
            Arguments = new() 
            { 
                [1] = ("min", XenonRT.T_NUMBER), 
                [2] = ("max", XenonRT.T_NUMBER) 
            },
            Method = RandomRange
        }
    };

    public override Dictionary<string, LuaValue> Properties => new()
    {
        ["pi"] = Math.PI,
        ["biggest"] = int.MaxValue,
        ["smallest"] = int.MinValue,
    };
    public override string Name => "math";
}