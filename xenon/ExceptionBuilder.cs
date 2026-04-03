namespace xenon;

public class XenonRuntimeException : Exception
{
    public XenonRuntimeException(string msg) : base(msg) { }
}

public class XenonSyntaxException : XenonRuntimeException
{
    public XenonSyntaxException(string msg) : base(msg) { }
}

public class ExceptionBuilder
{
    public static XenonRuntimeException IndexOpaqueType(string type, string? nuance = null) 
        => new($"cannot index into opaque type {type}" + (nuance is null ? "" : $" ({nuance})"));
    public static XenonRuntimeException ModifyImmutable(string type, string name, string? nuance = null) 
        => new($"cannot modify immutable {type} {name}" + (nuance is null ? "" : $" ({nuance})"));
    public static XenonRuntimeException TypeMismatch(string expected, string got, string? nuance = null) 
        => new($"expected type {expected}, got {got}" + (nuance is null ? "" : $" ({nuance})"));
    public static XenonRuntimeException MissingMember(string property, string type, string? nuance = null)
        => new($"missing member {property} in type {type}" + (nuance is null ? "" : $" ({nuance})"));
    public static XenonRuntimeException ReadOnlyProperty(string property, string type, string? nuance = null)
        => new($"property {property} in type {type} is readonly" + (nuance is null ? "" : $" ({nuance})"));
    public static XenonRuntimeException InvalidKeywordOperation(string op, string keyword, string? nuance = null)
        => new($"cannot perform {op} on keyword {keyword}" + (nuance is null ? "" : $" ({nuance})"));
    
    public static XenonSyntaxException SyntaxMissingArg(string keyword, string arg, string? nuance = null)
        => new($"missing arg {arg} on keyword {keyword}" + (nuance is null ? "" : $" ({nuance})"));
}