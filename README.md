# Transient graphics in Inventor — sample project

Companion code for the blog series on building and rendering transient bodies with Inventor's
`TransientBRep` / `TransientGeometry` / client-graphics APIs, published at
https://basautomationservices.com/blog. Every code block in the posts is lifted from this project.
The repository grows with the series: each post's code is added when the post goes live.

| Post | File |
|---|---|
| [Constructing a transient beam in Inventor](https://basautomationservices.com/blog/constructing-a-transient-beam/) | (introduction, no code) |
| [The TransientBRep construction tree in Inventor](https://basautomationservices.com/blog/inventor-transientbrep-construction-tree/) | `InventorGeometry.cs`, `Post02_ConstructionTree.cs` |

## Building

Requires the .NET 8 SDK and an Inventor installation (the project references
`Autodesk.Inventor.Interop.dll` from `C:\Program Files\Autodesk\Inventor <version>\Bin\Public Assemblies`;
set `InventorVersion` if yours isn't 2025):

```
dotnet build src/TransientGraphics.Samples
```

The managed geometry types come from the
[`BasAutomation.Geometry`](https://www.nuget.org/packages/BasAutomation.Geometry) package
(MIT, [source on GitHub](https://github.com/Basnederveen/BasAutomation.Geometry)).

## Running

The samples are plain static methods taking `Inventor.Application`; call them from an add-in,
or from any C# scripting host attached to a running Inventor. For example:

```csharp
var sheet = Post02_ConstructionTree.BuildSheet(app);
Console.WriteLine(sheet.Faces.Count);   // 1
```

## License

MIT — see [LICENSE](LICENSE).
