using Lua;

namespace xenon.Core;

public class XenonArrayClass : XenonClass<XenonArrayClass>
{
    private static int Count(LuaTable arr) => XenonArrayUtil.Count(arr);

    private static async ValueTask<int> Set(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        LuaValue val = ctx.GetArgument(2);
        string expectedType = arr.Metatable!["__arrayType"].Read<string>();
        if (expectedType != XenonRT.GetType(val))
            throw ExceptionBuilder.TypeMismatch(expectedType, XenonRT.GetType(val), "array type");
        if (!key.TryRead(out int idx) || idx < 0)
            throw new IndexOutOfRangeException($"index {key} out of bounds of array");
        arr[idx] = val;
        return 0;
    }

    private static async ValueTask<int> Get(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        LuaTable arr = ctx.GetArgument<LuaTable>(0);
        LuaValue key = ctx.GetArgument(1);
        if (!key.TryRead(out int idx) || idx < 0 || idx >= Count(arr))
            throw new IndexOutOfRangeException($"index {key} out of bounds of array");
        ctx.Return(arr[idx]);
        return 1;
    }

    private static async ValueTask<int> Length(LuaFunctionExecutionContext ctx, CancellationToken ct)
    {
        ctx.Return(Count(ctx.GetArgument<LuaTable>(0)));
        return 1;
    }

    public static LuaTable CreateArray(string type, params LuaValue[] items)
    {
        LuaTable array = new();
        array.Metatable = new()
        {
            ["__type"] = XenonRT.T_ARRAY,
            ["__arrayType"] = type,
            ["__newindex"] = new LuaFunction("__set", Set),
            ["__index"] = new LuaFunction("__get", Get),
            ["__len"] = new LuaFunction("__length", Length),
        };
        for (int i = 0; i < items.Length; i++)
        {
            if (XenonRT.GetType(items[i]) != type)
                throw ExceptionBuilder.TypeMismatch(type, XenonRT.GetType(items[i]), "array type");
            array[i] = items[i];
        }
        return array;
    }

    private static bool IsTypeSpec(LuaValue val)
    {
        if (val.Type != LuaValueType.Table) return false;
        LuaTable spec = val.Read<LuaTable>();
        return spec.ContainsKey(1)
            && spec[1].Type == LuaValueType.String
            && spec[1].Read<string>().StartsWith("t_", StringComparison.Ordinal);
    }

    public async override ValueTask<LuaValue> Constructor(LuaTable args)
    {
        LuaTable source = args;
        if (!args.ContainsKey(2) && args.ContainsKey(1) && args[1].Type == LuaValueType.Table && !IsTypeSpec(args[1]))
        {
            LuaTable inner = args[1].Read<LuaTable>();
            if (IsTypeSpec(inner[0]) || IsTypeSpec(inner[1]))
                source = inner;
        }

        int typeKey = IsTypeSpec(source[0]) ? 0
            : IsTypeSpec(source[1]) ? 1
            : throw new XenonRuntimeException("array constructor: missing type specifier");

        string type = source[typeKey].Read<LuaTable>()[1].Read<string>();
        List<LuaValue> items = new();
        int itemStart = typeKey + 1;
        foreach (var kvp in source)
        {
            if (kvp.Key.TryRead(out int idx) && idx >= itemStart)
                items.Add(kvp.Value);
        }
        return CreateArray(type, items.ToArray());
    }

    private static async ValueTask<LuaValue> ArrayTransform(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (func.Metatable!["__returnType"].Read<string>() != type)
            throw ExceptionBuilder.TypeMismatch(type, func.Metatable!["__returnType"].Read<string>(), "transform return");
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        List<LuaValue> newArray = new();
        int i = 0;
        foreach (LuaValue item in XenonArrayUtil.Values(array))
        {
            LuaTable funcArgs = new() { ["item"] = item, ["index"] = i++ };
            newArray.Add((await XenonRT.Runtime.CallAsync(innerFunc, [func, funcArgs]))[0]);
        }
        return CreateArray(type, newArray.ToArray());
    }

    private static async ValueTask<LuaValue> ArrayFilter(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (func.Metatable!["__returnType"].Read<string>() != XenonRT.T_BOOLEAN)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_BOOLEAN, func.Metatable!["__returnType"].Read<string>(), "filter return");
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        List<LuaValue> newArray = new();
        int i = 0;
        foreach (LuaValue item in XenonArrayUtil.Values(array))
        {
            LuaTable funcArgs = new() { ["item"] = item, ["index"] = i++ };
            if ((await XenonRT.Runtime.CallAsync(innerFunc, [func, funcArgs]))[0].ToBoolean())
                newArray.Add(item);
        }
        return CreateArray(type, newArray.ToArray());
    }

    private static async ValueTask<LuaValue> ArraySlice(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        int length = Count(array);
        string type = array.Metatable!["__arrayType"].Read<string>();
        int startIdx = args[2].Read<int>();
        int endIdx = args[3].Read<int>();
        if (startIdx < 0 || startIdx > length || endIdx < 0 || endIdx > length || startIdx > endIdx)
            throw new IndexOutOfRangeException($"slice range [{startIdx}, {endIdx}) out of bounds");
        List<LuaValue> newArray = new();
        for (int i = startIdx; i < endIdx; i++)
            newArray.Add(array[i]);
        return CreateArray(type, newArray.ToArray());
    }

    private static async ValueTask<LuaValue> ArrayEnumerate(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        int i = 0;
        foreach (LuaValue item in XenonArrayUtil.Values(array))
        {
            LuaTable funcArgs = new() { ["item"] = item, ["index"] = i++ };
            await XenonRT.Runtime.CallAsync(innerFunc, [func, funcArgs]);
        }
        return LuaValue.Nil;
    }

    private static async ValueTask<LuaValue> ArrayMeasure(LuaTable args) => Count(args[1].Read<LuaTable>());

    private static async ValueTask<LuaValue> ArrayAppend(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (type != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(type, XenonRT.GetType(item), "array type");
        array[Count(array)] = item;
        return array;
    }

    private static async ValueTask<LuaValue> ArrayPrepend(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (type != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(type, XenonRT.GetType(item), "array type");
        LuaTable result = CreateArray(type, []);
        result[0] = item;
        int i = 1;
        foreach (LuaValue v in XenonArrayUtil.Values(array))
            result[i++] = v;
        return result;
    }

    private static async ValueTask<LuaValue> ArrayInsert(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        int idx = args[3].Read<int>();
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (type != XenonRT.GetType(item))
            throw ExceptionBuilder.TypeMismatch(type, XenonRT.GetType(item), "array type");
        if (idx < 0 || idx > Count(array))
            throw new IndexOutOfRangeException($"index {idx} out of bounds");
        List<LuaValue> items = XenonArrayUtil.Values(array).ToList();
        items.Insert(idx, item);
        return CreateArray(type, items.ToArray());
    }

    private static async ValueTask<LuaValue> ArrayContains(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        foreach (LuaValue v in XenonArrayUtil.Values(array))
            if (v.Equals(item)) return true;
        return false;
    }

    private static async ValueTask<LuaValue> ArrayFind(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaValue item = args[2];
        int i = 0;
        foreach (LuaValue v in XenonArrayUtil.Values(array))
        {
            if (v.Equals(item)) return i;
            i++;
        }
        return LuaValue.Nil;
    }

    private static async ValueTask<LuaValue> ArrayReduce(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        LuaValue acc = args[3];
        string returnType = func.Metatable!["__returnType"].Read<string>();
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        int i = 0;
        foreach (LuaValue item in XenonArrayUtil.Values(array))
        {
            LuaTable funcArgs = new() { ["acc"] = acc, ["item"] = item, ["index"] = i++ };
            acc = (await XenonRT.Runtime.CallAsync(innerFunc, [func, funcArgs]))[0];
        }
        return acc;
    }

    private static async ValueTask<LuaValue> ArrayReverse(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        string type = array.Metatable!["__arrayType"].Read<string>();
        List<LuaValue> items = XenonArrayUtil.Values(array).Reverse().ToList();
        return CreateArray(type, items.ToArray());
    }

    private static async ValueTask<LuaValue> ArrayFirst(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        if (Count(array) == 0) return LuaValue.Nil;
        return array[0];
    }

    private static async ValueTask<LuaValue> ArrayLast(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        int c = Count(array);
        if (c == 0) return LuaValue.Nil;
        return array[c - 1];
    }

    private static async ValueTask<LuaValue> ArrayRemove(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        int idx = args[2].Read<int>();
        string type = array.Metatable!["__arrayType"].Read<string>();
        List<LuaValue> items = XenonArrayUtil.Values(array).ToList();
        if (idx < 0 || idx >= items.Count)
            throw new IndexOutOfRangeException($"index {idx} out of bounds");
        items.RemoveAt(idx);
        return CreateArray(type, items.ToArray());
    }

    private static async ValueTask<LuaValue> ArraySort(LuaTable args)
    {
        LuaTable array = args[1].Read<LuaTable>();
        LuaTable func = args[2].Read<LuaTable>();
        string type = array.Metatable!["__arrayType"].Read<string>();
        if (func.Metatable!["__returnType"].Read<string>() != XenonRT.T_BOOLEAN)
            throw ExceptionBuilder.TypeMismatch(XenonRT.T_BOOLEAN, func.Metatable!["__returnType"].Read<string>(), "sort comparator");
        LuaFunction innerFunc = func.Metatable!["__call"].Read<LuaFunction>();
        List<LuaValue> items = XenonArrayUtil.Values(array).ToList();
        for (int i = 0; i < items.Count; i++)
        {
            for (int j = i + 1; j < items.Count; j++)
            {
                LuaTable cmpArgs = new() { ["a"] = items[i], ["b"] = items[j] };
                bool keepOrder = (await XenonRT.Runtime.CallAsync(innerFunc, [func, cmpArgs]))[0].ToBoolean();
                if (!keepOrder)
                    (items[i], items[j]) = (items[j], items[i]);
            }
        }
        return CreateArray(type, items.ToArray());
    }

    public override Dictionary<string, XenonClassMethod> Methods => new()
    {
        ["transform"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("transformer", XenonRT.T_FUNCTION) }, Method = ArrayTransform },
        ["filter"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("predicate", XenonRT.T_FUNCTION) }, Method = ArrayFilter },
        ["slice"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("start", XenonRT.T_NUMBER), [3] = ("end", XenonRT.T_NUMBER) }, Method = ArraySlice },
        ["enumerate"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("enumerator", XenonRT.T_FUNCTION) }, Method = ArrayEnumerate },
        ["measure"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY) }, Method = ArrayMeasure },
        ["append"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("item", XenonRT.T_ANY) }, Method = ArrayAppend },
        ["prepend"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("item", XenonRT.T_ANY) }, Method = ArrayPrepend },
        ["insert"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("item", XenonRT.T_ANY), [3] = ("index", XenonRT.T_NUMBER) }, Method = ArrayInsert },
        ["contains"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("item", XenonRT.T_ANY) }, Method = ArrayContains },
        ["find"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("item", XenonRT.T_ANY) }, Method = ArrayFind },
        ["reduce"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("folder", XenonRT.T_FUNCTION), [3] = ("initial", XenonRT.T_ANY) }, Method = ArrayReduce },
        ["reverse"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY) }, Method = ArrayReverse },
        ["first"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY) }, Method = ArrayFirst },
        ["last"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY) }, Method = ArrayLast },
        ["remove"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("index", XenonRT.T_NUMBER) }, Method = ArrayRemove },
        ["sort"] = new() { Arguments = new() { [1] = ("array", XenonRT.T_ARRAY), [2] = ("comparator", XenonRT.T_FUNCTION) }, Method = ArraySort },
    };
    public override string Name => "array";
}