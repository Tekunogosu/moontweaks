# Third-party notices

MoonTweaks is distributed as a single assembly, `moontweaks.dll`, which is
compiled from this project's sources together with the vendored MoonSharp
interpreter. That makes every binary release a redistribution of MoonSharp, so
its notice travels with the build and is reproduced in full below.

MoonTweaks itself is under the MIT License; see `LICENSE`.

Vintage Story's assemblies are referenced at build time and are never
redistributed, so they carry no notice here.

---

## MoonSharp

<https://github.com/moonsharp-devs/moonsharp>, vendored from the fork at
<https://github.com/Tekunogosu/moonsharp>, which adds one upstream-bound fix and
compiles the sources under a disabled nullable context. Licensed under the
3-clause BSD License, reproduced verbatim:

```
Copyright (c) 2014-2016, Marco Mastropaolo
All rights reserved.

Parts of the string library are based on the KopiLua project (https://github.com/NLua/KopiLua)
Copyright (c) 2012 LoDC

Visual Studio Code debugger code is based on code from Microsoft vscode-mono-debug project (https://github.com/Microsoft/vscode-mono-debug).
Copyright (c) Microsoft Corporation - released under MIT license.

Remote Debugger icons are from the Eclipse project (https://www.eclipse.org/).
Copyright of The Eclipse Foundation

The MoonSharp icon is (c) Isaac, 2014-2015

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the {organization} nor the names of its
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
