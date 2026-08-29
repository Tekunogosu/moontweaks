# Third-party notices

MoonTweaks is distributed as `moontweaks.dll`, compiled from this project's
sources, and `Lua.dll`, which is Lua-CSharp shipped beside it. That makes every
binary release a redistribution of Lua-CSharp, so its notice travels with the
build and is reproduced in full below.

MoonTweaks itself is under the MIT License; see `LICENSE`.

Vintage Story's assemblies are referenced at build time and are never
redistributed, so they carry no notice here.

---

## Lua-CSharp

<https://github.com/nuskey8/Lua-CSharp>, a Lua interpreter written in C#, shipped
beside the mod as `Lua.dll` rather than compiled into it. The interpreter every
script runs on, and the one measured by
`scripts/bench.sh`. Licensed under the MIT License, reproduced verbatim:

```
MIT License

Copyright (c) 2025 Yusuke Nakada

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
