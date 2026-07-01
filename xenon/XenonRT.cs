using System.Text;
using Lua;
using Lua.Standard;
using xenon.Core;
using xenon.Libs;

namespace xenon;

public class XenonRT
{
    public static LuaState Runtime = LuaState.Create();
    public static LuaState Compiler = LuaState.Create();
    private static LuaTable _actualEnvironment = new();
    private static Dictionary<string, string> _immutableGlobals = new();

    public const string T_STRING = "t_str";
    public const string T_NUMBER = "t_num";
    public const string T_BOOLEAN = "t_bool";
    public const string T_FUNCTION = "t_func";
    public const string T_LUA_FUNCTION = "t_ufunc"; // unmanaged func (lua)
    public const string T_VOID = "t_void";
    public const string T_ANY = "t_any";
    public const string T_ARRAY = "t_array";
    public const string T_DICTIONARY = "t_dict";
    
    public static void Bootstrap() => XenonBootstrap.Initialize();

    internal static void Reset()
    {
        Runtime.Dispose();
        Compiler.Dispose();
        Runtime = LuaState.Create();
        Compiler = LuaState.Create();
        _actualEnvironment = new();
        _immutableGlobals = new();
        XenonBootstrap.Reset();
    }

    internal static LuaValue GetGlobal(string key) => _actualEnvironment[key];

    internal static void InstallRuntimeEnvironmentGuards()
    {
        // NOTE FOR FUTURE SELF: compiler does not need immutability, do not add
        Runtime.Environment.Metatable = new()
        {
            ["__index"] = new LuaFunction(async (ctx, ct) =>
            {
                string key = ctx.GetArgument<string>(1);
                ctx.Return(_actualEnvironment[key]);
                return 1;
            }),
            ["__newindex"] = new LuaFunction(async (ctx, ct) =>
            {
                string key = ctx.GetArgument<string>(1);
                LuaValue val = ctx.GetArgument(2);

                if (_immutableGlobals.ContainsKey(key))
                    throw ExceptionBuilder.ModifyImmutable(_immutableGlobals[key], key);

                _actualEnvironment[key] = val;
                return 0;
            })
        };
    }

    public static void SetImmutability(string key, string type) => _immutableGlobals[key] = type;
    
    public static void RegisterType(string type)
    {
        _actualEnvironment[type] = type;
        Compiler.Environment[type] = type;
        SetImmutability(type, "type");
    }

    public static void RegisterFunc(string name, LuaFunction func)
    {
        _actualEnvironment[name] = func;
        Compiler.Environment[name] = func;
    }

    public static void RegisterClass<TXenonClass>(string name) where TXenonClass : class, new()
    {
        _actualEnvironment[name] = XenonClass<TXenonClass>.Static();   
        Compiler.Environment[name] = XenonClass<TXenonClass>.Static();  
        SetImmutability(name, "keyword");
    }
    
    public static void RegisterStaticClass<TXenonStaticClass>() where TXenonStaticClass : XenonStaticClass<TXenonStaticClass>, new()
        => RegisterStaticClass<TXenonStaticClass>(new TXenonStaticClass().Name);

    public static void RegisterStaticClass<TXenonStaticClass>(string name) where TXenonStaticClass : class, new()
    {
        _actualEnvironment[name] = XenonStaticClass<TXenonStaticClass>.Static();
        Compiler.Environment[name] = XenonStaticClass<TXenonStaticClass>.Static();
        SetImmutability(name, "keyword");
    }

    public static string GetType(LuaValue v)
    {
        LuaValueType t = v.Type;

        if (t is LuaValueType.Boolean) return T_BOOLEAN;
        if (t is LuaValueType.Function) return T_LUA_FUNCTION;
        if (t is LuaValueType.Number) return T_NUMBER;
        if (t is LuaValueType.String) return T_STRING;
        if (t is LuaValueType.Nil) return T_VOID;
        if (t is LuaValueType.Table)
        {
            LuaTable tbl = v.Read<LuaTable>();
            LuaTable? meta = tbl.Metatable;
            if (meta == null) return T_ANY;
            
            string? type = meta["__type"].Read<string?>();
            if (type == null) return T_ANY;

            if (type.StartsWith("t_")) return type;
            else return "t_" + type;
        }

        throw new Exception($"could not get type of {v.Type.ToString()} ({v.ToString()})");
    }
    
    public static LuaFunction Typeof = new(async (ctx, ct) =>
    {
        LuaTable arguments = ctx.Arguments[0].Read<LuaTable>();
        ctx.Return(GetType(arguments[1]));

        return 1;
    });
    
    public static LuaFunction UnmanagedBlock = new(async (ctx, ct) =>
    {
        string block = ctx.Arguments[0].Read<string>();
        LuaState unmanaged = LuaState.Create();
        unmanaged.OpenStandardLibraries();
        // LuaState.Environment unfortunately does not have a setter, so we need to iterate
        foreach (var kvp in Runtime.Environment) unmanaged.Environment[kvp.Key] = kvp.Value;
        
        LuaValue[] results = await unmanaged.DoStringAsync(block);
        // restore env
        foreach (var kvp in unmanaged.Environment) Runtime.Environment[kvp.Key] = kvp.Value;
        foreach (LuaValue result in results) ctx.Return(result);
        
        unmanaged.Dispose();
        return results.Length;
    });
}