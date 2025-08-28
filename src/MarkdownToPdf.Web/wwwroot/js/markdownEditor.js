document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('markdownInput');
    const preview = document.getElementById('preview');
    const pdfPreview = document.getElementById('pdfPreview');
    const pdfStatus = document.getElementById('pdfStatus');
    const applyOverlayCheckbox = document.getElementById('applyOverlay');
    const letterheadInput = document.getElementById('letterheadPdf');
    const removeLetterheadBtn = document.getElementById('removeLetterhead');
    const offsetTopHidden = document.getElementById('offsetY');
    const offsetLeftHidden = document.getElementById('offsetX');
    const offsetTopDisplay = document.getElementById('offsetYDisplay');
    const offsetLeftDisplay = document.getElementById('offsetXDisplay');
    const offsetTopUnit = document.getElementById('offsetYUnit');
    const offsetLeftUnit = document.getElementById('offsetXUnit');
    const previewScroll = preview;
    const editorPane = document.getElementById('editorPane');
    const previewPane = document.getElementById('previewPane');
    const editorTab = document.getElementById('editor-tab');
    const previewTab = document.getElementById('preview-tab');
    const htmlModeTab = document.getElementById('htmlModeTab');
    const pdfModeTab = document.getElementById('pdfModeTab');
    const whyModeTab = document.getElementById('whyModeTab');
    const whyContent = document.getElementById('whyContent');
    if (!input || !preview || !editorTab || !previewTab || !htmlModeTab || !pdfModeTab || !whyModeTab || !whyContent) {
        return;
    }
    let previewMode = 'html';
    let pdfRefreshTimer = null;
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

    input.addEventListener('input', () => {
        updatePreview();
        if (previewMode === 'pdf') {
            if (pdfRefreshTimer) clearTimeout(pdfRefreshTimer);
            pdfRefreshTimer = setTimeout(() => {
                submitPdfPreview();
                pdfRefreshTimer = null;
            }, 800);
        }
    });
    htmlModeTab.addEventListener('click', () => {
        previewMode = 'html';
        if (pdfRefreshTimer) { clearTimeout(pdfRefreshTimer); pdfRefreshTimer = null; }
        htmlModeTab.classList.add('active');
        pdfModeTab.classList.remove('active');
        whyModeTab.classList.remove('active');
        whyContent.classList.add('d-none');
        preview.classList.remove('d-none');
        if (pdfPreview) pdfPreview.classList.add('d-none');
        if (pdfStatus) pdfStatus.classList.add('d-none');
        updatePreview();
    });
    pdfModeTab.addEventListener('click', () => {
        // Switch UI to PDF iframe view; do not re-render background unless the user selects a file
        previewMode = 'pdf';
        pdfModeTab.classList.add('active');
        htmlModeTab.classList.remove('active');
        whyModeTab.classList.remove('active');
        whyContent.classList.add('d-none');
        if (pdfPreview) pdfPreview.classList.remove('d-none');
        preview.classList.add('d-none');
        if (pdfStatus) {
            updatePdfStatus();
            pdfStatus.classList.remove('d-none');
        }
        // If no background is selected, render a plain PDF so the preview isn't empty
        if (!letterheadInput || !letterheadInput.files || letterheadInput.files.length === 0) {
            submitPdfPreview();
        }
    });
    whyModeTab.addEventListener('click', () => {
        if (pdfRefreshTimer) { clearTimeout(pdfRefreshTimer); pdfRefreshTimer = null; }
        whyModeTab.classList.add('active');
        htmlModeTab.classList.remove('active');
        pdfModeTab.classList.remove('active');
        preview.classList.add('d-none');
        whyContent.classList.remove('d-none');
        if (pdfPreview) pdfPreview.classList.add('d-none');
        if (pdfStatus) pdfStatus.classList.add('d-none');
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

    document.querySelectorAll('.copy-example').forEach(btn => {
        btn.addEventListener('click', () => {
            const targetId = btn.getAttribute('data-copy-target');
            const text = document.getElementById(targetId)?.innerText;
            if (text) {
                navigator.clipboard.writeText(text.trim());
                btn.textContent = 'Copied!';
                setTimeout(() => (btn.textContent = 'Copy'), 2000);
            }
        });
    });

    // Submit the form to the preview endpoint only when letterhead changes
    const form = document.getElementById('pdfForm');

    // Unit conversion helpers for offset inputs
    function toPoints(value, unit) {
        const v = parseFloat(value || '0');
        if (!isFinite(v)) return 0;
        switch ((unit || 'pt').toLowerCase()) {
            case 'in': return v * 72.0;
            case 'mm': return v * (72.0 / 25.4);
            default: return v; // pt
        }
    }
    function updateOffsetsHidden() {
        if (offsetTopHidden && offsetTopDisplay && offsetTopUnit) {
            offsetTopHidden.value = String(toPoints(offsetTopDisplay.value, offsetTopUnit.value));
        }
        if (offsetLeftHidden && offsetLeftDisplay && offsetLeftUnit) {
            offsetLeftHidden.value = String(toPoints(offsetLeftDisplay.value, offsetLeftUnit.value));
        }
    }
    function updatePdfStatus() {
        if (!pdfStatus) return;
        const fileName = (letterheadInput && letterheadInput.files && letterheadInput.files.length > 0)
            ? letterheadInput.files[0].name
            : 'None';
        const applied = (applyOverlayCheckbox && applyOverlayCheckbox.checked) ? 'On' : 'Off';
        pdfStatus.textContent = 'Background: ' + fileName + ' | Apply Background: ' + applied;
    }

    function submitPdfPreview() {
        if (!form || !pdfPreview) return;
        // Ensure hidden point values reflect UI units
        try { updateOffsetsHidden && updateOffsetsHidden(); } catch {}
        const originalAction = form.getAttribute('action');
        const originalTarget = form.getAttribute('target');
        // Target the iframe and hit the PreviewPdf action
        form.setAttribute('target', 'pdfPreview');
        // Build a preview URL based on current action
        try {
            const url = new URL(form.action, window.location.origin);
            url.pathname = url.pathname.replace(/GeneratePdf$/i, 'PreviewPdf');
            form.setAttribute('action', url.toString());
        } catch {
            form.setAttribute('action', '/Home/PreviewPdf');
        }
        form.submit();
        // Restore attributes immediately after submit
        if (originalAction) form.setAttribute('action', originalAction); else form.removeAttribute('action');
        if (originalTarget) form.setAttribute('target', originalTarget); else form.removeAttribute('target');
        updatePdfStatus();
    }

    if (letterheadInput) {
        letterheadInput.addEventListener('change', () => {
            // If a background file is selected, show PDF tab and refresh preview
            if (letterheadInput.files && letterheadInput.files.length > 0) {
                pdfModeTab.click();
                submitPdfPreview();
            }
            else {
                // No file selected; if user cleared via dialog, update status and preview
                pdfModeTab.click();
                submitPdfPreview();
            }
        });
    }
    if (applyOverlayCheckbox) {
        applyOverlayCheckbox.addEventListener('change', () => {
            // Always refresh the PDF preview when overlay toggle changes
            pdfModeTab.click();
            submitPdfPreview();
        });
    }
    function onOffsetChanged() {
        // Refresh preview when offsets change
        try { updateOffsetsHidden && updateOffsetsHidden(); } catch {}
        pdfModeTab.click();
        // debounce quickly to avoid double-submits on spinner clicks
        if (pdfRefreshTimer) clearTimeout(pdfRefreshTimer);
        pdfRefreshTimer = setTimeout(() => {
            submitPdfPreview();
            pdfRefreshTimer = null;
        }, 300);
    }
    if (offsetTopDisplay) {
        offsetTopDisplay.addEventListener('input', onOffsetChanged);
        offsetTopDisplay.addEventListener('change', onOffsetChanged);
    }
    if (offsetLeftDisplay) {
        offsetLeftDisplay.addEventListener('input', onOffsetChanged);
        offsetLeftDisplay.addEventListener('change', onOffsetChanged);
    }
    if (offsetTopUnit) offsetTopUnit.addEventListener('change', onOffsetChanged);
    if (offsetLeftUnit) offsetLeftUnit.addEventListener('change', onOffsetChanged);
    if (removeLetterheadBtn && letterheadInput) {
        removeLetterheadBtn.addEventListener('click', () => {
            // Clear the selected file and refresh preview without background
            letterheadInput.value = '';
            pdfModeTab.click();
            submitPdfPreview();
        });
    }

    updatePreview();
    try { updateOffsetsHidden && updateOffsetsHidden(); } catch {}
    updatePdfStatus();
});
