using System.Text;
using Lua;

namespace xenon.Core;

public class XenonFunctionClass : XenonClass<XenonFunctionClass>
{
    private static string LuaStrLit(string s)
    {
        return "\"" + s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal) + "\"";
    }
    
    public static async Task<LuaFunction> CompileFunc(Dictionary<string, string> args, string body, string chunkName, string name = "anonymous", string returnType = XenonRT.T_VOID)
    {
        StringBuilder funcSig = new();
        funcSig.AppendLine($"local function {name}(this, a)");

        // param type checking
        foreach (var kvp in args)
        {
            string keyLit = LuaStrLit(kvp.Key);
            string expectedLit = LuaStrLit(kvp.Value);
            funcSig.AppendLine(
                $"if typeof{{a.{kvp.Key}}} ~= {kvp.Value} then error{{\"parameter \"..{keyLit}..\": expected \"..{expectedLit}..\", got \"..typeof{{a.{kvp.Key}}}}} end");
        }
    
        // in order to check types, we wrap the inner func and check the return before returning the actual thing
        if (returnType != XenonRT.T_VOID)
        {
            string returnTypeLit = LuaStrLit(returnType);
            funcSig.AppendLine("local function __inner()");
            funcSig.AppendLine(body);
            funcSig.AppendLine("end");
            funcSig.AppendLine("local __result = __inner()");
            funcSig.AppendLine($"if __result ~= nil and typeof{{__result}} ~= {returnType} then");
            funcSig.AppendLine($"error{{\"return type mismatch: expected \"..{returnTypeLit}..\", got \"..typeof{{__result}}}}");
            funcSig.AppendLine("end");
            funcSig.AppendLine("return __result");
        }
        else
            funcSig.AppendLine(body);
    
        funcSig.AppendLine("end");
        funcSig.AppendLine($"return {name};");
    
        LuaValue[] res = await XenonRT.Compiler.DoStringAsync(funcSig.ToString(), chunkName);
        return res[0].Read<LuaFunction>();
    }
    
    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        if (!args.ContainsKey(1)) 
            throw ExceptionBuilder.SyntaxMissingArg(Name, "argument map", "argument 1");
        LuaTable funcArgs = args[1].Read<LuaTable>();
        Dictionary<string, string> typeMap = new();
        foreach (var kvp in funcArgs)
            typeMap.Add(kvp.Key.Read<string>(), kvp.Value.Read<string>());
        
        if (!args.ContainsKey(2)) 
            throw ExceptionBuilder.SyntaxMissingArg(Name, "function body", "argument 2");
        string body = args[2].Read<string>();
        
        if (!args.ContainsKey(3)) 
            throw ExceptionBuilder.SyntaxMissingArg(Name, "return type", "argument 3");
        string returnType = args[3].Read<string>();
        
        LuaFunction luaFunc = await CompileFunc(typeMap, body, "xenon:func", returnType: returnType);
        LuaTable returnFunc = new();
        returnFunc.Metatable = new()
        {
            ["__returnType"] = returnType,
            ["__funcArgs"] = funcArgs,
            ["__type"] = XenonRT.T_FUNCTION,
            
            ["__call"] = luaFunc,
            ["__len"] = new LuaFunction("__length", async (innerCtx, _) => innerCtx.Return(typeMap.Count)),
            ["__index"] = new LuaFunction("__get", async (_, _) => throw ExceptionBuilder.IndexOpaqueType(XenonRT.T_FUNCTION)),
            ["__newindex"] = new LuaFunction("__set", async (_, _) => throw ExceptionBuilder.IndexOpaqueType(XenonRT.T_FUNCTION)),
        };
        return returnFunc;
    }
    
    public static async ValueTask<LuaValue> GetArgumentMap(LuaTable args)
    {
        LuaTable func = args[1].Read<LuaTable>();
        // we can be certain that there will be a metatable
        // because funcs are always returned with one in ctor
        Dictionary<LuaValue, LuaValue> dict = new();
        foreach (var kvp in func.Metatable!["__funcArgs"].Read<LuaTable>())
            dict[kvp.Key.Read<string>()] = kvp.Value.Read<string>();

        return XenonDictionaryClass.CreateDict(XenonRT.T_STRING, XenonRT.T_STRING, dict);
    }
    
    public static async ValueTask<LuaValue> GetReturnType(LuaTable args)
    {
        LuaTable func = args[1].Read<LuaTable>();
        return func.Metatable!["__returnType"];
    }

    public override Dictionary<string, XenonClassMethod> Methods { get; } = new()
    {
        ["getArgMap"] = new()
        {
            Arguments = new()
            {
                [1] = ("func", XenonRT.T_FUNCTION)
            },
            Method = GetArgumentMap,
        },
        ["getReturnType"] = new()
        {
            Arguments = new()
            {
                [1] = ("func", XenonRT.T_FUNCTION)
            },
            Method = GetReturnType,
        }
    };
    public override string Name => "func";
}