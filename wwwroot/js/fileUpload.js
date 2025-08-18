document.addEventListener('DOMContentLoaded', () => {
    const fileInput = document.getElementById('fileInput');
    const fileLoading = document.getElementById('fileLoading');
    const fileError = document.getElementById('fileError');
    const markdownInput = document.getElementById('markdownInput');
    const markdownTab = document.getElementById('markdown-tab');

    if (!fileInput || !fileLoading || !fileError || !markdownInput || !markdownTab) {
        return;
    }

    fileInput.addEventListener('change', async () => {
        const file = fileInput.files[0];
        if (!file) {
            return;
        }

        fileError.textContent = '';
        fileLoading.classList.remove('d-none');
        const formData = new FormData();
        formData.append('upload', file);
        try {
            const response = await fetch('/Home/UploadFile', {
                method: 'POST',
                body: formData
            });
            const data = await response.json();
            if (response.ok) {
                markdownInput.value = data.markdown;
                markdownInput.dispatchEvent(new Event('input'));
                bootstrap.Tab.getOrCreateInstance(markdownTab).show();
            } else {
                fileError.textContent = data.error || 'Failed to parse file.';
            }
        } catch (err) {
            fileError.textContent = err.message;
        } finally {
            fileLoading.classList.add('d-none');
            fileInput.value = '';
        }
    });
});
