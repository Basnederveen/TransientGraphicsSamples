using Inventor;
using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;

namespace TransientGraphics.Samples;

public static class Post02_ConstructionTree
{
    public static SurfaceBody BuildSheet(Inventor.Application app)
    {
        var transientBRep = app.TransientBRep;
        var geo = new InventorGeometry(app.TransientGeometry);

        var p1 = new Point3D(0, 0, 0);
        var p2 = new Point3D(10, 0, 0);
        var p3 = new Point3D(10, 5, 0);
        var p4 = new Point3D(0, 5, 0);

        var l12 = new Line3D(p1, p2);
        var l23 = new Line3D(p2, p3);
        var l34 = new Line3D(p3, p4);
        var l41 = new Line3D(p4, p1);
        var plane = new Plane3D(p1, Vector3D.ZAxis);

        var bodyDef  = transientBRep.CreateSurfaceBodyDefinition();
        var lumpDef  = bodyDef.LumpDefinitions.Add();
        var shellDef = lumpDef.FaceShellDefinitions.Add();

        var v1 = bodyDef.VertexDefinitions.Add(geo.Pt(p1));
        var v2 = bodyDef.VertexDefinitions.Add(geo.Pt(p2));
        var v3 = bodyDef.VertexDefinitions.Add(geo.Pt(p3));
        var v4 = bodyDef.VertexDefinitions.Add(geo.Pt(p4));

        var e12 = bodyDef.EdgeDefinitions.Add(v1, v2, geo.Seg(l12));
        var e23 = bodyDef.EdgeDefinitions.Add(v2, v3, geo.Seg(l23));
        var e34 = bodyDef.EdgeDefinitions.Add(v3, v4, geo.Seg(l34));
        var e41 = bodyDef.EdgeDefinitions.Add(v4, v1, geo.Seg(l41));

        var faceDef = shellDef.FaceDefinitions.Add(geo.Pln(plane), false);
        var loop    = faceDef.EdgeLoopDefinitions.Add();
        loop.EdgeUseDefinitions.Add(e12, false);
        loop.EdgeUseDefinitions.Add(e23, false);
        loop.EdgeUseDefinitions.Add(e34, false);
        loop.EdgeUseDefinitions.Add(e41, false);

        var sheet = bodyDef.CreateTransientSurfaceBody(out NameValueMap errors);
        ThrowIfErrors(errors);
        return sheet;
    }

    public static void ThrowIfErrors(NameValueMap? errors)
    {
        if (errors != null && errors.Count > 0)
        {
            var messages = new List<string>();
            for (int i = 1; i <= errors.Count; i++)
            {
                messages.Add($"{errors.Name[i]}: {errors.Value[errors.Name[i]]}");
            }
            throw new InvalidOperationException(
                $"Surface body creation errors: {string.Join("; ", messages)}");
        }
    }
}
