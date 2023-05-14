using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GBookEdit.WPF
{
    public static class RichTextUtils
    {
        public static void ToggleUnderline(RichTextBox editor)
        {
            var sel = editor.Selection;
            var ranges = GetRunsInRange(sel).ToList();
            var existing = ranges.Aggregate(((bool?)null, false), (acc, e) =>
            {
                var val = e.GetPropertyValue(Inline.TextDecorationsProperty);
                var hasUnderline = (val as TextDecorationCollection)?.Any(dec => dec.Location == TextDecorationLocation.Underline);
                if (!acc.Item2)
                    return (hasUnderline, true);
                return ((val == null || hasUnderline != acc.Item1 ? null : acc.Item1), true);
            });
            editor.BeginChange();
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
            editor.EndChange();
        }

        public static void ToggleStrikethrough(RichTextBox editor)
        {
            var sel = editor.Selection;
            var ranges = GetRunsInRange(sel).ToList();
            var existing = ranges.Aggregate(((bool?)null, false), (acc, e) =>
            {
                var val = e.GetPropertyValue(Inline.TextDecorationsProperty);
                var hasStrikethrough = (val as TextDecorationCollection)?.Any(dec => dec.Location == TextDecorationLocation.Strikethrough);
                if (!acc.Item2)
                    return (hasStrikethrough, true);
                return ((val == null || hasStrikethrough != acc.Item1 ? null : acc.Item1), true);
            });
            editor.BeginChange();
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
            editor.EndChange();
        }

        public static void SetParagraphType(RichTextBox editor, string type)
        {
            if (editor == null) return;

            editor.BeginChange();
            var sel = editor.Selection;
            if (sel.IsEmpty)
            {
                var para = sel.Start.Paragraph;
                if (para != null)
                {
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
                    para.Tag = new ParagraphTypeMarker(type);
                    para = para.NextBlock as Paragraph;
                }
            }
            editor.EndChange();
        }

        private static IEnumerable<TextRange> GetRunsInRange(TextRange range)
        {
            var start = range.Start;
            var end = range.End;

            if (start == end || ReferenceEquals(start.Parent, end.Parent))
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
                        GoChildOrNext(s.Inlines, s, end, ref start, out current);
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
                        start = range1.End;
                        GoNext(i, end, ref start, out current);
                    }
                    else if (i is not LineBreak)
                    {
                        throw new NotImplementedException("Unimplemented inline type " + i.GetType().Name);
                    }
                }
                else if (current is ListItem l)
                {
                    GoChildOrNext(l.Blocks, l, end, ref start, out current);
                }
                else if (current is Block b)
                {
                    if (b is Paragraph p2)
                    {
                        GoChildOrNext(p2.Inlines, p2, end, ref start, out current);
                    }
                    else if (b is List l2)
                    {
                        GoChildOrNext(l2.ListItems, l2, end, ref start, out current);
                    }
                    else if (b is Section s)
                    {
                        GoChildOrNext(s.Blocks, s, end, ref start, out current);
                    }
                    else throw new NotImplementedException("Unimplemented block type " + b.GetType().Name);
                }
            }
        }

        private static void GoChildOrNext<T>(TextElementCollection<T> list, TextElement current, TextPointer end, ref TextPointer start, out TextElement? next)
            where T : TextElement
        {
            if (list.Count > 0)
            {
                next = list.First();
                start = next.ContentStart;
            }
            else
            {
                GoNext(current, end, ref start, out next);
            }
        }

        private static void GoNext(TextElement current, TextPointer end, ref TextPointer start, out TextElement? next)
        {
            if (start.CompareTo(end) < 0)
            {
                next = GoToNextOrParent(current);
                if (next == null) throw new InvalidOperationException("Could not find next element. Has the collection been modified?");
                start = next.ContentStart;
            }
            else
            {
                next = null;
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
                return null; // End of document
            else throw new NotImplementedException("Unimplemented parent type " + current.GetType().Name);
        }

        public static void PastePlain(RichTextBox editor)
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }
            editor.BeginChange();
            editor.Selection.Text = "";
            var lines = Clipboard.GetText().Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string? line = lines[i];
                if (i > 0)
                {
                    editor.CaretPosition.InsertParagraphBreak();
                }
                editor.CaretPosition.InsertTextInRun(line);
            }
            editor.EndChange();
        }

        public static void InsertChapterBreak(RichTextBox editor)
        {
            editor.BeginChange();
            var chapterBreak = BookToFlow.CreateChapterBreak();
            var ptr = GetOrCreateParagraphBreak(editor.CaretPosition);
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
            editor.EndChange();
        }

        public static void InsertSectionBreak(RichTextBox editor)
        {
            editor.BeginChange();
            var chapterBreak = BookToFlow.CreateSectionBreak();
            var ptr = GetOrCreateParagraphBreak(editor.CaretPosition);
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
            editor.EndChange();
        }

        private static TextPointer GetOrCreateParagraphBreak(TextPointer ptr)
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
    }
}
