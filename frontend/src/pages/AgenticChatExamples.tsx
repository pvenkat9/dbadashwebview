import React from 'react';
import AgenticChat from '../components/AgenticChat';

/**
 * Example page showing how to integrate the Agentic Chat component
 * 
 * This page demonstrates:
 * - Basic component usage
 * - Layout integration
 * - Error handling
 * - Responsive design
 */
export function ChatDashboardPage() {
  return (
    <div className="chat-dashboard-page">
      <header className="page-header">
        <h1>AI Chat Assistant</h1>
        <p>Get help with database administration and queries</p>
      </header>

      <main className="chat-main">
        {/* Chat component takes up available space */}
        <div className="chat-wrapper">
          <AgenticChat />
        </div>
      </main>
    </div>
  );
}

/**
 * Alternative: Sidebar chat integration
 * Show chat as a sidebar while keeping other content visible
 */
export function LayoutWithSidebarChat() {
  return (
    <div className="dashboard-with-sidebar-chat">
      <aside className="sidebar">
        <div className="sidebar-chat">
          <AgenticChat />
        </div>
      </aside>

      <main className="main-content">
        {/* Other dashboard content goes here */}
        <h2>Dashboard Content</h2>
      </main>
    </div>
  );
}

/**
 * Alternative: Modal chat integration
 * Show chat in a modal dialog
 */
export function ModalChatExample() {
  const [isOpen, setIsOpen] = React.useState(false);

  return (
    <div>
      <button onClick={() => setIsOpen(true)}>
        Open Chat Assistant
      </button>

      {isOpen && (
        <div className="chat-modal-overlay" onClick={() => setIsOpen(false)}>
          <div className="chat-modal" onClick={(e) => e.stopPropagation()}>
            <div className="chat-modal-header">
              <h2>AI Chat Assistant</h2>
              <button 
                className="close-button"
                onClick={() => setIsOpen(false)}
              >
                ✕
              </button>
            </div>
            <div className="chat-modal-body">
              <AgenticChat />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

/**
 * CSS for the examples:
 * 
 * .chat-dashboard-page {
 *   display: flex;
 *   flex-direction: column;
 *   height: 100vh;
 * }
 * 
 * .page-header {
 *   padding: 20px;
 *   background-color: #f9f9f9;
 *   border-bottom: 1px solid #e0e0e0;
 * }
 * 
 * .chat-main {
 *   flex: 1;
 *   overflow: hidden;
 * }
 * 
 * .chat-wrapper {
 *   width: 100%;
 *   height: 100%;
 * }
 * 
 * // Sidebar layout
 * .dashboard-with-sidebar-chat {
 *   display: grid;
 *   grid-template-columns: 400px 1fr;
 *   height: 100vh;
 *   gap: 16px;
 *   padding: 16px;
 * }
 * 
 * .sidebar {
 *   overflow: hidden;
 * }
 * 
 * .sidebar-chat {
 *   height: 100%;
 *   width: 100%;
 * }
 * 
 * .main-content {
 *   overflow-y: auto;
 * }
 * 
 * // Modal layout
 * .chat-modal-overlay {
 *   position: fixed;
 *   top: 0;
 *   left: 0;
 *   right: 0;
 *   bottom: 0;
 *   background-color: rgba(0, 0, 0, 0.5);
 *   display: flex;
 *   align-items: center;
 *   justify-content: center;
 *   z-index: 1000;
 * }
 * 
 * .chat-modal {
 *   width: 90%;
 *   max-width: 600px;
 *   height: 80vh;
 *   background-color: white;
 *   border-radius: 8px;
 *   display: flex;
 *   flex-direction: column;
 *   box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3);
 * }
 * 
 * .chat-modal-header {
 *   padding: 16px 20px;
 *   border-bottom: 1px solid #e0e0e0;
 *   display: flex;
 *   justify-content: space-between;
 *   align-items: center;
 * }
 * 
 * .chat-modal-header h2 {
 *   margin: 0;
 *   font-size: 18px;
 * }
 * 
 * .close-button {
 *   background: none;
 *   border: none;
 *   font-size: 24px;
 *   cursor: pointer;
 *   color: #666;
 * }
 * 
 * .chat-modal-body {
 *   flex: 1;
 *   overflow: hidden;
 * }
 */
