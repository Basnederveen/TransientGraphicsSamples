# Transient graphics in Inventor — sample project

Companion code for the blog series on building and rendering transient bodies with Inventor's
`TransientBRep` / `TransientGeometry` / client-graphics APIs, published at
https://basautomationservices.com/blog. Every code block in the posts is lifted from this project.
The repository grows with the series: each post's code is added when the post goes live.

| Post | File |
|---|---|
| [Constructing a transient beam in Inventor](https://basautomationservices.com/blog/constructing-a-transient-beam/) | (introduction, no code) |
| [The TransientBRep construction tree in Inventor](https://basautomationservices.com/blog/inventor-transientbrep-construction-tree/) | `InventorGeometry.cs`, `Post02_ConstructionTree.cs` |
| [Getting a transient body on screen in Inventor](https://basautomationservices.com/blog/inventor-transient-body-on-screen/) | `Post03_BaseGraphics.cs`, `TransientGraphics.Demo/` |

Two projects:

- `src/TransientGraphics.Samples` — a class library with the code from the posts.
  `BaseGraphics` / `BodyGraphics` (Post 3) own the client graphics everything is drawn into.
- `src/TransientGraphics.Demo` — a console application that attaches to the running Inventor
  from outside its process and draws one post's sample into the active document.

## Building

Requires the .NET 8 SDK and an Inventor installation (the projects reference
`Autodesk.Inventor.Interop.dll` from `C:\Program Files\Autodesk\Inventor <version>\Bin\Public Assemblies`;
set `InventorVersion` if yours isn't 2025):

```
dotnet build
```

The managed geometry types come from the
[`BasAutomation.Geometry`](https://www.nuget.org/packages/BasAutomation.Geometry) package
(MIT, [source on GitHub](https://github.com/Basnederveen/BasAutomation.Geometry)).

## Running

Start Inventor, open a part or assembly (or let the demo create a part), then:

```
dotnet run --project src/TransientGraphics.Demo -- 3
```

The argument is the post number. Each run replaces the previous drawing (the client graphics
live under one client id, and constructing `BodyGraphics` deletes what is already there);
`clear` removes it. The graphics are non-transacting: nothing lands in the undo list, and the
document is not marked modified. The demo draws into whatever document is active — including a
real one you have open — so run it against a scratch part.

The samples themselves are plain static methods taking `Inventor.Application`, so they can
equally be called from an add-in or a scripting host attached to Inventor:

```csharp
var graphics = new BodyGraphics(app, partDoc, "Samples");
graphics.AddBody(Post02_ConstructionTree.BuildSheet(app));
graphics.Update();
```

## License

MIT — see [LICENSE](LICENSE).
