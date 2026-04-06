import "./BackendDisconnected.css";

export default function BackendDisconnected() {
  return (
    <div className="backend-disconnected-overlay">
      <div className="backend-disconnected-container">
        <div className="disconnected-icon">
          <svg
            width="64"
            height="64"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
        </div>

        <h1>Backend Service Unavailable</h1>

        <p className="disconnect-message">
          The application is unable to connect to the backend service at this moment.
        </p>

        <div className="checking-status">
          <div className="spinner"></div>
          <p>Attempting to reconnect... Please wait</p>
        </div>

        <div className="troubleshooting-tips">
          <h3>Troubleshooting:</h3>
          <ul>
            <li>Ensure the backend server is running</li>
            <li>Check your internet connection</li>
            <li>Try refreshing the page</li>
            <li>If the issue persists, contact your system administrator</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
