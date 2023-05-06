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

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            mnuNew_Click(sender, e);
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            mnuOpen_Click(sender, e);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            mnuSave_Click(sender, e);
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Undo();
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Redo();
        }

        private void btnCut_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Cut();
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Copy();
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Paste();
        }

        private void btnAlignLeft_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(Block.TextAlignmentProperty, TextAlignment.Left);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnAlignCenter_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(Block.TextAlignmentProperty, TextAlignment.Center);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnAlignRight_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(Block.TextAlignmentProperty, TextAlignment.Right);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnAlignJustified_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(Block.TextAlignmentProperty, TextAlignment.Justify);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnBold_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(TextElement.FontWeightProperty, 
                GetTriState(rtbDocument.Selection.GetPropertyValue(TextElement.FontWeightProperty), FontWeights.Bold) == true 
                        ? FontWeights.Normal
                        : FontWeights.Bold);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnItalics_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(TextElement.FontStyleProperty, 
                GetTriState(rtbDocument.Selection.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic) == true
                ? FontStyles.Normal 
                : FontStyles.Italic);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnUnderline_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            var existing = GetRunsInRange(sel).Aggregate(((bool?)null, false), (acc, e) => {
                var val = e.GetPropertyValue(Inline.TextDecorationsProperty);
                var hasUnderline = val == TextDecorations.Underline || val == FlowToBook.UnderlineAndStrikethrough;
                if (!acc.Item2)
                    return (hasUnderline, true);
                return ((val == null || hasUnderline != acc.Item1 ? null : acc.Item1), true);
            });
            var set = !(existing.Item1 == true);
            foreach (var range in GetRunsInRange(sel))
            {
                var current = range.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection ?? FlowToBook.NoDecorations;
                TextDecorationCollection toApply;
                if (set == true)
                {
                    if (current == TextDecorations.Underline)
                        continue;
                    if (current == TextDecorations.Strikethrough)
                        toApply = FlowToBook.UnderlineAndStrikethrough;
                    else
                        toApply = TextDecorations.Underline;
                }
                else
                {
                    if (current.Count == 0)
                        continue;
                    if (current == FlowToBook.UnderlineAndStrikethrough)
                        toApply = TextDecorations.Strikethrough;
                    else
                        toApply = FlowToBook.NoDecorations;
                }
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, toApply);
            }
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnStrikethrough_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            var existing = GetRunsInRange(sel).Aggregate(((bool?)null, false), (acc, e) => {
                var val = e.GetPropertyValue(Inline.TextDecorationsProperty);
                var hasStrikethrough = val == TextDecorations.Strikethrough || val == FlowToBook.UnderlineAndStrikethrough;
                if (!acc.Item2)
                    return (hasStrikethrough, true);
                return ((val == null || hasStrikethrough != acc.Item1 ? null : acc.Item1), true);
            });
            var set = !(existing.Item1 == true);
            foreach (var range in GetRunsInRange(sel))
            {
                var current = range.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection ?? FlowToBook.NoDecorations;
                TextDecorationCollection toApply;
                if (set == true)
                {
                    if (current == TextDecorations.Strikethrough)
                        continue;
                    if (current == TextDecorations.Underline)
                        toApply = FlowToBook.UnderlineAndStrikethrough;
                    else
                        toApply = TextDecorations.Strikethrough;
                }
                else
                {
                    if (current.Count == 0)
                        continue;
                    if (current == FlowToBook.UnderlineAndStrikethrough)
                        toApply = TextDecorations.Underline;
                    else
                        toApply = FlowToBook.NoDecorations;
                }
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, toApply);
            }
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnFontSmall_Click(object sender, RoutedEventArgs e)
        {
            var selection = rtbDocument.Selection;
            var fontSize = selection.GetPropertyValue(TextElement.FontSizeProperty);
            if (fontSize != DependencyProperty.UnsetValue)
            {
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, NextFontSizeDown(fontSize));
            }
            else
            {
                foreach (var range in GetRunsInRange(selection))
                {
                    fontSize = range.GetPropertyValue(TextElement.FontSizeProperty);
                    if (fontSize == DependencyProperty.UnsetValue) fontSize = FlowToBook.DefaultFontSize;
                    range.ApplyPropertyValue(TextElement.FontSizeProperty, NextFontSizeDown(fontSize));
                }
            }
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void btnFontBig_Click(object sender, RoutedEventArgs e)
        {
            var selection = rtbDocument.Selection;
            var fontSize = selection.GetPropertyValue(TextElement.FontSizeProperty);
            if (fontSize != DependencyProperty.UnsetValue)
            {
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, NextFontSizeUp(fontSize));
            }
            else
            {
                foreach (var range in GetRunsInRange(selection))
                {
                    fontSize = range.GetPropertyValue(TextElement.FontSizeProperty);
                    if (fontSize == DependencyProperty.UnsetValue) fontSize = FlowToBook.DefaultFontSize;
                    range.ApplyPropertyValue(TextElement.FontSizeProperty, NextFontSizeUp(fontSize));
                }
            }
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private static double NextFontSizeDown(object fontSize)
        {
            return Math.Max(1, Convert.ToDouble(fontSize ?? FlowToBook.DefaultFontSize) - 1);
        }

        private static double NextFontSizeUp(object fontSize)
        {
            return Math.Max(1, Convert.ToDouble(fontSize ?? FlowToBook.DefaultFontSize) + 1);
        }

        private void cbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressFontSizeChange) return;
            if (rtbDocument == null)
                return;
            double fontSize = Convert.ToDouble((cbFontSize.SelectedItem as ComboBoxItem)?.Content ?? FlowToBook.DefaultFontSize);
            rtbDocument.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
        }

        private void ddColor_Click(object sender, RoutedEventArgs e)
        {
            var selection = rtbDocument.Selection;
            var existing = GetRunsInRange(selection).Aggregate(((Color?)null, false), (acc, e) => {
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

            TextPointer? p = start;
            TextElement? e = (TextElement)start.Parent;
            while (e != end.Parent && e != null)
            {
                if (e is Inline i)
                {
                    if (i is Span s)
                    {
                        e = s.Inlines.Count > 0 ? s.Inlines.First() : null;
                        p = e?.ContentStart;
                    }
                    else if (i is Run r)
                    {
                        var end2 = r.ContentEnd;
                        var range1 = new TextRange(p, end2);
                        yield return range1;
                        e = null; p = null;
                    }
                    else if (i is not LineBreak)
                    {
                        throw new NotImplementedException("Unimplemented inline type " + i.GetType().Name);
                    }
                    if (e == null)
                    {
                        e = i.NextInline ?? GoToNextParent(i);
                        p = e?.ContentStart;
                    }
                }
                else if (e is ListItem l)
                {
                    e = l.Blocks.Count > 0 ? l.Blocks.First() : null;
                    if (e == null)
                    {
                        e = l.NextListItem ?? GoToNextParent(l);
                        p = e?.ContentStart;
                    }
                }
                else if (e is Block b)
                {
                    if (b is Paragraph p2)
                    {
                        e = p2.Inlines.Count > 0 ? p2.Inlines.First() : null;
                    }
                    else if (b is List l2)
                    {
                        e = l2.ListItems.Count > 0 ? l2.ListItems.First() : null;
                    }
                    else if (b is Section s)
                    {
                        e = s.Blocks.Count > 0 ? s.Blocks.First() : null;
                    }
                    else throw new NotImplementedException("Unimplemented block type " + b.GetType().Name);
                    if (e == null)
                    {
                        e = b.NextBlock ?? GoToNextParent(b);
                        p = e?.ContentStart;
                    }
                }
            }



        }

        private static TextElement GoToNextParent(TextElement t)
        {
            var e = t.Parent;
            if (e is Inline i) return i.NextInline;
            else if (e is Block b) return b.NextBlock;
            else if (e is ListItem l) return l.NextListItem;
            else if (e is null) return null;
            else throw new NotImplementedException("Unimplemented parent type " + e.GetType().Name);
        }

        private void mnuNew_Click(object sender, RoutedEventArgs e)
        {
            if (Modified())
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    mnuSave_Click(sender, e);
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

        private void mnuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (Modified())
            {
                var result = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    mnuSave_Click(sender, e);
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

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentFileName == null)
            {
                mnuSaveAs_Click(sender, e);
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

        private void mnuSaveAs_Click(object sender, RoutedEventArgs e)
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
                mnuSave_Click(sender, e);
            }
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
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
                    mnuSave_Click(sender, new RoutedEventArgs());
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
        }

        private void btnClearFormatting_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Selection.ClearAllProperties();
        }

        private void mnuCut_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Cut();
        }

        private void mnuCopy_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Copy();
        }

        private void mnuPaste_Click(object sender, RoutedEventArgs e)
        {
            rtbDocument.Paste();
        }

        private void btnChapterBreak_Click(object sender, RoutedEventArgs e)
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

        private void btnSectionBreak_Click(object sender, RoutedEventArgs e)
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
