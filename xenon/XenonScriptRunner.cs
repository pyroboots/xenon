using Lua;
using Lua.Standard;

namespace xenon;

public static class XenonScriptRunner
{
    private static readonly Lock _runLock = new();

    public static async Task RunAsync(string source, string chunkName = "xenon:test", bool isolated = true)
    {
        lock (_runLock)
        {
            if (isolated)
                XenonRT.Reset();
            XenonRT.Bootstrap();
        }

        try
        {
            await XenonRT.Runtime.DoStringAsync(source, chunkName);
        }
        catch (LuaRuntimeException ex)
        {
            throw new XenonRuntimeException(FormatRuntimeError(chunkName, ex), ex);
        }
    }

    internal static string FormatRuntimeError(string chunkName, LuaRuntimeException ex)
    {
        string location = string.IsNullOrWhiteSpace(chunkName) ? "script" : chunkName;
        string message = ex.Message.Trim();
        if (message.StartsWith(location, StringComparison.Ordinal))
            return message;
        return $"{location}: {message}";
    }

    public static async Task RunFileAsync(string path)
    {
        string source = await File.ReadAllTextAsync(path);
        string chunkName = "@" + Path.GetFullPath(path).Replace('\\', '/');
        await RunAsync(source, chunkName);
    }
}