using System.Text;
using Lua;
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

    private static bool Managed = true;
    
    public static void Bootstrap()
    {
        RegisterType(new(T_STRING.Skip(2).ToArray()));
        RegisterType(new(T_NUMBER.Skip(2).ToArray()));
        RegisterType(new(T_BOOLEAN.Skip(2).ToArray()));
        RegisterType(new(T_FUNCTION.Skip(2).ToArray()));
        RegisterType(new(T_LUA_FUNCTION.Skip(2).ToArray()));
        RegisterType(new(T_VOID.Skip(2).ToArray()));
        SetImmutability("void", "type");
        RegisterType(new(T_ANY.Skip(2).ToArray()));
        RegisterType(new(T_ARRAY.Skip(2).ToArray()));
        RegisterType(new(T_DICTIONARY.Skip(2).ToArray()));
        
        XenonBasicLibrary.Implement(ref Runtime);
        XenonBasicLibrary.Implement(ref Compiler);
        SetImmutability("console", "core library");
        
        Runtime.Environment["func"] = XenonClass<XenonFunctionClass>.Static();
        Compiler.Environment["func"] = XenonClass<XenonFunctionClass>.Static();
        SetImmutability("func", "keyword");
        
        Runtime.Environment["type"] = XenonClass<XenonTypeClass>.Static();
        Compiler.Environment["type"] = XenonClass<XenonTypeClass>.Static();
        SetImmutability("type", "keyword");
        
        Runtime.Environment["array"] = XenonClass<XenonArrayClass>.Static();
        Compiler.Environment["array"] = XenonClass<XenonArrayClass>.Static();
        SetImmutability("array", "keyword");
        
        Runtime.Environment["str"] = XenonClass<XenonStringClass>.Static();
        Compiler.Environment["str"] = XenonClass<XenonStringClass>.Static();
        SetImmutability("str", "core library");
        
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
                
                if (_immutableGlobals.ContainsKey(key) && Managed)
                    throw ExceptionBuilder.ModifyImmutable(_immutableGlobals[key], key);
                
                _actualEnvironment[key] = val;
                return 0;
            })
        };
    }

    public static void SetImmutability(string key, string type) => _immutableGlobals[key] = type;
    
    public static void RegisterType(string type)
    {
        string typeName = $"t_{type}";
        _actualEnvironment[typeName] = typeName;
        Compiler.Environment[typeName] = typeName;
        SetImmutability(typeName, "type");
    }

    public static void RegisterFunc(string name, LuaFunction func)
    {
        _actualEnvironment[name] = func;
        Compiler.Environment[name] = func;
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
        string block = ctx.Arguments[0].Read<String>();
        
        Managed = false;
        await Runtime.DoStringAsync(block);
        Managed = true;
        return 0;
    });
}