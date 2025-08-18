document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('markdownInput');
    const preview = document.getElementById('preview');
    const previewScroll = preview;
    const editorPane = document.getElementById('editorPane');
    const previewPane = document.getElementById('previewPane');
    const editorTab = document.getElementById('editor-tab');
    const previewTab = document.getElementById('preview-tab');
    const htmlModeTab = document.getElementById('htmlModeTab');
    const pdfModeTab = document.getElementById('pdfModeTab');
    if (!input || !preview || !editorTab || !previewTab || !htmlModeTab || !pdfModeTab) {
        return;
    }
    let previewMode = 'html';
    let lines = [];
    let elementMap = new Map();
    let syncingFromInput = false;
    let syncingFromPreview = false;

    function splitParagraphLines() {
        preview.querySelectorAll('p').forEach(p => {
            if (p.querySelector('br')) {
                const fragment = document.createDocumentFragment();
                p.innerHTML.split(/<br\s*\/?\s*>/i).forEach(html => {
                    const span = document.createElement('span');
                    span.className = 'line-block';
                    span.innerHTML = html.trim();
                    fragment.appendChild(span);
                });
                p.replaceWith(fragment);
            }
        });
    }

    function updatePreview() {
        const raw = input.value;
        let processed;

        if (previewMode === 'pdf') {
            processed = raw
                .replace(/_{2,}\s*<!--\s*\{\{text:([^,}]+).*?\}\}\s*-->/g, (m, name) => `<input type="text" name="${name}" />`)
                .replace(/\[\s+]\s*<!--\s*\{\{check:([^,}]+).*?\}\}\s*-->/g, (m, name) => `<input type="checkbox" name="${name}" />`)
                .replace(/\(\s+\)\s*<!--\s*\{\{radio:([^,}]+),group=([^,}]+),value=([^,}]+).*?\}\}\s*-->/g,
                    (m, _name, group, value) => `<input type="radio" name="${group}" value="${value}" />`)
                .replace(/\s*<!--\s*\{\{\s*pagebreak\s*\}\}\s*-->\s*/gi,
                    '\n<div class="page-break"></div>\n')
                .replace(/<!--\s*\{\{.*?\}\}\s*-->/g, '');
        } else {
            processed = raw.replace(/<!--\s*\{\{.*?\}\}\s*-->/g, '');
        }

        lines = processed.split('\n');
        preview.innerHTML = marked.parse(processed);
        splitParagraphLines();
        elementMap.clear();
        const elements = Array.from(
            preview.querySelectorAll('li, p, pre, blockquote, h1, h2, h3, h4, h5, h6, tr, .line-block')
        ).filter(el => el.tagName === 'LI' || !el.closest('li'));
        let index = 0;
        for (let i = 0; i < lines.length && index < elements.length; i++) {
            const line = lines[i];
            if (/^\s*\|?(?:\s*:?-+:?\s*\|)+\s*:?-+:?\s*\|?\s*$/.test(line)) {
                continue;
            }
            const plain = line
                .replace(/<!--\s*\{\{.*?\}\}\s*-->/g, '')
                .replace(/\[(.*?)\]\(.*?\)/g, '$1')
                .replace(/[|*_`#>\[\]-]/g, '')
                .replace(/\s+/g, ' ')
                .trim();
            if (plain) {
                elementMap.set(i, elements[index++]);
            }
        }
    }

    function showEditor() {
        editorPane.classList.remove('d-none');
        previewPane.classList.add('d-none');
        previewPane.classList.remove('d-flex');
        editorTab.classList.add('active');
        previewTab.classList.remove('active');
    }

    function showPreview() {
        previewPane.classList.remove('d-none');
        previewPane.classList.add('d-flex');
        editorPane.classList.add('d-none');
        previewTab.classList.add('active');
        editorTab.classList.remove('active');
    }

    function syncScroll(source, target) {
        const percentage = source.scrollTop / (source.scrollHeight - source.clientHeight);
        target.scrollTop = percentage * (target.scrollHeight - target.clientHeight);
    }

    editorTab.addEventListener('click', showEditor);
    previewTab.addEventListener('click', showPreview);

    input.addEventListener('input', updatePreview);
    htmlModeTab.addEventListener('click', () => {
        previewMode = 'html';
        htmlModeTab.classList.add('active');
        pdfModeTab.classList.remove('active');
        updatePreview();
    });
    pdfModeTab.addEventListener('click', () => {
        previewMode = 'pdf';
        pdfModeTab.classList.add('active');
        htmlModeTab.classList.remove('active');
        updatePreview();
    });

    input.addEventListener('scroll', () => {
        if (syncingFromPreview) {
            syncingFromPreview = false;
            return;
        }
        syncingFromInput = true;
        syncScroll(input, previewScroll);
    });

    previewScroll.addEventListener('scroll', () => {
        if (syncingFromInput) {
            syncingFromInput = false;
            return;
        }
        syncingFromPreview = true;
        syncScroll(previewScroll, input);
    });

    function clearHighlight() {
        preview.querySelectorAll('.highlight').forEach(el => el.classList.remove('highlight'));
    }

    function highlightPreviewLine(line) {
        clearHighlight();
        const target = elementMap.get(line);
        if (target) {
            target.classList.add('highlight');
        }
    }

    function hoveredLine(e) {
        const rect = input.getBoundingClientRect();
        const lineHeight = parseInt(window.getComputedStyle(input).lineHeight);
        return Math.floor((e.clientY - rect.top + input.scrollTop) / lineHeight);
    }

    input.addEventListener('mousemove', (e) => {
        highlightPreviewLine(hoveredLine(e));
    });

    input.addEventListener('mouseleave', clearHighlight);

    updatePreview();
});
