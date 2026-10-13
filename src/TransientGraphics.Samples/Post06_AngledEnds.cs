using Inventor;
using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;

namespace TransientGraphics.Samples;

// Post 6: angled ends. Build the beam over-long with perpendicular caps, then subtract a cut
// body at each end whose flat face lies on the cut plane.
public static class Post06_AngledEnds
{
    // A beam whose centerline runs from `start` to `end`, cut at both ends by the given planes.
    // The plane normals do not need to point any particular way; the side of the plane that
    // contains the beam is worked out from the centerline.
    public static SurfaceBody Build(
        Inventor.Application app,
        IReadOnlyList<ICurve3D> outerProfile,
        IReadOnlyList<IReadOnlyList<ICurve3D>> innerProfiles,
        Point3D start, Point3D end,
        Plane3D startCut, Plane3D endCut)
    {
        var direction = (end - start).Normalized();
        var length    = start.DistanceTo(end);

        // 1. How far past each cut plane the beam has to be built.
        var diagonal       = ProfileDiagonal(outerProfile);
        var startExtension = ExtensionForCut(AngleFromPerpendicular(startCut, direction), diagonal);
        var endExtension   = ExtensionForCut(AngleFromPerpendicular(endCut,   direction), diagonal);

        // 2. Elongate the centerline by those amounts.
        var elongatedStart  = start - direction * startExtension;
        var elongatedLength = length + startExtension + endExtension;

        // 3. Build the over-long beam with perpendicular ends (the previous posts).
        var beam = Post05_StraightBeam.Build(app, outerProfile, innerProfiles, elongatedStart, direction, elongatedLength);

        // 4. Subtract a cut body at each end. DoBoolean mutates its first argument.
        var startTool = CutBody(app, startCut, awayFromBeam: -direction, reach: startExtension + diagonal);
        var endTool   = CutBody(app, endCut,   awayFromBeam:  direction, reach: endExtension + diagonal);
        app.TransientBRep.DoBoolean(beam, startTool, BooleanTypeEnum.kBooleanTypeDifference);
        app.TransientBRep.DoBoolean(beam, endTool,   BooleanTypeEnum.kBooleanTypeDifference);
        return beam;
    }

    // Angle between the cut plane's normal and the centerline, folded into [0, pi/2].
    // 0 = perpendicular cut; approaching pi/2 = cut nearly parallel to the beam.
    public static double AngleFromPerpendicular(Plane3D cutPlane, Vector3D direction)
    {
        var cos = Math.Abs(cutPlane.Normal.Normalized().Dot(direction.Normalized()));
        return Math.Acos(Math.Min(1.0, cos));
    }

    // How far the beam has to stick out past the cut plane so the plane crosses the whole
    // cross-section. Geometrically the minimum is (diagonal / 2) * tan(angle); this uses the
    // more generous 1.5 * diagonal / cos(angle) plus a 1 cm margin.
    public static double ExtensionForCut(double angleFromPerpendicular, double diagonal)
    {
        if (angleFromPerpendicular < 1e-6) return 0;

        var cos = Math.Cos(angleFromPerpendicular);
        if (cos < 1e-3)
            throw new ArgumentException("Cut plane is too close to parallel with the centerline.");

        return 1.5 * diagonal / cos + 1.0;
    }

    public static double ProfileDiagonal(IReadOnlyList<ICurve3D> profile)
    {
        // Sample each curve; good enough for a bounding box.
        var pts = profile.SelectMany(c => Enumerable.Range(0, 17).Select(i => c.PointAt(i / 16.0))).ToList();
        double w = pts.Max(p => p.X) - pts.Min(p => p.X);
        double h = pts.Max(p => p.Y) - pts.Min(p => p.Y);
        return Math.Sqrt(w * w + h * h);
    }

    // A square block sitting on the cut plane and extruded away from the beam, big enough to
    // swallow the whole over-long stub: every point of the stub is within `reach` of the plane
    // origin. Built with the same straight-beam code as the beam itself.
    private static SurfaceBody CutBody(Inventor.Application app, Plane3D cutPlane, Vector3D awayFromBeam, double reach)
    {
        var n = cutPlane.Normal.Normalized();
        if (n.Dot(awayFromBeam) < 0) n = -n;
        return Post05_StraightBeam.Build(app, Profiles.Rectangle(2 * reach, 2 * reach), [], cutPlane.Origin, n, reach);
    }
}
