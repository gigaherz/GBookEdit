using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace GBookEdit.WPF
{
    public class FlowToBook
    {
        public static readonly TextDecorationCollection NoDecorations = new();
        public static readonly TextDecorationCollection UnderlineAndStrikethrough = new(TextDecorations.Underline.Concat(TextDecorations.Strikethrough));

        public static readonly float DefaultFontSize = 12.0f;

        public static XmlDocument ProcessDoc(FlowDocument fdoc, string title)
        {
            var doc = new XmlDocument();

            var root = doc.CreateElement("book");
            root.SetAttribute("title", title);

            doc.AppendChild(root);

            if (!Approximately(fdoc.FontSize, DefaultFontSize))
            {
                root.SetAttribute("fontSize", (fdoc.FontSize / DefaultFontSize).ToString());
            }

            var chapter = doc.CreateElement("chapter");
            root.AppendChild(chapter);

            var section = doc.CreateElement("section"); // TODO: support multiple actual <section>s
            chapter.AppendChild(section);


            foreach (var block in fdoc.Blocks)
            {
                var tag = ParseBlock(block, Wrap(fdoc), doc, fdoc, false);
                section.AppendChild(tag);
            }

            return doc;
        }

        private static XmlElement ParseBlock(Block block, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc, bool isTopLevel)
        {
            return block switch
            {
                Paragraph p => ParseParagraph(p, parent, doc, fdoc, isTopLevel),
                Section s => ParseSection(s, parent, doc, fdoc, isTopLevel),
                _ => throw new NotImplementedException("Do not know how to convert a Block of type " + block.GetType().Name),
            };
        }

        private static XmlElement ParseSection(Section s, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc, bool isTopLevel)
        {
            if (isTopLevel)
            {
                var section = doc.CreateElement("section");
                ApplyModifiedAttributes(section, Wrap(s), parent);

                foreach (var block in s.Blocks)
                {
                    var tag = ParseBlock(block, Wrap(s), doc, fdoc, false);
                    section.AppendChild(tag);
                }

                return section;
            }
            else if (s.Blocks.Count != 1 || !Approximately(s.FontSize, s.Blocks.FirstBlock.FontSize))
            {
                var section = doc.CreateElement("group");
                ApplyModifiedAttributes(section, Wrap(s), parent);

                foreach (var block in s.Blocks)
                {
                    var tag = ParseBlock(block, Wrap(s), doc, fdoc, false);
                    section.AppendChild(tag);
                }

                return section;
            }
            else
            {
                return ParseBlock(s.Blocks.FirstBlock, parent, doc, fdoc, isTopLevel);
            }
        }

        private static XmlElement ParseParagraph(Paragraph p, IFormatDescriber parent, XmlDocument doc, FlowDocument fdoc, bool isTopLevel)
        {
            var tag = doc.CreateElement("p");
            ApplyModifiedAttributes(tag, Wrap(p), parent);

            foreach (var inline in p.Inlines)
            {
                var child = ParseInline(inline, Wrap(p), doc, fdoc);
                tag.AppendChild(child);
            }

            if (isTopLevel)
            {
                var wrap = doc.CreateElement("section");
                wrap.AppendChild(tag);
                return wrap;
            }
            else
            {
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
                tag.SetAttribute("color", current.Color.ToString());
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

        private readonly record struct WrapFlowDocument(FlowDocument Document) : IFormatDescriber
        {
            public double FontSize => Document.FontSize;
            public FontFamily FontFamily => Document.FontFamily;
            public bool IsBold => Document.FontWeight >= FontWeights.Bold;
            public bool IsItalics => Document.FontStyle == FontStyles.Italic;
            public bool IsUnderline => false;
            public bool IsStrikethrough => false;
            public Color Color => (Document.Foreground is SolidColorBrush b ? b.Color : Colors.Black);
        }

        private readonly record struct WrapInline(Inline Element) : IFormatDescriber
        {
            public double FontSize => Element.FontSize;
            public FontFamily FontFamily => Element.FontFamily;
            public bool IsBold => Element.FontWeight >= FontWeights.Bold;
            public bool IsItalics => Element.FontStyle == FontStyles.Italic;
            public bool IsUnderline => Element.TextDecorations == TextDecorations.Underline || Element.TextDecorations == UnderlineAndStrikethrough;
            public bool IsStrikethrough => Element.TextDecorations == TextDecorations.Strikethrough || Element.TextDecorations == UnderlineAndStrikethrough;
            public Color Color => (Element.Foreground is SolidColorBrush b ? b.Color : Colors.Black);
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
        }
    }
}
