﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;

namespace GBookEdit.WPF
{
    internal class BookToFlow
    {
        internal static void Load(FlowDocument fdoc, XmlReader reader, out List<string> warnings, out List<string> errors)
        {
            warnings = new List<string>();
            errors = new List<string>();

            var document = new XmlDocument();
            document.Load(reader);

            var root = document.DocumentElement;
            if (root == null || root.Name != "book")
            {
                errors.Add("Invalid root element");
                return;
            }

            LoadBook(fdoc, root, "/" + root.Name, warnings, errors);
        }

        private static void LoadBook(FlowDocument document, XmlElement root, string xmlPath, List<string> warnings, List<string> errors)
        {
            var baseStyle = new Style(null);

            if (root.HasAttribute("fontSize"))
            {
                baseStyle.FontSize = FlowToBook.DefaultFontSize * double.Parse(root.GetAttribute("fontSize"));
            }

            document.FontSize = baseStyle.FontSize;

            document.Tag = new AdditionalBookProperties()
            {
                Title = root.GetAttribute("title") ?? ""
            };

            int chapterCount = 0;
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
                            if (chapterCount > 0)
                            {
                                document.Blocks.Add(CreateChapterBreak());
                            }
                            chapterCount++;
                            LoadChapter(document.Blocks, element, baseStyle, xmlPath + "/" + element.Name, warnings, errors);
                            break;
                        default:
                            warnings.Add("Tag '" + element.Name + "' is not recognized and will be ignored. At: " + xmlPath);
                            break;
                    }
                }
                // else ignore
            }
        }

        private static void LoadChapter(BlockCollection parent, XmlElement chapter, Style style, string xmlPath, List<string> warnings, List<string> errors)
        {
            // TODO: actually support chapters

            //var block = new Section();

            var sectionCount = 0;
            foreach (XmlNode node in chapter.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    errors.Add("Text content found in unexpected location: " + xmlPath);
                }
                else if (node.NodeType == XmlNodeType.Element)
                {
                    if (sectionCount > 0)
                    {
                        parent.Add(CreateSectionBreak());
                    }
                    sectionCount++;

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
            // TODO: actually support sections

            //var block = new Section();

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
                        case "title":
                        {
                            var style1 = GetStyleFromAttributes(style, element);
                            LoadParagraph(parent, element, style1, xmlPath + "/" + element.Name, warnings, errors);
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

        private static void LoadParagraph(BlockCollection parent, XmlElement paragraph, Style style, string xmlPath, List<string> warnings, List<string> errors)
        {
            var block = new Paragraph();

            block.Tag = new ParagraphTypeMarker(paragraph.Name == "p" ? "normal" : paragraph.Name);

            if (paragraph.Name == "title")
            {
                block.FontWeight = FontWeights.Bold;
            }

            ApplyStyle(block, style);

            foreach (XmlNode node in paragraph.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    var text = node.InnerText; // Fixme: do I need to decode CDATA?
                    var run = new Run() { Text = text };
                    block.Inlines.Add(run);
                    ApplyStyle(run, style);
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
                    inlines.Add(run);
                    ApplyStyle(run, style);
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
            if (element.HasAttribute("align"))
                style.Align = ParseAlignment(element.GetAttribute("align"));
            if (element.HasAttribute("color"))
                style.Color = ParseColor(element.GetAttribute("color"));
            return style;
        }

        private static TextAlignment ParseAlignment(string v)
        {
            return v switch
            {
                "left" => TextAlignment.Left,
                "center" => TextAlignment.Center,
                "right" => TextAlignment.Right,
                "justify" => TextAlignment.Justify,
                _ => throw new Exception("Invalid alignment: " + v),
            };
        }

        public static System.Windows.Media.Color ParseColor(string color, bool requireHash = true)
        {
            if (color.StartsWith("#")) color = color.Substring(1);
            else if (requireHash) throw new Exception("Invalid color format: Color needs to startwith #. Color: " + color);
            if (color.Length == 3)
            {
                var r = int.Parse(color.AsSpan(0, 1), System.Globalization.NumberStyles.HexNumber) * 0x11;
                var g = int.Parse(color.AsSpan(1, 1), System.Globalization.NumberStyles.HexNumber) * 0x11;
                var b = int.Parse(color.AsSpan(2, 1), System.Globalization.NumberStyles.HexNumber) * 0x11;
                return System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            if (color.Length == 6)
            {
                var r = int.Parse(color.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber);
                var g = int.Parse(color.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber);
                var b = int.Parse(color.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber);
                return System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            if (color.Length == 8)
            {
                var a = int.Parse(color.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber);
                var r = int.Parse(color.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber);
                var g = int.Parse(color.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber);
                var b = int.Parse(color.AsSpan(6, 2), System.Globalization.NumberStyles.HexNumber);
                return System.Windows.Media.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
            }
            throw new Exception("Invalid color format: " + color);
        }

        public static bool TryParseColor(string text, out System.Windows.Media.Color color, bool requireHash = true)
        {
            if (text.StartsWith("#")) text = text.Substring(1);
            else if (requireHash)
            {
                color = Colors.Black;
                return false;
            }
            if (text.Length == 3)
            {
                if (int.TryParse(text.AsSpan(0, 1), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                        int.TryParse(text.AsSpan(1, 1), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                        int.TryParse(text.AsSpan(2, 1), System.Globalization.NumberStyles.HexNumber, null, out var b))
                {
                    color = System.Windows.Media.Color.FromRgb((byte)(r & 0x11), (byte)(g & 0x11), (byte)(b & 0x11));
                    return true;
                }
            }
            if (text.Length == 6)
            {
                if (int.TryParse(text.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                        int.TryParse(text.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                        int.TryParse(text.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                {
                    color = System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
                    return true;
                }
            }
            if (text.Length == 8)
            {
                if (int.TryParse(text.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var a) &&
                        int.TryParse(text.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                        int.TryParse(text.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                        int.TryParse(text.AsSpan(6, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                {
                    color = System.Windows.Media.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                    return true;
                }
            }
            color = Colors.Black;
            return false;
        }

        private static void ApplyStyle(FlowDocument document, Style style)
        {
            var range = new TextRange(document.ContentStart, document.ContentEnd);
            ApplyStyle(style, range);
        }

        private static void ApplyStyle(TextElement element, Style style)
        {
            var range = new TextRange(element.ContentStart, element.ContentEnd);
            ApplyStyle(style, range);
        }

        private static void ApplyStyle(Style style, TextRange range)
        {
            range.ApplyPropertyValue(TextElement.FontWeightProperty, style.Bold ? FontWeights.Bold : FontWeights.Normal);
            range.ApplyPropertyValue(TextElement.FontStyleProperty, style.Italics ? FontStyles.Italic : FontStyles.Normal);
            range.ApplyPropertyValue(TextElement.FontSizeProperty, style.FontSize);
            range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush() { Color = style.Color });
            range.ApplyPropertyValue(Block.TextAlignmentProperty, style.Align);
            if (style.Underline && style.Strikethrough)
            {
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, FlowToBook.UnderlineAndStrikethrough);
            }
            else if (style.Underline)
            {
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
            }
            else if (style.Strikethrough)
            {
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
            }
            else
            {
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, FlowToBook.NoDecorations);
            }
        }

        internal static BlockUIContainer CreateChapterBreak()
        {
            return new BlockUIContainer
            {
                Tag = new ChapterBreakMarker(),
                Child = new BreakMarkerControl() { Header = "Chapter Break", HorizontalAlignment = HorizontalAlignment.Stretch }
            };
        }

        internal static BlockUIContainer CreateSectionBreak()
        {
            return new BlockUIContainer
            {
                Tag = new SectionBreakMarker(),
                Child = new BreakMarkerControl() { Header = "Section Break", HorizontalAlignment = HorizontalAlignment.Stretch }
            };
        }

        private class Style
        {
            private readonly Style? _parent;

            private bool? _bold;
            private bool? _italics;
            private bool? _underline;
            private bool? _strikethrough;
            private double? _fontSize;
            private TextAlignment? _align;
            private System.Windows.Media.Color? _color;

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
            public TextAlignment Align
            {
                get => (_align ?? _parent?.Align) ?? TextAlignment.Left;
                set => _align = value;
            }
            public System.Windows.Media.Color Color
            {
                get => (_color ?? _parent?.Color) ?? Colors.Black;
                set => _color = value;
            }

            public Style? Parent { get; }
        }
    }
}