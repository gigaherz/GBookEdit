document.addEventListener("DOMContentLoaded", function () {

    let timeout = null;
    let changeCallback = function () { };
    const previewPane = document.getElementById("preview");

    const editor = new Quill('#editor', {
        placeholder: "Type here to start writing your book...",
        formats: ["bold", "italic", "underline", "strike", "color", "size", "header", "align", "indent", "image"],
        modules: {
            toolbar: {
                container: "#toolbar",
            }
        }
    });

    const defaultAttributes = {
        align: "left",
        color: "black",
        size: "20pt",
        bold: false,
        italic: false,
        underline: false,
        strike: false
    };

    function accountFormat(fmt, attrs) {
        for (var key of editor.options.formats) {
            let value = attrs[key] || defaultAttributes[key];
            let previous = fmt[key];
            if (Array.isArray(previous)) {
                if (!previous.includes(value))
                    previous.push(value);
            }
            else if ((key in fmt) && previous !== value) {
                (fmt[key] = [previous]).push(value);
            } else {
                fmt[key] = value;
            }
        }
    }

    function findFormats(selection) {
        if (selection == null) selection = { index: 0, length: 0 };
        if (selection.length > 0) {
            const content = editor.getContents(selection.index, selection.length);
            const ops = content.ops;
            let fmt = {};
            for (let i = 0; i < ops.length; i++) {
                let op = ops[i];
                let attrs = op.attributes || {};
                accountFormat(fmt, attrs);
            }
            return fmt;
        }
        else {
            let fmt = {};
            let formats = editor.getFormat(selection);
            accountFormat(fmt, formats);
            return fmt;
        }
    }

    function findLineFormats(selection) {
        if (selection == null) selection = { index: 0, length: 0 };
        if (selection.length > 0) {
            const content = editor.getLines(selection.index, selection.length);
            let fmt = {};
            for (let i = 0; i < content.length; i++) {
                let blot = content[i];
                let attrs = blot.formats() || {};
                accountFormat(fmt, attrs);
            }
            return fmt;
        }
        else {
            let fmt = {};
            let formats = editor.getFormat(selection);
            accountFormat(fmt, formats);
            return fmt;
        }
    }

    const tbColor = document.getElementById("tbColor");
    const tbAlignLeft = document.getElementById("tbAlignLeft");
    const tbAlignCenter = document.getElementById("tbAlignCenter");
    const tbAlignRight = document.getElementById("tbAlignRight");
    const tbAlignJustify = document.getElementById("tbAlignJustify");

    editor.on("editor-change", (event, value, oldValue, source) => {
        if (timeout != null) clearTimeout(timeout);
        timeout = setTimeout(changeCallback, 500);

        const selection = editor.getSelection();

        // Inline-level formatting
        let fmt = findFormats(selection);
        if (!Array.isArray(fmt.color)) {
            tbColor.querySelector(".colorsquare").style.backgroundColor = fmt.color || "black";
        }
        else {
            tbColor.querySelector(".colorsquare").style.backgroundColor = 'rgba(0,0,0,0)';
        }

        // Paragraph-level formatting
        fmt = findLineFormats(selection);
        if (!Array.isArray(fmt.align)) {
            switch (fmt.align) {
                default:
                    tbAlignLeft.classList.add("tb-active");
                    tbAlignCenter.classList.remove("tb-active");
                    tbAlignRight.classList.remove("tb-active");
                    tbAlignJustify.classList.remove("tb-active");
                    break;
                case "center":
                    tbAlignLeft.classList.remove("tb-active");
                    tbAlignCenter.classList.add("tb-active");
                    tbAlignRight.classList.remove("tb-active");
                    tbAlignJustify.classList.remove("tb-active");
                    break;
                case "right":
                    tbAlignLeft.classList.remove("tb-active");
                    tbAlignCenter.classList.remove("tb-active");
                    tbAlignRight.classList.add("tb-active");
                    tbAlignJustify.classList.remove("tb-active");
                    break;
                case "justify":
                    tbAlignLeft.classList.remove("tb-active");
                    tbAlignCenter.classList.remove("tb-active");
                    tbAlignRight.classList.remove("tb-active");
                    tbAlignJustify.classList.add("tb-active");
                    break;
            }
        }
        else {
            tbAlignLeft.classList.remove("tb-active");
            tbAlignCenter.classList.remove("tb-active");
            tbAlignRight.classList.remove("tb-active");
            tbAlignJustify.classList.remove("tb-active");
        }
    });

    changeCallback = () => {
        if (timeout != null) clearTimeout(timeout);
        timeout = null;
        var contents = editor.getContents();
        previewPane.innerText = JSON.stringify(contents, null, 2);
    };

    tbAlignLeft.addEventListener("click", () => {
        const selection = editor.getSelection(true);
        editor.formatLine(selection.index, selection.length, "align", "", Quill.sources.API);
    });

    tbAlignCenter.addEventListener("click", () => {
        const selection = editor.getSelection(true);
        editor.formatLine(selection.index, selection.length, "align", "center", Quill.sources.API);
    });

    tbAlignRight.addEventListener("click", () => {
        const selection = editor.getSelection(true);
        editor.formatLine(selection.index, selection.length, "align", "right", Quill.sources.API);
    });


    tbAlignJustify.addEventListener("click", () => {
        const selection = editor.getSelection(true);
        editor.formatLine(selection.index, selection.length, "align", "justify", Quill.sources.API);
    });

    document.getElementById("tbColor").addEventListener("click", () => {
        const selection = editor.getSelection(true);
        editor.formatText(selection.index, selection.length, "color", "red", Quill.sources.API);
    });

    document.getElementById("tbSizeDown").addEventListener("click", () => {
        const selection = editor.getSelection(true);
        editor.formatText(selection.index, selection.length, "size", 5, Quill.sources.API);
    });

    const cbParagraphType = document.getElementById("cbParagraphType");
    cbParagraphType.addEventListener("change", () => {
        let type = cbParagraphType.value;
        const selection = editor.getSelection(true);
        editor.formatLine(selection.index, selection.length, "header", type == "title" ? 1 : 0, Quill.sources.API);
    });

    document.getElementById("tbUndo").addEventListener("click", () => {
        editor.history.undo();
    });

    document.getElementById("tbRedo").addEventListener("click", () => {
        editor.history.redo();
    });

    document.getElementById("tbOpen").addEventListener("click", () => {
        let warnings = [];
        let errors = [];

        var input = document.createElement('input');
        input.type = 'file';
        input.accept = 'text/xml';
        input.onchange = e => {

            // getting a hold of the file reference
            var file = e.target.files[0];

            // setting up the reader
            var reader = new FileReader();
            reader.readAsText(file, 'UTF-8');

            // here we tell the reader what to do when it's done reading...
            reader.onload = readerEvent => {
                var content = readerEvent.target.result; // this is the content!

                let deltas = loadBookFromXml(content, warnings, errors)
                editor.setContents(deltas);
                console.log(content);
            }

        };

        input.click();
    });

    editor.enable();
    editor.focus();

    document.title = "(untitled)* - GBookEdit v1.0.0";

});