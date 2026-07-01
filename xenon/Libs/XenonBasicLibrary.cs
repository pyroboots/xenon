using Lua;
using Lua.Runtime;
using Lua.Standard;
using xenon;
using xenon.Core;

namespace xenon.Libs;

public static class XenonBasicLibrary
{
    public static void Register()
    {
        Implement(ref XenonRT.Runtime);
        Implement(ref XenonRT.Compiler);
        XenonRT.SetImmutability("console", "core library");
    }

    public static void Implement(ref LuaState state)
    {
        LuaFunction[] funcs =
        [
            new("error", Error),
            new("assert", Assert),
            new("cast", Cast),
            new("try", Try),
            new("import", Import),
            new("forEach", ForEach),
        ];

        foreach (LuaFunction f in funcs)
            state.Environment[f.Name] = f;

        state.Environment["console"] = new ConsoleIO();
    }

    public static async ValueTask<int> Error(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable args = ctx.GetArgument<LuaTable>(0);
        string msg = args[1].Read<string>();
        throw new Exception(msg);
    }

    public static async ValueTask<int> Assert(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable args = ctx.GetArgument<LuaTable>(0);
        if (!args[1].ToBoolean())
        {
            string msg = args.ArrayLength >= 2 && args[2].Type != LuaValueType.Nil
                ? args[2].Read<string>()
                : "assertion failed";
            throw new XenonRuntimeException(msg);
        }

        return 0;
    }

    public static async ValueTask<int> Cast(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable args = ctx.GetArgument<LuaTable>(0);
        ctx.Return(XenonCast.Cast(args[1], args[2].Read<string>()));
        return 1;
    }

    public static async ValueTask<int> Try(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable args = ctx.GetArgument<LuaTable>(0);
        LuaTable func = args[1].Read<LuaTable>();
        LuaTable callArgs = args.ContainsKey(2) ? args[2].Read<LuaTable>() : new LuaTable();
        LuaTable result = new();

        try
        {
            LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
            LuaValue[] results = await XenonRT.Runtime.CallAsync(innerFunc, [func, callArgs]);
            result["ok"] = true;
            result["value"] = results.Length > 0 ? results[0] : LuaValue.Nil;
        }
        catch (Exception ex)
        {
            result["ok"] = false;
            result["error"] = ex.Message;
        }

        ctx.Return(result);
        return 1;
    }

    public static async ValueTask<int> Import(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable args = ctx.GetArgument<LuaTable>(0);
        string path = args[1].Read<string>();
        string fullPath = Path.GetFullPath(path);
        string source = await File.ReadAllTextAsync(fullPath);
        string chunkName = "@" + fullPath.Replace('\\', '/');
        await XenonRT.Runtime.DoStringAsync(source, chunkName);
        return 0;
    }

    public static async ValueTask<int> ForEach(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable args = ctx.GetArgument<LuaTable>(0);
        LuaTable arr = args[1].Read<LuaTable>();
        string varName = args[2].Read<string>();
        string body = args[3].Read<string>();
        string elementType = arr.Metatable!["__arrayType"].Read<string>();

        Dictionary<string, string> funcArgs = new()
        {
            [varName] = elementType,
            ["index"] = XenonRT.T_NUMBER,
        };

        LuaFunction innerFunc = await XenonFunctionClass.CompileFunc(funcArgs, body, "xenon:for");
        int count = XenonArrayUtil.Count(arr);
        for (int i = 0; i < count; i++)
        {
            LuaTable callArgs = new()
            {
                [varName] = arr[i],
                ["index"] = i,
            };
            LuaTable loopFunc = new()
            {
                Metatable = new()
                {
                    ["__type"] = XenonRT.T_FUNCTION,
                    ["__returnType"] = XenonRT.T_VOID,
                    ["__call"] = innerFunc,
                }
            };
            await XenonRT.Runtime.CallAsync(innerFunc, [loopFunc, callArgs]);
        }

        return 0;
    }
}

[LuaObject]
public partial class ConsoleIO
{
    [LuaMember("outLn")]
    public static void OutLine(LuaTable args) => Console.WriteLine(args[1]);

    [LuaMember("out")]
    public static void Out(LuaTable args) => Console.Write(args[1]);

    [LuaMember("errLn")]
    public static void ErrLine(LuaTable args) => Console.Error.WriteLine(args[1]);

    [LuaMember("err")]
    public static void Err(LuaTable args) => Console.Error.Write(args[1]);

    [LuaMember("inLn")]
    public static string InLine() => Console.ReadLine() ?? "";

    [LuaMember("in")]
    public static string In() => Console.ReadKey().KeyChar.ToString();
}