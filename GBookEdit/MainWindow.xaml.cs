//#define SHOW_XAML_IN_PREVIEW

using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.RightsManagement;
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

        private string? currentFileName;
        private string titleSuffix;

        private bool modified;

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

            rtbDocument.InputBindings.Clear();
            KeyBinding keyBinding = new(ApplicationCommands.NotACommand, Key.U, ModifierKeys.Control);
            rtbDocument.InputBindings.Add(keyBinding);

            rtbDocument.Document.FontSize = FlowToBook.DefaultFontSize;
            rtbDocument.Document.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Minecraft");

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
            Title = (currentFileName != null ? System.IO.Path.GetFileName(currentFileName) : "(Untitled)") + (Modified() ? "*" : "") + " - " + titleSuffix;
        }

        private void rtbDocument_TextChanged(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            timer.Start();
            modified = true;
            UpdateTitle();
        }

        private void UpdatePreview(object? sender, EventArgs e)
        {
#if SHOW_XAML_IN_PREVIEW
            var fdoc = rtbDocument.Document;

            var range = new TextRange(fdoc.ContentStart, fdoc.ContentEnd);

            using (var ms = new MemoryStream())
            {

                range.Save(ms, DataFormats.Xaml, true);

                ms.Position = 0;

                using (var sr = XmlReader.Create(ms))
                {
                    var document = new XmlDocument();
                    document.Load(sr);
                    var sb = new StringBuilder();
                    using (var xw = XmlWriter.Create(sb, new XmlWriterSettings()
                    {
                        Indent = true,
                        IndentChars = "  ",
                        NewLineChars = Environment.NewLine,
                    }))
                    {
                        document.WriteTo(xw);
                    }
                    tbPreview.Text = sb.ToString();
                }

            }
#else
            var fdoc = rtbDocument.Document;
            var xml = FlowToBook.ProcessDoc(fdoc, currentFileName != null ? System.IO.Path.GetFileNameWithoutExtension(currentFileName) : "Untitled");
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, new XmlWriterSettings() {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
            }))
            {
                xml.WriteTo(xw);
            }
            tbPreview.Text = sb.ToString();
#endif
        }

        private void cbFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void cmdUnderline_Execute(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            var ranges = GetRunsInRange(sel).ToList();
            var existing = ranges.Aggregate(((bool?)null, false), (acc, e) => {
                var val = e.GetPropertyValue(Inline.TextDecorationsProperty);
                var hasUnderline = (val as TextDecorationCollection)?.Any(dec => dec.Location == TextDecorationLocation.Underline);
                if (!acc.Item2)
                    return (hasUnderline, true);
                return ((val == null || hasUnderline != acc.Item1 ? null : acc.Item1), true);
            });
            var set = !(existing.Item1 == true);
            foreach (var range in ranges)
            {
                var current = range.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection ?? FlowToBook.NoDecorations;
                TextDecorationCollection toApply;
                if (set == true)
                {
                    if (current.Any(dec => dec.Location == TextDecorationLocation.Underline))
                        continue;
                    toApply = current.Clone();
                    toApply.Add(TextDecorations.Underline);
                }
                else
                {
                    if (!current.TryRemove(TextDecorations.Underline, out toApply))
                        continue;
                }
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, toApply);
            }
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void cmdStrikethrough_Execute(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            var ranges = GetRunsInRange(sel).ToList();
            var existing = ranges.Aggregate(((bool?)null, false), (acc, e) => {
                var val = e.GetPropertyValue(Inline.TextDecorationsProperty);
                var hasStrikethrough = (val as TextDecorationCollection)?.Any(dec => dec.Location == TextDecorationLocation.Strikethrough);
                if (!acc.Item2)
                    return (hasStrikethrough, true);
                return ((val == null || hasStrikethrough != acc.Item1 ? null : acc.Item1), true);
            });
            var set = !(existing.Item1 == true);
            foreach (var range in ranges)
            {
                var current = range.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection ?? FlowToBook.NoDecorations;
                TextDecorationCollection toApply;
                if (set == true)
                {
                    if (current.Any(dec => dec.Location == TextDecorationLocation.Strikethrough))
                        continue;
                    toApply = current.Clone();
                    toApply.Add(TextDecorations.Strikethrough);
                }
                else
                {
                    if (!current.TryRemove(TextDecorations.Strikethrough, out toApply))
                        continue;
                }
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, toApply);
            }
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void cbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressFontSizeChange) return;
            if (rtbDocument == null)
                return;
            double fontSize = Convert.ToDouble((cbFontSize.SelectedItem as ComboBoxItem)?.Content ?? FlowToBook.DefaultFontSize);
            rtbDocument.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
        }

        private void cmdChooseColor_Execute(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            var ranges = GetRunsInRange(sel).ToList();
            var existing = ranges.Aggregate(((Color?)null, false), (acc, e) => {
                var val = e.GetPropertyValue(TextElement.ForegroundProperty);
                if (!acc.Item2)
                    return ((val as SolidColorBrush)?.Color, true);
                return ((val == null || val.Equals(acc.Item1) ? null : acc.Item1), true);
            });
            var color = existing.Item1;
            if (color == null)
                color = Colors.Black;
            var dialog = new ColorDialog() { Title = "Select Color", SelectedColor = color.Value, Owner = this, WindowStartupLocation=WindowStartupLocation.CenterOwner };
            dialog.Apply += ColorDialog_Apply;
            if (dialog.ShowDialog() == true)
            {
                ColorDialog_Apply(sender, new ColorEventArgs(e.RoutedEvent, dialog.SelectedColor));
            }
        }

        private void ColorDialog_Apply(object sender, ColorEventArgs e)
        {
            var selection = rtbDocument.Selection;
            selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(e.Color));
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private bool suppressFontSizeChange = false;
        private void rtbDocument_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;

            // Current font size
            suppressFontSizeChange = true;
            var fontSize = sel.GetPropertyValue(TextElement.FontSizeProperty);
            if (fontSize != DependencyProperty.UnsetValue)
                cbFontSize.Text = fontSize.ToString();
            else
                cbFontSize.Text = "";
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
                var item = cbParagraphType.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag as string == type);
                cbParagraphType.SelectedItem = item;
            }
            else
            {
                cbParagraphType.SelectedItem = null;
            }
        }

        private bool? GetTriState(object v, object value)
        {
            if (v == null || v == DependencyProperty.UnsetValue)
                return null;
            return v?.Equals(value);
        }

        private static IEnumerable<TextRange> GetRunsInRange(TextRange range)
        {
            var start = range.Start;
            var end = range.End;

            if (start == end || start.Parent == end.Parent)
            {
                yield return range;
                yield break;
            }

            TextElement? current = start.Parent as TextElement;
            while (start.CompareTo(end) < 0)
            {
                if (current is Inline i)
                {
                    if (i is Span s)
                    {
                        current = s.Inlines.Count > 0 ? s.Inlines.First() : null;
                        if (current != null) start = current.ContentStart;
                    }
                    else if (i is Run r)
                    {
                        bool reachedEnd = false;
                        var end2 = r.ContentEnd;
                        if (end2.CompareTo(end) > 0)
                        {
                            end2 = end;
                            reachedEnd = true;
                        }
                        var range1 = new TextRange(start, end2);
                        yield return range1;
                        if (reachedEnd)
                            break;
                        current = null;
                        start = range1.End;
                    }
                    else if (i is not LineBreak)
                    {
                        throw new NotImplementedException("Unimplemented inline type " + i.GetType().Name);
                    }
                    if (current == null)
                    {
                        current = i.NextInline ?? GoToNextOrParent(i);
                        if (current != null) start = current.ContentStart;
                    }
                }
                else if (current is ListItem l)
                {
                    current = l.Blocks.Count > 0 ? l.Blocks.First() : null;
                    if (current == null)
                    {
                        current = l.NextListItem ?? GoToNextOrParent(l);
                        if (current != null) start = current.ContentStart;
                    }
                }
                else if (current is Block b)
                {
                    if (b is Paragraph p2)
                    {
                        current = p2.Inlines.Count > 0 ? p2.Inlines.First() : null;
                    }
                    else if (b is List l2)
                    {
                        current = l2.ListItems.Count > 0 ? l2.ListItems.First() : null;
                    }
                    else if (b is Section s)
                    {
                        current = s.Blocks.Count > 0 ? s.Blocks.First() : null;
                    }
                    else throw new NotImplementedException("Unimplemented block type " + b.GetType().Name);
                    if (current == null)
                    {
                        current = b.NextBlock ?? GoToNextOrParent(b);
                        if (current != null) start = current.ContentStart;
                    }
                }
            }
        }

        private static TextElement? GoToNextOrParent(TextElement? current)
        {
            if (current is Inline i) 
                return i.NextInline ?? GoToNextOrParent(current.Parent as TextElement);
            else if (current is Block b)
                return b.NextBlock ?? GoToNextOrParent(current.Parent as TextElement);
            else if (current is ListItem l)
                return l.NextListItem ?? GoToNextOrParent(current.Parent as TextElement);
            else if (current is null)
                return null;
            else throw new NotImplementedException("Unimplemented parent type " + current.GetType().Name);
        }

        private void cmdNew_Execute(object sender, RoutedEventArgs e)
        {
            if (Modified())
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    cmdSave_Execute(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            rtbDocument.BeginChange();
            rtbDocument.Document.Blocks.Clear();
            rtbDocument.FontSize = FlowToBook.DefaultFontSize;
            rtbDocument.EndChange();
            currentFileName = null;
            modified = false;
            UpdateTitle();
        }

        private void cmdOpen_Execute(object sender, RoutedEventArgs e)
        {
            if (Modified())
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    cmdSave_Execute(sender, e);
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
                    //doc = (FlowDocument)XamlReader.Load(reader);
                    rtbDocument.BeginChange();
                    rtbDocument.Document.Blocks.Clear();
                    BookToFlow.Load(rtbDocument.Document, reader, out var warnings, out var errors);
                    rtbDocument.EndChange();

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
                            sb.AppendLine("And " + (errors.Count-limit) + " more...");
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
                            sb.AppendLine("And " + (warnings.Count-limit) + " more...");
                        }
                        MessageBox.Show(sb.ToString(), "Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                modified = false;
                currentFileName = dlg.FileName;
                UpdateTitle();
            }
        }

        private void cmdSave_Execute(object sender, RoutedEventArgs e)
        {
            if (currentFileName == null)
            {
                cmdSaveAs_Execute(sender, e);
                return;
            }

            var fdoc = rtbDocument.Document;
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

        private void cmdSaveAs_Execute(object sender, RoutedEventArgs e)
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
                cmdSave_Execute(sender, e);
            }
        }

        private void cmdExit_Execute(object sender, RoutedEventArgs e)
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
                    cmdSave_Execute(sender, new RoutedEventArgs());
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
        }

        private void cmdClearFormatting_Execute(object sender, RoutedEventArgs e)
        {
            rtbDocument.Selection.ClearAllProperties();
        }

        private void cmdPastePlain_Execute(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }
            rtbDocument.Selection.Text = "";
            var lines = Clipboard.GetText().Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string? line = lines[i];
                if (i > 0)
                {
                    rtbDocument.CaretPosition.InsertParagraphBreak();
                }
                rtbDocument.CaretPosition.InsertTextInRun(line);
            }
        }

        private void cmdInsertChapterBreak_Execute(object sender, RoutedEventArgs e)
        {
            var chapterBreak = BookToFlow.CreateChapterBreak();
            var ptr = GetOrCreateParagraphBreak(rtbDocument.CaretPosition);
            var para = ptr.Paragraph;
            var parent = para.Parent;
            if (parent is Section s)
            {
                s.Blocks.InsertBefore(para, chapterBreak);
            }
            if (parent is FlowDocument d)
            {
                d.Blocks.InsertBefore(para, chapterBreak);
            }
        }

        private void cmdInsertSectionBreak_Execute(object sender, RoutedEventArgs e)
        {
            var chapterBreak = BookToFlow.CreateSectionBreak();
            var ptr = GetOrCreateParagraphBreak(rtbDocument.CaretPosition);
            var para = ptr.Paragraph;
            var parent = para.Parent;
            if (parent is Section s)
            {
                s.Blocks.InsertBefore(para, chapterBreak);
            }
            if (parent is FlowDocument d)
            {
                d.Blocks.InsertBefore(para, chapterBreak);
            }
        }

        private TextPointer GetOrCreateParagraphBreak(TextPointer ptr)
        {
            DependencyObject parent = ptr.Parent;
            while (parent is not FlowDocument)
            {
                if (parent is Inline l)
                {
                    if (ptr.CompareTo(l.ContentEnd) < 0)
                    {
                        return ptr.InsertParagraphBreak();
                    }

                    if (l.NextInline != null)
                    {
                        return ptr.InsertParagraphBreak();
                    }

                    parent = l.Parent;
                }
                else if (parent is Paragraph b)
                {
                    return b.NextBlock.ContentStart;
                }
            }
            return ptr.InsertParagraphBreak();
        }

        private void cbParagraphType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rtbDocument == null) return;

            var sel = rtbDocument.Selection;
            if (sel.IsEmpty)
            {
                var para = sel.Start.Paragraph;
                if (para != null)
                {
                    var type = ((cbParagraphType.SelectedItem as ComboBoxItem)?.Tag as string) ?? "normal";
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
                    var type = ((cbParagraphType.SelectedItem as ComboBoxItem)?.Tag as string) ?? "normal";
                    para.Tag = new ParagraphTypeMarker(type);
                    para = para.NextBlock as Paragraph;
                }
            }
        }
    }
}
