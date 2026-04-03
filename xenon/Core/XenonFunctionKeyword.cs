using System.Text;
using Lua;

namespace xenon;

public class XenonFunctionKeyword : XenonKeyword<XenonFunctionKeyword>
{
    private static string LuaStrLit(string s)
    {
        return "\"" + s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal) + "\"";
    }
    
    public static async Task<LuaFunction> CompileFunc(Dictionary<string, string> args, string body, string chunkName, string name = "anonymous")
    {
        StringBuilder funcSig = new();
        funcSig.AppendLine($"local function {name}(this, a)");

        foreach (var kvp in args)
        {
            string keyLit = LuaStrLit(kvp.Key);
            string expectedLit = LuaStrLit(kvp.Value);
            funcSig.AppendLine(
                $"if typeof{{a.{kvp.Key}}} ~= {kvp.Value} then error{{\"parameter \"..{keyLit}..\": expected \"..{expectedLit}..\", got \"..typeof{{a.{kvp.Key}}}}} end");
        }
        
        funcSig.AppendLine(body);
        funcSig.AppendLine("end");
        funcSig.AppendLine($"return {name};");
        
        LuaValue[] res = await XenonRT.Compiler.DoStringAsync(funcSig.ToString(), chunkName);
        return res[0].Read<LuaFunction>();
    }
    
    public async override ValueTask<LuaTable> Constructor(LuaTable args)
    {
        LuaTable funcArgs = args[1].Read<LuaTable>();
        Dictionary<string, string> typeMap = new();
        foreach (var kvp in funcArgs)
            typeMap.Add(kvp.Key.Read<string>(), kvp.Value.Read<string>());
        
        string body = args[2].Read<string>();
        
        LuaFunction luaFunc = await CompileFunc(typeMap, body, "xenon:func");
        LuaTable returnFunc = new();
        returnFunc.Metatable = new()
        {
            ["__call"] = luaFunc,
            ["__len"] = new LuaFunction("__length", async (innerCtx, _) => innerCtx.Return(typeMap.Count)),
            ["__funcArgs"] = funcArgs,
            ["__type"] = XenonRT.T_FUNCTION,
            ["__index"] = new LuaFunction("__get", async (_, _) => throw ExceptionBuilder.IndexOpaqueType(XenonRT.T_FUNCTION)),
            ["__newindex"] = new LuaFunction("__set", async (_, _) => throw ExceptionBuilder.IndexOpaqueType(XenonRT.T_FUNCTION)),
        };
        return returnFunc;
    }

    public override Dictionary<string, LuaFunction> Methods { get; } = new();
    public override string Name => "func";
}