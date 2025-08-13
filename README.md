# markdown-to-pdf
Tool to convert markdown into pdf/fillable pdf.

## Features

- Converts Markdown to PDF using Markdig and iText.
- Advanced Markdown extensions are enabled, providing support for tables and other complex Markdown constructs.

## Page Breaks

Insert `<!-- {{pagebreak}} -->` in your Markdown to force subsequent content to begin on a new page in the generated PDF. For example:

```markdown
First page content.

<!-- {{pagebreak}} -->

Second page content.
```
