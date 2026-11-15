import {
  AfterViewChecked,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatMessageUI, Citation } from '../models';
import { MarkdownPipe } from '../markdown.pipe';
import { GenerativeAnswerComponent } from './generative-answer.component';

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, MarkdownPipe, GenerativeAnswerComponent],
  template: `
    <section class="chat">
      <div class="chat-bar">
        <div class="scope">
          <span class="scope-label">Scope</span>
          <span class="scope-value" [class.none]="!scopeName">{{ scopeName || 'No knowledge base' }}</span>
        </div>
        <button class="clear" *ngIf="messages.length" (click)="clear.emit()">Clear</button>
      </div>

      <div class="scroller" #scroller>
        <div class="column">
          <!-- Empty state -->
          <div class="welcome" *ngIf="messages.length === 0">
            <h2>Ask your documents</h2>
            <p>
              Answers are grounded strictly in what you have indexed, and every claim
              comes back with the source passage attached.
            </p>
            <div class="suggestions">
              <button *ngFor="let s of suggestions" (click)="useSuggestion(s)">{{ s }}</button>
            </div>
          </div>

          <!-- Turns -->
          <article *ngFor="let msg of messages; let i = index" class="turn" [class.user]="msg.role === 'user'">
            <ng-container *ngIf="msg.role === 'user'; else assistantTurn">
              <div class="user-bubble">{{ msg.content }}</div>
            </ng-container>

            <ng-template #assistantTurn>
              <div class="avatar" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"
                     stroke-linecap="round" stroke-linejoin="round">
                  <path d="M12 3 3 7.5v9L12 21l9-4.5v-9L12 3Z" />
                  <path d="m3 7.5 9 4.5 9-4.5M12 21v-9" />
                </svg>
              </div>
              <div class="answer">
                <div class="who">DigiRAG</div>

                <!-- Generative UI: the model chose these components. -->
                <app-generative-answer *ngIf="msg.blocks?.length"
                                       [blocks]="msg.blocks!"></app-generative-answer>

                <!-- Fallback for plain prose, e.g. a provider that ignored the block format. -->
                <div class="prose" *ngIf="!msg.blocks?.length && msg.content"
                     [class.streaming]="msg.isStreaming"
                     [innerHTML]="msg.content | markdown"></div>

                <div class="thinking"
                     *ngIf="!msg.blocks?.length && !msg.content && msg.isStreaming && !msg.errorText">
                  <span></span><span></span><span></span>
                </div>

                <div class="failure" *ngIf="msg.errorText">
                  <strong>Request failed</strong>
                  <span>{{ msg.errorText }}</span>
                </div>

                <!-- Hover actions, as ChatGPT puts under each reply. -->
                <div class="actions" *ngIf="!msg.isStreaming && (msg.blocks?.length || msg.content)">
                  <button (click)="copy(msg)" [title]="copiedIndex === i ? 'Copied' : 'Copy'">
                    <svg *ngIf="copiedIndex !== i" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                      <rect x="9" y="9" width="12" height="12" rx="2" /><path d="M5 15V5a2 2 0 0 1 2-2h10" />
                    </svg>
                    <svg *ngIf="copiedIndex === i" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="m20 6-11 11-5-5" />
                    </svg>
                  </button>
                  <button (click)="rate(i, true)" [class.on]="msg.rating === 'up'" title="Good answer">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"
                         stroke-linecap="round" stroke-linejoin="round">
                      <path d="M7 10v11H4a1 1 0 0 1-1-1v-9a1 1 0 0 1 1-1h3Zm0 0 4-7a2 2 0 0 1 3 2l-1 5h5a2 2 0 0 1 2 2.4l-1.4 7A2 2 0 0 1 16.6 21H7" />
                    </svg>
                  </button>
                  <button (click)="rate(i, false)" [class.on]="msg.rating === 'down'" title="Bad answer">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"
                         stroke-linecap="round" stroke-linejoin="round">
                      <path d="M17 14V3h3a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1h-3Zm0 0-4 7a2 2 0 0 1-3-2l1-5H6a2 2 0 0 1-2-2.4l1.4-7A2 2 0 0 1 7.4 3H17" />
                    </svg>
                  </button>
                </div>

                <div class="citations" *ngIf="msg.citations?.length">
                  <div class="citations-head">
                    Sources
                    <span class="cite-count">{{ msg.citations!.length }}</span>
                  </div>
                  <div class="chips">
                    <button *ngFor="let c of msg.citations; let i = index"
                            class="chip" (click)="openCitation.emit(c)"
                            [title]="c.documentName">
                      <span class="chip-index">{{ i + 1 }}</span>
                      <span class="chip-name">{{ c.documentName }}</span>
                      <span class="chip-page" *ngIf="c.pageNumber">p.{{ c.pageNumber }}</span>
                    </button>
                  </div>
                </div>
              </div>
            </ng-template>
          </article>
        </div>
      </div>

      <!-- Composer -->
      <div class="composer-wrap">
        <div class="composer">
          <textarea #input
                    [(ngModel)]="draft"
                    (keydown.enter)="onEnter($event)"
                    (input)="autoGrow(input)"
                    [disabled]="isGenerating"
                    rows="1"
                    placeholder="Ask a question about your documents…"></textarea>
          <button *ngIf="!isGenerating" class="send" (click)="submit()" [disabled]="!draft.trim()"
                  aria-label="Send message">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 19V5M5 12l7-7 7 7" />
            </svg>
          </button>
          <button *ngIf="isGenerating" class="send stop" (click)="stop.emit()"
                  aria-label="Stop generating" title="Stop generating">
            <span class="stop-dot"></span>
          </button>
        </div>
        <p class="hint">Enter to send · Shift + Enter for a new line</p>
      </div>
    </section>
  `,
  styles: [`
    .chat { display: flex; flex-direction: column; min-width: 0; height: 100%; background: var(--bg-base); }

    .chat-bar {
      display: flex; align-items: center; justify-content: space-between;
      height: 44px; padding: 0 18px; flex: none;
      border-bottom: 1px solid var(--border);
    }
    .scope { display: flex; align-items: center; gap: 8px; min-width: 0; }
    .scope-label {
      font-size: 10.5px; font-weight: 600; letter-spacing: 0.07em;
      text-transform: uppercase; color: var(--text-faint);
    }
    .scope-value {
      font-size: 12.5px; color: var(--text);
      background: var(--bg-input); border-radius: 20px; padding: 2px 10px;
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 320px;
    }
    .scope-value.none { color: var(--text-faint); }

    .clear {
      font-size: 12.5px; color: var(--text-faint);
      padding: 4px 10px; border-radius: var(--radius-sm); transition: background 0.15s, color 0.15s;
    }
    .clear:hover { background: var(--bg-hover); color: var(--text); }

    .scroller { flex: 1; overflow-y: auto; min-height: 0; }
    .column {
      max-width: var(--chat-max-width);
      margin: 0 auto;
      padding: 28px 20px 40px;
      display: flex; flex-direction: column; gap: 26px;
    }

    /* Empty state */
    .welcome { padding: 48px 0 8px; text-align: center; }
    .welcome h2 { font-size: 22px; font-weight: 650; letter-spacing: -0.02em; margin-bottom: 8px; }
    .welcome p { font-size: 13.5px; color: var(--text-muted); max-width: 440px; margin: 0 auto 22px; }
    .suggestions { display: flex; flex-wrap: wrap; gap: 8px; justify-content: center; }
    .suggestions button {
      font-size: 12.5px; color: var(--text-muted);
      background: var(--bg-raised); border: 1px solid var(--border);
      border-radius: 20px; padding: 7px 14px; transition: all 0.15s;
    }
    .suggestions button:hover { border-color: var(--accent); color: var(--text); background: var(--accent-soft); }

    /* Turns */
    .turn { display: flex; gap: 13px; }
    .turn.user { justify-content: flex-end; }

    .user-bubble {
      background: var(--accent); color: #fff;
      padding: 10px 15px; border-radius: 16px 16px 4px 16px;
      font-size: 14px; max-width: 78%;
      white-space: pre-wrap; overflow-wrap: anywhere;
    }

    .avatar {
      display: grid; place-items: center; flex: none;
      width: 28px; height: 28px; margin-top: 2px;
      border-radius: 8px; background: var(--bg-raised);
      border: 1px solid var(--border); color: var(--accent);
    }
    .avatar svg { width: 15px; height: 15px; }

    .answer { flex: 1; min-width: 0; }
    .who { font-size: 12px; font-weight: 600; color: var(--text-muted); margin-bottom: 5px; }

    /* Rendered Markdown. Angular sanitizes the bound HTML, so model output
       cannot introduce scripts or event handlers. */
    .prose {
      font-size: 14.5px; line-height: 1.68; color: var(--text);
      overflow-wrap: anywhere;
    }

    /* Markdown arrives through innerHTML, so these elements never receive this
       component's style-scoping attribute. ::ng-deep pierces that, and the :host
       prefix keeps the rules from leaking out of the chat panel. */
    :host ::ng-deep .prose > :first-child { margin-top: 0; }
    :host ::ng-deep .prose > :last-child { margin-bottom: 0; }
    :host ::ng-deep .prose :is(p, ul, ol, pre, table, blockquote) { margin: 0 0 12px; }

    :host ::ng-deep .prose :is(h1, h2, h3, h4) {
      font-size: 15px; font-weight: 650; letter-spacing: -0.01em;
      margin: 18px 0 8px; color: var(--text);
    }
    :host ::ng-deep .prose h1 { font-size: 17px; }
    :host ::ng-deep .prose h2 { font-size: 16px; }

    :host ::ng-deep .prose strong { font-weight: 650; color: #fff; }
    :host ::ng-deep .prose em { font-style: italic; color: var(--text-muted); }

    :host ::ng-deep .prose :is(ul, ol) { padding-left: 22px; }
    :host ::ng-deep .prose li { margin: 3px 0; }
    :host ::ng-deep .prose li::marker { color: var(--text-faint); }
    :host ::ng-deep .prose li > :is(ul, ol) { margin: 4px 0 2px; }

    :host ::ng-deep .prose a { color: var(--accent); text-decoration: underline; text-underline-offset: 2px; }

    :host ::ng-deep .prose code {
      font-family: ui-monospace, 'SF Mono', Menlo, Consolas, monospace;
      font-size: 0.88em;
      background: var(--bg-input); border: 1px solid var(--border);
      border-radius: 4px; padding: 1px 5px;
    }
    :host ::ng-deep .prose pre {
      background: var(--bg-input); border: 1px solid var(--border);
      border-radius: var(--radius-sm); padding: 12px 14px; overflow-x: auto;
    }
    :host ::ng-deep .prose pre code { background: none; border: none; padding: 0; font-size: 12.5px; line-height: 1.6; }

    :host ::ng-deep .prose blockquote {
      border-left: 2px solid var(--border-strong);
      padding-left: 12px; color: var(--text-muted);
    }

    :host ::ng-deep .prose table { border-collapse: collapse; width: 100%; font-size: 13px; display: block; overflow-x: auto; }
    :host ::ng-deep .prose :is(th, td) { border: 1px solid var(--border); padding: 6px 10px; text-align: left; }
    :host ::ng-deep .prose th { background: var(--bg-input); font-weight: 600; }

    :host ::ng-deep .prose hr { border: none; border-top: 1px solid var(--border); margin: 16px 0; }

    /* Superscript refs matching the numbered citation chips. */
    :host ::ng-deep .prose .cite-ref {
      display: inline-block; min-width: 15px; height: 15px; line-height: 15px;
      margin: 0 1px; padding: 0 4px; border-radius: 20px;
      background: var(--accent-soft); color: var(--accent);
      font-size: 9.5px; font-weight: 600; text-align: center;
      vertical-align: super;
    }

    /* Caret rides the end of the last rendered block while tokens arrive. */
    :host ::ng-deep .prose.streaming > :last-child::after {
      content: '';
      display: inline-block; width: 7px; height: 14px;
      margin-left: 3px; vertical-align: text-bottom;
      background: var(--accent); border-radius: 1px;
      animation: blink 1.05s step-end infinite;
    }
    @keyframes blink { 50% { opacity: 0; } }

    .thinking { display: flex; gap: 4px; padding: 6px 0; }
    .thinking span {
      width: 6px; height: 6px; border-radius: 50%;
      background: var(--text-faint); animation: bounce 1.3s infinite;
    }
    .thinking span:nth-child(2) { animation-delay: 0.16s; }
    .thinking span:nth-child(3) { animation-delay: 0.32s; }
    @keyframes bounce { 0%, 60%, 100% { opacity: 0.3; } 30% { opacity: 1; } }

    .failure {
      display: flex; flex-direction: column; gap: 3px;
      margin-top: 8px; padding: 10px 12px;
      background: rgba(242, 112, 109, 0.09);
      border: 1px solid rgba(242, 112, 109, 0.3);
      border-radius: var(--radius-sm);
      font-size: 12.5px; color: var(--danger);
      white-space: pre-wrap; overflow-wrap: anywhere;
    }
    .failure strong { font-weight: 600; }

    /* Citations */
    .citations { margin-top: 14px; }
    .citations-head {
      display: flex; align-items: center; gap: 6px; margin-bottom: 7px;
      font-size: 10.5px; font-weight: 600; letter-spacing: 0.07em;
      text-transform: uppercase; color: var(--text-faint);
    }
    .cite-count { background: var(--bg-input); border-radius: 20px; padding: 0 6px; letter-spacing: 0; }

    .chips { display: flex; flex-wrap: wrap; gap: 6px; }
    .chip {
      display: inline-flex; align-items: center; gap: 6px; max-width: 100%;
      background: var(--bg-raised); border: 1px solid var(--border);
      border-radius: 20px; padding: 4px 11px 4px 5px;
      font-size: 12px; color: var(--text-muted); transition: all 0.15s;
    }
    .chip:hover { border-color: var(--accent); color: var(--text); background: var(--accent-soft); }
    .chip-index {
      display: grid; place-items: center; flex: none;
      width: 17px; height: 17px; border-radius: 50%;
      background: var(--accent-soft); color: var(--accent);
      font-size: 10px; font-weight: 600;
    }
    .chip-name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 190px; }
    .chip-page { color: var(--text-faint); flex: none; }

    /* Composer */
    .composer-wrap { flex: none; padding: 0 20px 14px; background: var(--bg-base); }
    .composer {
      display: flex; align-items: flex-end; gap: 8px;
      max-width: var(--chat-max-width); margin: 0 auto;
      background: var(--bg-raised);
      border: 1px solid var(--border-strong);
      border-radius: var(--radius-lg);
      padding: 8px 8px 8px 14px;
      transition: border-color 0.15s;
    }
    .composer:focus-within { border-color: var(--accent); }

    .composer textarea {
      flex: 1; background: none; border: none; outline: none; resize: none;
      color: var(--text); font-size: 14.5px; line-height: 1.55;
      padding: 6px 0; max-height: 180px; min-height: 24px;
    }
    .composer textarea::placeholder { color: var(--text-faint); }
    .composer textarea:disabled { opacity: 0.6; }

    .send {
      display: grid; place-items: center; flex: none;
      width: 32px; height: 32px; border-radius: 9px;
      background: var(--accent); color: #fff; transition: background 0.15s, opacity 0.15s;
    }
    .send:hover:enabled { background: var(--accent-hover); }
    .send:disabled { opacity: 0.35; cursor: not-allowed; }
    .send svg { width: 17px; height: 17px; }
    .send.stop { background: var(--bg-hover); border: 1px solid var(--border-strong); }
    .send.stop:hover { background: var(--bg-input); }
    .stop-dot { width: 10px; height: 10px; border-radius: 2px; background: var(--text); }

    /* Reply actions */
    .actions { display: flex; gap: 2px; margin-top: 10px; opacity: 0; transition: opacity 0.15s; }
    .turn:hover .actions, .actions:focus-within { opacity: 1; }
    .actions button {
      display: grid; place-items: center; width: 26px; height: 26px;
      border-radius: var(--radius-sm); color: var(--text-faint);
      transition: background 0.15s, color 0.15s;
    }
    .actions button:hover { background: var(--bg-hover); color: var(--text); }
    .actions button.on { color: var(--accent); background: var(--accent-soft); }
    .actions button svg { width: 14px; height: 14px; }

    .hint {
      max-width: var(--chat-max-width); margin: 7px auto 0;
      font-size: 11px; color: var(--text-faint); text-align: center;
    }

    @media (max-width: 720px) {
      .column { padding: 20px 14px 32px; }
      .composer-wrap { padding: 0 14px 12px; }
      .user-bubble { max-width: 88%; }
    }
  `]
})
export class ChatPanelComponent implements AfterViewChecked {
  @Input() messages: ChatMessageUI[] = [];
  @Input() isGenerating = false;
  @Input() scopeName = '';

  @Output() send = new EventEmitter<string>();
  @Output() clear = new EventEmitter<void>();
  @Output() openCitation = new EventEmitter<Citation>();
  @Output() stop = new EventEmitter<void>();
  @Output() feedback = new EventEmitter<{ index: number; isPositive: boolean }>();

  copiedIndex = -1;

  /** Flattens an answer back to plain text for the clipboard. */
  copy(msg: ChatMessageUI): void {
    const text = msg.blocks?.length ? this.blocksToText(msg.blocks) : msg.content;
    navigator.clipboard?.writeText(text).then(() => {
      this.copiedIndex = this.messages.indexOf(msg);
      setTimeout(() => (this.copiedIndex = -1), 1600);
    });
  }

  private blocksToText(blocks: NonNullable<ChatMessageUI['blocks']>): string {
    return blocks
      .map(b => {
        switch (b.type) {
          case 'heading':
          case 'paragraph':
          case 'callout':
          case 'code':
            return b.text;
          case 'bullets':
            return b.items.map(i => `- ${i}`).join('\n');
          case 'steps':
            return b.items.map((i, n) => `${n + 1}. ${i}`).join('\n');
          case 'facts':
            return b.items.map(f => `${f.label}: ${f.value}`).join('\n');
          case 'table':
            return [b.headers.join(' | '), ...b.rows.map(r => r.join(' | '))].join('\n');
          default:
            return '';
        }
      })
      .filter(Boolean)
      .join('\n\n');
  }

  rate(index: number, isPositive: boolean): void {
    this.feedback.emit({ index, isPositive });
  }

  @ViewChild('scroller') private scroller?: ElementRef<HTMLDivElement>;

  draft = '';

  readonly suggestions = [
    'Summarise the key points',
    'What policies are covered?',
    'List the main requirements'
  ];

  private lastScrollHeight = 0;

  /**
   * Keeps the newest content in view while tokens stream in, but only when the
   * reader is already near the bottom - otherwise scrolling up to re-read an
   * earlier answer would be yanked back down on every token.
   */
  ngAfterViewChecked(): void {
    const el = this.scroller?.nativeElement;
    if (!el || el.scrollHeight === this.lastScrollHeight) return;

    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    const wasNearBottom = distanceFromBottom - (el.scrollHeight - this.lastScrollHeight) < 120;

    this.lastScrollHeight = el.scrollHeight;
    if (wasNearBottom) {
      el.scrollTop = el.scrollHeight;
    }
  }

  useSuggestion(text: string): void {
    this.draft = text;
    this.submit();
  }

  onEnter(event: Event): void {
    const key = event as KeyboardEvent;
    if (key.shiftKey) return;
    key.preventDefault();
    this.submit();
  }

  submit(): void {
    const text = this.draft.trim();
    if (!text || this.isGenerating) return;
    this.draft = '';
    this.send.emit(text);
  }

  autoGrow(el: HTMLTextAreaElement): void {
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 180) + 'px';
  }
}
