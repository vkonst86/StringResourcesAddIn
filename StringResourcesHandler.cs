using System;
using Gtk;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TextEditing;

namespace StringResourcesAddIn
{
    public class StringResourcesHandler : CommandHandler
    {
        public StringResourcesHandler ()
        {
        }

        protected override void Run ()
        {
            base.Run ();
        }

        protected override void Run (object dataItem)
        {
            ResourceNameDialog dialog;

            string selectedtext = GetSelectedText ();
            var existingEntry = ResXHelper.GetExistingEntryByValue (selectedtext);

            if (existingEntry.HasValue)
                dialog = new ResourceNameDialog (existingEntry.Value.Value.Key.ToString (), existingEntry.Value.Key);
            else
                dialog = new ResourceNameDialog ();
            
            dialog.OnOkPressed += (sender, e) => {

                if (existingEntry.HasValue)
                {
                    ReplaceText (existingEntry.Value.Key, existingEntry.Value.Value.Key.ToString ());
                    dialog.Destroy ();
                    return;
                }

                var resource = ResXHelper.FindResourceByKey (e.FilePath, e.PropertyName);
                if (resource.HasValue)
                {
                    MessageDialog messageDialog = new MessageDialog (dialog, DialogFlags.DestroyWithParent,
                                                                     MessageType.Error, ButtonsType.Close,
                                                                     "Resource with key '{0}' already exists!", e.PropertyName);

                    messageDialog.Run ();
                    messageDialog.Destroy ();
                    return;
                }

                if (ResXHelper.AddResource (e.FilePath, e.PropertyName, selectedtext))
                {
                    ReplaceText (e.FilePath, e.PropertyName);
                    dialog.Destroy ();
                }

            };
            dialog.Run ();

        }

        /// <summary>
        /// Replaces the text.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="propertyName">Property name.</param>
        private void ReplaceText(string filePath, string propertyName)
        {
            string className = ResXHelper.GetClassName (filePath);
            Document doc = IdeApp.Workbench.ActiveDocument;
            var content = doc.GetContent<TextEditor> ();
            if (content != null && !string.IsNullOrEmpty (className))
            {
                TextSegment segment = new TextSegment (content.SelectionRange.Offset - 1, content.SelectionRange.Length + 2);
                content.SelectionRange = segment;
                content.SelectedText = string.Format ("{0}.{1}", className, propertyName);
            }
        }

        /// <summary>
        /// Gets the selected text.
        /// </summary>
        /// <returns>The selected text.</returns>
        private string GetSelectedText()
        {
            Document doc = IdeApp.Workbench.ActiveDocument;
            var content = doc.GetContent<TextEditor> ();
            if (content != null)
                return content.SelectedText;

            return string.Empty;

        }

        protected override void Update (CommandInfo info)
        {
            Document doc = IdeApp.Workbench.ActiveDocument;
            info.Enabled = doc != null && !string.IsNullOrEmpty (GetSelectedText ());
        }
    }
}
