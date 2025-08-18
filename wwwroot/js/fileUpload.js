document.addEventListener('DOMContentLoaded', () => {
    const fileInput = document.getElementById('fileInput');
    const fileProgress = document.getElementById('fileProgress');
    const progressBar = fileProgress ? fileProgress.querySelector('.progress-bar') : null;
    const fileError = document.getElementById('fileError');
    const markdownInput = document.getElementById('markdownInput');
    const markdownTab = document.getElementById('markdown-tab');
    const fileInfo = document.getElementById('fileInfo');
    const uploadedFileName = document.getElementById('uploadedFileName');
    const uploadedFileDetails = document.getElementById('uploadedFileDetails');
    const uploadLabel = document.getElementById('uploadLabel');

    if (!fileInput || !fileProgress || !progressBar || !fileError || !markdownInput || !markdownTab || !fileInfo || !uploadedFileName || !uploadedFileDetails || !uploadLabel) {
        return;
    }

    fileInput.addEventListener('change', async () => {
        const file = fileInput.files[0];
        if (!file) {
            return;
        }

        fileError.textContent = '';
        fileInfo.classList.add('d-none');
        fileProgress.classList.remove('d-none');
        progressBar.style.width = '0%';
        let progress = 0;
        const interval = setInterval(() => {
            progress = Math.min(progress + 10, 95);
            progressBar.style.width = progress + '%';
        }, 100);

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
                uploadedFileName.textContent = data.details.fileName;
                uploadedFileDetails.innerHTML = `
                    <li>Word count: ${data.details.wordCount}</li>
                    <li>Headings: ${data.details.headingCount}</li>
                    <li>List items: ${data.details.listItemCount}</li>
                    <li>Checkboxes: ${data.details.checkboxCount}</li>`;
                fileInfo.classList.remove('d-none');
                uploadLabel.textContent = 'Upload Different File';
                bootstrap.Tab.getOrCreateInstance(markdownTab).show();
            } else {
                fileError.textContent = data.error || 'Failed to parse file.';
            }
        } catch (err) {
            fileError.textContent = err.message;
        } finally {
            clearInterval(interval);
            progressBar.style.width = '100%';
            setTimeout(() => {
                fileProgress.classList.add('d-none');
            }, 200);
            fileInput.value = '';
        }
    });
});
