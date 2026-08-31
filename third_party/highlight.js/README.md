# highlight.js

Colours the Lua examples on the generated reference page. Vendored rather than
fetched from a content delivery network: the site then loads nothing from anybody
else, works from a checkout with no network at all, and cannot change under us
between one visit and the next.

| | |
| --- | --- |
| Version | 11.11.2 |
| Licence | BSD 3-Clause, `LICENSE` beside this file |
| Upstream | <https://github.com/highlightjs/highlight.js> |
| Taken from | <https://github.com/highlightjs/cdn-release> at tag `11.11.2` |
| Commit | `597126c27160a33f9e8a54f129f9cff40d4980a9` |

`highlight.min.js` is the project's own published browser build, which carries about
forty common languages, Lua among them. The two stylesheets are its GitHub themes,
one per colour scheme.

## What is here

| File | From | SHA-256 |
| --- | --- | --- |
| `highlight.min.js` | `build/highlight.min.js` | `62960a35954a685dbe12958092f661a185231e9f5f5c44dc3c1e237d9e087d5a` |
| `github.min.css` | `build/styles/github.min.css` | `3a9a5def8b9c311e5ae43abde85c63133185eed4f0d9f67fea4b00a8308cf066` |
| `github-dark.min.css` | `build/styles/github-dark.min.css` | `9f208d022102b1d0c7aebfecd8e42ca7997d5de636649d2b31ea63093d809019` |
| `LICENSE` | `LICENSE` | `5f289f36595e0ef6c53d9f4b4e51d7cc1efc5e2b3ba6130a875d177c54789eaf` |

## Updating it

Pick a tag from the CDN release repository, take the same four paths from it, and
rewrite the version, the commit and the digests above. The build copies whatever is
here into `docs/` and links it from the page, so nothing else needs to change.

```sh
VERSION=11.11.2
BASE="https://raw.githubusercontent.com/highlightjs/cdn-release/$VERSION"
curl -sSfLO "$BASE/LICENSE"
curl -sSfLO "$BASE/build/highlight.min.js"
curl -sSfL -O "$BASE/build/styles/github.min.css" -O "$BASE/build/styles/github-dark.min.css"
sha256sum highlight.min.js github.min.css github-dark.min.css LICENSE
```

The licence travels with the site: `docs/` carries a copy of `LICENSE`, which is what
the BSD 3-Clause terms ask of anyone redistributing the build — and publishing the
page is redistributing it.
