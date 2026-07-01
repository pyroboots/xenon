using Lua;

namespace xenon.Core;

public class XenonTypeClass : XenonClass<XenonTypeClass>
{
    private const string PROPERTY_TYPE_KEY = "__xenon_typemap";
    private const string PROPERTY_READONLY_KEY = "__xenon_readonlymap";
    private const string PROPERTY_OPTIONAL_KEY = "__xenon_optionalmap";
    private const string METHOD_MAP_KEY = "__xenon_methodmap";
    private const string VALUE_PROXY_KEY = "__xenon_vals";
    private const string IS_INSTANCE_KEY = "__xenon_instance";

    private static bool IsTypeMethod(LuaTable table)
    {
        LuaTable? meta = table.Metatable;
        if (meta == null)
            return false;

        if (meta["__type"].Type != LuaValueType.String)
            return false;

        return meta["__type"].Read<string>() == XenonRT.T_FUNCTION;
    }

    private static bool IsInstance(LuaTable tbl) => tbl.ContainsKey(IS_INSTANCE_KEY);

    private static void MergeMaps(LuaTable target, LuaTable source)
    {
        foreach (var kvp in source)
            target[kvp.Key] = kvp.Value;
    }

    private static LuaTable? ResolveParentTemplate(LuaTable args)
    {
        foreach (var kvp in args)
        {
            if (kvp.Key.Type != LuaValueType.String || kvp.Key.Read<string>() != "extends")
                continue;
            if (kvp.Value.Type != LuaValueType.Table)
                throw new XenonRuntimeException("extends must reference a type template");
            return kvp.Value.Read<LuaTable>();
        }

        return null;
    }

    private static LuaTable BindInstanceMethod(LuaTable instance, LuaTable funcTable)
    {
        LuaFunction innerFunc = funcTable.Metatable!["__call"].Read<LuaFunction>();
        LuaTable bound = new();
        bound.Metatable = new()
        {
            ["__type"] = XenonRT.T_FUNCTION,
            ["__returnType"] = funcTable.Metatable!["__returnType"],
            ["__funcArgs"] = funcTable.Metatable!["__funcArgs"],
            ["__call"] = new LuaFunction("__boundMethod", async (ctx, _) =>
            {
                LuaTable callArgs = ctx.GetArgument<LuaTable>(1);
                LuaValue[] results = await XenonRT.Runtime.CallAsync(innerFunc, [instance, callArgs]);
                foreach (LuaValue result in results)
                    ctx.Return(result);
                return results.Length;
            }),
            ["__index"] = new LuaFunction("__get", async (_, _) => throw ExceptionBuilder.IndexOpaqueType(XenonRT.T_FUNCTION)),
            ["__newindex"] = new LuaFunction("__set", async (_, _) => throw ExceptionBuilder.IndexOpaqueType(XenonRT.T_FUNCTION)),
        };
        return bound;
    }

    private static async ValueTask<int> TypeConstructor(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable template = ctx.GetArgument<LuaTable>(0);
        LuaTable args = ctx.GetArgument<LuaTable>(1);
        LuaTable typeMap = template[PROPERTY_TYPE_KEY].Read<LuaTable>();
        LuaTable methodMap = template[METHOD_MAP_KEY].Read<LuaTable>();
        LuaTable optionalMap = template[PROPERTY_OPTIONAL_KEY].Read<LuaTable>();
        string typeName = template.Metatable!["__type"].Read<string>();
        LuaTable valProxy = new();

        foreach (var field in typeMap)
        {
            string k = field.Key.Read<string>();
            if (methodMap.ContainsKey(k))
                continue;

            if (!args.ContainsKey(k))
            {
                if (optionalMap[k].ToBoolean())
                    continue;
                throw ExceptionBuilder.SyntaxMissingArg(typeName, k, "constructor");
            }
        }

        foreach (var kvp in args)
        {
            string k = kvp.Key.Read<string>();
            LuaValue v = kvp.Value;

            if (methodMap.ContainsKey(k))
                throw ExceptionBuilder.MethodNotAssignable(k, typeName, "cannot pass methods in constructor");

            if (!typeMap.ContainsKey(k))
                throw ExceptionBuilder.MissingMember(k, typeName, "unknown constructor field");

            string expectedType = typeMap[k].Read<string>();
            string actualType = XenonRT.GetType(v);
            if (expectedType != actualType)
                throw ExceptionBuilder.TypeMismatch(expectedType, actualType);
            else valProxy[k] = v;
        }

        LuaTable instance = new()
        {
            [PROPERTY_TYPE_KEY] = typeMap,
            [PROPERTY_READONLY_KEY] = template[PROPERTY_READONLY_KEY],
            [PROPERTY_OPTIONAL_KEY] = optionalMap,
            [METHOD_MAP_KEY] = methodMap,
            [VALUE_PROXY_KEY] = valProxy,
            [IS_INSTANCE_KEY] = true,
        };
        instance.Metatable = template.Metatable;
        ctx.Return(instance);

        return 1;
    }

    private static async ValueTask<int> TypeGet(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        string typeName = tbl.Metatable!["__type"].Read<string>();

        LuaTable methodMap = tbl[METHOD_MAP_KEY].Read<LuaTable>();
        if (methodMap.ContainsKey(key))
        {
            ctx.Return(BindInstanceMethod(tbl, methodMap[key].Read<LuaTable>()));
            return 1;
        }

        if (!IsInstance(tbl))
            throw ExceptionBuilder.MissingMember(key.Read<string>(), typeName, "type template has no fields");

        LuaTable valProxy = tbl[VALUE_PROXY_KEY].Read<LuaTable>();
        if (valProxy[key].Type == LuaValueType.Nil)
            throw ExceptionBuilder.MissingMember(key.Read<string>(), typeName);
        else ctx.Return(valProxy[key]);
        return 1;
    }

    private static async ValueTask<int> TypeSet(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        if (!IsInstance(tbl))
            throw ExceptionBuilder.InvalidKeywordOperation("set field on", "type", "use an instance");

        LuaTable readonlyMap = tbl[PROPERTY_READONLY_KEY].Read<LuaTable>();
        LuaTable valProxy = tbl[VALUE_PROXY_KEY].Read<LuaTable>();
        LuaTable typeMap = tbl[PROPERTY_TYPE_KEY].Read<LuaTable>();
        LuaTable methodMap = tbl[METHOD_MAP_KEY].Read<LuaTable>();
        LuaValue key = ctx.GetArgument(1);
        LuaValue val = ctx.GetArgument(2);
        string typeName = tbl.Metatable!["__type"].Read<string>();

        if (methodMap.ContainsKey(key))
            throw ExceptionBuilder.MethodNotAssignable(key.Read<string>(), typeName);

        if (readonlyMap[key].ToBoolean())
            throw ExceptionBuilder.ReadOnlyProperty(key.Read<string>(), typeName);
        if (typeMap.ContainsKey(key) == false)
            throw ExceptionBuilder.MissingMember(key.Read<string>(), typeName);

        string expectedType = typeMap[key].Read<string>();
        string actualType = XenonRT.GetType(val);
        if (expectedType != actualType)
            throw ExceptionBuilder.TypeMismatch(expectedType, actualType);

        valProxy[key] = val;
        return 0;
    }

    private static async ValueTask<int> TypeLength(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        if (!IsInstance(tbl))
            throw ExceptionBuilder.InvalidKeywordOperation("measure", "type");

        LuaTable valProxy = tbl[VALUE_PROXY_KEY].Read<LuaTable>();
        int count = 0;
        foreach (var _ in valProxy)
            count++;

        ctx.Return(count);
        return 1;
    }

    private static async ValueTask<int> TypeToString(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable tbl = ctx.GetArgument<LuaTable>(0);
        ctx.Return(tbl.Metatable!["__type"].Read<string>());
        return 1;
    }

    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        if (!args.ContainsKey(1))
            throw ExceptionBuilder.SyntaxMissingArg(Name, "type name", "argument 1");
        string name = args[1].Read<string>();
        XenonRT.RegisterType($"t_{name}");

        LuaTable type = new();
        LuaTable meta = new()
        {
            ["__call"] = new LuaFunction($"{name}__ctor", TypeConstructor),
            ["__index"] = new LuaFunction($"{name}__get", TypeGet),
            ["__newindex"] = new LuaFunction($"{name}__set", TypeSet),
            ["__len"] = new LuaFunction($"{name}__length", TypeLength),
            ["__tostring"] = new LuaFunction($"{name}__string", TypeToString),
            ["__type"] = name,
        };
        LuaTable propertyTypeMap = new();
        LuaTable propertyReadonlyMap = new();
        LuaTable propertyOptionalMap = new();
        LuaTable methodMap = new();

        LuaTable? parentTemplate = ResolveParentTemplate(args);
        if (parentTemplate != null)
        {
            MergeMaps(propertyTypeMap, parentTemplate[PROPERTY_TYPE_KEY].Read<LuaTable>());
            MergeMaps(propertyReadonlyMap, parentTemplate[PROPERTY_READONLY_KEY].Read<LuaTable>());
            MergeMaps(propertyOptionalMap, parentTemplate[PROPERTY_OPTIONAL_KEY].Read<LuaTable>());
            MergeMaps(methodMap, parentTemplate[METHOD_MAP_KEY].Read<LuaTable>());
        }

        foreach (var kvp in args.Skip(1))
        {
            string propName = kvp.Key.Read<string>();
            if (propName == "extends")
                continue;

            LuaValueType valType = kvp.Value.Type;

            if (valType == LuaValueType.Table)
            {
                LuaTable valTbl = kvp.Value.Read<LuaTable>();
                if (IsTypeMethod(valTbl))
                {
                    methodMap[propName] = valTbl;
                    continue;
                }

                if (valTbl["const"].Type == LuaValueType.Boolean) propertyReadonlyMap[propName] = valTbl["const"];
                if (valTbl["optional"].Type == LuaValueType.Boolean) propertyOptionalMap[propName] = valTbl["optional"];
                if (valTbl["type"].Type == LuaValueType.String) propertyTypeMap[propName] = valTbl["type"];
            }
            else if (valType == LuaValueType.String)
                propertyTypeMap[propName] = kvp.Value.Read<string>();
        }

        type.Metatable = meta;
        type[PROPERTY_TYPE_KEY] = propertyTypeMap;
        type[PROPERTY_READONLY_KEY] = propertyReadonlyMap;
        type[PROPERTY_OPTIONAL_KEY] = propertyOptionalMap;
        type[METHOD_MAP_KEY] = methodMap;
        return type;
    }

    public override Dictionary<string, XenonClassMethod> Methods => new();
    public override string Name => "type";
}