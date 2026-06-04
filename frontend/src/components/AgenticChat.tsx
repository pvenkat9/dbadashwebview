import React, { useCallback, useEffect, useRef, useState } from 'react';
import './AgenticChat.css';

type AgenticChatProps = {
  variant?: 'page' | 'panel';
};

const AgenticChat: React.FC<AgenticChatProps> = ({ variant = 'page' }) => {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [connected, setConnected] = useState(false);

  const shellSrc =
    variant === 'panel'
      ? '/agentic-chat-shell.html?embedded=1'
      : '/agentic-chat-shell.html';

  const reloadShell = useCallback(() => {
    setLoading(true);
    setError(null);
    setConnected(false);
    const iframe = iframeRef.current;
    if (iframe) {
      iframe.src = shellSrc;
    }
  }, [shellSrc]);

  useEffect(() => {
    const iframe = iframeRef.current;
    if (!iframe) return;

    const onLoad = () => {
      setLoading(false);
    };
    const onError = () => {
      setLoading(false);
      setError('Failed to load the chat interface. Please try again.');
      setConnected(false);
    };

    iframe.addEventListener('load', onLoad);
    iframe.addEventListener('error', onError);
    iframe.src = shellSrc;

    return () => {
      iframe.removeEventListener('load', onLoad);
      iframe.removeEventListener('error', onError);
    };
  }, [shellSrc]);

  useEffect(() => {
    const onMessage = (event: MessageEvent) => {
      if (!event.origin.includes(window.location.hostname)) {
        return;
      }
      const data = event.data;
      if (!data || typeof data !== 'object') return;

      const { type, payload } = data as { type?: string; payload?: Record<string, unknown> };
      switch (type) {
        case 'CONNECTION_STATUS_CHANGED':
          setConnected(Boolean(payload?.connected));
          if (payload?.connected) setError(null);
          break;
        case 'ERROR':
          setError(String(payload?.message ?? 'Chat could not start'));
          setConnected(false);
          setLoading(false);
          break;
        case 'LOG':
          console.log('[AgenticChat]', payload);
          break;
        default:
          break;
      }
    };

    window.addEventListener('message', onMessage);
    return () => window.removeEventListener('message', onMessage);
  }, []);

  return (
    <div
      className={
        variant === 'panel'
          ? 'agentic-chat-embedded h-full min-h-0 flex flex-col'
          : 'agentic-chat-page -m-6 flex h-[calc(100vh-3.5rem)] min-h-[32rem] flex-col'
      }
    >
      <div className="agentic-chat-container min-h-0 flex-1 flex flex-col">
        {loading && (
          <div className="agentic-chat-loading">
            <div className="spinner" aria-hidden />
            <p>Loading chat…</p>
          </div>
        )}

        {error && (
          <div className="agentic-chat-error">
            <div className="error-icon" aria-hidden>
              ⚠️
            </div>
            <div className="error-message">
              <strong>Chat could not start</strong>
              <p>{error}</p>
            </div>
            <button type="button" className="retry-button" onClick={reloadShell}>
              Retry
            </button>
          </div>
        )}

        <iframe
          ref={iframeRef}
          className="agentic-chat-iframe"
          title="Agentic Chat"
          style={error ? { display: 'none' } : undefined}
        />

        {!loading && !error && (
          <div
            className={`connection-status ${connected ? 'connected' : 'disconnected'}`}
            aria-live="polite"
          >
            <span className="status-dot" />
            {connected ? 'Connected' : 'Connecting…'}
          </div>
        )}
      </div>
    </div>
  );
};

export default AgenticChat;
