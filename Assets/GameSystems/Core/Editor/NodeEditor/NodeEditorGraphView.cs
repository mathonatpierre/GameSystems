#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameSystems.Core.Editor.NodeEditor
{
    public sealed class NodeEditorNodeView : Node
    {
        public NodeEditorNodeView(string id, string title, Vector2 position,
            bool acceptsInput, bool providesOutput)
        {
            Id = id;
            this.title = title;
            viewDataKey = id;
            if (acceptsInput)
            {
                Input = InstantiatePort(Orientation.Vertical, Direction.Input,
                    Port.Capacity.Single, typeof(bool));
                Input.portName = string.Empty;
                inputContainer.Add(Input);
            }
            if (providesOutput)
            {
                Output = InstantiatePort(Orientation.Vertical, Direction.Output,
                    Port.Capacity.Multi, typeof(bool));
                Output.portName = string.Empty;
                outputContainer.Add(Output);
            }
            SetPosition(new Rect(position, new Vector2(220f, 110f)));
            RefreshExpandedState();
            RefreshPorts();
        }

        public string Id { get; }
        public Port Input { get; }
        public Port Output { get; }
    }

    public abstract class NodeEditorGraphView : GraphView
    {
        readonly Dictionary<string, NodeEditorNodeView> nodes = new();
        protected IEnumerable<NodeEditorNodeView> NodeViews => nodes.Values;

        protected NodeEditorGraphView()
        {
            style.flexGrow = 1f;
            Insert(0, new GridBackground());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = HandleGraphChanged;
        }

        protected void ClearGraph()
        {
            DeleteElements(graphElements.ToList());
            nodes.Clear();
        }

        protected NodeEditorNodeView AddNodeView(string id, string title, Vector2 position,
            bool acceptsInput, bool providesOutput)
        {
            var view = new NodeEditorNodeView(id, title, position, acceptsInput, providesOutput);
            view.RegisterCallback<MouseDownEvent>(_ => OnNodeSelected(id));
            nodes[id] = view;
            AddElement(view);
            return view;
        }

        protected void AddConnection(string parentId, string childId)
        {
            if (!nodes.TryGetValue(parentId, out NodeEditorNodeView parent) ||
                !nodes.TryGetValue(childId, out NodeEditorNodeView child) ||
                parent.Output == null || child.Input == null) return;
            AddElement(parent.Output.ConnectTo(child.Input));
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
        {
            var compatible = new List<Port>();
            ports.ForEach(port =>
            {
                if (port != startPort && port.direction != startPort.direction &&
                    port.node != startPort.node) compatible.Add(port);
            });
            return compatible;
        }

        GraphViewChange HandleGraphChanged(GraphViewChange change)
        {
            if (change.movedElements != null)
                for (int i = 0; i < change.movedElements.Count; i++)
                    if (change.movedElements[i] is NodeEditorNodeView moved)
                        OnNodeMoved(moved.Id, moved.GetPosition().position);
            if (change.edgesToCreate != null)
                for (int i = 0; i < change.edgesToCreate.Count; i++)
                {
                    Edge edge = change.edgesToCreate[i];
                    if (edge.output.node is NodeEditorNodeView parent &&
                        edge.input.node is NodeEditorNodeView child)
                        OnConnected(parent.Id, child.Id);
                }
            if (change.elementsToRemove != null)
                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    GraphElement element = change.elementsToRemove[i];
                    if (element is Edge edge && edge.output?.node is NodeEditorNodeView parent &&
                        edge.input?.node is NodeEditorNodeView child)
                        OnDisconnected(parent.Id, child.Id);
                    else if (element is NodeEditorNodeView node)
                        OnNodeRemoved(node.Id);
                }
            return change;
        }

        protected abstract void OnConnected(string parentId, string childId);
        protected abstract void OnDisconnected(string parentId, string childId);
        protected abstract void OnNodeRemoved(string id);
        protected abstract void OnNodeMoved(string id, Vector2 position);
        protected abstract void OnNodeSelected(string id);
    }
}
#endif
