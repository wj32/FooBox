using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FooBox.Common
{
    public enum ChangeType
    {
        /// <summary>
        /// No change.
        /// </summary>
        None,

        /// <summary>
        /// Add a document, modify a document (add a document version), add a folder,
        /// or rename a document or folder.
        /// </summary>
        Add,

        /// <summary>
        /// Set the display name of a document or folder.
        /// </summary>
        SetDisplayName,

        /// <summary>
        /// Delete a document, or delete a folder recursively.
        /// </summary>
        Delete,

        /// <summary>
        /// Undelete a document. Does not apply to folders.
        /// </summary>
        Undelete
    }

    public class ChangeItem
    {
        public string FullName { get; set; }
        public ChangeType Type { get; set; }

        /// <summary>
        /// Whether we are adding a document or a folder.
        /// </summary>
        public bool IsFolder { get; set; }
    }

    public class ChangeNode : ChangeItem
    {
        public static ChangeNode CreateRoot()
        {
            return new ChangeNode
            {
                Name = "",
                FullName = "",
                Type = ChangeType.Add,
                IsFolder = true,
                Nodes = new Dictionary<string, ChangeNode>()
            };
        }

        public static ChangeNode FromItems(IEnumerable<ChangeItem> list)
        {
            ChangeNode root = CreateRoot();

            foreach (var item in list)
            {
                var components = item.FullName.ToUpperInvariant().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                ChangeNode currentNode = root;

                if (components.Length == 0)
                    throw new Exception("Invalid file name '" + item.FullName + "'");

                // Find the direct parent to the file.

                for (int i = 0; i < components.Length - 1; i++)
                {
                    var name = components[i];

                    if (currentNode.Nodes.ContainsKey(name))
                    {
                        currentNode = currentNode.Nodes[name];

                        if (!currentNode.IsFolder)
                            throw new Exception("Inconsistent change item '" + currentNode.FullName + "'");
                    }
                    else
                    {
                        ChangeNode newNode = new ChangeNode
                        {
                            Name = name,
                            FullName = currentNode.FullName + "/" + name,
                            Type = ChangeType.None,
                            IsFolder = true,
                            Parent = currentNode
                        };

                        if (currentNode.Nodes == null)
                            currentNode.Nodes = new Dictionary<string, ChangeNode>();

                        currentNode.Nodes.Add(newNode.Name, newNode);
                        currentNode = newNode;
                    }

                    if (currentNode.Type == ChangeType.Delete)
                    {
                        currentNode = null;
                        break;
                    }

                    if (currentNode.Nodes == null)
                        currentNode.Nodes = new Dictionary<string, ChangeNode>();
                }

                // Add the node to direct parent.

                if (currentNode != null)
                {
                    var name = components[components.Length - 1];
                    ChangeNode node = null;

                    if (currentNode.Nodes.TryGetValue(name, out node))
                    {
                        bool inconsistent = false;

                        if (item.IsFolder && item.Type == ChangeType.Add)
                            inconsistent = node.Type != ChangeType.None && node.Type != ChangeType.Add;
                        else
                            inconsistent = node.Type != ChangeType.None;

                        if (node.IsFolder != item.IsFolder)
                            inconsistent = true;

                        if (inconsistent)
                            throw new Exception("Duplicate change item '" + node.FullName + "'.");

                        node.Type = item.Type;

                        if (node.Type == ChangeType.Delete)
                            node.Nodes = null;
                    }
                    else
                    {
                        node = new ChangeNode
                        {
                            Name = name,
                            FullName = currentNode.FullName + "/" + name,
                            Type = item.Type,
                            IsFolder = item.IsFolder,
                            Parent = currentNode
                        };
                        currentNode.Nodes.Add(node.Name, node);
                    }
                }
            }

            root.PropagateAdd();

            return root;
        }

        public string Name { get; set; }

        public ChangeNode Parent { get; set; }

        public Dictionary<string, ChangeNode> Nodes { get; set; }

        public ChangeNode ShallowClone(ChangeNode newParent)
        {
            return new ChangeNode { Name = Name, FullName = FullName, Type = Type, IsFolder = IsFolder, Parent = newParent };
        }

        public void PropagateAdd()
        {
            if (Nodes == null)
                return;

            bool add = false;

            foreach (var node in Nodes.Values)
            {
                node.PropagateAdd();

                if (node.Type == ChangeType.Add || node.Type == ChangeType.Undelete)
                    add = true;
            }

            if (add)
                Type = ChangeType.Add;
        }

        /// <summary>
        /// Merges <paramref name="other"/> with this node, assuming that
        /// <paramref name="other"/> occurred after the changes specified by this node.
        /// </summary>
        /// <param name="other">The more recent node.</param>
        public void SequentialMerge(ChangeNode other)
        {
            if (other.Nodes == null)
                return;

            foreach (var otherNode in other.Nodes.Values)
            {
                ChangeNode ourNode = null;

                if (Nodes != null && Nodes.TryGetValue(otherNode.Name, out ourNode))
                {
                    switch (otherNode.Type)
                    {
                        case ChangeType.Add:
                            ourNode.Type = ChangeType.Add;

                            if (ourNode.IsFolder != otherNode.IsFolder)
                            {
                                // The file was replaced by a folder, or vice versa. Delete all children.
                                ourNode.IsFolder = otherNode.IsFolder;
                                ourNode.Nodes = null;
                            }
                            break;
                        case ChangeType.SetDisplayName:
                            if (ourNode.Type == ChangeType.None)
                                ourNode.Type = ChangeType.SetDisplayName;
                            break;
                        case ChangeType.Delete:
                            ourNode.Type = ChangeType.Delete;
                            // Recursive delete.
                            ourNode.Nodes = null;
                            break;
                        case ChangeType.Undelete:
                            if (ourNode.Type == ChangeType.None ||
                                ourNode.Type == ChangeType.SetDisplayName ||
                                ourNode.Type == ChangeType.Delete)
                            {
                                ourNode.Type = ChangeType.Undelete;
                            }
                            break;
                    }
                }
                else
                {
                    if (Nodes == null)
                        Nodes = new Dictionary<string, ChangeNode>();

                    ourNode = otherNode.ShallowClone(this);
                    Nodes.Add(ourNode.Name, ourNode);
                }

                if (ourNode.Type != ChangeType.Delete && ourNode.IsFolder)
                    ourNode.SequentialMerge(otherNode);
            }
        }

        /// <summary>
        /// Determines if a preserving merge will succeed.
        /// 
        /// Note: a.PreservingConflicts(b) is always equal to b.PreservingConflicts(a).
        /// </summary>
        /// <param name="other">The other node.</param>
        /// <param name="callback">A callback invoked for each conflict.</param>
        public bool PreservingConflicts(ChangeNode other, Action<ChangeNode, ChangeNode> callback = null)
        {
            if (Nodes == null || other.Nodes == null)
                return false;

            bool conflict = false;

            foreach (var otherNode in other.Nodes.Values)
            {
                ChangeNode ourNode = null;

                if (!Nodes.TryGetValue(otherNode.Name, out ourNode))
                    continue;

                switch (otherNode.Type)
                {
                    case ChangeType.Add:
                        if (ourNode.Type == ChangeType.Add && (!ourNode.IsFolder || !otherNode.IsFolder))
                        {
                            conflict = true;
                            if (callback != null)
                                callback(ourNode, otherNode);
                            else
                                return true;
                        }
                        if (ourNode.Type == ChangeType.Undelete && otherNode.IsFolder)
                        {
                            conflict = true;
                            if (callback != null)
                                callback(ourNode, otherNode);
                            else
                                return true;
                        }
                        break;
                    case ChangeType.Undelete:
                        if (ourNode.Type == ChangeType.Add && ourNode.IsFolder)
                        {
                            conflict = true;
                            if (callback != null)
                                callback(ourNode, otherNode);
                            else
                                return true;
                        }
                        break;
                }

                if (!conflict && ourNode.IsFolder && otherNode.IsFolder &&
                    ourNode.Type != ChangeType.Delete && otherNode.Type != ChangeType.Delete)
                {
                    if (ourNode.PreservingConflicts(otherNode, callback))
                    {
                        conflict = true;
                        if (callback == null)
                            return true;
                    }
                }
            }

            return conflict;
        }

        /// <summary>
        /// Merges <paramref name="other"/> with this node, attempting to preserve
        /// data where possible. No assumptions are made about which changes were
        /// made first. It is assumed that <see cref="PreservingConflicts"/> returns
        /// true.
        /// 
        /// Note: (a.PreservingMerge(b), a) is always equal to (b.PreservingMerge(a), b).
        /// </summary>
        /// <param name="other">The other node.</param>
        public void PreservingMerge(ChangeNode other)
        {
            if (other.Nodes == null)
                return;

            foreach (var otherNode in other.Nodes.Values)
            {
                ChangeNode ourNode = null;

                if (Nodes != null && Nodes.TryGetValue(otherNode.Name, out ourNode))
                {
                    switch (otherNode.Type)
                    {
                        case ChangeType.Add:
                            if (ourNode.Type == ChangeType.Add && (!ourNode.IsFolder || !otherNode.IsFolder))
                                throw new Exception("Add conflicts with Add.");
                            if (ourNode.Type == ChangeType.Undelete && otherNode.IsFolder)
                                throw new Exception("Add folder conflicts with Undelete file.");

                            ourNode.Type = ChangeType.Add;
                            ourNode.IsFolder = otherNode.IsFolder;
                            break;
                        case ChangeType.SetDisplayName:
                            if (ourNode.Type == ChangeType.None)
                                ourNode.Type = ChangeType.SetDisplayName;
                            break;
                        case ChangeType.Delete:
                            if (ourNode.Type == ChangeType.None || ourNode.Type == ChangeType.SetDisplayName)
                                ourNode.Type = ChangeType.Delete;
                            break;
                        case ChangeType.Undelete:
                            if (ourNode.Type == ChangeType.Add && ourNode.IsFolder)
                                throw new Exception("Add folder conflicts with Undelete file.");

                            if (ourNode.Type == ChangeType.None ||
                                ourNode.Type == ChangeType.SetDisplayName ||
                                ourNode.Type == ChangeType.Delete)
                            {
                                ourNode.Type = ChangeType.Undelete;
                            }
                            break;
                    }
                }
                else
                {
                    if (Nodes == null)
                        Nodes = new Dictionary<string, ChangeNode>();

                    ourNode = otherNode.ShallowClone(this);
                    Nodes.Add(ourNode.Name, ourNode);
                }

                if (ourNode.IsFolder && otherNode.IsFolder &&
                    ourNode.Type != ChangeType.Delete && otherNode.Type != ChangeType.Delete)
                {
                    ourNode.PreservingMerge(otherNode);
                }
            }
        }

        /// <summary>
        /// Makes <paramref name="other"/> sequential with respect to this node, so that
        /// <code>this.MakePreserving(other); this.SequentialMerge(other);</code> has the
        /// same effect as <code>this.PreservingMerge(other)</code>. It is assumed that
        /// <see cref="PreservingConflicts"/> returns true.
        /// </summary>
        /// <param name="other">The other node.</param>
        /// <param name="callback">A callback invoked for each change removed.</param>
        public void MakeSequentialByPreserving(ChangeNode other, Action<ChangeNode, ChangeNode> callback = null)
        {
            if (other.Nodes == null)
                return;

            foreach (var otherNode in other.Nodes.Values)
            {
                ChangeNode ourNode = null;

                if (Nodes != null && Nodes.TryGetValue(otherNode.Name, out ourNode))
                {
                    switch (otherNode.Type)
                    {
                        case ChangeType.Add:
                            if (ourNode.Type == ChangeType.Add && (!ourNode.IsFolder || !otherNode.IsFolder))
                                throw new Exception("Add conflicts with Add.");
                            if (ourNode.Type == ChangeType.Undelete && otherNode.IsFolder)
                                throw new Exception("Add folder conflicts with Undelete file.");
                            break;
                        case ChangeType.SetDisplayName:
                            if (ourNode.Type == ChangeType.Delete)
                            {
                                if (callback != null)
                                    callback(ourNode, otherNode);
                                otherNode.Type = ChangeType.None;
                            }
                            break;
                        case ChangeType.Delete:
                            if (ourNode.Type == ChangeType.Add ||
                                ourNode.Type == ChangeType.Delete ||
                                ourNode.Type == ChangeType.Undelete)
                            {
                                if (callback != null)
                                    callback(ourNode, otherNode);
                                otherNode.Type = ChangeType.None;
                            }
                            break;
                        case ChangeType.Undelete:
                            if (ourNode.Type == ChangeType.Add && ourNode.IsFolder)
                                throw new Exception("Add folder conflicts with Undelete file.");

                            if (ourNode.Type == ChangeType.Add ||
                                ourNode.Type == ChangeType.Undelete)
                            {
                                if (callback != null)
                                    callback(ourNode, otherNode);
                                otherNode.Type = ChangeType.None;
                            }
                            break;
                    }

                    if (ourNode.IsFolder && otherNode.IsFolder &&
                        ourNode.Type != ChangeType.Delete && otherNode.Type != ChangeType.Delete)
                    {
                        ourNode.MakeSequentialByPreserving(otherNode, callback);
                    }
                }
            }
        }

        public IEnumerable<ChangeNode> RecursiveEnumerate()
        {
            Stack<ChangeNode> stack = new Stack<ChangeNode>();

            if (Nodes != null)
            {
                foreach (var node in Nodes.Values)
                    stack.Push(node);
            }

            while (stack.Count != 0)
            {
                var node = stack.Pop();

                yield return node;

                if (node.Nodes != null)
                {
                    foreach (var subNode in node.Nodes.Values)
                        stack.Push(subNode);
                }
            }
        }

        private void ToItems(List<ChangeItem> list)
        {
            if (Nodes == null)
                return;

            foreach (var node in Nodes.Values)
            {
                if (node.Type != ChangeType.None)
                    list.Add(new ChangeItem { FullName = node.FullName, Type = node.Type, IsFolder = node.IsFolder });

                node.ToItems(list);
            }
        }

        public List<ChangeItem> ToItems()
        {
            List<ChangeItem> list = new List<ChangeItem>();
            this.ToItems(list);
            return list;
        }
    }
}