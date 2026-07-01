using Lua;

namespace xenon;

public static class XenonRepl
{
    public static async Task RunAsync()
    {
        XenonRT.Reset();
        XenonRT.Bootstrap();
        Console.WriteLine("xenon repl — type 'exit' or press Ctrl+D to quit");

        int lineNo = 1;
        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line is null || line.Trim() == "exit")
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string chunkName = $"repl:{lineNo++}";
            try
            {
                await XenonRT.Runtime.DoStringAsync(line, chunkName);
            }
            catch (LuaRuntimeException ex)
            {
                Console.Error.WriteLine(XenonScriptRunner.FormatRuntimeError(chunkName, ex));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}