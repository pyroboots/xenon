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
    
    public static void Bootstrap()
    {
        RegisterType(T_STRING);
        RegisterType(T_NUMBER);
        RegisterType(T_BOOLEAN);
        RegisterType(T_FUNCTION);
        RegisterType(T_LUA_FUNCTION);
        RegisterType(T_VOID);
        SetImmutability("void", "keyword");
        RegisterType(T_ANY);
        RegisterType(T_ARRAY);
        RegisterType(T_DICTIONARY);
        
        XenonBasicLibrary.Implement(ref Runtime);
        XenonBasicLibrary.Implement(ref Compiler);
        SetImmutability("console", "core library");
        
        RegisterKeyword<XenonFunctionClass>("func");
        RegisterKeyword<XenonTypeClass>("type");
        RegisterKeyword<XenonArrayClass>("array");
        RegisterKeyword<XenonStringClass>("str");
        RegisterKeyword<XenonBoolClass>("bool");
        RegisterKeyword<XenonDictionaryClass>("dict");
        
        RegisterFunc("typeof", Typeof);
        SetImmutability("typeof", "keyword");
        RegisterFunc("unmanaged", UnmanagedBlock);
        SetImmutability("unmanaged", "keyword");

        // immutability protection
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
        // NOTE FOR FUTURE SELF: compiler does not need immutability, do not add
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

    public static void RegisterKeyword<TXenonClass>(string name) where TXenonClass : class, new()
    {
        _actualEnvironment[name] = XenonClass<TXenonClass>.Static();   
        Compiler.Environment[name] = XenonClass<TXenonClass>.Static();  
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