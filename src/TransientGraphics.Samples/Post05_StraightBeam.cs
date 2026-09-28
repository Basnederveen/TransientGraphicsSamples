using Inventor;
using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;
using static TransientGraphics.Samples.Post04_CapFaces;

namespace TransientGraphics.Samples;

// Post 5: side faces. One face per profile curve between the two caps: line → plane,
// arc → cylinder, circle → cylinder with two loops. Cap edges are shared with the side faces.
public static class Post05_StraightBeam
{
    public static SurfaceBody Build(
        Inventor.Application app,
        IReadOnlyList<ICurve3D> outerProfile,
        IReadOnlyList<IReadOnlyList<ICurve3D>> innerProfiles,
        Point3D start, Vector3D direction, double length)
    {
        var geo = new InventorGeometry(app.TransientGeometry);
        var bodyDef  = app.TransientBRep.CreateSurfaceBodyDefinition();
        var shellDef = bodyDef.LumpDefinitions.Add().FaceShellDefinitions.Add();

        direction = direction.Normalized();
        var end = start + direction * length;
        var atStart = Profiles.FrameAt(start, direction);
        var atEnd   = Profiles.FrameAt(end, direction);

        // Cap topology for every loop, at both ends.
        var startOuter = AddProfileEdges(bodyDef, geo, Profiles.Transform(outerProfile, atStart));
        var endOuter   = AddProfileEdges(bodyDef, geo, Profiles.Transform(outerProfile, atEnd));
        var startInner = innerProfiles.Select(p => AddProfileEdges(bodyDef, geo, Profiles.Transform(p, atStart))).ToList();
        var endInner   = innerProfiles.Select(p => AddProfileEdges(bodyDef, geo, Profiles.Transform(p, atEnd))).ToList();

        AddCapFace(shellDef, geo, new Plane3D(start, -direction), startOuter, startInner, outwardIsProfileNormal: false);
        AddCapFace(shellDef, geo, new Plane3D(end, direction), endOuter, endInner, outwardIsProfileNormal: true);

        // Side faces: connectors are straight segments along the beam; surfaces are planes and cylinders.
        object Connector(Point3D p) => geo.Seg(new Line3D(p, p + direction * length));
        object Surface(ICurve3D c)  => SideSurface(geo, c, direction);

        AddSideFaces(bodyDef, shellDef, Profiles.Transform(outerProfile, atStart), startOuter, endOuter, Connector, Surface);
        for (int k = 0; k < innerProfiles.Count; k++)
            AddSideFaces(bodyDef, shellDef, Profiles.Transform(innerProfiles[k], atStart), startInner[k], endInner[k], Connector, Surface);

        var body = bodyDef.CreateTransientSurfaceBody(out NameValueMap errors);
        Post02_ConstructionTree.ThrowIfErrors(errors);
        return body;
    }

    // One side face per profile curve. `connector` gives the curve a profile vertex traces from
    // the start cap to the end cap; `surface` gives the surface a profile curve sweeps to.
    // The same code serves the straight beam here and the curved beam in Post 8.
    public static void AddSideFaces(
        SurfaceBodyDefinition bodyDef, FaceShellDefinition shellDef,
        IReadOnlyList<ICurve3D> startProfile, ProfileTopology startTopo, ProfileTopology endTopo,
        Func<Point3D, object> connector, Func<ICurve3D, object> surface)
    {
        int n = startProfile.Count;

        // One connector edge per profile vertex.
        var connectors = new EdgeDefinition[n];
        for (int j = 0; j < n; j++)
            connectors[j] = bodyDef.EdgeDefinitions.Add(startTopo.Vertices[j], endTopo.Vertices[j], connector(startProfile[j].StartPoint));

        for (int i = 0; i < n; i++)
        {
            var curve   = startProfile[i];
            var faceDef = shellDef.FaceDefinitions.Add(surface(curve), false);

            // Loops are walked so the face lies to the LEFT when walking with the surface's own
            // normal up. For a profile curve that is: forward along the curve if it runs
            // counter-clockwise about the beam direction (a convex arc, a circle), opposed if it
            // runs clockwise (a concave fillet). Planes and cylinders forgive the wrong choice;
            // tori (curved beams, later in the series) do not.
            bool opposed = curve is Arc3D a && a.SweepAngle < 0;

            if (Profiles.IsClosedCurve(curve))
            {
                // Two loops, one per cap, no connectors: start circle forward, end circle opposed.
                var startLoop = faceDef.EdgeLoopDefinitions.Add();
                startLoop.EdgeUseDefinitions.Add(startTopo.Edges[i], false);
                var endLoop = faceDef.EdgeLoopDefinitions.Add();
                endLoop.EdgeUseDefinitions.Add(endTopo.Edges[i], true);
                continue;
            }

            // Four-edge loop: start curve, connector to the end cap, end curve back, connector home.
            int next = (i + 1) % n;
            var loop = faceDef.EdgeLoopDefinitions.Add();
            if (!opposed)
            {
                loop.EdgeUseDefinitions.Add(startTopo.Edges[i], false);
                loop.EdgeUseDefinitions.Add(connectors[next],   false);
                loop.EdgeUseDefinitions.Add(endTopo.Edges[i],   true);
                loop.EdgeUseDefinitions.Add(connectors[i],      true);
            }
            else
            {
                loop.EdgeUseDefinitions.Add(startTopo.Edges[i], true);
                loop.EdgeUseDefinitions.Add(connectors[i],      false);
                loop.EdgeUseDefinitions.Add(endTopo.Edges[i],   false);
                loop.EdgeUseDefinitions.Add(connectors[next],   true);
            }
        }
    }

    // The swept surface for one profile curve on a straight beam.
    public static object SideSurface(InventorGeometry geo, ICurve3D curve, Vector3D direction)
    {
        switch (curve)
        {
            case Line3D l:
                // Plane through the line; normal = (line direction × beam direction), which for a
                // counter-clockwise profile is the outward normal.
                return geo.Pln(new Plane3D(l.StartPoint, l.Direction.Cross(direction).Normalized()));
            case Arc3D a:
                // Cylinder coaxial with the arc.
                return geo.Tg.CreateCylinder(geo.Pt(a.Center), geo.Unit(a.Normal), a.Radius);
            case Circle3D c:
                return geo.Tg.CreateCylinder(geo.Pt(c.Center), geo.Unit(c.Normal), c.Radius);
            case Ellipse3D e:
                return geo.Tg.CreateEllipticalCylinder(geo.Pt(e.Center), geo.Unit(e.Normal),
                    geo.Vec(e.MajorAxisDirection * e.SemiMajorAxis), e.SemiMinorAxis / e.SemiMajorAxis);
            default:
                throw new NotSupportedException(curve.GetType().Name);
        }
    }
}
