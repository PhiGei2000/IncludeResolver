using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IncludeResolver.Elements {
    /// <summary>
    /// Interaction logic for IncludeNode.xaml
    /// </summary>
    public partial class IncludeNodeControl : UserControl {
        public IncludeGraph Graph { get; set; }

        public delegate void OpenFileEventHandler(object sender, string filename, EventArgs e);
        public event OpenFileEventHandler OpenFile;

        public IncludeNodeControl() {
            InitializeComponent();
        }

        public void DrawGraph() {
            if (Graph == null)
                throw new InvalidOperationException();

            output.Items.Clear();
            output.Items.Add(GetNode(Graph.Root));
        }

        private TreeViewItem GetNode(IncludeGraphNode node) {
            TreeViewItem item = new TreeViewItem() {
                Header = node.Name
            };

            var treeViewItemMenu = new ContextMenu();
            var expandItem = new MenuItem() {
                Header = "Expand subtree",
                Tag = item
            };

            expandItem.Click += ExpandItem_Click;
            treeViewItemMenu.Items.Add(expandItem);
            
            var unexpandItem = new MenuItem() {
                Header = "Unexpand subtree",
                Tag = item
            };

            unexpandItem.Click += UnexpandItem_Click;
            treeViewItemMenu.Items.Add(unexpandItem);

            var openFile = new MenuItem() {
                Header = "Open file",
                Tag = item
            };

            openFile.Click += (sender, e) => {
                TreeViewItem tmp = (sender as MenuItem)?.Tag as TreeViewItem;

                if (tmp != null) {
                    string filename = tmp.Header as string;
                    OpenFile?.Invoke(sender, filename, e);
                }
            };
            treeViewItemMenu.Items.Add(openFile);

            item.ContextMenu = treeViewItemMenu;

            foreach (var childNode in node.Includes) {
                item.Items.Add(GetNode(childNode));
            }

            return item;
        }

        private void MenuItem_ExpandAll_Click(object sender, RoutedEventArgs e) {
            foreach (TreeViewItem item in output.Items) {
                item.ExpandSubtree();
            }
        }

        private void MenuItem_UnexpandAll_Click(object sender, RoutedEventArgs e) {
            foreach (TreeViewItem item in output.Items) {
                UnexpandItem(item);
            }
        }

        private static void UnexpandItem(TreeViewItem item) {
            foreach (TreeViewItem child in item.Items) {
                UnexpandItem(child);
            }

            item.IsExpanded = false;
        }

        private void ExpandItem_Click(object sender, RoutedEventArgs e) {
            TreeViewItem item = (sender as MenuItem)?.Tag as TreeViewItem;

            if (item != null)
                item.ExpandSubtree();
        }

        private void UnexpandItem_Click(object sender, RoutedEventArgs e) {
            TreeViewItem item = (sender as MenuItem)?.Tag as TreeViewItem;

            if (item != null)
                UnexpandItem(item);
        }
    }
}
