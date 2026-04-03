using Lua;

namespace xenon;

public abstract class XenonKeyword<T> where T : class, new()
{
    public abstract ValueTask<LuaTable> Constructor(LuaTable args);
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
            return ctx.Return(func);
        else
            throw ExceptionBuilder.MissingProperty(key, Name, "keyword");
    }

    private async ValueTask<int> _set(LuaFunctionExecutionContext ctx, CancellationToken _)
        => throw ExceptionBuilder.InvalidKeywordOperation("set", Name);

    public abstract Dictionary<string, LuaFunction> Methods { get; }
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
                keyword[kvp.Key] = kvp.Value;
            
            keyword.Metatable =  meta;
            return keyword;
        }
    }

    public static LuaTable Static() => (Activator.CreateInstance<T>() as XenonKeyword<T>)!.Keyword;
}