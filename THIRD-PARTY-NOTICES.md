# Third-party notices

MoonTweaks is distributed as `moontweaks.dll`, compiled from this project's
sources, and `Lua.dll`, which is Lua-CSharp shipped beside it. That makes every
binary release a redistribution of Lua-CSharp, so its notice travels with the
build and is reproduced in full below.

MoonTweaks itself is under the MIT License; see `LICENSE`.

The documentation site is a separate redistribution with a dependency of its own:
it carries highlight.js, which colours the Lua examples on the reference page. That
notice is reproduced below as well. Nothing of it reaches the mod — no release zip
contains it.

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

---

## highlight.js

<https://github.com/highlightjs/highlight.js>, the syntax highlighter the generated
reference page loads. Version 11.11.2, vendored under `third_party/highlight.js`
rather than fetched from a content delivery network, and copied into `docs/` beside
the page that loads it — where a copy of this licence goes with it, as
`LICENSE.highlight.txt`. `third_party/highlight.js/README.md` records where each
file came from and its digest. Licensed under the BSD 3-Clause License, reproduced
verbatim:

```
BSD 3-Clause License

Copyright (c) 2006-2019, Ivan Sagalaev.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the copyright holder nor the names of its
  contributors may be used to endorse or promote products derived from
  this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```
