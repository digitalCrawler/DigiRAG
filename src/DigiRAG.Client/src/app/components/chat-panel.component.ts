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

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
          <article *ngFor="let msg of messages" class="turn" [class.user]="msg.role === 'user'">
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

                <div class="prose" *ngIf="msg.content">{{ msg.content }}<span
                  class="caret" *ngIf="msg.isStreaming"></span></div>

                <div class="thinking" *ngIf="!msg.content && msg.isStreaming && !msg.errorText">
                  <span></span><span></span><span></span>
                </div>

                <div class="failure" *ngIf="msg.errorText">
                  <strong>Request failed</strong>
                  <span>{{ msg.errorText }}</span>
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
          <button class="send" (click)="submit()" [disabled]="!draft.trim() || isGenerating"
                  aria-label="Send message">
            <svg *ngIf="!isGenerating" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 19V5M5 12l7-7 7 7" />
            </svg>
            <span *ngIf="isGenerating" class="stop-dot"></span>
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

    /* pre-wrap keeps the model's line breaks without interpreting any markup,
       so nothing the model emits can inject HTML. */
    .prose {
      font-size: 14.5px; line-height: 1.68; color: var(--text);
      white-space: pre-wrap; overflow-wrap: anywhere;
    }

    .caret {
      display: inline-block; width: 7px; height: 15px;
      margin-left: 2px; vertical-align: text-bottom;
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
    .stop-dot {
      width: 11px; height: 11px; border-radius: 2px; background: #fff;
      animation: blink 1.05s step-end infinite;
    }

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
