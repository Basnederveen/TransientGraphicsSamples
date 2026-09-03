using Inventor;
using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;

namespace TransientGraphics.Samples;

// A thin adapter from managed geometry (BasAutomation.Geometry) to Inventor's COM geometry.
// Introduced in Post 2; grows by one method per new curve/surface type as the series goes on.
public sealed class InventorGeometry(TransientGeometry tg)
{
    public TransientGeometry Tg => tg;

    // Post 2
    public Point          Pt(Point3D p)     => tg.CreatePoint(p.X, p.Y, p.Z);
    public UnitVector     Unit(Vector3D v)  => tg.CreateUnitVector(v.X, v.Y, v.Z);
    public Vector         Vec(Vector3D v)   => tg.CreateVector(v.X, v.Y, v.Z);
    public Inventor.Plane Pln(Plane3D pl)   => tg.CreatePlane(Pt(pl.Origin), Vec(pl.Normal));
    public LineSegment    Seg(Line3D l)     => tg.CreateLineSegment(Pt(l.StartPoint), Pt(l.EndPoint));

    // Post 4: arcs and circles.
    // Inventor's Arc3d always sweeps counter-clockwise about its normal, so a clockwise managed
    // arc is expressed with the normal flipped and a positive sweep. The reference vector points
    // at the arc's start point, so the start angle is 0.
    public Arc3d Arc(Arc3D a)
    {
        var toStart = (a.StartPoint - a.Center).Normalized();
        var axis    = a.SweepAngle >= 0 ? a.Normal : -a.Normal;
        return tg.CreateArc3d(Pt(a.Center), Unit(axis), Unit(toStart), a.Radius, 0, Math.Abs(a.SweepAngle));
    }

    public Circle Circ(Circle3D c) => tg.CreateCircle(Pt(c.Center), Unit(c.Normal), c.Radius);

    public EllipseFull Ell(Ellipse3D e) =>
        tg.CreateEllipseFull(Pt(e.Center), Unit(e.Normal),
            Vec(e.MajorAxisDirection * e.SemiMajorAxis), e.SemiMinorAxis / e.SemiMajorAxis);

    // Any profile curve. Returned as object because that is what EdgeDefinitions.Add takes.
    public object Curve(ICurve3D c) => c switch
    {
        Line3D l    => Seg(l),
        Arc3D a     => Arc(a),
        Circle3D k  => Circ(k),
        Ellipse3D e => Ell(e),
        _ => throw new NotSupportedException($"No Inventor conversion for {c.GetType().Name}"),
    };
}
