using Lua;
using xenon.Core;
using xenon.Libs;

namespace xenon;

/// <summary>
/// Central registration for Xenon builtins. Add new keywords, static libraries,
/// or core hooks here instead of growing XenonRT.Bootstrap().
/// </summary>
public static class XenonBootstrap
{
    private static bool _initialized;
    private static readonly Lock _initLock = new();
    private static readonly string[] BuiltinTypes =
    [
        XenonRT.T_STRING,
        XenonRT.T_NUMBER,
        XenonRT.T_BOOLEAN,
        XenonRT.T_FUNCTION,
        XenonRT.T_LUA_FUNCTION,
        XenonRT.T_VOID,
        XenonRT.T_ANY,
        XenonRT.T_ARRAY,
        XenonRT.T_DICTIONARY,
    ];

    private static readonly (string Name, Action Register)[] KeywordClasses =
    [
        ("func", () => XenonRT.RegisterClass<XenonFunctionClass>("func")),
        ("type", () => XenonRT.RegisterClass<XenonTypeClass>("type")),
        ("enum", () => XenonRT.RegisterClass<XenonEnumClass>("enum")),
        ("array", () => XenonRT.RegisterClass<XenonArrayClass>("array")),
        ("str", () => XenonRT.RegisterClass<XenonStringClass>("str")),
        ("bool", () => XenonRT.RegisterClass<XenonBoolClass>("bool")),
        ("dict", () => XenonRT.RegisterClass<XenonDictionaryClass>("dict")),
    ];

    private static readonly Action[] StaticLibraries =
    [
        () => XenonRT.RegisterStaticClass<XenonMathLibrary>(),
        () => XenonRT.RegisterStaticClass<XenonNumberLibrary>(),
        () => XenonRT.RegisterStaticClass<XenonFsLibrary>(),
        () => XenonRT.RegisterStaticClass<XenonJsonLibrary>(),
        () => XenonRT.RegisterStaticClass<XenonTimeLibrary>(),
        () => XenonRT.RegisterStaticClass<XenonPathLibrary>(),
        () => XenonRT.RegisterStaticClass<XenonEnumLibrary>(),
    ];

    private static readonly (string Name, LuaFunction Func)[] BuiltinFunctions =
    [
        ("typeof", XenonRT.Typeof),
        ("unmanaged", XenonRT.UnmanagedBlock),
    ];

    internal static void Reset() => _initialized = false;

    public static void Initialize()
    {
        lock (_initLock)
        {
            if (_initialized)
                return;

            RegisterBuiltinTypes();
            XenonBasicLibrary.Register();
            RegisterKeywordClasses();
            RegisterStaticLibraries();
            RegisterBuiltinFunctions();
            XenonRT.InstallRuntimeEnvironmentGuards();
            _initialized = true;
        }
    }

    private static void RegisterBuiltinTypes()
    {
        foreach (string type in BuiltinTypes)
            XenonRT.RegisterType(type);

        XenonRT.SetImmutability("void", "keyword");
    }

    private static void RegisterKeywordClasses()
    {
        foreach (var (_, register) in KeywordClasses)
            register();
    }

    private static void RegisterStaticLibraries()
    {
        foreach (Action register in StaticLibraries)
            register();
    }

    private static void RegisterBuiltinFunctions()
    {
        foreach (var (name, func) in BuiltinFunctions)
        {
            XenonRT.RegisterFunc(name, func);
            XenonRT.SetImmutability(name, "keyword");
        }
    }
}