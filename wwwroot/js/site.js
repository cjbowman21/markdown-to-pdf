// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
//
// Custom editor logic for inserting placeholders and toggling between
// WYSIWYG (Quill) and raw markdown modes.

window.addEventListener('DOMContentLoaded', function () {
    const editorContainer = document.getElementById('editor');
    if (!editorContainer) {
        return;
    }

    const placeholderDefs = [
        { type: 'checkbox', label: 'Checkbox', html: '<span class="placeholder" data-type="checkbox">☐</span>' },
        { type: 'textbox', label: 'Textbox', html: '<span class="placeholder" data-type="textbox">[__]</span>' }
    ];

    // Build toolbar
    const toolbar = document.getElementById('placeholderToolbar');
    placeholderDefs.forEach(ph => {
        const item = document.createElement('div');
        item.className = 'placeholder-item';
        item.textContent = ph.label;
        item.draggable = true;
        item.dataset.type = ph.type;
        item.addEventListener('dragstart', e => {
            e.dataTransfer.setData('text/plain', ph.type);
        });
        toolbar.appendChild(item);
    });

    const quill = new Quill('#editor', { theme: 'snow' });
    const quillEditor = editorContainer.querySelector('.ql-editor');

    function insertPlaceholder(type) {
        const def = placeholderDefs.find(p => p.type === type);
        if (!def) return;
        const range = quill.getSelection(true) || { index: quill.getLength(), length: 0 };
        quill.clipboard.dangerouslyPasteHTML(range.index, def.html);
        quill.setSelection(range.index + 1);
    }

    quillEditor.addEventListener('dragover', e => e.preventDefault());
    quillEditor.addEventListener('drop', e => {
        e.preventDefault();
        const type = e.dataTransfer.getData('text/plain');
        insertPlaceholder(type);
    });

    const rawInput = document.getElementById('rawInput');
    const hiddenInput = document.getElementById('markdownInput');
    const toggleBtn = document.getElementById('toggleView');
    let rawMode = false;

    function quillToRaw() {
        let html = quill.root.innerHTML;
        html = html.replace(/<span class="placeholder" data-type="(.*?)".*?>.*?<\/span>/g, '<!--{{$1}}-->');
        html = html.replace(/<p><br><\/p>/g, '\n');
        html = html.replace(/<p>/g, '').replace(/<\/p>/g, '\n');
        html = html.replace(/<br\s*\/?>/g, '\n');
        const div = document.createElement('div');
        div.innerHTML = html;
        const text = div.textContent;
        rawInput.value = text;
        hiddenInput.value = text;
        return text;
    }

    function rawToQuill(text) {
        let html = text.replace(/<!--\s*\{\{(.*?)\}\}\s*-->/g, (match, p1) => {
            const def = placeholderDefs.find(p => p.type === p1.trim());
            return def ? def.html : match;
        });
        html = html.split(/\n/).map(line => `<p>${line}</p>`).join('');
        quill.root.innerHTML = '';
        quill.clipboard.dangerouslyPasteHTML(html);
    }

    toggleBtn.addEventListener('click', () => {
        if (!rawMode) {
            quillToRaw();
            editorContainer.classList.add('d-none');
            rawInput.classList.remove('d-none');
            toggleBtn.textContent = 'Toggle WYSIWYG';
            rawMode = true;
        } else {
            rawToQuill(rawInput.value);
            rawInput.classList.add('d-none');
            editorContainer.classList.remove('d-none');
            toggleBtn.textContent = 'Toggle Raw';
            rawMode = false;
        }
    });

    document.querySelector('form').addEventListener('submit', () => {
        if (!rawMode) {
            quillToRaw();
        } else {
            hiddenInput.value = rawInput.value;
        }
    });

    rawToQuill(rawInput.value);
});
