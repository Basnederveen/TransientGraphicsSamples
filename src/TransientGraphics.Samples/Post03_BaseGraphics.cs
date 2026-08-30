using System.Runtime.InteropServices;
using Inventor;

namespace TransientGraphics.Samples;

// Post 3: the object that owns what gets drawn. Every preview in the series goes through a
// class derived from this one. It owns a ClientGraphics collection (where the graphics live)
// and a GraphicsDataSets collection (where the coordinate, colour and index sets for point,
// line and triangle graphics live), hands out GraphicsNodes with unique ids, and deletes
// everything it created in one call.
public abstract class BaseGraphics
{
    protected readonly Inventor.Application app;
    protected readonly Document? document;
    protected readonly ClientGraphics clientGraphics;
    protected readonly GraphicsDataSets graphicsData;

    // The nodes this object created, in creation order.
    public List<GraphicsNode> Nodes { get; } = new();

    public ClientGraphics ClientGraphics => clientGraphics;
    public GraphicsDataSets GraphicsData => graphicsData;

    // Draws into a document: a non-transacting ClientGraphics on its component definition and
    // a GraphicsDataSets collection on the document, both under `clientId`. A collection that
    // already exists under that id (the same code ran earlier) is deleted first, so
    // constructing this object is also how a previous run's graphics are replaced.
    protected BaseGraphics(Inventor.Application app, Document document, string clientId)
    {
        this.app = app;
        this.document = document;
        var compDef = ComponentDefinitionOf(document);

        TryDelete(() => compDef.ClientGraphicsCollection[clientId].Delete());
        clientGraphics = compDef.ClientGraphicsCollection.AddNonTransacting(clientId);

        TryDelete(() => document.GraphicsDataSetsCollection[clientId].Delete());
        graphicsData = document.GraphicsDataSetsCollection.AddNonTransacting(clientId);
    }

    // Draws during a command: the InteractionGraphics owns both collections, and they go
    // away with the interaction session.
    protected BaseGraphics(Inventor.Application app, InteractionGraphics interactionGraphics, InteractionGraphicsMode mode)
    {
        this.app = app;
        graphicsData   = interactionGraphics.GraphicsDataSets;
        clientGraphics = mode == InteractionGraphicsMode.Overlay
            ? interactionGraphics.OverlayClientGraphics
            : interactionGraphics.PreviewClientGraphics;
    }

    // A new node with an id above every id already in the collection. AddNode with an id that
    // is already in use does not throw and does not replace the existing node; it adds a
    // second node with the same id.
    protected GraphicsNode CreateNewGraphicsNode()
    {
        int nodeId = 1;
        foreach (GraphicsNode existing in clientGraphics)
            if (existing.Id >= nodeId) nodeId = existing.Id + 1;

        var node = clientGraphics.AddNode(nodeId);
        Nodes.Add(node);
        return node;
    }

    // The view to draw in. Application.ActiveView is null when Inventor's window has no
    // active view (a document is active, but the application window is not); the document's
    // own first view is there regardless.
    public Inventor.View? View =>
        app.ActiveView ?? (document != null && document.Views.Count > 0 ? document.Views[1] : null);

    // Flush to the viewport. Graphics changes are otherwise shown at the next redraw.
    public void Update() => View?.Update();

    // Delete the graphics, and with `deleteData` the data sets too.
    public void Delete(bool deleteData = true)
    {
        if (deleteData) TryDelete(() => graphicsData.Delete());
        TryDelete(() => clientGraphics.Delete());
        Nodes.Clear();
        Update();
    }

    private static ComponentDefinition ComponentDefinitionOf(Document document) => document switch
    {
        PartDocument part         => (ComponentDefinition)part.ComponentDefinition,
        AssemblyDocument assembly => (ComponentDefinition)assembly.ComponentDefinition,
        _ => throw new ArgumentException($"{document.DisplayName} is not a part or assembly document."),
    };

    // Indexing a collection by an id it does not contain, and deleting a collection whose host
    // is already gone, both throw. Either way there is nothing to delete.
    private static void TryDelete(Action delete)
    {
        try { delete(); }
        catch (COMException) { }
        catch (ArgumentException) { }
    }
}

public enum InteractionGraphicsMode
{
    Preview,   // depth-tested against the model
    Overlay,   // drawn on top of everything
}

// The first concrete graphics class: shows SurfaceBodies, one node per body.
public sealed class BodyGraphics : BaseGraphics
{
    public BodyGraphics(Inventor.Application app, Document document, string clientId)
        : base(app, document, clientId) { }

    public BodyGraphics(Inventor.Application app, InteractionGraphics interactionGraphics, InteractionGraphicsMode mode)
        : base(app, interactionGraphics, mode) { }

    public GraphicsNode AddBody(SurfaceBody body, Color? color = null)
    {
        var node = CreateNewGraphicsNode();
        node.Selectable = true;

        var surface = node.AddSurfaceGraphics(body);
        surface.Color = color ?? app.TransientObjects.CreateColor(0, 135, 0, 0.35);
        return node;
    }
}
