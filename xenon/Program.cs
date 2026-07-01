namespace xenon;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "--repl" or "-r")
        {
            await XenonRepl.RunAsync();
            return;
        }

        string path = args.Length > 0 ? args[0] : "test.xnn";
        await XenonScriptRunner.RunFileAsync(path);
    }
}