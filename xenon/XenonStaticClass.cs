using Lua;

namespace xenon;

public abstract class XenonStaticClass<T> where T : class, new()
{
    private async ValueTask<int> _length(LuaFunctionExecutionContext ctx, CancellationToken _)
        => throw ExceptionBuilder.InvalidKeywordOperation("get length", Name);

    private async ValueTask<int> _toString(LuaFunctionExecutionContext ctx, CancellationToken _)
        => ctx.Return($"XenonClass: " + Name.GetHashCode());
    
    private async ValueTask<int> _get(LuaFunctionExecutionContext ctx, CancellationToken _)
    {
        string key = ctx.GetArgument<string>(1);

        if (Methods.TryGetValue(key, out var func))
        {
            LuaFunction wrapper = new LuaFunction(key, async (ctx, _) =>
                ctx.Return(await func.Method(ctx.GetArgument<LuaTable>(0))));
            return ctx.Return(wrapper);
        }
        else if (Properties.TryGetValue(key, out var prop))
            return ctx.Return(prop);
        else
            throw ExceptionBuilder.MissingMember(key, Name, "keyword");
    }

    private async ValueTask<int> _set(LuaFunctionExecutionContext ctx, CancellationToken _)
        => throw ExceptionBuilder.InvalidKeywordOperation("set", Name);
    private async ValueTask<int> _constructor(LuaFunctionExecutionContext ctx, CancellationToken _)
        => throw ExceptionBuilder.InvalidKeywordOperation("invoke", Name, "static");
    
    public abstract Dictionary<string, XenonClass<T>.XenonClassMethod> Methods { get; }
    public abstract Dictionary<string, LuaValue> Properties { get; }
    public abstract string Name { get; }

    public LuaTable Keyword
    {
        get
        {
            LuaTable keyword = new();
            LuaTable meta = new()
            {
                ["__call"] = new LuaFunction($"__{Name}__ctor", _constructor),
                ["__index"] = new LuaFunction($"__{Name}__get", _get),
                ["__newindex"] = new LuaFunction($"__{Name}__set", _set),
                ["__len"] = new LuaFunction($"__{Name}__length", _length),
                ["__tostring"] = new LuaFunction($"__{Name}__string", _toString),
            };
            foreach (var kvp in Methods)
            {
                LuaFunction wrapper = new LuaFunction(kvp.Key, async (ctx, _) =>
                {
                    // function call argument checking at runtime
                    LuaTable args = ctx.GetArgument<LuaTable>(0);
                    if (args.ArrayLength != kvp.Value.Arguments.Count)
                        throw ExceptionBuilder.SyntaxIncorrectArgCount(kvp.Value.Arguments.Count, args.ArrayLength, "correct call pattern: " + string.Join(' ', kvp.Value.Arguments.Values));

                    foreach (var argument in kvp.Value.Arguments)
                    {
                        if (args.ContainsKey(argument.Key) == false)
                            throw ExceptionBuilder.SyntaxMissingArg(Name, argument.Value.Name, argument.Key.Read<string>());
                        if (argument.Value.Type != XenonRT.T_ANY && (XenonRT.GetType(args[argument.Key]) != argument.Value.Type))
                            throw ExceptionBuilder.TypeMismatch(argument.Value.Type, XenonRT.GetType(args[argument.Key]), $"argument {argument.Key.ToString()}");
                    }
                    
                    return ctx.Return(await kvp.Value.Method(ctx.GetArgument<LuaTable>(0)));
                });
                
                keyword[kvp.Key] = wrapper;
            }
            
            keyword.Metatable =  meta;
            return keyword;
        }
    }

    public static LuaTable Static() => (Activator.CreateInstance<T>() as XenonStaticClass<T>)!.Keyword;
}