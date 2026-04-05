using Lua;

namespace xenon;

public abstract class XenonClass<T> where T : class, new()
{
    public abstract ValueTask<LuaValue> Constructor(LuaTable args);
    private async ValueTask<int> _constructor(LuaFunctionExecutionContext ctx, CancellationToken _)
        => ctx.Return(await Constructor(ctx.GetArgument<LuaTable>(1)));

    private async ValueTask<int> _length(LuaFunctionExecutionContext ctx, CancellationToken _)
        => throw ExceptionBuilder.InvalidKeywordOperation("get length", Name);

    private async ValueTask<int> _toString(LuaFunctionExecutionContext ctx, CancellationToken _)
        => throw ExceptionBuilder.InvalidKeywordOperation("cast to string", Name);
    
    private async ValueTask<int> _get(LuaFunctionExecutionContext ctx, CancellationToken _)
    {
        string key = ctx.GetArgument<string>(1);

        if (Methods.TryGetValue(key, out var func))
        {
            LuaFunction wrapper = new LuaFunction(key, async (ctx, _) =>
                ctx.Return(await func.Method(ctx.GetArgument<LuaTable>(0))));
            return ctx.Return(wrapper);
        }
        else
            throw ExceptionBuilder.MissingMember(key, Name, "keyword");
    }

    private async ValueTask<int> _set(LuaFunctionExecutionContext ctx, CancellationToken _)
        => throw ExceptionBuilder.InvalidKeywordOperation("set", Name);

    public struct XenonClassMethod
    {
        public required Dictionary<LuaValue, string> Arguments;
        public required Func<LuaTable, ValueTask<LuaValue>> Method;
    }
    
    public abstract Dictionary<string, XenonClassMethod> Methods { get; }
    public abstract string Name { get; }

    public LuaTable Keyword
    {
        get
        {
            LuaTable keyword = new();
            LuaTable meta = new()
            {
                ["__call"] = new LuaFunction("__ctor", _constructor),
                ["__index"] = new LuaFunction("__get", _get),
                ["__newindex"] = new LuaFunction("__set", _set),
                ["__len"] = new LuaFunction("__length", _length),
                ["__tostring"] = new LuaFunction("__string", _toString),
            };
            foreach (var kvp in Methods)
            {
                LuaFunction wrapper = new LuaFunction(kvp.Key, async (ctx, _) =>
                {
                    // function call argument checking at runtime
                    LuaTable args = ctx.GetArgument<LuaTable>(0);
                    if (args.ArrayLength != kvp.Value.Arguments.Count)
                        throw ExceptionBuilder.SyntaxIncorrectArgCount(kvp.Value.Arguments.Count, args.ArrayLength, "correct call layout: " + string.Join(' ', kvp.Value.Arguments.Values));

                    foreach (var argument in kvp.Value.Arguments)
                        if (args.ContainsKey(argument.Key) == false)
                            throw ExceptionBuilder.SyntaxMissingArg(Name, argument.Value, argument.Key.Read<string>());
                    
                    return ctx.Return(await kvp.Value.Method(ctx.GetArgument<LuaTable>(0)));
                });
                
                keyword[kvp.Key] = wrapper;
            }
            
            keyword.Metatable =  meta;
            return keyword;
        }
    }

    public static LuaTable Static() => (Activator.CreateInstance<T>() as XenonClass<T>)!.Keyword;
}