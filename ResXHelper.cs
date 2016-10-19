using System;
using System.Linq;
using System.Collections;
using System.Resources;
using System.IO;
using System.CodeDom;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using MonoDevelop.Projects;

namespace StringResourcesAddIn
{

    public class ResXHelper
    {
        /// <summary>
        /// Finds the resource by value.
        /// </summary>
        /// <returns>The resource by value.</returns>
        /// <param name="resxFilePath">RESX file path.</param>
        /// <param name="resValue">Res value.</param>
        public static DictionaryEntry? FindResourceByValue(string resxFilePath, string resValue)
        {
            using (ResXResourceReader resxReader = new ResXResourceReader (resxFilePath))
            {
                foreach (DictionaryEntry entry in resxReader)
                {
                    if (entry.Value.ToString ().Equals (resValue))
                        return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the resource by key.
        /// </summary>
        /// <returns>The resource by key.</returns>
        /// <param name="resxFilePath">RESX file path.</param>
        /// <param name="key">Key.</param>
        public static DictionaryEntry? FindResourceByKey (string resxFilePath, string key)
        {
            using (ResXResourceReader resxReader = new ResXResourceReader (resxFilePath))
            {
                foreach (DictionaryEntry entry in resxReader)
                {
                    if (entry.Key.ToString ().Equals (key))
                        return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds the resource.
        /// </summary>
        /// <returns><c>true</c>, if resource was added, <c>false</c> otherwise.</returns>
        /// <param name="resxFilePath">RESX file path.</param>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="overwrite">If set to <c>true</c> overwrite.</param>
        public static bool AddResource(string resxFilePath, string key, string value, bool overwrite = false)
        {
            if (!overwrite)
            {
                var duplicateResource = FindResourceByKey (resxFilePath, key);
                if (duplicateResource.HasValue)
                    return false;
            }

            Hashtable newData = new Hashtable ();
            newData.Add (key, value);

            return UpdateResourceFile (newData, resxFilePath);
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <returns>The class name.</returns>
        /// <param name="resxFilePath">RESX file path.</param>
        public static string GetClassName(string resxFilePath)
        {
            string designerCsPath = resxFilePath.Replace (".resx", ".Designer.cs");

            CSharpParser parser = new CSharpParser ();

            using (var stream = File.OpenText (designerCsPath))
            {
                var parsedFile = parser.Parse (stream, designerCsPath);

                if (parsedFile == null || parsedFile.Members.Count == 0)
                    return string.Empty;

                var namespaceDeclaration = parsedFile.Members.FirstOrDefault ((arg) => arg is NamespaceDeclaration) as NamespaceDeclaration;

                if (namespaceDeclaration == null)
                    return string.Empty;

                var typeDeclaration = namespaceDeclaration.Members.FirstOrDefault ((arg) => arg is TypeDeclaration) as TypeDeclaration;

                if (typeDeclaration == null)
                    return string.Empty;

                return typeDeclaration.Name;
            }
        }

        /// <summary>
        /// Updates the resource file.
        /// </summary>
        /// <returns><c>true</c>, if resource file was updated, <c>false</c> otherwise.</returns>
        /// <param name="data">Data.</param>
        /// <param name="path">Path.</param>
        private static bool UpdateResourceFile (Hashtable data, string path)
        {
            Hashtable resourceEntries = new Hashtable ();

            //Get existing resources
            ResXResourceReader reader = new ResXResourceReader (path);
            reader.UseResXDataNodes = true;
            ResXResourceWriter resourceWriter = new ResXResourceWriter (path);
            System.ComponentModel.Design.ITypeResolutionService typeres = null;
            if (reader != null)
            {
                IDictionaryEnumerator id = reader.GetEnumerator ();
                foreach (DictionaryEntry d in reader)
                {
                    //Read from file:
                    string val = "";
                    if (d.Value == null)
                        resourceEntries.Add (d.Key.ToString (), "");
                    else
                    {
                        val = ((ResXDataNode)d.Value).GetValue (typeres).ToString ();
                        resourceEntries.Add (d.Key.ToString (), val);
                    }

                    //Write (with read to keep xml file order)
                    ResXDataNode dataNode = (ResXDataNode)d.Value;

                    //resourceWriter.AddResource(d.Key.ToString(), val);
                    resourceWriter.AddResource (dataNode);

                }
                reader.Close ();
            }

            //Add new data (at the end of the file):
            Hashtable newRes = new Hashtable ();
            foreach (string key in data.Keys)
            {
                if (!resourceEntries.ContainsKey (key))
                {

                    string value = data [key].ToString ();
                    if (value == null) value = "";

                    resourceWriter.AddResource (key, value);
                }
            }

            //Write to file
            resourceWriter.Generate ();
            resourceWriter.Close ();

            return RegenerateDesignerCs (path);
        }

        /// <summary>
        /// Regenerates the designer cs.
        /// </summary>
        /// <returns><c>true</c>, if designer cs was regenerated, <c>false</c> otherwise.</returns>
        /// <param name="resxFilePath">RESX file path.</param>
        private static bool RegenerateDesignerCs(string resxFilePath)
        {
            string [] unmatchedElements;
            var codeProvider = new Microsoft.CSharp.CSharpCodeProvider ();
            string designerCsPath = resxFilePath.Replace (".resx", ".Designer.cs");

            CSharpParser parser = new CSharpParser ();
            string typeName = string.Empty;
            string namespaceName = string.Empty;

            using (var stream = File.OpenText (designerCsPath))
            {
                var parsedFile = parser.Parse (stream, designerCsPath);

                if (parsedFile == null || parsedFile.Members.Count == 0)
                    return false;

                var namespaceDeclaration = parsedFile.Members.FirstOrDefault ((arg) => arg is NamespaceDeclaration) as NamespaceDeclaration;

                if (namespaceDeclaration == null)
                    return false;

                var typeDeclaration = namespaceDeclaration.Members.FirstOrDefault ((arg) => arg is TypeDeclaration) as TypeDeclaration;

                if (typeDeclaration == null)
                    return false;

                typeName = typeDeclaration.Name;
                namespaceName = namespaceDeclaration.FullName;
            }
            try
            {
                CodeCompileUnit code =
                    System.Resources.Tools.StronglyTypedResourceBuilder.Create (
                        resxFilePath, typeName, namespaceName, codeProvider,
                        false, out unmatchedElements); // Needs System.Design.dll

                using (StreamWriter writer = new StreamWriter (designerCsPath, false,
                    System.Text.Encoding.UTF8))
                {
                    codeProvider.GenerateCodeFromCompileUnit (code, writer,
                        new System.CodeDom.Compiler.CodeGeneratorOptions ());
                }
                return unmatchedElements.Length == 0;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Enumerates the resx files.
        /// </summary>
        /// <returns>The resx files.</returns>
        public static List<ProjectFile> EnumerateResXFiles ()
        {
            List<ProjectFile> resxFiles = new List<ProjectFile> ();
            Solution solution = MonoDevelop.Ide.IdeApp.Workspace.Items [0] as Solution;
            var projects = solution.GetAllProjects ();
            foreach (var project in projects)
            {
                var files = project.Files.Where (file => file.Name.EndsWith ("resx", StringComparison.InvariantCultureIgnoreCase));
                resxFiles.AddRange (files);
            }

            return resxFiles;
        }

        /// <summary>
        /// Gets the existing entry by value.
        /// </summary>
        /// <returns>The existing entry by value.</returns>
        /// <param name="value">Value.</param>
        public static KeyValuePair<string, DictionaryEntry>? GetExistingEntryByValue(string value)
        {
            var files = EnumerateResXFiles ();
            var fileNames = files.Select (file => file.Name).ToArray ();

            DictionaryEntry? existingEntry = null;
            string fileName = string.Empty;
            foreach (var file in fileNames)
            {
                existingEntry = FindResourceByValue (file, value);
                if (existingEntry.HasValue)
                {
                    fileName = file;
                    break;
                }
            }

            if (existingEntry.HasValue)
                return new KeyValuePair<string, DictionaryEntry> (fileName, existingEntry.Value);

            return null;
        }
    }
}
