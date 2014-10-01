using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FooBox.Models
{
    public enum SyncChangeType
    {
        /// <summary>
        /// No change.
        /// </summary>
        None,

        /// <summary>
        /// Add a document, modify a document (add a document version), or add a folder.
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

    public class SyncItem
    {
        public string FullName { get; set; }
        public SyncChangeType Type { get; set; }
        public bool IsFolder { get; set; }
    }

    public class SyncNode
    {
        public static SyncNode FromItems(IEnumerable<SyncItem> list)
        {
            SyncNode root = new SyncNode
            {
                Name = "",
                FullName = "",
                Type = SyncChangeType.Add,
                IsFolder = true,
                Nodes = new Dictionary<string, SyncNode>()
            };

            foreach (var item in list)
            {
                var components = item.FullName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                SyncNode currentNode = root;

                if (components.Length == 0)
                    throw new Exception("Invalid file name '" + item.FullName + "'");

                for (int i = 0; i < components.Length - 1; i++)
                {
                    var name = components[i];

                    if (currentNode.Nodes.ContainsKey(name))
                    {
                        currentNode = currentNode.Nodes[name];

                        if (!currentNode.IsFolder)
                            throw new Exception("Inconsistent sync item '" + currentNode.FullName + "'");
                    }
                    else
                    {
                        SyncNode newNode = new SyncNode
                        {
                            Name = name,
                            FullName = currentNode.FullName + "/" + name,
                            Type = SyncChangeType.None,
                            IsFolder = true
                        };

                        if (currentNode.Nodes == null)
                            currentNode.Nodes = new Dictionary<string, SyncNode>();

                        currentNode.Nodes.Add(newNode.Name, newNode);
                        currentNode = newNode;
                    }

                    if (currentNode.Type == SyncChangeType.Delete)
                    {
                        currentNode = null;
                        break;
                    }

                    if (currentNode.Nodes == null)
                        currentNode.Nodes = new Dictionary<string, SyncNode>();
                }

                if (currentNode != null)
                {
                    var name = components[components.Length - 1];
                    SyncNode node = null;

                    if (currentNode.Nodes.TryGetValue(name, out node))
                    {
                        bool inconsistent = false;

                        if (item.IsFolder && item.Type == SyncChangeType.Add)
                            inconsistent = node.Type != SyncChangeType.None && node.Type != SyncChangeType.Add;
                        else
                            inconsistent = node.Type != SyncChangeType.None;

                        if (node.IsFolder != item.IsFolder)
                            inconsistent = true;

                        if (inconsistent)
                            throw new Exception("Duplicate sync item '" + node.FullName + "'.");

                        node.Type = item.Type;

                        if (node.Type == SyncChangeType.Delete)
                            node.Nodes = null;
                    }
                    else
                    {
                        node = new SyncNode
                        {
                            Name = name,
                            FullName = currentNode.FullName + "/" + name,
                            Type = item.Type,
                            IsFolder = item.IsFolder
                        };
                        currentNode.Nodes.Add(node.Name, node);
                    }
                }
            }

            root.PropagateAdd();

            return root;
        }

        public string Name { get; set; }
        public string FullName { get; set; }
        public SyncChangeType Type { get; set; }

        /// <summary>
        /// Whether we are adding a document or a folder.
        /// </summary>
        public bool IsFolder { get; set; }

        public Dictionary<string, SyncNode> Nodes { get; set; }

        public SyncNode ShallowClone()
        {
            return new SyncNode { Name = Name, FullName = FullName, Type = Type, IsFolder = IsFolder };
        }

        public void PropagateAdd()
        {
            if (Nodes == null)
                return;

            bool add = false;

            foreach (var node in Nodes.Values)
            {
                node.PropagateAdd();

                if (node.Type == SyncChangeType.Add || node.Type == SyncChangeType.Undelete)
                    add = true;
            }

            if (add)
                Type = SyncChangeType.Add;
        }

        /// <summary>
        /// Merges <paramref name="other"/> with this node, assuming that
        /// <paramref name="other"/> occurred after the changes specified by this node.
        /// </summary>
        /// <param name="other">The more recent node.</param>
        public void SequentialMerge(SyncNode other)
        {
            if (other.Nodes == null)
                return;

            foreach (var otherNode in other.Nodes.Values)
            {
                SyncNode ourNode = null;

                if (Nodes.TryGetValue(otherNode.Name, out ourNode))
                {
                    switch (otherNode.Type)
                    {
                        case SyncChangeType.Add:
                            ourNode.Type = SyncChangeType.Add;

                            if (ourNode.IsFolder != otherNode.IsFolder)
                            {
                                // The file was replaced by a folder, or vice versa. Delete all children.
                                ourNode.IsFolder = otherNode.IsFolder;
                                ourNode.Nodes = null;
                            }
                            break;
                        case SyncChangeType.SetDisplayName:
                            if (ourNode.Type == SyncChangeType.None)
                                ourNode.Type = SyncChangeType.SetDisplayName;
                            break;
                        case SyncChangeType.Delete:
                            ourNode.Type = SyncChangeType.Delete;
                            // Recursive delete.
                            ourNode.Nodes = null;
                            break;
                        case SyncChangeType.Undelete:
                            if (ourNode.Type == SyncChangeType.None ||
                                ourNode.Type == SyncChangeType.SetDisplayName ||
                                ourNode.Type == SyncChangeType.Delete)
                            {
                                ourNode.Type = SyncChangeType.Undelete;
                            }
                            break;
                    }
                }
                else
                {
                    ourNode = otherNode.ShallowClone();
                    Nodes.Add(otherNode.Name, ourNode);
                }

                if (ourNode.Type != SyncChangeType.Delete && ourNode.IsFolder)
                    ourNode.SequentialMerge(otherNode);
            }
        }

        /// <summary>
        /// Merges <paramref name="other"/> with this node, attempting to preserve
        /// data where possible. There are no assumptions about which changes were
        /// made first. It is assumed that <see cref="PreservingConflicts"/> returns
        /// true.
        /// </summary>
        /// <param name="other">The other node.</param>
        public void PreservingMerge(SyncNode other)
        {
            if (other.Nodes == null)
                return;

            foreach (var otherNode in other.Nodes.Values)
            {
                SyncNode ourNode = null;

                if (Nodes.TryGetValue(otherNode.Name, out ourNode))
                {
                    switch (otherNode.Type)
                    {
                        case SyncChangeType.Add:
                            if (ourNode.Type == SyncChangeType.Add && (!ourNode.IsFolder || !otherNode.IsFolder))
                                throw new Exception("Add conflicts with Add.");
                            if (ourNode.Type == SyncChangeType.Undelete && otherNode.IsFolder)
                                throw new Exception("Add folder conflicts with Undelete file.");

                            ourNode.Type = SyncChangeType.Add;
                            ourNode.IsFolder = otherNode.IsFolder;
                            break;
                        case SyncChangeType.SetDisplayName:
                            if (ourNode.Type == SyncChangeType.None)
                                ourNode.Type = SyncChangeType.SetDisplayName;
                            break;
                        case SyncChangeType.Delete:
                            if (ourNode.Type == SyncChangeType.None || ourNode.Type == SyncChangeType.SetDisplayName)
                                ourNode.Type = SyncChangeType.Delete;
                            break;
                        case SyncChangeType.Undelete:
                            if (ourNode.Type == SyncChangeType.Add && ourNode.IsFolder)
                                throw new Exception("Add folder conflicts with Undelete file.");

                            if (ourNode.Type == SyncChangeType.None ||
                                ourNode.Type == SyncChangeType.SetDisplayName ||
                                ourNode.Type == SyncChangeType.Delete)
                            {
                                ourNode.Type = SyncChangeType.Undelete;
                            }
                            break;
                    }
                }
                else
                {
                    ourNode = otherNode.ShallowClone();
                    Nodes.Add(otherNode.Name, ourNode);
                }

                if (ourNode.IsFolder && otherNode.IsFolder &&
                    ourNode.Type != SyncChangeType.Delete && otherNode.Type != SyncChangeType.Delete)
                {
                    ourNode.PreservingMerge(otherNode);
                }
            }
        }

        /// <summary>
        /// Determines if a preserving merge will succeed.
        /// Note: a.PreservingConflicts(b) is always equal to b.PreservingConflicts(a).
        /// </summary>
        /// <param name="other">The other node.</param>
        public bool PreservingConflicts(SyncNode other)
        {
            if (other.Nodes == null)
                return false;

            foreach (var otherNode in other.Nodes.Values)
            {
                SyncNode ourNode = null;

                if (!Nodes.TryGetValue(otherNode.Name, out ourNode))
                    continue;

                switch (otherNode.Type)
                {
                    case SyncChangeType.Add:
                        if (ourNode.Type == SyncChangeType.Add && (!ourNode.IsFolder || !otherNode.IsFolder))
                            return true;
                        if (ourNode.Type == SyncChangeType.Undelete && otherNode.IsFolder)
                            return true;
                        break;
                    case SyncChangeType.Undelete:
                        if (ourNode.Type == SyncChangeType.Add && ourNode.IsFolder)
                            return true;
                        break;
                }

                if (ourNode.IsFolder && otherNode.IsFolder &&
                    ourNode.Type != SyncChangeType.Delete && otherNode.Type != SyncChangeType.Delete)
                {
                    if (ourNode.PreservingConflicts(otherNode))
                        return true;
                }
            }

            return true;
        }
    }
}