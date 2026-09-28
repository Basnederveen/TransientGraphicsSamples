using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;

namespace TransientGraphics.Samples;

// Profiles are closed, head-to-tail lists of curves in the XY plane, walked counter-clockwise
// when viewed from +Z (the beam direction). Values in centimetres — Inventor's internal unit.
public static class Profiles
{
    public static IReadOnlyList<ICurve3D> Rectangle(double width, double height)
    {
        double w = width / 2, h = height / 2;
        var p1 = new Point3D(-w, -h, 0);
        var p2 = new Point3D( w, -h, 0);
        var p3 = new Point3D( w,  h, 0);
        var p4 = new Point3D(-w,  h, 0);
        return [new Line3D(p1, p2), new Line3D(p2, p3), new Line3D(p3, p4), new Line3D(p4, p1)];
    }

    // Line, arc, line, arc, line, arc, line, arc — eight curves.
    public static IReadOnlyList<ICurve3D> RoundedRectangle(double width, double height, double radius)
    {
        double w = width / 2, h = height / 2, r = radius;
        var z = Vector3D.ZAxis;
        var x = Vector3D.XAxis;
        const double q = Math.PI / 2;

        Arc3D Corner(double cx, double cy, double startAngle) =>
            new(new Point3D(cx, cy, 0), z, x, r, startAngle, startAngle + q);

        var bottomRight = Corner( w - r, -h + r, -q);     // -90° → 0°
        var topRight    = Corner( w - r,  h - r,  0);     //   0° → 90°
        var topLeft     = Corner(-w + r,  h - r,  q);     //  90° → 180°
        var bottomLeft  = Corner(-w + r, -h + r,  2 * q); // 180° → 270°

        return
        [
            new Line3D(bottomLeft.EndPoint,  bottomRight.StartPoint), bottomRight,
            new Line3D(bottomRight.EndPoint, topRight.StartPoint),    topRight,
            new Line3D(topRight.EndPoint,    topLeft.StartPoint),     topLeft,
            new Line3D(topLeft.EndPoint,     bottomLeft.StartPoint),  bottomLeft,
        ];
    }


    // An I-section with root fillets, e.g. IPE 200 = IBeam(20, 10, 0.56, 0.85, 1.2). Sixteen curves;
    // the four fillets are concave, so they run clockwise inside the counter-clockwise loop.
    public static IReadOnlyList<ICurve3D> IBeam(double height, double flangeWidth, double webThickness, double flangeThickness, double rootRadius)
    {
        double h = height / 2, b = flangeWidth / 2, tw = webThickness / 2, tf = flangeThickness, r = rootRadius;
        var z = Vector3D.ZAxis;
        var x = Vector3D.XAxis;
        const double q = Math.PI / 2;

        Point3D P(double px, double py) => new(px, py, 0);
        // Clockwise quarter arc (concave root fillet).
        Arc3D Fillet(double cx, double cy, double startAngle) => new(P(cx, cy), z, x, r, startAngle, startAngle - q, isCounterClockwise: false);

        var f1 = Fillet( tw + r, -h + tf + r, 3 * q); // bottom-right root
        var f2 = Fillet( tw + r,  h - tf - r, 2 * q); // top-right root
        var f3 = Fillet(-tw - r,  h - tf - r, q);     // top-left root
        var f4 = Fillet(-tw - r, -h + tf + r, 0);     // bottom-left root

        return
        [
            new Line3D(P(-b, -h), P(b, -h)),
            new Line3D(P(b, -h), P(b, -h + tf)),
            new Line3D(P(b, -h + tf), f1.StartPoint), f1,
            new Line3D(f1.EndPoint, f2.StartPoint), f2,
            new Line3D(f2.EndPoint, P(b, h - tf)),
            new Line3D(P(b, h - tf), P(b, h)),
            new Line3D(P(b, h), P(-b, h)),
            new Line3D(P(-b, h), P(-b, h - tf)),
            new Line3D(P(-b, h - tf), f3.StartPoint), f3,
            new Line3D(f3.EndPoint, f4.StartPoint), f4,
            new Line3D(f4.EndPoint, P(-b, -h + tf)),
            new Line3D(P(-b, -h + tf), P(-b, -h)),
        ];
    }

    public static IReadOnlyList<ICurve3D> Circle(double radius) =>
        [new Circle3D(new Point3D(0, 0, 0), radius, Vector3D.ZAxis)];

    public static IReadOnlyList<ICurve3D> Ellipse(double semiMajor, double semiMinor) =>
        [new Ellipse3D(new Point3D(0, 0, 0), semiMajor, semiMinor, Vector3D.ZAxis, Vector3D.XAxis)];

    // Same shape, moved rigidly. Used to place the start and end profiles of a beam.
    public static IReadOnlyList<ICurve3D> Transform(IReadOnlyList<ICurve3D> profile, Matrix3D m) =>
        profile.Select(c => Transform(c, m)).ToList();

    public static ICurve3D Transform(ICurve3D curve, Matrix3D m) => curve switch
    {
        Line3D l    => new Line3D(m.Transform(l.StartPoint), m.Transform(l.EndPoint)),
        Arc3D a     => new Arc3D(m.Transform(a.Center), m.Transform(a.Normal), m.Transform(a.XAxis),
                                 a.Radius, a.StartAngle, a.EndAngle, a.IsCounterClockwise),
        Circle3D k  => new Circle3D(m.Transform(k.Center), k.Radius, m.Transform(k.Normal)),
        Ellipse3D e => new Ellipse3D(m.Transform(e.Center), e.SemiMajorAxis, e.SemiMinorAxis,
                                     m.Transform(e.Normal), m.Transform(e.MajorAxisDirection)),
        _ => throw new NotSupportedException(curve.GetType().Name),
    };

    public static bool IsClosedCurve(ICurve3D c) => c is Circle3D or Ellipse3D;

    // Frame that maps the XY profile plane onto a plane through `origin` with normal `direction`.
    public static Matrix3D FrameAt(Point3D origin, Vector3D direction) =>
        Matrix3D.CreateRotationFromTo(Vector3D.ZAxis, direction.Normalized()).SetTranslation(origin);
}
