# What?

A syntactically sugarcoated version of Lua disguised as a programming language because I'm too lazy for parsing

# Where?

- [`XenonRT.cs`](xenon/XenonRT.cs) for runtime state and registration primitives
- [`XenonBootstrap.cs`](xenon/XenonBootstrap.cs) for bootstrapping keywords, stdlibs, and builtins
- [`xenon/Core`](xenon/Core) for keyword implementation (`func`, `array`, `type`, etc...)
  - [`XenonClass.cs`](xenon/XenonClass.cs) for instance keyword table construction
  - [`XenonStaticClass.cs`](xenon/XenonStaticClass.cs) for static stdlib keyword construction
- [`xenon/Libs`](xenon/Libs) for standard libraries (`math`, `number`, `console`, etc.)
- [`test.xnn`](test.xnn) for example syntax
- [`xenon.Tests`](xenon.Tests) for automated script and bootstrap tests

# Why?

I've made multiple attempts in the past, some simple, some too complex, but never really got further than a tokenizer or an AST. So why not skip the whole thing and use Lua as a backbone instead since it's syntax is already so malleable?

# Adding a new stdlib

1. Create a class in `xenon/Libs` extending `XenonStaticClass<T>`
2. Add one line to the `StaticLibraries` array in [`XenonBootstrap.cs`](xenon/XenonBootstrap.cs)

# Enums and type methods

```xenon
Color = enum {[[Color]], [[Red]], [[Green]], Blue = 5}

Person = type {[[Person]],
    name = t_str,
    color = t_Color,
    greet = func {{}, [[
        console.outLn{this.name}
    ]], t_void},
}

john = Person { name = "John", color = Color.Red }
john.greet{}

Animal = type {[[Animal]], name = t_str}
Dog = type {[[Dog]], extends = Animal, breed = t_str}
```

# Builtins and stdlibs

- **Builtins:** `typeof`, `cast`, `try`, `import`, `forEach`, `assert`, `error`, `unmanaged`
- **Stdlibs:** `array`, `dict`, `str`, `bool`, `number`, `math`, `fs`, `json`, `time`, `path`, `enumlib`, `console`

```xenon
forEach {arr, "item", [[ console.outLn{a.item} ]]}
result = try{myFunc, {x = 1}}
import {"./utils.xnn"}
data = json.parse{fs.read{"config.json"}}
```

# Running

```bash
dotnet run --project xenon -- test.xnn
dotnet run --project xenon -- --repl
dotnet test
```