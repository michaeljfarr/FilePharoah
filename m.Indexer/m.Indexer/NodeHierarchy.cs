using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace m.Indexer
{
    public class NodeHierarchy
    {
        public NodeEnvelope Envelope { get; set; }
        public virtual String Name { get { return Envelope.Header.FileName; } }
        public List<NodeHierarchy> Children { get; set; }
        public Dictionary<string, int> ClassCounts { get; set; }
        public NodeHierarchy Parent { get; set; }

        public String GetFullPath()
        {
            var names = new List<String>() { Name };
            var currentNode = this;
            while (currentNode.Parent != null)
            {
                currentNode = currentNode.Parent;
                names.Insert(0, currentNode.Name);
            }
            return Path.Combine(names.ToArray());
        }

        public NodeHierarchy FindSubNode(NodeEnvelope node)
        {
            if(GetFullPath().Length > node.Header.FullDirectoryPath.Length)
            {
                return this;
            }
            string subPath = node.Header.FullDirectoryPath.Substring(GetFullPath().Length + 1);
            var nodeParts = subPath.Split('\\');
            var searchNode = this;
            for (int i = 0; i < nodeParts.Length; i++)
            {
                var child = searchNode.Children.FirstOrDefault(a => a.Name == nodeParts[i]);
                if (child != null)
                {
                    searchNode = child;
                }
                else
                {
                    throw new ApplicationException($"Could not find subpath: {subPath}");
                }
            }
            return searchNode;
        }

        public override string ToString()
        {
            return $"{this.GetFullPath()}";
        }

        protected static void RecurseUpTreeCalculatingStats(NodeHierarchy node, int depth)
        {
            if (node.Children.Any())
            {
                node.ClassCounts = new Dictionary<string, int>() { { node.Envelope.GetClass(), 1 } };
                foreach (var child in node.Children)
                {
                    RecurseUpTreeCalculatingStats(child, depth + 1);
                    foreach (var classCount in child.ClassCounts)
                    {
                        if (node.ClassCounts.ContainsKey(classCount.Key))
                        {
                            node.ClassCounts[classCount.Key] += classCount.Value;
                        }
                        else
                        {
                            node.ClassCounts[classCount.Key] = classCount.Value;
                        }
                    }
                }
            }
            else
            {
                node.ClassCounts = new Dictionary<string, int>() { { node.Envelope.GetClass(), 1 } };
            }
        }

    }
}