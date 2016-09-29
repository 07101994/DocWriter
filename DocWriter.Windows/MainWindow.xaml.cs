using Avalon.Windows.Dialogs;
using DocWriter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Xceed.Wpf.Toolkit;

using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

namespace DocWriter
{
    public partial class MainWindow : Window, IEditorWindow, IWebView
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public string WindowPath { get; private set; }

        public DocModel DocModel { get; private set; }

        public string RunJS(string name, params string[] args)
        {
            return webView.InvokeScript(name, args) as string;
        }

        DocNode currentObject;
        public DocNode CurrentObject
        {
            get { return currentObject; }
            set
            {
                if (currentObject != value)
                {
                    if (currentObject != null)
                    {
                        this.SaveCurrentObject();
                    }

                    currentObject = value;

                    SelectItem(currentObject);

                    var ihtml = currentObject as IHtmlRender;
                    if (ihtml != null)
                    {
                        string contents;
                        try
                        {
                            contents = ihtml.Render();
                        }
                        catch (Exception ex)
                        {
                            var text = System.Web.HttpUtility.HtmlEncode(ex.ToString());
                            contents = $"<body><p>Error Loading the contents for the new node<p>Exception:<p><pre>{text}</pre>";
                        }
                        webView.NavigateToString(contents);
                    }
                }
            }
        }

        public void UpdateStatus(string status)
        {
            statusLabel.Content = status;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // load the last opened docs
            var lastPath = Settings.Default.LastUsedPath;
            if (Directory.Exists(lastPath) && File.Exists(Path.Combine(lastPath, "index.xml")))
            {
                OpenDirectory(lastPath);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // save last selected node
            if (CurrentObject != null)
            {
                Settings.Default.LastExpandedNode = CurrentObject.ReferenceString;
                Settings.Default.Save();
            }

            CurrentObject = null;
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new FolderBrowserDialog
            {
                BrowseFiles = false,
                BrowseShares = true,
                Title = "Select an ECMA XML documentation directory:"
            };
            if (openDialog.ShowDialog(this) == true)
            {
                var docsDir = Path.Combine(openDialog.SelectedPath, "en");
                if (Directory.Exists(docsDir) && File.Exists(Path.Combine(docsDir, "index.xml")))
                {
                    // save the last opened docs
                    Settings.Default.LastUsedPath = docsDir;
                    Settings.Default.Save();

                    OpenDirectory(docsDir);
                }
                else
                {
                    MessageBox.Show(
                        "The selected directory is not the toplevel directory for ECMA XML documentation. Those should contain a subdirectory 'en' and a file 'en\\index.xml'",
                        "Not an ECMA XML Documentation Directory",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void OpenDirectory(string path)
        {
            WindowPath = path;

            DocModel = new DocModel(WindowPath);

            outline.ItemsSource = DocModel.Namespaces;
            Title = "DocWriter - " + WindowPath;

            // restore the last open node
            CurrentObject = DocModel.ParseReference(Settings.Default.LastExpandedNode);
        }

        private bool SelectItem(DocNode newNode)
        {
            var item = SelectItem(outline, newNode);
            if (item != null)
            {
                item.IsSelected = true;
            }
            return item != null;
        }

        public TreeViewItem SelectItem(ItemsControl container, DocNode newNode)
        {
            if (container == null)
            {
                return null;
            }

            var item = container.ItemContainerGenerator.ContainerFromItem(newNode) as TreeViewItem;
            if (item != null)
            {
                return item;
            }

            foreach (var i in container.Items)
            {
                item = container.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
                item = SelectItem(item, newNode);
                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        private void webView_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            switch (e.Uri.Scheme)
            {
                case "ecma":
                    var url = e.Uri.AbsolutePath.Substring(7);
                    CurrentObject = DocModel.ParseReference(url);
                    return;

                // This is one of our rendered ecma links, we want to extract the target
                // from the text, not the href attribute value (since this is not easily
                // editable, and the text is.
                case "goto":
                    url = RunJS("getText", e.Uri.Host);
                    CurrentObject = DocModel.ParseReference(url);
                    break;
            }
        }

        private void outline_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            CurrentObject = e.NewValue as DocNode;
        }

        //        [Export("insertImage:")]
        //        void insertImage(NSObject sender)
        //        {
        //            var nodePath = Window.CurrentNodePath;
        //            if (nodePath == null)
        //                return;
        //            var nodeImageDir = Path.Combine(nodePath, "_images");

        //            var dlg = NSOpenPanel.OpenPanel;
        //            dlg.CanChooseFiles = true;
        //            dlg.CanChooseDirectories = false;
        //            dlg.AllowsMultipleSelection = false;
        //            dlg.AllowedFileTypes = new string[] { "png", "jpg", "gif" };

        //            if (dlg.RunModal() != 1)
        //                return;

        //            if (dlg.Urls.Length == 0)
        //                return;

        //            var path = dlg.Urls.FirstOrDefault().Path;
        //            var target = Path.Combine(nodeImageDir, Path.GetFileName(path));

        //            if (File.Exists(target))
        //            {
        //                var alert = new NSAlert()
        //                {
        //                    MessageText = "Overwrite the existing image?",
        //                    InformativeText = "There is already a file with the same name in the images folder, do you want to overwrite, or automatically rename the file?",
        //                    AlertStyle = NSAlertStyle.Warning
        //                };
        //                alert.AddButton("Overwrite");
        //                alert.AddButton("Rename");
        //                var code = alert.RunModal();
        //                switch (code)
        //                {
        //                    case 1000: // Overwrite
        //                        break;
        //                    case 1001: // Rename
        //                        int i = 0;
        //                        do
        //                        {
        //                            target = Path.Combine(nodeImageDir, Path.GetFileNameWithoutExtension(path) + i + Path.GetExtension(path));
        //                            i++;
        //                        } while (File.Exists(target));
        //                        break;
        //                }
        //            }

        //            try
        //            {
        //                File.Copy(path, target);
        //            }
        //            catch (Exception e)
        //            {
        //                var a = new NSAlert()
        //                {
        //                    MessageText = "Failure to copy the file",
        //                    InformativeText = e.ToString(),
        //                    AlertStyle = NSAlertStyle.Critical
        //                };
        //                a.RunModal();
        //                return;
        //            }
        //            InsertHtml("<img src='{0}'>", target);
        //        }

        private void InsertUrlMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //string url = "http://www.xamarin.com";
            //string caption = "Xamarin";

            //var urlController = new InsertUrlController(this);
            //urlController.ShowWindow(this);
        }

        private void InsertReferenceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: show picker 

            this.InsertReference();
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e) => this.SaveCurrentObject();
    }
}
