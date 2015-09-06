using System;
using System.Collections.Generic;
using System.Linq;

namespace m.Indexer
{
    public class NodeHierarchyRoot : NodeHierarchy
    {
        public string RootPath { get; set; }
        public override string Name => RootPath;

        public void CalculateStats()
        {
            RecurseUpTreeCalculatingStats(this, 0);
        }


        public static NodeHierarchyRoot CreateNodeHierarchy(List<NodeEnvelope> nodeEnvelopes)
        {
            var orderdEnvelopes = new List<NodeEnvelope>(nodeEnvelopes);
            var rootEnvelope = orderdEnvelopes[0];
            orderdEnvelopes.RemoveAt(0);
            var root = new NodeHierarchyRoot()
            {
                RootPath = rootEnvelope.GetFullPath(),  
                Children = new List<NodeHierarchy>(),
                Envelope = rootEnvelope
            };
            var currentNode = (NodeHierarchy)root;
            while (orderdEnvelopes.Any())
            {
                var currentEnvelope = orderdEnvelopes[0];
                //the full path of the target node should be the parent of the path of the envelope we are handling.
                if (currentNode.GetFullPath() != currentEnvelope.Header.FullDirectoryPath)
                {
                    currentNode = root.FindSubNode(currentEnvelope);
                }
                if (currentNode.GetFullPath() == currentEnvelope.Header.FullDirectoryPath)
                {
                    orderdEnvelopes.RemoveAt(0);
                    currentNode.Children.Add(new NodeHierarchy()
                    {
                        Envelope = currentEnvelope,
                        Children = new List<NodeHierarchy>(),
                        Parent = currentNode
                    });
                }
                else
                {
                    throw new ApplicationException(
                        $"FullDirectoryPath {currentNode.GetFullPath()} does not match {currentEnvelope.Header.FullDirectoryPath}");
                }
            }
            return root;
        }
    }
}