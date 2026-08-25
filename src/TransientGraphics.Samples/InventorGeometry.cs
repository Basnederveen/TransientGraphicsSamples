using Inventor;
using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;

namespace TransientGraphics.Samples;

// A thin adapter from managed geometry (BasAutomation.Geometry) to Inventor's COM geometry.
// Introduced in Post 2; grows by one method per new curve/surface type as the series goes on.
public sealed class InventorGeometry(TransientGeometry tg)
{
    public TransientGeometry Tg => tg;

    public Point          Pt(Point3D p)    => tg.CreatePoint(p.X, p.Y, p.Z);
    public UnitVector     Unit(Vector3D v) => tg.CreateUnitVector(v.X, v.Y, v.Z);
    public Vector         Vec(Vector3D v)  => tg.CreateVector(v.X, v.Y, v.Z);
    public Inventor.Plane Pln(Plane3D pl)  => tg.CreatePlane(Pt(pl.Origin), Vec(pl.Normal));
    public LineSegment    Seg(Line3D l)    => tg.CreateLineSegment(Pt(l.StartPoint), Pt(l.EndPoint));
}
