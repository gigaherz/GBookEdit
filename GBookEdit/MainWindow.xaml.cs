using Microsoft.Win32;
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
using System.Windows.Threading;
using System.Xml;

namespace GBookEdit.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(500) };

        private string? currentFileName;

        private bool modified;

        private bool Modified()
        {
            return modified; // TODO: maybe check if the document is different from the last saved version?
        }

        public MainWindow()
        {
            InitializeComponent();

            rtbDocument.FontSize = FlowToBook.DefaultFontSize;

            timer.Tick += UpdatePreview;
        }

        private void rtbDocument_TextChanged(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            timer.Start();
            modified = true;
        }

        private void UpdatePreview(object? sender, EventArgs e)
        {
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

        private void tglBold_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(TextElement.FontWeightProperty, 
                GetTriState(rtbDocument.Selection.GetPropertyValue(TextElement.FontWeightProperty), FontWeights.Bold) == true 
                        ? FontWeights.Normal
                        : FontWeights.Bold);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void tglItalics_Click(object sender, RoutedEventArgs e)
        {
            var sel = rtbDocument.Selection;
            sel.ApplyPropertyValue(TextElement.FontStyleProperty, 
                GetTriState(rtbDocument.Selection.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic) == true
                ? FontStyles.Normal 
                : FontStyles.Italic);
            rtbDocument_SelectionChanged(sender, e);
            rtbDocument_TextChanged(sender, e);
        }

        private void tglUnderline_Click(object sender, RoutedEventArgs e)
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
            rtbDocument?.Selection?.ApplyPropertyValue(TextElement.FontSizeProperty, Convert.ToDouble((cbFontSize.SelectedItem as ComboBoxItem)?.Content ?? FlowToBook.DefaultFontSize));
        }

        private void ddColor_Click(object sender, RoutedEventArgs e)
        {

        }

        private void rtbDocument_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // TODO FIXME: Switch to some other control, ToggleButtons are not a good match since they update the IsPressed before sending the click event
            //tglBold.IsChecked = GetTriState(rtbDocument.Selection.GetPropertyValue(TextElement.FontWeightProperty), FontWeights.Bold);
            //tglItalics.IsChecked = GetTriState(rtbDocument.Selection.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic);
            //tglUnderline.IsChecked = GetTriState(rtbDocument.Selection.GetPropertyValue(Inline.TextDecorationsProperty), TextDecorations.Underline);
            //tglStrikethrough.IsChecked = GetTriState(rtbDocument.Selection.GetPropertyValue(Inline.TextDecorationsProperty), TextDecorations.Strikethrough);
            var fontSize = rtbDocument.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            if (fontSize != DependencyProperty.UnsetValue)
                cbFontSize.Text = fontSize.ToString();
            else
                cbFontSize.Text = "";
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
            rtbDocument.Document.Blocks.Clear();
            currentFileName = null;
            modified = false;
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
                    var doc = BookToFlow.Load(reader, out var warnings, out var errors);
                    rtbDocument.Document = doc;

                    if (errors.Count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("The following errors were encountered while loading the file:");
                        foreach (var err in errors)
                        {
                            sb.AppendLine(err.ToString());
                        }
                        MessageBox.Show(sb.ToString(), "Errors", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (warnings.Count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("The following warnings were encountered while loading the file:");
                        foreach (var warn in warnings)
                        {
                            sb.AppendLine(warn.ToString());
                        }
                        MessageBox.Show(sb.ToString(), "Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                modified = false;
                currentFileName = dlg.FileName;
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
    }
}
