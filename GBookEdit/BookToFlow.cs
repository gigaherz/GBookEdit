using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using static System.Collections.Specialized.BitVector32;
using Section = System.Windows.Documents.Section;

namespace GBookEdit.WPF
{
    internal class BookToFlow
    {
        internal static FlowDocument Load(XmlReader reader, out List<string> warnings, out List<string> errors)
        {
            warnings = new List<string>();
            errors = new List<string>();

            var document = new XmlDocument();
            document.Load(reader);

            var root = document.DocumentElement;
            if (root == null || root.Name != "book")
            {
                errors.Add("Invalid root element");
                return new FlowDocument();
            }

            return LoadBook(root, "/" + root.Name, warnings, errors);
        }

        private static FlowDocument LoadBook(XmlElement root, string xmlPath, List<string> warnings, List<string> errors)
        {
            var document = new FlowDocument();

            var baseStyle = new Style(null);

            if (root.HasAttribute("fontSize"))
            {
                baseStyle.FontSize = FlowToBook.DefaultFontSize * double.Parse(root.GetAttribute("fontSize"));
                document.FontSize = baseStyle.FontSize;
            }

            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    errors.Add("Text content found in unexpected location: " + xmlPath);
                }
                else if (node.NodeType == XmlNodeType.Element)
                {
                    var element = (XmlElement)node;
                    switch (element.Name)
                    {
                        case "chapter":
                            LoadChapter(document.Blocks, element, baseStyle, xmlPath + "/" + element.Name, warnings, errors);
                            break;
                        default:
                            warnings.Add("Tag '" + element.Name + "' is not recognized and will be ignored. At: " + xmlPath);
                            break;
                    }
                }
                // else ignore
            }

            return document;
        }

        private static void LoadChapter(BlockCollection parent, XmlElement chapter, Style style, string xmlPath, List<string> warnings, List<string> errors)
        {
            // TODO: actually support chapters

            //var block = new Section();

            foreach (XmlNode node in chapter.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    errors.Add("Text content found in unexpected location: " + xmlPath);
                }
                else if (node.NodeType == XmlNodeType.Element)
                {
                    var element = (XmlElement)node;
                    switch (element.Name)
                    {
                        case "page":
                            warnings.Add("Legacy tag 'page' will be converted into a section. At: " + xmlPath);
                            LoadSection(parent, element, style, xmlPath + "/" + element.Name, warnings, errors);
                            break;
                        case "section":
                            LoadSection(parent, element, style, xmlPath + "/" + element.Name, warnings, errors);
                            break;
                        default:
                            warnings.Add("Tag '" + element.Name + "' is not recognized and will be ignored. At: " + xmlPath);
                            break;
                    }
                }
                // else ignore
            }

        }

        private static void LoadSection(BlockCollection parent, XmlElement section, Style style, string xmlPath, List<string> warnings, List<string> errors)
        {
            var block = new Section();

            foreach (XmlNode node in section.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    errors.Add("Text content found in unexpected location: " + xmlPath);
                }
                else if (node.NodeType == XmlNodeType.Element)
                {
                    var element = (XmlElement)node;
                    switch (element.Name)
                    {
                        case "p":
                            LoadParagraph(block.Blocks, element, style, xmlPath + "/" + element.Name, warnings, errors);
                            break;
                        default:
                            warnings.Add("Tag '" + element.Name + "' is not recognized and will be ignored. At: " + xmlPath);
                            break;
                    }
                }
                // else ignore
            }

            parent.Add(block);
        }

        private static void LoadParagraph(BlockCollection parent, XmlElement section, Style style, string xmlPath, List<string> warnings, List<string> errors)
        {
            var block = new Paragraph();

            foreach (XmlNode node in section.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    var text = node.InnerText; // Fixme: do I need to decode CDATA?
                    block.Inlines.Add(new Run() { Text = text });
                }
                else if (node.NodeType == XmlNodeType.Element)
                {
                    var element = (XmlElement)node;
                    switch (element.Name)
                    {
                        case "span":
                        {
                            var style1 = GetStyleFromAttributes(style, element);
                            LoadSpan(block.Inlines, element, style1, xmlPath + "/" + element.Name, warnings, errors);
                            break;
                        }
                        default:
                            warnings.Add("Tag '" + element.Name + "' is not recognized and will be ignored. At: " + xmlPath);
                            break;
                    }
                }
                // else ignore
            }

            parent.Add(block);
        }

        private static void LoadSpan(InlineCollection inlines, XmlElement span, Style style, string xmlPath, List<string> warnings, List<string> errors)
        {
            foreach (XmlNode node in span.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    var text = node.InnerText; // Fixme: do I need to decode CDATA?
                    var run = new Run() { Text = text };
                    ApplyStyleToRun(run, style);
                    inlines.Add(run);
                }
                else if (node.NodeType == XmlNodeType.Element)
                {
                    var element = (XmlElement)node;
                    switch (element.Name)
                    {
                        case "span":
                        {
                            var style1 = GetStyleFromAttributes(style, element);
                            LoadSpan(inlines, element, style1, xmlPath + "/" + element.Name, warnings, errors);
                            break;
                        }
                        default:
                            warnings.Add("Tag '" + element.Name + "' is not recognized and will be ignored. At: " + xmlPath);
                            break;
                    }
                }
                // else ignore
            }
        }

        private static Style GetStyleFromAttributes(Style parent, XmlElement element)
        {
            var style = new Style(parent);
            if (element.HasAttribute("bold")) 
                style.Bold = element.GetAttribute("bold") == "true";
            if (element.HasAttribute("italics")) 
                style.Italics = element.GetAttribute("italics") == "true";
            if (element.HasAttribute("underline")) 
                style.Underline = element.GetAttribute("underline") == "true";
            if (element.HasAttribute("strikethrough")) 
                style.Strikethrough = element.GetAttribute("strikethrough") == "true";
            if (element.HasAttribute("scale")) 
                style.FontSize *= double.Parse(element.GetAttribute("scale"));
            return style;
        }

        private static void ApplyStyleToRun(Run run, Style style)
        {
            if (style.Bold)
            {
                run.FontWeight = FontWeights.Bold;
            }
            if (style.Italics)
            {
                run.FontStyle = FontStyles.Italic;
            }
            if (style.Underline)
            {
                run.TextDecorations.Add(TextDecorations.Underline);
            }
            if (style.Strikethrough)
            {
                run.TextDecorations.Add(TextDecorations.Strikethrough);
            }
            run.FontSize = style.FontSize;
        }

        private class Style
        {
            private readonly Style? _parent;

            private bool? _bold;
            private bool? _italics;
            private bool? _underline;
            private bool? _strikethrough;
            private double? _fontSize;

            public Style(Style? parent)
            {
                _parent = parent;
            }

            public bool Bold
            {
                get => (_bold ?? _parent?.Bold) ?? false;
                set => _bold = value;
            }
            public bool Italics
            {
                get => (_italics ?? _parent?.Italics) ?? false;
                set => _italics = value;
            }
            public bool Underline
            {
                get => (_underline ?? _parent?.Underline) ?? false;
                set => _underline = value;
            }
            public bool Strikethrough
            {
                get => (_strikethrough ?? _parent?.Strikethrough) ?? false;
                set => _strikethrough = value;
            }
            public double FontSize
            {
                get => (_fontSize ?? _parent?.FontSize) ?? FlowToBook.DefaultFontSize;
                set => _fontSize = value;
            }
        }
    }
}