import { Pipe, PipeTransform } from '@angular/core';
import { marked } from 'marked';

/**
 * Renders the model's Markdown answer to HTML.
 *
 * The result is bound with [innerHTML], which Angular runs through its own
 * sanitizer in SecurityContext.HTML. Nothing here calls bypassSecurityTrustHtml,
 * so scripts, event-handler attributes and javascript: URLs are stripped even
 * though the content originates from a language model reading untrusted documents.
 */
@Pipe({ name: 'markdown', standalone: true })
export class MarkdownPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';

    // Neutralise tag delimiters before parsing. Markdown needs neither, so this costs
    // nothing, and it means raw HTML from the model - or from a poisoned document it is
    // quoting - renders as visible text instead of relying on the sanitizer downstream.
    const safe = value.replace(/</g, '&lt;').replace(/>/g, '&gt;');

    const html = marked.parse(safe, {
      gfm: true,
      // Models emit single newlines inside a paragraph and expect them to show.
      breaks: true,
      async: false
    }) as string;

    // Turn the "[1]" citation markers the prompt asks for into superscript refs
    // that line up with the numbered chips under the answer. Markdown links have
    // already become <a> tags by this point, so this cannot damage them.
    return html.replace(/\[(\d{1,2})\]/g, '<sup class="cite-ref">$1</sup>');
  }
}
