using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IncludeResolver {
    static class ExtensionMethods {
        public static string GetRelativePath(this DirectoryInfo directory, string filename) {
            string directoryName = directory.FullName;

            return Path.GetFullPath(filename).Replace(directoryName, string.Empty);
        }
    }
}
