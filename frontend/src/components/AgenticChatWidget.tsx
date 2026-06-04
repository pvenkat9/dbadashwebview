import { useEffect, useState } from 'react';
import { MessageSquare, X } from 'lucide-react';
import { clsx } from 'clsx';
import AgenticChat from './AgenticChat';
import './AgenticChatWidget.css';

/**
 * Floating AI chat launcher (top-right) with slide-over panel.
 */
export default function AgenticChatWidget() {
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    if (open) setMounted(true);
  }, [open]);

  useEffect(() => {
    const onOpen = () => setOpen(true);
    window.addEventListener('agentic-chat:open', onOpen);
    return () => window.removeEventListener('agentic-chat:open', onOpen);
  }, []);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open]);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className={clsx('agentic-chat-fab', open && 'agentic-chat-fab--hidden')}
        title="Open AI Chat"
        aria-label="Open AI Chat"
      >
        <MessageSquare className="h-5 w-5" aria-hidden />
      </button>

      {open && (
        <div
          className="agentic-chat-backdrop"
          role="presentation"
          onClick={() => setOpen(false)}
        />
      )}

      <div
        className={clsx('agentic-chat-panel', open && 'agentic-chat-panel--open')}
        role="dialog"
        aria-modal={open}
        aria-label="AI Chat Assistant"
        aria-hidden={!open}
      >
        <div className="agentic-chat-panel__header">
          <span className="agentic-chat-panel__title">AI Chat</span>
          <button
            type="button"
            onClick={() => setOpen(false)}
            className="agentic-chat-panel__close"
            title="Close chat"
            aria-label="Close chat"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="agentic-chat-panel__body">
          {mounted && <AgenticChat variant="panel" />}
        </div>
      </div>
    </>
  );
}
