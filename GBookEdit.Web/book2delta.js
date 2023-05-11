class Style {
    #_parent;
    #_bold;
    #_italics;
    #_underline;
    #_strikethrough;
    #_fontSize;
    #_align;
    #_color;

    constructor(parent = null) {
        this.#_parent = parent;
        this.#_bold;
        this.#_italics;
        this.#_underline;
        this.#_strikethrough;
        this.#_fontSize;
        this.#_align;
        this.#_color;
    }

    get bold() { return (this.#_bold ?? this.#_parent?.bold) ?? false; }
    set bold(value) { this.#_bold = value; }


    get italics() { return (this.#_italics ?? this.#_parent?.italics) ?? false; }
    set italics(value) { this.#_italics = value; }

    get underline() { return (this.#_underline ?? this.#_parent?.underline) ?? false; }
    set underline(value) { this.#_underline = value; }

    get strikethrough() { return (this.#_strikethrough ?? this.#_parent?.strikethrough) ?? false; }
    set strikethrough(value) { this.#_strikethrough = value; }

    get fontSize() { return (this.#_fontSize ?? this.#_parent?.fontSize) ?? DEFAULT_FONT_SIZE; }
    set fontSize(value) { this.#_fontSize = value; }

    get align() { return (this.#_align ?? this.#_parent?.align) ?? "left"; }
    set align(value) { this.#_align = value; }

    get color() { return (this.#_color ?? this.#_parent?.color) ?? "black"; }
    set color(value) { this.#_color = value; }

    get parent() { return this.#_parent; }
}

const NODE_ELEMENT = 1;
const NODE_TEXT = 3;
const NODE_CDATA = 4;
const DEFAULT_FONT_SIZE = 20;

function loadBookFromXml(bookXml, warnings, errors)
{
    let deltas = {
        ops: []
    };

    const parser = new DOMParser();

    bookXml = bookXml.replace(/\>\s+\</g, '><');

    let document = parser.parseFromString(bookXml, "text/xml");

    let root = document.documentElement;
    if (root === null || root.nodeName !== "book") {
        errors.push("Invalid root element");
        return;
    }

    loadBook(deltas, root, "/" + root.nodeName, warnings, errors);

    return deltas;
}

function loadBook(deltas, root, xmlPath, warnings, errors)
{
    let baseStyle = new Style();

    if (root.hasAttribute("fontSize")) {
        baseStyle.FontSize = DEFAULT_FONT_SIZE * Number(root.getAttribute("fontSize"));
    }

    deltas.title = {
        title: root.getAttribute("title") ?? ""
    };

    let chapterCount = 0;
    for(let node of root.childNodes)
    {
        if (node.nodeType === NODE_TEXT || node.nodeType === NODE_CDATA) {
            errors.push("Text content found in unexpected location: " + xmlPath);
        }
        else if (node.nodeType === NODE_ELEMENT) {
            let element = node;
            switch (element.nodeName) {
                case "chapter":
                    if (chapterCount > 0) {
                        deltas.ops.push(createChapterBreak());
                    }
                    chapterCount++;
                    loadChapter(deltas, element, baseStyle, xmlPath + "/" + element.nodeName, warnings, errors);
                    break;
                default:
                    warnings.push("Tag '" + element.nodeName + "' is not recognized and will be ignored. At: " + xmlPath);
                    break;
            }
        }
        // else ignore
    }
}

function loadChapter(deltas, chapter, style, xmlPath, warnings,  errors)
{
    // TODO: actually support chapters

    //let block = new Section();

    let sectionCount = 0;
    for(let node of chapter.childNodes)
    {
        if (node.nodeType === NODE_TEXT || node.nodeType === NODE_TEXT) {
            errors.push("Text content found in unexpected location: " + xmlPath);
        }
        else if (node.nodeType === NODE_ELEMENT) {
            if (sectionCount > 0) {
                deltas.ops.push(createSectionBreak());
            }
            sectionCount++;

            let element = node;
            switch (element.nodeName) {
                case "page":
                    warnings.push("Legacy tag 'page' will be converted into a section. At: " + xmlPath);
                    loadSection(deltas, element, style, xmlPath + "/" + element.nodeName, warnings, errors);
                    break;
                case "section":
                    loadSection(deltas, element, style, xmlPath + "/" + element.nodeName, warnings, errors);
                    break;
                default:
                    warnings.push("Tag '" + element.nodeName + "' is not recognized and will be ignored. At: " + xmlPath);
                    break;
            }
        }
        // else ignore
    }

}

function loadSection(parent, section, style, xmlPath, warnings, errors)
{
    for(let node of section.childNodes)
    {
        if (node.nodeType === NODE_TEXT || node.nodeType === NODE_TEXT) {
            errors.push("Text content found in unexpected location: " + xmlPath);
        }
        else if (node.nodeType === NODE_ELEMENT) {
            let element = node;
            switch (element.nodeName) {
                case "p":
                case "title":
                    {
                        let style1 = getStyleFromAttributes(style, element);
                        loadParagraph(parent, element, style1, xmlPath + "/" + element.nodeName, warnings, errors);
                        break;
                    }
                default:
                    warnings.push("Tag '" + element.nodeName + "' is not recognized and will be ignored. At: " + xmlPath);
                    break;
            }
        }
        // else ignore
    }
}

function loadParagraph(deltas, paragraph, style, xmlPath, warnings, errors)
{
    let block = { insert: '\n' };
    applyStyle(block, style, true);

    // Apply title paragraph
    if (paragraph.nodeName !== "p")
        (block.attributes = (block.attributes || {})).header = 1;


    for(let node of paragraph.childNodes)
    {
        if (node.nodeType === NODE_TEXT || node.nodeType === NODE_TEXT) {
            let text = node.nodeValue; // Fixme: do I need to decode CDATA?
            loadSimpleSpan(deltas, style, text);
        }
        else if (node.nodeType === NODE_ELEMENT) {
            let element = node;
            switch (element.nodeName) {
                case "span":
                    {
                        let style1 = getStyleFromAttributes(style, element);
                        loadSpan(deltas, element, style1, xmlPath + "/" + element.nodeName, warnings, errors);
                        break;
                    }
                default:
                    warnings.push("Tag '" + element.nodeName + "' is not recognized and will be ignored. At: " + xmlPath);
                    break;
            }
        }
        // else ignore
    }

    deltas.ops.push(block);
}

function loadSpan(deltas, span, style, xmlPath, warnings, errors)
{
    for(let node of span.childNodes)
    {
        if (node.nodeType === NODE_TEXT || node.nodeType === NODE_TEXT) {
            let text = node.nodeValue; // Fixme: do I need to decode CDATA?
            loadSimpleSpan(deltas, style, text);
        }
        else if (node.nodeType === NODE_ELEMENT) {
            let element = node;
            switch (element.nodeName) {
                case "span":
                    {
                        let style1 = getStyleFromAttributes(style, element);
                        loadSpan(inlines, element, style1, xmlPath + "/" + element.nodeName, warnings, errors);
                        break;
                    }
                default:
                    warnings.push("Tag '" + element.nodeName + "' is not recognized and will be ignored. At: " + xmlPath);
                    break;
            }
        }
        // else ignore
    }
}

function loadSimpleSpan(deltas, style, text) {
    let run = { insert: text };
    applyStyle(run, style);
    deltas.ops.push(run);
}

function getStyleFromAttributes(parent, element)
{
    let style = new Style(parent);
    if (element.hasAttribute("bold"))
        style.bold = element.getAttribute("bold") === "true";
    if (element.hasAttribute("italics"))
        style.italics = element.getAttribute("italics") === "true";
    if (element.hasAttribute("underline"))
        style.underline = element.getAttribute("underline") === "true";
    if (element.hasAttribute("strikethrough"))
        style.strikethrough = element.getAttribute("strikethrough") === "true";
    if (element.hasAttribute("scale"))
        style.fontSize *= parseFloat(element.getAttribute("scale"));
    if (element.hasAttribute("align"))
        style.align = parseAlignment(element.getAttribute("align"));
    if (element.hasAttribute("color"))
        style.color = parseColor(element.getAttribute("color"));
    return style;
}

const alignments = ["left", "center", "right", "justify"];
function parseAlignment(v)
{
    if (!alignments.includes(v)) throw new Error("Invalid alignment: " + v);
    return v;
}

function parseColor(_color, requireHash = true)
{
    let color = _color;
    if (color.startsWith("#")) color = color.substring(1);
    else if (requireHash) throw new Error("Invalid color format: Color needs to startwith #. Color: " + color);
    if (color.length === 3) {
        let r = parseInt(color.substring(0, 1), 16) * 0x11;
        let g = parseInt(color.substring(1, 2), 16) * 0x11;
        let b = parseInt(color.substring(2, 3), 16) * 0x11;
        return isNaN(r) || isNaN(g) || isNaN(b) ? null : "rgb(" + r + "," + g + "," + b + ")";
    }
    if (color.length === 6) {
        let r = parseInt(color.substring(0, 2), 16);
        let g = parseInt(color.substring(2, 4), 16);
        let b = parseInt(color.substring(4, 6), 16);
        return isNaN(r) || isNaN(g) || isNaN(b) ? null : "rgb(" + r + "," + g + "," + b + ")";
    }
    if (color.length === 8) {
        let a = parseInt(color.substring(0, 2), 16);
        let r = parseInt(color.substring(2, 4), 16);
        let g = parseInt(color.substring(4, 6), 16);
        let b = parseInt(color.substring(6, 8), 16);
        return isNaN(a) || isNaN(r) || isNaN(g) || isNaN(b) ? null : "rgba(" + r + "," + g + "," + b + "," + (a/255) + ")";
    }
    throw new Error("Invalid color format: " + _color);
}

function applyStyle(op, style, paragraph = false)
{
    if (!paragraph) {
        if (style.bold)
            (op.attributes = (op.attributes || {})).bold = true;
        if (style.italics)
            (op.attributes = (op.attributes || {})).italic = true;
        if (style.underline)
            (op.attributes = (op.attributes || {})).underline = true;
        if (style.strikethrough)
            (op.attributes = (op.attributes || {})).strikethrough = true;
        if (style.color)
            (op.attributes = (op.attributes || {})).color = style.color;
        if (style.fontSize !== DEFAULT_FONT_SIZE)
            (op.attributes = (op.attributes || {})).size = style.fontSize + "pt";
    }
    else {
        if (style.align !== "left")
            (op.attributes = (op.attributes || {})).align = style.align;
    }
}

function createChapterBreak()
{
    /*return new BlockUIContainer
    {
        Tag = new ChapterBreakMarker(),
            Child = new BreakMarkerControl() { Header = "Chapter Break", HorizontalAlignment = HorizontalAlignment.Stretch }
    };*/
    return { insert: "\n[TODO: CHAPTER BREAK]\n" };
}

function createSectionBreak()
{
    /*return new BlockUIContainer
    {
        Tag = new SectionBreakMarker(),
            Child = new BreakMarkerControl() { Header = "Section Break", HorizontalAlignment = HorizontalAlignment.Stretch }
    };*/
    return { insert: "\n[TODO: SECTION BREAK]\n" };
}

window.loadBookFromXml = loadBookFromXml;