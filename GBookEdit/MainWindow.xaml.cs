using Microsoft.Win32;
using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Xml;

namespace GBookEdit.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer previewTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

        public ObservableCollection<RecentFile> RecentFiles {get; } = new();

        private readonly string titleSuffix;
        private string? currentFileName;
        private bool suppressFontSizeChange = false;

        private bool Modified { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            var appname = Assembly.GetExecutingAssembly().GetName().Name?.ToString() ?? "GBookEdit";
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";
            titleSuffix = $" - {appname} {version}";

            Editor.InputBindings.Add(new KeyBinding(ApplicationCommands.NotACommand, Key.U, ModifierKeys.Control));

            Editor.Document.FontSize = FlowToBook.DefaultFontSize;
            Editor.Document.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Minecraft");

            foreach (FontFamily fontFamily in Fonts.GetFontFamilies(new Uri("pack://application:,,,/"), "./Fonts/"))
            {
                // Perform action.
                var t = fontFamily;
            }

            previewTimer.Tick += UpdatePreview;

            LoadRecents();

            JumpList appJumpList = App.AppJumpList;
            appJumpList.JumpItemsRejected += (s, e) =>
            {
                foreach (var item in e.RejectedItems.OfType<JumpPath>())
                {
                    RecentFiles.Remove(new RecentFile(item.Path));
                    SaveRecents();
                }
            };
            appJumpList.JumpItemsRemovedByUser += (s, e) =>
            {
                foreach(var item in e.RemovedItems.OfType<JumpPath>())
                {
                    RecentFiles.Remove(new RecentFile(item.Path));
                    SaveRecents();
                }
            };


            Modified = false;

            UpdateTitle();
        }

        private void LoadRecents()
        {
            var user = Registry.CurrentUser;
            using RegistryKey? software = user.OpenSubKey("Software"),
                    gbookedit = software?.OpenSubKey("GBookEdit"),
                    recents = gbookedit?.OpenSubKey("RecentList");
            if (recents != null)
            {;
                foreach (var (_, path) in recents.GetValueNames().Select(name => {
                    int? index = int.TryParse(name, out var n) ? n : null;
                    return (index, path: index != null ? recents.GetValue(name)?.ToString() : null);
                }).Where(kv => kv.index != null && !string.IsNullOrEmpty(kv.path) && File.Exists(kv.path))
                .OrderBy(kv => kv.index))
                {
                    RecentFiles.Add(new RecentFile(path!));
                }
            }
        }

        private void SaveRecents()
        {
            var user = Registry.CurrentUser;
            using RegistryKey software = user.CreateSubKey("Software"),
                    gbookedit = software.CreateSubKey("GBookEdit"),
                    recents = gbookedit.CreateSubKey("RecentList");
            var set = recents.GetValueNames().ToHashSet();
            for(int i=0;i<RecentFiles.Count;i++)
            {
                var path = RecentFiles[i].FullName;
                string key = i.ToString();
                recents.SetValue(key, path);
                set.Remove(key);
            }
            foreach(var key in set)
            {
                if (key != "")
                    recents.DeleteValue(key);
            }
        }

        private void MakeRecent(string fileName)
        {
            RecentFiles.Remove(new RecentFile(fileName));
            RecentFiles.Insert(0, new RecentFile(fileName));
            App.AddRecent(fileName);

            if (RecentFiles.Count > 10)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }

            SaveRecents();
        }

        private void UpdateTitle()
        {
            Title = (currentFileName != null ? Path.GetFileName(currentFileName) : "(Untitled)") + (Modified ? "*" : "") + titleSuffix;
        }

        private void UpdatePreview(object? sender, EventArgs e)
        {
            var fdoc = Editor.Document;
            var xml = FlowToBook.ProcessDoc(fdoc, currentFileName != null ? Path.GetFileNameWithoutExtension(currentFileName) : "Untitled");
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
            }))
            {
                xml.WriteTo(xw);
            }
            PreviewTextBox.Text = sb.ToString();
        }

        private void UpdateUIFromSelection()
        {
            var sel = Editor.Selection;

            // TODO: Paragraph alignment, Bold, Italic, Underline, Strikethrough, Font

            // Current font size
            suppressFontSizeChange = true;
            var fontSize = sel.GetPropertyValue(TextElement.FontSizeProperty);
            if (fontSize != DependencyProperty.UnsetValue)
                FontDropdownSize.Text = fontSize.ToString();
            else
                FontDropdownSize.Text = "";
            suppressFontSizeChange = false;

            // Current color
            var color = sel.GetPropertyValue(TextElement.ForegroundProperty);
            if (color == DependencyProperty.UnsetValue)
            {
                bColor.Background = new DrawingBrush()
                {
                    TileMode = TileMode.Tile,
                    Viewport = new Rect(0, 0, 4, 4),
                    ViewportUnits = BrushMappingMode.Absolute,
                    Drawing = new GeometryDrawing()
                    {
                        Geometry = Geometry.Parse("M0,0 H1 V1 H2 V2 H1 V1 H0Z"),
                        Brush = Brushes.DarkGray
                    }
                };
            }
            else
            {
                bColor.Background = (Brush)color;
            }

            // Current paragraph type
            var start = sel.Start;
            var end = sel.End;
            var para = start.Paragraph;
            object? paragraphType = null;
            while (para != null && para.ContentStart.CompareTo(end) < 0)
            {
                var type = (para.Tag as ParagraphTypeMarker)?.Type ?? "normal";
                if (paragraphType == null)
                    paragraphType = type;
                else
                    paragraphType = DependencyProperty.UnsetValue;
                para = para.NextBlock as Paragraph;
            }

            if (paragraphType != DependencyProperty.UnsetValue)
            {
                var type = paragraphType as string ?? "normal";
                var item = ParagraphTypeDropdown.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag as string == type);
                ParagraphTypeDropdown.SelectedItem = item;
            }
            else
            {
                ParagraphTypeDropdown.SelectedItem = null;
            }
        }

        private bool ShowSavePrompt()
        {
            if (Modified)
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveDocument(currentFileName);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        private void ShowOpen()
        {
            if (!ShowSavePrompt()) return;

            var dlg = new OpenFileDialog
            {
                Filter = "Guidebook files (*.xml)|*.xml|All files (*.*)|*.*",
                AddExtension = true,
                Multiselect = false,
                Title = "Open Guidebook"
            };
            if (dlg.ShowDialog() == true)
            {
                OpenDocument(dlg.FileName);
            }
        }

        private void ShowSaveAs()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Guidebook files (*.xml)|*.xml|All files (*.*)|*.*",
                AddExtension = true,
                Title = "Save Guidebook"
            };
            if (dlg.ShowDialog() == true)
            {
                SaveDocument(dlg.FileName);
            }
        }

        private void NewDocument()
        {
            if (!ShowSavePrompt()) return;
            Editor.BeginChange();
            Editor.Document.Blocks.Clear();
            Editor.FontSize = FlowToBook.DefaultFontSize;
            Editor.EndChange();
            currentFileName = null;
            Modified = false;
            UpdateTitle();
        }

        internal void OpenDocument(string openPath)
        {
            using (var reader = XmlReader.Create(openPath))
            {
                Editor.BeginChange();
                Editor.Document.Blocks.Clear();
                BookToFlow.Load(Editor.Document, reader, out var warnings, out var errors);
                Editor.EndChange();

                if (errors.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("The following errors were encountered while loading the file:");
                    var limit = Math.Min(errors.Count, 10);
                    for (int i = 0; i < limit; i++)
                    {
                        string? err = errors[i];
                        sb.AppendLine(err.ToString());
                    }
                    if (errors.Count > limit)
                    {
                        sb.AppendLine("And " + (errors.Count - limit) + " more...");
                    }
                    MessageBox.Show(sb.ToString(), "Errors", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (warnings.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("The following warnings were encountered while loading the file:");
                    var limit = Math.Min(warnings.Count, 10);
                    for (int i = 0; i < limit; i++)
                    {
                        string? warn = warnings[i];
                        sb.AppendLine(warn.ToString());
                    }
                    if (warnings.Count > limit)
                    {
                        sb.AppendLine("And " + (warnings.Count - limit) + " more...");
                    }
                    MessageBox.Show(sb.ToString(), "Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            Modified = false;
            currentFileName = openPath;
            MakeRecent(openPath);
            UpdateTitle();
        }

        private void SaveDocument(string? fileName)
        {
            if (fileName == null)
            {
                ShowSaveAs();
                return;
            }

            var fdoc = Editor.Document;
            var xml = FlowToBook.ProcessDoc(fdoc, Path.GetFileNameWithoutExtension(fileName));
            using (var xw = XmlWriter.Create(fileName, new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
            }))
            {
                xml.WriteTo(xw);
                Modified = false;

                if (currentFileName != fileName)
                {
                    currentFileName = fileName;
                    MakeRecent(fileName);
                }

                UpdateTitle();
            }
        }

        private void ShowChooseColor()
        {
            var sel = Editor.Selection;
            var existing1 = sel.GetPropertyValue(TextElement.ForegroundProperty);
            var color = (existing1 == DependencyProperty.UnsetValue ? null : (existing1 as SolidColorBrush)?.Color) ?? Colors.Black;
            var dialog = new ColorDialog() { Title = "Select Color", SelectedColor = color, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            dialog.Apply += (sender, args) => ColorDialog_Apply(args.Color);
            if (dialog.ShowDialog() == true)
            {
                ColorDialog_Apply(dialog.SelectedColor);
            }

            void ColorDialog_Apply(Color color)
            {
                var selection = Editor.Selection;
                selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            }
        }

        #region Change Events

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ShowSavePrompt())
                e.Cancel = true;
        }

        private void Editor_TextChanged(object sender, RoutedEventArgs e)
        {
            previewTimer.Stop();
            previewTimer.Start();
            Modified = true;
            UpdateTitle();
            UpdateUIFromSelection();
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateUIFromSelection();
        }

        private void FontDropdownSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressFontSizeChange) return;
            if (Editor == null)
                return;
            double fontSize = Convert.ToDouble((FontDropdownSize.SelectedItem as ComboBoxItem)?.Content ?? FlowToBook.DefaultFontSize);
            Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
        }

        private void FontDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO
        }

        private void ParagraphTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Editor == null)
                return;
            var type = ((ParagraphTypeDropdown.SelectedItem as ComboBoxItem)?.Tag as string) ?? "normal";
            RichTextUtils.SetParagraphType(Editor, type);
        }

        private void RegisterFileAssociation_Click(object sender, RoutedEventArgs e)
        {
            App.RegisterAssociation();
        }

        #endregion

        #region Command Handlers

        // File menu

        private void NewCommand_Execute(object sender, RoutedEventArgs e)
        {
            NewDocument();
        }

        private void OpenCommand_Execute(object sender, RoutedEventArgs e)
        {
            ShowOpen();
        }

        private void OpenRecent_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is RecentFile fi && !string.IsNullOrEmpty(fi.FullName);
        }

        private void OpenRecent_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is RecentFile fi)
            {
                OpenDocument(fi.FullName);
            }
        }

        private void SaveCommand_Execute(object sender, RoutedEventArgs e)
        {
            SaveDocument(currentFileName);
        }

        private void SaveAsCommand_Execute(object sender, RoutedEventArgs e)
        {
            ShowSaveAs();
        }

        private void ExitCommand_Execute(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Edit menu

        private void PastePlainCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.PastePlain(Editor);
        }

        // Insert menu

        private void InsertChapterBreakCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.InsertChapterBreak(Editor);
        }

        private void InsertSectionBreakCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.InsertSectionBreak(Editor);
        }

        // Format menu

        private void ToggleUnderlineCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.ToggleUnderline(Editor);
        }

        private void ToggleStrikethroughCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.ToggleStrikethrough(Editor);
        }

        private void ChooseColorCommand_Execute(object sender, RoutedEventArgs e)
        {
            ShowChooseColor();
        }

        private void ClearFormattingCommand_Execute(object sender, RoutedEventArgs e)
        {
            Editor.Selection.ClearAllProperties();
        }

        #endregion
    }
}
