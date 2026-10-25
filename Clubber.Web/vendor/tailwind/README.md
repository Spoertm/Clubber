# Vendored Tailwind CSS engine

`index.css` is the stock `tailwindcss/index.css` from the `tailwindcss` npm package,
committed here so the stylesheet build needs no `node_modules` and no `npm install`.

The `@tailwindcss/cli` binary is still used to run the build, but it is fetched on
demand by `npx` and lives outside the repository:

```powershell
npx -y @tailwindcss/cli@4.1.8 -i ./Styles/input.css -o ./wwwroot/css/site.css --minify
```

## Current version

- `tailwindcss` **4.1.8** (`index.css`)
- Generated output is committed at `wwwroot/css/site.css`

## Upgrading

1. Pick the target version, e.g. `4.2.0`.
2. Copy `index.css` out of the published package and overwrite the file here:
    ```powershell
    npx -y -p tailwindcss@4.2.0 -- node -e "console.log(require.resolve('tailwindcss/index.css'))"
    ```
    (or unpack the tarball from npmjs and take `package/index.css`).
3. Update the version number in the comment at the top of `Styles/input.css` and in
   `.vscode/tasks.json`.
4. Rebuild: `npx -y @tailwindcss/cli@4.2.0 -i ./Styles/input.css -o ./wwwroot/css/site.css --minify`
5. Check the diff of `wwwroot/css/site.css` before committing.

`index.css` is self-contained (theme + preflight + the `@tailwind utilities` hook); it
has no relative imports, so nothing else needs vendoring.

## Why not node_modules?

`@import "tailwindcss"` is resolved from the importing stylesheet's directory, so the
`tailwindcss` package has to exist in some `node_modules` above `Styles/`. Vendoring
`index.css` sidesteps that resolution entirely, which is what makes a pure `npx` build
possible.

Licensed under MIT — see `LICENSE`.
