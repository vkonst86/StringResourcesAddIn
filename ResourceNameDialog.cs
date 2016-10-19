using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Projects;

namespace StringResourcesAddIn
{
    public class ResourceEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string PropertyName { get; set; }
    }

    public partial class ResourceNameDialog : Gtk.Dialog
    {

        public event EventHandler<ResourceEventArgs> OnOkPressed;

        public ResourceNameDialog (string propertyName = "", string filePath = "")
        {
            this.Build ();
            var files = ResXHelper.EnumerateResXFiles ();
            var fileNames = files.Select (file => file.Name).ToArray ();

            foreach (var fileName in fileNames)
                cbResName.AppendText (System.IO.Path.GetFileName (fileName));

            if (!string.IsNullOrEmpty (filePath))
                cbResName.Active = fileNames.IndexOf (filePath);
            else if (fileNames.Length > 0)
                cbResName.Active = 0;

            entryResName.Text = propertyName;

            buttonOk.Clicked += (sender, e) => {
                if (cbResName.Active >= 0)
                {
                    var file = files [cbResName.Active];
                    string resName = entryResName.Text;

                    if (OnOkPressed != null && !string.IsNullOrEmpty (resName))
                    {
                        OnOkPressed (this, new ResourceEventArgs () { FilePath = file.FilePath, PropertyName = resName });
                    }
                }
            };

            buttonCancel.Clicked += (sender, e) => {
                this.Destroy ();
            };
        }       
    }
}
