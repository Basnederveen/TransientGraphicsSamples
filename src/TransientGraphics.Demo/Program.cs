using Inventor;
using BasAutomation.Geometry;
using BasAutomation.Geometry.Curves;
using TransientGraphics.Samples;
using Profiles = TransientGraphics.Samples.Profiles;

namespace TransientGraphics.Demo;

// Runs one post's sample against the running Inventor, drawing into the active document.
//
//   dotnet run --project src/TransientGraphics.Demo -- 3
//
// The argument is the post number. Run it again to replace the previous drawing; run it
// with "clear" to remove it. Each later post adds its own case to Draw().
public static class Program
{
    private const string ClientId = "TransientGraphics.Demo";

    [STAThread]
    public static int Main(string[] args)
    {
        var what = args.Length > 0 ? args[0] : "3";

        Inventor.Application app;
        try { app = InventorConnection.GetRunning(); }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }

        var doc = app.ActiveDocument;
        if (doc == null)
        {
            try
            {
                doc = app.Documents.Add(DocumentTypeEnum.kPartDocumentObject, "", true);
                Console.WriteLine("No active document; created a new part.");
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                // Seen while Inventor was busy in its own UI: E_FAIL from Documents.Add.
                Console.Error.WriteLine($"No active document and Inventor refused to create one ({ex.Message}). Open a part or assembly and try again.");
                return 1;
            }
        }
        Console.WriteLine($"Inventor {app.SoftwareVersion.DisplayVersion}, drawing in {doc.DisplayName}");

        var graphics = new BodyGraphics(app, doc, ClientId);
        if (what == "clear")
        {
            graphics.Delete();
            return 0;
        }

        try
        {
            Draw(what, app, graphics);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }

        FrameCamera(app, graphics);
        graphics.Update();

        foreach (GraphicsNode node in graphics.ClientGraphics)
            Console.WriteLine($"node {node.Id}: {node.Count} graphics primitive(s)");
        return 0;
    }

    private static void Draw(string post, Inventor.Application app, BodyGraphics graphics)
    {
        var tg = app.TransientGeometry;
        var tb = app.TransientBRep;
        var y  = Vector3D.YAxis;

        IReadOnlyList<ICurve3D> rhs      = Profiles.RoundedRectangle(10, 5, 0.6);
        IReadOnlyList<ICurve3D> rhsInner = Profiles.RoundedRectangle(9, 4, 0.15);
        IReadOnlyList<ICurve3D> ipe      = Profiles.IBeam(12, 6.4, 0.44, 0.63, 0.7);

        switch (post)
        {
            case "1":
                throw new ArgumentException("Post 1 is the introduction and has no code to run; try 2 to 5.");
            case "2":
                graphics.AddBody(Post02_ConstructionTree.BuildSheet(app));
                break;
            case "3":
            {
                // The one-shot primitives from the first post, drawn through BodyGraphics.
                var cylinder = tb.CreateSolidCylinderCone(tg.CreatePoint(0, 0, 0), tg.CreatePoint(0, 0, 20), 3, 3, 3);
                var sphere   = tb.CreateSolidSphere(tg.CreatePoint(12, 0, 4), 4);
                var box      = tg.CreateBox();
                box.MinPoint = tg.CreatePoint(20, -3, 0);
                box.MaxPoint = tg.CreatePoint(28, 3, 12);
                graphics.AddBody(cylinder);
                graphics.AddBody(sphere);
                graphics.AddBody(tb.CreateSolidBlock(box));
                break;
            }
            case "4":
                graphics.AddBody(Post04_CapFaces.BuildHollowCap(app));
                break;
            case "5":
                graphics.AddBody(Post05_StraightBeam.Build(app, ipe, [], new Point3D(0, 0, 0), y, 60));
                graphics.AddBody(Post05_StraightBeam.Build(app, rhs, [rhsInner], new Point3D(16, 0, 0), y, 60));
                graphics.AddBody(Post05_StraightBeam.Build(app, Profiles.Circle(3.5), [Profiles.Circle(3)], new Point3D(30, 0, 0), y, 60));
                break;
            default:
                throw new ArgumentException($"Unknown post '{post}'. Use 2-5, or clear.");
        }
    }

    // View.Fit ignores client graphics, so frame the camera on what was drawn: the union of
    // the primitives' range boxes, seen from an isometric direction.
    private static void FrameCamera(Inventor.Application app, BaseGraphics graphics)
    {
        var tg = app.TransientGeometry;
        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (GraphicsNode node in graphics.ClientGraphics)
        foreach (GraphicsPrimitive primitive in node)
        {
            var box = primitive.RangeBox;   // world space; includes node.Transformation
            minX = Math.Min(minX, box.MinPoint.X); maxX = Math.Max(maxX, box.MaxPoint.X);
            minY = Math.Min(minY, box.MinPoint.Y); maxY = Math.Max(maxY, box.MaxPoint.Y);
            minZ = Math.Min(minZ, box.MinPoint.Z); maxZ = Math.Max(maxZ, box.MaxPoint.Z);
        }
        if (minX > maxX) return;

        var center   = new Point3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        var diagonal = new Vector3D(maxX - minX, maxY - minY, maxZ - minZ).Length;
        var eyeDir   = new Vector3D(-1, -0.6, 0.6).Normalized();
        var eye      = center + eyeDir * (diagonal * 4 + 100);

        // Extents are width and height in model units; give them the view's own aspect ratio,
        // otherwise Inventor re-fits them and the drawing lands off-centre.
        var view = graphics.View;
        if (view == null) return;
        double aspect = (double)view.Width / view.Height;
        double size   = diagonal * 1.2;

        var camera = view.Camera;
        camera.Perspective = false;
        camera.Target   = tg.CreatePoint(center.X, center.Y, center.Z);
        camera.Eye      = tg.CreatePoint(eye.X, eye.Y, eye.Z);
        camera.UpVector = tg.CreateUnitVector(0, 0, 1);
        camera.SetExtents(aspect >= 1 ? size * aspect : size, aspect >= 1 ? size : size / aspect);
        camera.Apply();
    }
}
