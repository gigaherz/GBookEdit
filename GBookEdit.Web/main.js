document.addEventListener("DOMContentLoaded", function () {
    let timeout = null;

    const editor = document.getElementById("editor");

    //document.execCommand("defaultParagraphSeparator", false, "p");
    document.execCommand("styleWithCSS", false, true);

    editor.addEventListener("input", () => {
        onChangeEvent();
    });

    var previewPane = document.getElementById("preview");

    function onChangeEvent() {
        if (timeout != null) clearTimeout(timeout);
        timeout = setTimeout(changeCallback, 500);
    }

    function changeCallback() {
        if (timeout != null) clearTimeout(timeout);
        timeout = null;

        cleanupAfterChange();

        previewPane.innerText = editor.outerHTML;
    };

    document.getElementById("tbClearFormat").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            if (!isSelectionInsideEditor()) return;
            document.execCommand("removeFormat");
        }, 0);
    });
    document.getElementById("tbAlignLeft").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("justifyLeft");
        }, 0);
    });
    document.getElementById("tbAlignCenter").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("justifyCenter");
        }, 0);
    });
    document.getElementById("tbAlignRight").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("justifyRight");
        }, 0);
    });
    document.getElementById("tbAlignJustify").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("justifyFull");
        }, 0);
    });
    document.getElementById("tbBold").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("bold");
            onChangeEvent();
        }, 0);
    });
    document.getElementById("tbItalics").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("italic");
        }, 0);
    });
    document.getElementById("tbUnderline").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("underline");
        }, 0);
    });
    document.getElementById("tbStrikethrough").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("strikeThrough");
        }, 0);
    });
    document.getElementById("tbSizeDown").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            processSelection((span) => {
                span.style.fontSize = "0.5em";
            });
        }, 0);
    });
    document.getElementById("tbSizeUp").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            processSelection((span) => {
                span.style.fontSize = "2.0em";
            });
        }, 0);
    });
    document.getElementById("tbColor").addEventListener("click", () => {
        // TODO: Show color picker
        setTimeout(function () {
            editor.focus();
            document.execCommand("foreColor", false, "#FF0000");
        }, 0);
    });
    document.getElementById("tbUndo").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("undo");
        }, 0);
    });
    document.getElementById("tbRedo").addEventListener("click", () => {
        setTimeout(function () {
            editor.focus();
            document.execCommand("redo");
        }, 0);
    });

    function processSelection(spanConsumer) {
        let totalChanges = 0;
        let selection = window.getSelection();
        for (let i = 0; i < selection.rangeCount; i++) {
            let range = selection.getRangeAt(i);
            console.log(range.startContainer, range.startOffset, range.endContainer, range.endOffset);
            let changeCount = 0;
            if (range.collapsed) {

                continue;
            }
            let frag = range.cloneContents();
            range.deleteContents();
            if (frag.firstElementChild == null) {
                let span = wrapText(frag.textContent);
                spanConsumer(span);
                range.insertNode(span);
                changeCount++;
            } else {
                let e = frag.childNodes[0];
                while (e !== null) {
                    let node = e;
                    let next = e.nextSibling;
                    if (node.nodeType === 1 && node.nodeName === "SPAN") {
                        spanConsumer(node);
                        range.insertNode(node);
                        changeCount++;
                    } else if (node.nodeType === 3) {
                        let span = wrapText(node.textContent);
                        spanConsumer(span);
                        range.insertNode(span);
                        changeCount++;
                    } else {
                        // append as-is
                        range.insertNode(node);
                    }
                    e = next;
                }
            }
            if (changeCount > 0) {
                range = selection.getRangeAt(i);
                let startNode = range.startContainer;
                let startOffset = range.startOffset;
                let endNode = range.endContainer;
                let endOffset = range.endOffset;
                let parent = range.commonAncestorContainer;
                console.log("change parent", parent);
                let e = parent.firstChild;
                while (e !== null) {
                    let node = e;
                    let next = node.nextSibling;
                    if (next !== null && node.nodeType === 1 && next.nodeType === 1 && node.nodeName === "SPAN" && next.nodeName === "SPAN") {
                        if (compareStyleLists(node.style, next.style)) {
                            console.log("merge!", node, next);

                            let setStart = false;
                            let setEnd = false;

                            console.log("start", startNode, startNode === next);
                            if (startNode === next) {
                                setStart = true;
                                startNode = node;
                                startOffset = node.textContent.length + range.startOffset;
                            }

                            console.log("end", endNode, endNode === next);
                            if (endNode === next) {
                                setEnd = true;
                                endNode = node;
                                endOffset = node.textContent.length + range.startOffset;
                            }

                            let toRemove = next;
                            node.textContent += toRemove.textContent;
                            parent.removeChild(toRemove);
                            next = node; // repeat the same node again if merged

                            if (setStart)
                                range.setStart(startNode, startOffset);
                            if (setEnd)
                                range.setEnd(startNode, startOffset);
                        }
                    }
                    e = next;
                }
                totalChanges++;
            }
        }
        if (totalChanges > 0) {
            onChangeEvent();
        }
    }

    function compareStyleLists(style1, style2) {
        for (let prop of style1) {
            if (style1.getPropertyValue(prop) !== style2.getPropertyValue(prop))
                return false;
        }
        for (let prop of style2) {
            if (style1.getPropertyValue(prop) !== style2.getPropertyValue(prop))
                return false;
        }
        return true;
    }

    function wrapText(text) {
        let span = document.createElement("span");
        span.innerText = text;
        return span;
    }

    function isSelectionInsideEditor() {
        let selection = window.getSelection();
        for (let i = 0; i < selection.rangeCount; i++) {
            let range = selection.getRangeAt(i);
            if (range.collapsed)
                continue;
            var ancestor = range.commonAncestorContainer;
            if (!editor.contains(ancestor))
                return false;
        }
        return true;
    }

    function cleanupAfterChange() {
        if (editor.childNodes.length == 0)
            return;
        let e = editor.childNodes[0];
        while (e !== null) {
            let node = e;
            let next = node.nextSibling;
            if (node.nodeType === 3) {
                let span = document.createElement("span");
                span.textContent = node.textContent;
                editor.replaceChild(span, node);
                node = span;
            }
            // there isn't meant to be an else here
            if (node.nodeType === 1) {
                if (node.nodeName === "SPAN") {
                    let para = document.createElement("div");
                    editor.replaceChild(para, node);
                    para.appendChild(node);
                    node = para;
                }
                // there isn't meant to be an else here
                if (node.nodeName === "DIV") {
                    cleanupParagraph(node);
                }
                else {
                    console.warn("unexpected tag " + node.nodeName + " inside paragraph", node);
                }
            }
            e = next;
        }
    }

    function cleanupParagraph(para) {
        if (para.childNodes.length == 0)
            return;
        let e = para.childNodes[0];
        while (e !== null) {
            let node = e;
            let next = node.nextSibling;
            if (node.nodeType === 3) {
                let span = document.createElement("span");
                span.textContent = node.textContent;
                para.replaceChild(span, node);
                node = span;
            }
            // there isn't meant to be an else here
            if (node.nodeType === 1) {
                if (node.nodeName === "DIV") {
                    console.warn("unexpected div inside paragraph", node);
                }
                else if (node.nodeName !== "SPAN") {
                    console.warn("unexpected tag " + node.nodeName + " inside paragraph", node);
                }
            }
            e = next;
        }
    }

    editor.focus();

    document.title = "(untitled)* - GBookEdit v1.0.0";

});