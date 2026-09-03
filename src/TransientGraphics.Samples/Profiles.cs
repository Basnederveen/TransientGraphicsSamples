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

    public static IReadOnlyList<ICurve3D> Circle(double radius) =>
        [new Circle3D(new Point3D(0, 0, 0), radius, Vector3D.ZAxis)];

    public static IReadOnlyList<ICurve3D> Ellipse(double semiMajor, double semiMinor) =>
        [new Ellipse3D(new Point3D(0, 0, 0), semiMajor, semiMinor, Vector3D.ZAxis, Vector3D.XAxis)];

    public static bool IsClosedCurve(ICurve3D c) => c is Circle3D or Ellipse3D;
}
