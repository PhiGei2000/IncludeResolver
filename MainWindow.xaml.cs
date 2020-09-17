using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Forms;

using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using TextBox = System.Windows.Controls.TextBox;
using System.Diagnostics;

namespace IncludeResolver {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private string includeDirectory;
        private string sourceDirectory;

        public MainWindow() {
            InitializeComponent();

            graphVisualizer.OpenFile += graphVisualizer_OpenFile;
#if DEBUG
            txtBox_includeDir.Text = @"D:\Users\Philipp\Source\Repos\Voxelgame\include";
            txtBox_sourceDir.Text = @"D:\Users\Philipp\Source\Repos\Voxelgame\src";
#endif
        }

        private void btn_openIncludeDirectory_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog openDialog = new FolderBrowserDialog() {
                ShowNewFolderButton = false,
                Description = "Select include directory",
            };

            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                includeDirectory = openDialog.SelectedPath;

                txtBox_includeDir.Text = includeDirectory;
            }
        }

        private void btn_openSourceDirectory_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog openDialog = new FolderBrowserDialog() {
                ShowNewFolderButton = false,
                Description = "Select source directory"
            };

            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                sourceDirectory = openDialog.SelectedPath;

                txtBox_sourceDir.Text = sourceDirectory;
            }
        }

        private void btn_drawGraph_Click(object sender, RoutedEventArgs e) {
            if (includeDirectory == null)
                includeDirectory = txtBox_includeDir.Text;

            if (sourceDirectory == null)
                sourceDirectory = txtBox_sourceDir.Text;

            string file = Path.Combine(sourceDirectory, sourceListView.SelectedItem as string);


            try {
                IncludeGraph graph = IncludeGraph.GetIncludeGraph(new DirectoryInfo(includeDirectory), new DirectoryInfo(sourceDirectory), new FileInfo(file));
                graphVisualizer.Graph = graph;
                graphVisualizer.DrawGraph();
            }
            catch (Exception exp) {
                MessageBox.Show(exp.Message + "\r\n" + exp.StackTrace);
            }
        }

        private void txtBox_sourceDir_TextChanged(object sender, TextChangedEventArgs e) {
            DirectoryInfo info = new DirectoryInfo((sender as TextBox).Text);

            if (info.Exists) {
                //sourceListView.Items.Clear();
                sourceListView.ItemsSource = info.GetFiles("*.cpp", SearchOption.AllDirectories).Select(f => info.GetRelativePath(f.FullName).Trim('/', '\\'));
            }
        }

        private void graphVisualizer_OpenFile(object sender, string filename, EventArgs e) {
            if (filename.EndsWith(".h")) {
                Process.Start(Path.Combine(includeDirectory, filename));
            }
            else if(filename.EndsWith(".cpp")) {
                Process.Start(Path.Combine(sourceDirectory, filename));
            }
        }
    }
}
