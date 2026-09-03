using Inventor;
using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;

namespace TransientGraphics.Samples;

// Post 4: cap faces. A profile loop becomes body-level vertices + edges, and an edge loop on
// a planar face. Hollow profiles add inner loops on the same face, walked the other way round.
public static class Post04_CapFaces
{
    // The body-level topology for one profile loop: one vertex per curve start (shared with the
    // previous curve's end), one edge per curve. A closed curve (circle, ellipse) is a single
    // edge whose start and end vertex are the same.
    public sealed record ProfileTopology(VertexDefinition[] Vertices, EdgeDefinition[] Edges);

    public static ProfileTopology AddProfileEdges(
        SurfaceBodyDefinition bodyDef, InventorGeometry geo, IReadOnlyList<ICurve3D> profile)
    {
        int n = profile.Count;
        var vertices = new VertexDefinition[n];
        var edges    = new EdgeDefinition[n];

        for (int i = 0; i < n; i++)
            vertices[i] = bodyDef.VertexDefinitions.Add(geo.Pt(profile[i].StartPoint));

        for (int i = 0; i < n; i++)
        {
            var start = vertices[i];
            var end   = Profiles.IsClosedCurve(profile[i]) ? vertices[i] : vertices[(i + 1) % n];
            edges[i]  = bodyDef.EdgeDefinitions.Add(start, end, geo.Curve(profile[i]));
        }
        return new ProfileTopology(vertices, edges);
    }

    // Walk a profile's edges into an edge loop. `reversed` walks the loop the other way round:
    // edges in reverse order, each opposed to its own direction.
    public static void AddLoop(FaceDefinition faceDef, EdgeDefinition[] edges, bool reversed)
    {
        var loop = faceDef.EdgeLoopDefinitions.Add();
        if (!reversed)
        {
            foreach (var e in edges) loop.EdgeUseDefinitions.Add(e, false);
        }
        else
        {
            for (int i = edges.Length - 1; i >= 0; i--) loop.EdgeUseDefinitions.Add(edges[i], true);
        }
    }

    // A planar cap on `plane` (whose normal is the face's outward normal). `outwardIsProfileNormal`
    // says whether that outward normal is the profile's +Z (end cap) or the opposite (start cap);
    // the loops flip so the outer loop is counter-clockwise about the outward normal and inner
    // loops clockwise.
    public static FaceDefinition AddCapFace(
        FaceShellDefinition shellDef, InventorGeometry geo, Plane3D plane,
        ProfileTopology outer, IEnumerable<ProfileTopology> inners, bool outwardIsProfileNormal)
    {
        var faceDef = shellDef.FaceDefinitions.Add(geo.Pln(plane), false);
        AddLoop(faceDef, outer.Edges, reversed: !outwardIsProfileNormal);
        foreach (var inner in inners)
            AddLoop(faceDef, inner.Edges, reversed: outwardIsProfileNormal);
        return faceDef;
    }

    // Standalone example: a single hollow cap (an RHS end face) as a planar sheet.
    public static SurfaceBody BuildHollowCap(Inventor.Application app)
    {
        var geo = new InventorGeometry(app.TransientGeometry);
        var bodyDef  = app.TransientBRep.CreateSurfaceBodyDefinition();
        var shellDef = bodyDef.LumpDefinitions.Add().FaceShellDefinitions.Add();

        var outer = AddProfileEdges(bodyDef, geo, Profiles.RoundedRectangle(10, 5, 0.5));
        var inner = AddProfileEdges(bodyDef, geo, Profiles.Rectangle(8, 3));

        var plane = new Plane3D(new Point3D(0, 0, 0), Vector3D.ZAxis);
        AddCapFace(shellDef, geo, plane, outer, [inner], outwardIsProfileNormal: true);

        var body = bodyDef.CreateTransientSurfaceBody(out NameValueMap errors);
        Post02_ConstructionTree.ThrowIfErrors(errors);
        return body;
    }
}
