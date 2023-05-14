using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;

namespace GBookEdit.WPF
{
    public class FlowToBook
    {
        public static readonly TextDecorationCollection NoDecorations = new();
        public static readonly TextDecorationCollection UnderlineAndStrikethrough = new(TextDecorations.Underline.Concat(TextDecorations.Strikethrough));

        public static readonly float DefaultFontSize = 20.0f;

        public static XmlDocument ProcessDoc(FlowDocument fdoc, string defaultTitle)
        {
            var doc = new XmlDocument();

            var additionalProps = fdoc.Tag as AdditionalBookProperties;

            var root = doc.CreateElement("book");
            root.SetAttribute("title", additionalProps?.Title ?? defaultTitle);

            doc.AppendChild(root);

            if (!Approximately(fdoc.FontSize, DefaultFontSize))
            {
                root.SetAttribute("fontSize", (fdoc.FontSize / DefaultFontSize).ToString());
            }

            var chapter = doc.CreateElement("chapter");
            root.AppendChild(chapter);

            var section = doc.CreateElement("section");
            chapter.AppendChild(section);

            foreach (var block in fdoc.Blocks)
            {
                if (block is BlockUIContainer container)
                {
                    var ctag = block.Tag;
                    if (ctag is ChapterBreakMarker)
                    {
                        chapter = doc.CreateElement("chapter");
                        root.AppendChild(chapter);

                        section = doc.CreateElement("section");
                        chapter.AppendChild(section);
                    }
                    else if (ctag is SectionBreakMarker)
                    {
                        section = doc.CreateElement("section");
                        chapter.AppendChild(section);
                    }
                    continue;
                }

                var tag = ParseBlock(block, Wrap(fdoc), doc, fdoc);
                section.AppendChild(tag);
            }

            return doc;
        }

        private static XmlElement ParseBlock(Block block, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc)
        {
            return block switch
            {
                Paragraph p => ParseParagraph(p, parent, doc, fdoc),
                Section s => ParseSection(s, parent, doc, fdoc),
                _ => throw new NotImplementedException("Do not know how to convert a Block of type " + block.GetType().Name),
            };
        }

        private static XmlElement ParseSection(Section s, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc)
        {
            if (s.Blocks.Count == 1)
                return ParseBlock(s.Blocks.FirstBlock, Wrap(s), doc, fdoc);

            var section = doc.CreateElement("group");
            ApplyModifiedAttributes(section, Wrap(s), parent);

            foreach (var block in s.Blocks)
            {
                var tag = ParseBlock(block, Wrap(s), doc, fdoc);
                section.AppendChild(tag);
            }

            return section;
        }

        private static XmlElement ParseParagraph(Paragraph p, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc)
        {
            var type = (p.Tag as ParagraphTypeMarker)?.Type ?? "normal";
            
            // TODO: Prepare style defaults for title paragraphs
            
            var tag = doc.CreateElement(type == "normal" ? "p" : type);
            ApplyModifiedAttributes(tag, Wrap(p), parent);

            if (p.Inlines.Count == 1 && p.Inlines.FirstInline is Run r)
            {
                ApplyModifiedAttributes(tag, Wrap(r), parent);
                tag.AppendChild(doc.CreateTextNode(r.Text));
                return tag;
            }
            else
            {
                foreach (var inline in p.Inlines)
                {
                    var child = ParseInline(inline, Wrap(p), doc, fdoc);
                    tag.AppendChild(child);
                }

                return tag;
            }
        }

        private static XmlNode ParseInline(Inline inline, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc)
        {
            return inline switch
            {
                Run r => ParseRun(r, parent, doc, fdoc),
                Span s => ParseSpan(s, parent, doc, fdoc),
                LineBreak _ => doc.CreateElement("br"),
                _ => throw new NotImplementedException("Do not know how to convert an Inline of type " + inline.GetType().Name),
            };
        }

        private static XmlNode ParseRun(Run r, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc)
        {
            if (HasModifiedAttributes(Wrap(r), parent))
            {
                var tag = doc.CreateElement("span");
                ApplyModifiedAttributes(tag, Wrap(r), parent);

                tag.AppendChild(doc.CreateTextNode(r.Text));
                return tag;
            }
            return doc.CreateTextNode(r.Text);
        }

        private static XmlNode ParseSpan(Span s, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc)
        {
            var tag = doc.CreateElement("span");
            ApplyModifiedAttributes(tag, Wrap(s), parent);

            foreach (var inline in s.Inlines)
            {
                var child = ParseInline(inline, Wrap(s), doc, fdoc);
                tag.AppendChild(child);
            }

            return tag;
        }

        private static bool HasModifiedAttributes(IFormatDescriber current, IFormatDescriber parent)
        {
            if (!Approximately(current.FontSize, parent.FontSize))
                return true;
            if (current.IsBold != parent.IsBold)
                return true;
            if (current.IsItalics != parent.IsItalics)
                return true;
            if (current.IsUnderline != parent.IsUnderline)
                return true;
            if (current.IsStrikethrough != parent.IsStrikethrough)
                return true;
            if (current.Color != parent.Color)
                return true;
            if (current.Align != parent.Align)
                return true;
            return false;
        }

        private static void ApplyModifiedAttributes(XmlElement tag, IFormatDescriber current, IFormatDescriber parent)
        {
            if (!Approximately(current.FontSize, parent.FontSize))
            {
                tag.SetAttribute("scale", (current.FontSize / parent.FontSize).ToString());
            }
            if (current.IsBold != parent.IsBold)
            {
                tag.SetAttribute("bold", current.IsBold ? "true": "false");
            }
            if (current.IsItalics != parent.IsItalics)
            {
                tag.SetAttribute("italics", current.IsItalics ? "true" : "false");
            }
            if (current.IsUnderline != parent.IsUnderline)
            {
                tag.SetAttribute("underline", current.IsUnderline ? "true" : "false");
            }
            if (current.IsStrikethrough != parent.IsStrikethrough)
            {
                tag.SetAttribute("strikethrough", current.IsStrikethrough ? "true" : "false");
            }
            if (current.Color != parent.Color)
            {
                var a = current.Color.A;
                var r = current.Color.R;
                var g = current.Color.G;
                var b = current.Color.B;

                if (a == 255)
                {
                    tag.SetAttribute("color", $"#{r:X2}{g:X2}{b:X2}");
                }
                else
                {
                    tag.SetAttribute("color", $"#{a:X2}{r:X2}{g:X2}{b:X2}");
                }
            }
            if (current.Align != parent.Align)
            {
                tag.SetAttribute("align", current.Align.ToString().ToLowerInvariant());
            }
        }

        private static bool Approximately(double a, double b)
        {
            if (a == b) return true;
            var div = Math.Max(Math.Abs(a), Math.Abs(b));
            if (div == 0) return false;
            var diff = Math.Abs(a - b) / Math.Max(a, b);
            return diff < float.Epsilon;
        }

        private interface IFormatDescriber
        {
            double FontSize { get; }
            FontFamily FontFamily { get; }
            bool IsBold { get; }
            bool IsItalics { get; }
            bool IsUnderline { get; }
            bool IsStrikethrough { get; }
            Color Color { get; }
            TextAlignment Align { get; }
        }

        private static IFormatDescriber Wrap(FlowDocument doc)
        {
            return new WrapFlowDocument(doc);
        }

        private static IFormatDescriber Wrap(Inline i)
        {
            return new WrapInline(i);
        }

        private static IFormatDescriber Wrap(TextElement e)
        {
            if (e is Inline i)
                return Wrap(i);
            return new WrapTextElement(e);
        }

        private static IFormatDescriber Wrap(DependencyObject e)
        {
            if (e is Inline i)
                return Wrap(i);
            else if (e is TextElement te)
                return Wrap(te);
            else if (e is FlowDocument doc)
                return Wrap(doc);
            throw new ArgumentException("Object is not a FlowDocument, TextElement, or Inline. Cannot wrap " + e.GetType(), nameof(e));
        }

        private static bool HasUnderline(TextDecorationCollection textDecorations)
        {
            return textDecorations.Any(td => td.Location == TextDecorationLocation.Underline);
        }

        private static bool HasStrikethrough(TextDecorationCollection textDecorations)
        {
            return textDecorations.Any(td => td.Location == TextDecorationLocation.Strikethrough);
        }

        private readonly record struct WrapFlowDocument(FlowDocument Document) : IFormatDescriber
        {
            public double FontSize => Document.FontSize;
            public FontFamily FontFamily => Document.FontFamily;
            public bool IsBold => Document.FontWeight >= FontWeights.Bold;
            public bool IsItalics => Document.FontStyle == FontStyles.Italic;
            public bool IsUnderline => false;
            public bool IsStrikethrough => false;
            public Color Color => (Document.Foreground is SolidColorBrush b ? b.Color : Colors.Black);
            public TextAlignment Align => Document.TextAlignment;
        }

        private readonly record struct WrapInline(Inline Element) : IFormatDescriber
        {
            public double FontSize => Element.FontSize;
            public FontFamily FontFamily => Element.FontFamily;
            public bool IsBold => Element.FontWeight >= FontWeights.Bold;
            public bool IsItalics => Element.FontStyle == FontStyles.Italic;
            public bool IsUnderline => HasUnderline(Element.TextDecorations);
            public bool IsStrikethrough => HasStrikethrough(Element.TextDecorations);
            public Color Color => (Element.Foreground is SolidColorBrush b ? b.Color : Colors.Black);
            public TextAlignment Align => Wrap(Element.Parent).Align;
        }

        private readonly record struct WrapTextElement(TextElement Element) : IFormatDescriber
        {
            public double FontSize => Element.FontSize;
            public FontFamily FontFamily => Element.FontFamily;
            public bool IsBold => Element.FontWeight >= FontWeights.Bold;
            public bool IsItalics => Element.FontStyle == FontStyles.Italic;
            public bool IsUnderline => false;
            public bool IsStrikethrough => false;
            public Color Color => (Element.Foreground is SolidColorBrush b ? b.Color : Colors.Black);
            public TextAlignment Align => Element is Block block ? block.TextAlignment : Wrap(Element.Parent).Align;
        }
    }
}
