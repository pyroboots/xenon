using Lua;
using Lua.Standard;
using xenon.Libs;

namespace xenon;

class Program
{
    static async Task Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : "test.xnn";
        string file = File.ReadAllText(path);
        
        XenonRT.Bootstrap();
        
        string chunkName = "@" + Path.GetFullPath(path).Replace('\\', '/');
        await XenonRT.Runtime.DoStringAsync(file, chunkName);
    }
}