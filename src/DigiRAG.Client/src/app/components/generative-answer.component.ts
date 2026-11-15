import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UiBlock } from '../models';
import { MarkdownPipe } from '../markdown.pipe';

/**
 * Renders the model's answer as real components rather than a wall of prose.
 * The model picks the block type per section; this maps each one to markup.
 * Inline emphasis, code spans and [n] citation markers inside a block's text
 * still go through the Markdown pipe, which Angular then sanitizes.
 */
@Component({
  selector: 'app-generative-answer',
  standalone: true,
  imports: [CommonModule, MarkdownPipe],
  template: `
    <div class="blocks">
      <ng-container *ngFor="let block of blocks">
        <h3 *ngIf="block.type === 'heading'" class="b-heading"
            [innerHTML]="block.text | markdown"></h3>

        <div *ngIf="block.type === 'paragraph'" class="b-para"
             [innerHTML]="block.text | markdown"></div>

        <ul *ngIf="block.type === 'bullets'" class="b-bullets">
          <li *ngFor="let item of block.items" [innerHTML]="item | markdown"></li>
        </ul>

        <ol *ngIf="block.type === 'steps'" class="b-steps">
          <li *ngFor="let item of block.items" [innerHTML]="item | markdown"></li>
        </ol>

        <dl *ngIf="block.type === 'facts'" class="b-facts">
          <ng-container *ngFor="let fact of block.items">
            <dt>{{ fact.label }}</dt>
            <dd [innerHTML]="fact.value | markdown"></dd>
          </ng-container>
        </dl>

        <div *ngIf="block.type === 'table'" class="b-table-wrap">
          <table class="b-table">
            <thead>
              <tr><th *ngFor="let h of block.headers">{{ h }}</th></tr>
            </thead>
            <tbody>
              <tr *ngFor="let row of block.rows">
                <td *ngFor="let cell of row" [innerHTML]="cell | markdown"></td>
              </tr>
            </tbody>
          </table>
        </div>

        <div *ngIf="block.type === 'callout'" class="b-callout"
             [ngClass]="'tone-' + (block.tone || 'info')">
          <span class="dot" aria-hidden="true"></span>
          <div [innerHTML]="block.text | markdown"></div>
        </div>

        <pre *ngIf="block.type === 'code'" class="b-code"><code>{{ block.text }}</code></pre>
      </ng-container>
    </div>
  `,
  styles: [`
    .blocks { display: flex; flex-direction: column; gap: 12px; font-size: 14.5px; line-height: 1.68; }

    .b-heading { font-size: 15px; font-weight: 650; letter-spacing: -0.01em; margin-top: 4px; }
    .b-para { color: var(--text); }

    .b-bullets, .b-steps { padding-left: 20px; display: flex; flex-direction: column; gap: 5px; }
    .b-bullets li::marker, .b-steps li::marker { color: var(--text-faint); }

    /* Attribute/value pairs: the shape most document answers actually have. */
    .b-facts {
      display: grid; grid-template-columns: minmax(110px, max-content) 1fr;
      gap: 7px 16px;
      background: var(--bg-raised);
      border: 1px solid var(--border);
      border-radius: var(--radius); padding: 13px 15px;
    }
    .b-facts dt { font-size: 12px; color: var(--text-faint); font-weight: 500; }
    .b-facts dd { font-size: 13.5px; color: var(--text); min-width: 0; overflow-wrap: anywhere; }

    .b-table-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: var(--radius); }
    .b-table { border-collapse: collapse; width: 100%; font-size: 13px; }
    .b-table th, .b-table td { padding: 8px 12px; text-align: left; border-bottom: 1px solid var(--border); }
    .b-table th { background: var(--bg-input); font-weight: 600; color: var(--text-muted); white-space: nowrap; }
    .b-table tr:last-child td { border-bottom: none; }

    .b-callout {
      display: flex; gap: 10px; align-items: flex-start;
      padding: 11px 14px; border-radius: var(--radius);
      border: 1px solid var(--border); background: var(--bg-raised);
      font-size: 13.5px;
    }
    .b-callout .dot { width: 7px; height: 7px; border-radius: 50%; margin-top: 7px; flex: none; }
    .tone-info { border-color: rgba(77, 142, 247, 0.34); }
    .tone-info .dot { background: var(--accent); }
    .tone-warn { border-color: rgba(245, 181, 68, 0.34); }
    .tone-warn .dot { background: var(--warning); }
    .tone-success { border-color: rgba(62, 207, 142, 0.34); }
    .tone-success .dot { background: var(--success); }

    .b-code {
      background: var(--bg-input); border: 1px solid var(--border);
      border-radius: var(--radius-sm); padding: 12px 14px; overflow-x: auto;
      font-family: ui-monospace, 'SF Mono', Menlo, Consolas, monospace;
      font-size: 12.5px; line-height: 1.6;
    }

    /* Markdown arrives through innerHTML and so carries no scoping attribute. */
    :host ::ng-deep strong { font-weight: 650; color: #fff; }
    :host ::ng-deep em { font-style: italic; color: var(--text-muted); }
    :host ::ng-deep a { color: var(--accent); text-decoration: underline; text-underline-offset: 2px; }
    :host ::ng-deep code {
      font-family: ui-monospace, 'SF Mono', Menlo, Consolas, monospace;
      font-size: 0.88em; background: var(--bg-input);
      border: 1px solid var(--border); border-radius: 4px; padding: 1px 5px;
    }
    /* The pipe emits single-paragraph HTML; strip its default block margins. */
    :host ::ng-deep p { margin: 0; }
    :host ::ng-deep .cite-ref {
      display: inline-block; min-width: 15px; height: 15px; line-height: 15px;
      margin: 0 1px; padding: 0 4px; border-radius: 20px;
      background: var(--accent-soft); color: var(--accent);
      font-size: 9.5px; font-weight: 600; text-align: center; vertical-align: super;
    }
  `]
})
export class GenerativeAnswerComponent {
  @Input() blocks: UiBlock[] = [];
}
