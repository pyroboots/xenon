using Lua;
using Lua.Runtime;
using Lua.Standard;

namespace xenon.Libs;

public static class XenonBasicLibrary
{
    public static void Implement(ref LuaState state)
    {
        LuaFunction[] funcs =
        [
            new("error", Error)
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
    
    public static async ValueTask<int> Cast(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        // TODO
        return 0;
    }
    
    public static async ValueTask<int> Try(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        // TODO
        return 0;
    }
}

[LuaObject]
public partial class ConsoleIO
{
    [LuaMember("outLn")] // pretty sure lua tbls start at idx 1, but ill find out
    public static void OutLine(LuaTable args) => Console.WriteLine(args[1]);
    
    [LuaMember("out")]
    public static void Out(LuaTable args) => Console.Write(args[1]);

    [LuaMember("inLn")]
    public static string InLine() => Console.ReadLine();
    
    [LuaMember("in")]
    public static string In() => Console.ReadKey().KeyChar.ToString();
}