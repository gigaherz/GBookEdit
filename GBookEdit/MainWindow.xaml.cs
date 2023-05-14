using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace GBookEdit.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };

        private readonly string titleSuffix;
        private string? currentFileName;
        private bool modified;
        private bool suppressFontSizeChange = false;

        private bool Modified()
        {
            return modified; // TODO: maybe check if the document is different from the last saved version?
        }

        public MainWindow()
        {
            InitializeComponent();

            var appname = Assembly.GetExecutingAssembly().GetName().Name?.ToString() ?? "GBookEdit";
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";
            titleSuffix = appname + " " + version;

            Editor.InputBindings.Clear();
            KeyBinding keyBinding = new(ApplicationCommands.NotACommand, Key.U, ModifierKeys.Control);
            Editor.InputBindings.Add(keyBinding);

            Editor.Document.FontSize = FlowToBook.DefaultFontSize;
            Editor.Document.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Minecraft");

            foreach (FontFamily fontFamily in Fonts.GetFontFamilies(new Uri("pack://application:,,,/"), "./Fonts/"))
            {
                // Perform action.
                var t = fontFamily;
            }

            timer.Tick += UpdatePreview;

            modified = false;

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Title = (currentFileName != null ? Path.GetFileName(currentFileName) : "(Untitled)") + (Modified() ? "*" : "") + " - " + titleSuffix;
        }

        private void Editor_TextChanged(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            timer.Start();
            modified = true;
            UpdateTitle();
            UpdateUIFromSelection();
        }

        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateUIFromSelection();
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

        private void FontDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO
        }

        private void ToggleUnderlineCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.ToggleUnderline(Editor);
        }

        private void ToggleStrikethroughCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.ToggleStrikethrough(Editor);
        }

        private void FontDropdownSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressFontSizeChange) return;
            if (Editor == null)
                return;
            double fontSize = Convert.ToDouble((FontDropdownSize.SelectedItem as ComboBoxItem)?.Content ?? FlowToBook.DefaultFontSize);
            Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
        }

        private void ChooseColorCommand_Execute(object sender, RoutedEventArgs e)
        {
            var sel = Editor.Selection;
            var existing1 = sel.GetPropertyValue(TextElement.ForegroundProperty);
            var color = (existing1 == DependencyProperty.UnsetValue ? null : (existing1 as SolidColorBrush)?.Color) ?? Colors.Black;
            var dialog = new ColorDialog() { Title = "Select Color", SelectedColor = color, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            dialog.Apply += ColorDialog_Apply;
            if (dialog.ShowDialog() == true)
            {
                ColorDialog_Apply(sender, new ColorEventArgs(e.RoutedEvent, dialog.SelectedColor));
            }

            void ColorDialog_Apply(object sender, ColorEventArgs e)
            {
                var selection = Editor.Selection;
                selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(e.Color));
            }
        }

        private void ClearFormattingCommand_Execute(object sender, RoutedEventArgs e)
        {
            Editor.Selection.ClearAllProperties();
        }

        private void PastePlainCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.PastePlain(Editor);
        }

        private void InsertChapterBreakCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.InsertChapterBreak(Editor);
        }

        private void InsertSectionBreakCommand_Execute(object sender, RoutedEventArgs e)
        {
            RichTextUtils.InsertSectionBreak(Editor);
        }

        private void ParagraphTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Editor == null) return;

            Editor.BeginChange();
            var sel = Editor.Selection;
            if (sel.IsEmpty)
            {
                var para = sel.Start.Paragraph;
                if (para != null)
                {
                    var type = ((ParagraphTypeDropdown.SelectedItem as ComboBoxItem)?.Tag as string) ?? "normal";
                    para.Tag = new ParagraphTypeMarker(type);
                }
            }
            else
            {
                var start = sel.Start;
                var end = sel.End;
                var para = start.Paragraph;
                while (para != null && para.ContentStart.CompareTo(end) < 0)
                {
                    var type = ((ParagraphTypeDropdown.SelectedItem as ComboBoxItem)?.Tag as string) ?? "normal";
                    para.Tag = new ParagraphTypeMarker(type);
                    para = para.NextBlock as Paragraph;
                }
            }
            Editor.EndChange();
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

        private void NewCommand_Execute(object sender, RoutedEventArgs e)
        {
            if (Modified())
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveCommand_Execute(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            Editor.BeginChange();
            Editor.Document.Blocks.Clear();
            Editor.FontSize = FlowToBook.DefaultFontSize;
            Editor.EndChange();
            currentFileName = null;
            modified = false;
            UpdateTitle();
        }

        private void OpenCommand_Execute(object sender, RoutedEventArgs e)
        {
            if (Modified())
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveCommand_Execute(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            var dlg = new OpenFileDialog
            {
                Filter = "Guidebook files (*.xml)|*.xml|All files (*.*)|*.*",
                AddExtension = true,
                Multiselect = false,
                Title = "Open Guidebook"
            };
            if (dlg.ShowDialog() == true)
            {
                using (var reader = XmlReader.Create(dlg.FileName))
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
                modified = false;
                currentFileName = dlg.FileName;
                UpdateTitle();
            }
        }

        private void SaveCommand_Execute(object sender, RoutedEventArgs e)
        {
            if (currentFileName == null)
            {
                SaveAsCommand_Execute(sender, e);
                return;
            }

            var fdoc = Editor.Document;
            var xml = FlowToBook.ProcessDoc(fdoc, System.IO.Path.GetFileNameWithoutExtension(currentFileName));
            using (var xw = XmlWriter.Create(currentFileName, new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
            }))
            {
                xml.WriteTo(xw);
                modified = false;
                UpdateTitle();
            }
        }

        private void SaveAsCommand_Execute(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Guidebook files (*.xml)|*.xml|All files (*.*)|*.*",
                AddExtension = true,
                Title = "Save Guidebook"
            };
            if (dlg.ShowDialog() == true)
            {
                currentFileName = dlg.FileName;
                SaveCommand_Execute(sender, e);
            }
        }

        private void ExitCommand_Execute(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Modified())
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveCommand_Execute(sender, new RoutedEventArgs());
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
        }
    }
}
