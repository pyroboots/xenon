# What?

A syntactically sugarcoated version of Lua disguised as a programming language because I'm too lazy for parsing

# Where?

- [`XenonRT.cs`](https://github.com/pyroboots/xenon/blob/master/xenon/XenonRT.cs) for bootstrapping and injection
- [`xenon/Core`](https://github.com/pyroboots/xenon/tree/master/xenon/Core) for keyword implementation (`func`, `array`, `type`, etc...)
  - [`XenonKeyword.cs`](https://github.com/pyroboots/xenon/blob/master/xenon/XenonKeyword.cs) for keyword table construction
- [`test.xnn`](https://github.com/pyroboots/xenon/blob/master/test.xnn) for example syntax

# Why?

I've made multiple attempts in the past, some simple, some too complex, but never really got further than a tokenizer or an AST. So why not skip the whole thing and use Lua as a backbone instead since it's syntax is already so malleable?
