using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IncludeResolver {
    public class IncludeGraphNode : ICloneable, IEquatable<IncludeGraphNode> {
        public List<IncludeGraphNode> Includes { get; set; }

        public string Name { get; set; }

        public IncludeGraphNode(string name, IEnumerable<IncludeGraphNode> includes) {
            this.Includes = includes.ToList();
            Name = name;
        }

        public IncludeGraphNode(string name) {
            Name = name;
            Includes = new List<IncludeGraphNode>();
        }

        public int GetDepth(int depth) {
            if(Includes.Count == 0) {
                return depth;
            }

            depth++;
            int maxDepth = depth;
            int subDepth;
            foreach (var node in Includes) {
                if ((subDepth = node.GetDepth(depth)) > maxDepth) {
                    maxDepth = subDepth;
                }
            }

            return maxDepth;
        }

        public IncludeGraphNode FindNode(string name) {
            if (Name == name) {
                return this;
            }
            else if (Includes.Count > 0) {
                foreach (var node in Includes) {
                    IncludeGraphNode foundNode = node.FindNode(name);
                    if (foundNode != null)
                        return foundNode;
                }
            }

            return null;
        }

        public bool ContainsNode(string name) {
            if (Name == name)
                return true;

            return Includes.Any(node => node.ContainsNode(name));
        }

        public void CleanUp(ref List<IncludeGraphNode> progressedNodes) {
#if DEBUG
            Debug.WriteLine(Name);
#endif

            // node double
            for (int i = 0; i < Includes.Count; i++) {
                var node = Includes[i];
                if (progressedNodes.Contains(node)) {
                    Includes.Remove(node);
                    Includes.Add(progressedNodes.First(n => n.Equals(node)));
                }
                else {
                    node.CleanUp(ref progressedNodes);
                }
            }

            progressedNodes.Add(this);
        }

        public void AddNode(IncludeGraphNode node, IncludeGraphNode parentNode) {
            if (Includes.Contains(parentNode)) {

            }
        }

        public object Clone() {
            return new IncludeGraphNode((string)Name.Clone(), Includes);
        }

        public bool Equals(IncludeGraphNode other) {
            return other.Name.Equals(Name);
        }

        public override string ToString() {
            return Name;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;

            if (obj is IncludeGraphNode node) {
                return this.Equals(node);
            }

            return false;
        }

        public override int GetHashCode() {
            int hashCode = 641787874;
            hashCode = hashCode * -1521134295 + EqualityComparer<List<IncludeGraphNode>>.Default.GetHashCode(Includes);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        public static bool operator ==(IncludeGraphNode left, IncludeGraphNode right) {
            return left.Equals(right);
        }

        public static bool operator !=(IncludeGraphNode left, IncludeGraphNode right) {
            return !left.Equals(right);
        }
    }
}