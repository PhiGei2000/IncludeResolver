using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;

namespace IncludeResolver {
    public class IncludeGraph {
        private IncludeGraphNode root;

        public IncludeGraphNode Root { get => root; }

        public int Depth {
            get {
                return root?.GetDepth(0) ?? 0;
            }
        }

        public bool FindNode(string name, out IncludeGraphNode node) {
            node = root.FindNode(name);
            return node != null;
        }

        public bool ContainsNode(string name) {
            return root.ContainsNode(name);
        }

        private void CleanUp() {
            List<IncludeGraphNode> nodes = new List<IncludeGraphNode>();
            root.CleanUp(ref nodes);
        }

        private static (string name, IEnumerable<string> includes) GetFileNode(DirectoryInfo includeDirectory, DirectoryInfo sourceDirectory, FileInfo file) {
            string nodeName;
            if (file.FullName.Contains(includeDirectory.FullName)) {
                nodeName = includeDirectory.GetRelativePath(file.FullName).Trim('/', '\\');
            }
            else if (file.FullName.Contains(sourceDirectory.FullName)) {
                nodeName = sourceDirectory.GetRelativePath(file.FullName).Trim('/', '\\');
            }
            else throw new ArgumentOutOfRangeException(nameof(file));


            var includes = GetIncludes(includeDirectory, file);

            return (nodeName, includes);
        }

        public static IncludeGraph GetIncludeGraph(DirectoryInfo includeDirectory, DirectoryInfo sourceDirectory, FileInfo file) {
            UnorderedIncludeGraph graph = GetUnorderedIncludeGraph(includeDirectory, sourceDirectory, file);

            return graph.GetIncludeGraph(sourceDirectory.GetRelativePath(file.FullName).TrimStart('/', '\\'));
        }

        private static UnorderedIncludeGraph GetUnorderedIncludeGraph(DirectoryInfo includeDirectory, DirectoryInfo sourceDirectory, FileInfo file) {
            UnorderedIncludeGraph includeGraph = new UnorderedIncludeGraph();

            var node = GetFileNode(includeDirectory, sourceDirectory, file);
            includeGraph.AddNode(node.name, node.includes.Except(new string[] { node.name }));

            foreach (var include in node.includes) {
                if (include.StartsWith("<"))
                    continue;

                FileInfo includedFile = includeDirectory.GetFiles(include)[0];

                includeGraph.Merge(GetUnorderedIncludeGraph(includeDirectory, sourceDirectory, includedFile));
            }

            return includeGraph;
        }

        private static IEnumerable<string> GetIncludes(DirectoryInfo includeDirectory, FileInfo file) {
#if DEBUG
            Debug.WriteLine($"File: {file.FullName}");
#endif

            IEnumerable<string> includes = File.ReadAllLines(file.FullName).Where(line =>
                line.StartsWith("#include"));
            foreach (var include in includes) {
                if (include.Contains('<')) {
                    // external file
                    int startIndex = include.IndexOf('<');
                    int endIndex = include.IndexOf('>') + 1;
                    string includePath = include.Substring(startIndex, endIndex - startIndex);

                    yield return includePath;

                }
                else if (include.Contains('\"')) {
                    int startIndex = include.IndexOf('\"') + 1;
                    int endIndex = include.LastIndexOf('\"');
                    string includePath = include.Substring(startIndex, endIndex - startIndex);

                    // get filePath
                    DirectoryInfo searchDirectory = file.Directory;
                    while (includePath.StartsWith("../")) {
                        searchDirectory = searchDirectory.Parent;
                        includePath = includePath.Substring(3);
                    }

                    FileInfo includeFile = searchDirectory.GetFiles(includePath)[0];

                    string includeName = includeDirectory.GetRelativePath(includeFile.FullName);
                    yield return includeName.TrimStart('/', '\\');
                }
            }
        }

        private class UnorderedIncludeGraph : ICloneable {
            public List<string> nodeNames;
            public Dictionary<string, IEnumerable<string>> connections;

            public (string name, IEnumerable<string> includes) this[string name] {
                get => GetNode(name);
            }

            public UnorderedIncludeGraph() {
                nodeNames = new List<string>();
                connections = new Dictionary<string, IEnumerable<string>>();
            }

            public void AddNode(string name, IEnumerable<string> includes) {
                if (!nodeNames.Contains(name)) {
                    nodeNames.Add(name);
                    connections.Add(name, includes);
                }
                else {
                    connections[name] = connections[name].Concat(includes);
                }
            }

            public (string name, IEnumerable<string> includes) GetNode(string name) {
                if (!nodeNames.Contains(name))
                    throw new ArgumentException();

                return (nodeNames.Find(n => n == name), connections[name]);
            }

            public IncludeGraph GetIncludeGraph(string root) {
                // create graph                
                List<IncludeGraphNode> createdNodes = new List<IncludeGraphNode>();
                IncludeGraph graph = new IncludeGraph();
                graph.root = GetIncludeGraphNode(root, ref createdNodes);

                // cleanup graph
                graph.CleanUp();

                return graph;
            }

            private IncludeGraphNode GetIncludeGraphNode(string name, ref List<IncludeGraphNode> createdNodes) {
                if (createdNodes.Any(node => node.Name == name)) {
                    return createdNodes.Find(node => node.Name == name);
                }

                if (name.StartsWith("<"))
                    return new IncludeGraphNode(name);

                try {
                    IEnumerable<string> includes = connections[name];

                    if (includes.Count() == 0) {
                        return new IncludeGraphNode(name);
                    }

                    List<IncludeGraphNode> includedNodes = new List<IncludeGraphNode>();
                    IncludeGraphNode node = new IncludeGraphNode(name, includedNodes);
                    createdNodes.Add(node);

                    foreach (var include in includes) {
                        includedNodes.Add(GetIncludeGraphNode(include, ref createdNodes));
                    }
                    node.Includes = includedNodes;

                    return node;
                }
                catch (KeyNotFoundException e) {
#if DEBUG
                    Debug.WriteLine($"node names: {string.Join(", ", nodeNames)}");
                    Debug.WriteLine($"Key {name}");
#endif
                    throw e;
                }
            }

            public static UnorderedIncludeGraph Merge(UnorderedIncludeGraph first, UnorderedIncludeGraph secnd) {
                UnorderedIncludeGraph result = (UnorderedIncludeGraph)first.Clone();

                foreach (var node in secnd.nodeNames) {
                    if (result.nodeNames.Contains(node)) {
                        result.connections[node] = result.connections[node].Union(secnd.connections[node]);
                    }
                    else {
                        string name = (string)node.Clone();
                        result.AddNode(name, secnd.connections[name].Select(str => (string)str.Clone()));
                    }
                }

                return result;
            }

            public void Merge(UnorderedIncludeGraph secnd) {
                foreach (var node in secnd.nodeNames) {
                    if (nodeNames.Contains(node)) {
                        connections[node] = connections[node].Union(secnd.connections[node]);
                    }
                    else {
                        string name = (string)node.Clone();
                        AddNode(name, secnd.connections[name].Select(str => (string)str.Clone()));
                    }
                }
            }

            public object Clone() {
                UnorderedIncludeGraph tmp = new UnorderedIncludeGraph();
                foreach (var node in nodeNames) {
                    string name = (string)node.Clone();
                    tmp.AddNode(name, connections[name].Select(str => (string)str.Clone()));
                }

                return tmp;
            }
        }
    }
}
