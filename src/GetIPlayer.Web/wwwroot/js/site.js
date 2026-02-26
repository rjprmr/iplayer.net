// get_iplayer Web UI - client-side JavaScript

'use strict';

// ===== SignalR connection =====
const downloadHub = (function () {
    let connection = null;

    function getConnection() {
        if (connection) return connection;
        if (typeof signalR === 'undefined') return null;
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/download')
            .withAutomaticReconnect()
            .build();
        connection.start().catch(err => console.error('SignalR error:', err));
        return connection;
    }

    return {
        subscribe(downloadId, onProgress, onComplete) {
            const conn = getConnection();
            if (!conn) return;
            conn.invoke('SubscribeToDownload', downloadId).catch(err => console.error(err));
            conn.on('DownloadProgress', data => {
                if (data.id === downloadId && onProgress) onProgress(data);
            });
            conn.on('DownloadComplete', data => {
                if (data.id === downloadId && onComplete) onComplete(data);
            });
        }
    };
})();

// ===== Toast notifications =====
function showToast(message, type) {
    type = type || 'info';
    const bgClass = type === 'success' ? 'bg-success' : type === 'danger' ? 'bg-danger' : 'bg-primary';
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    const id = 'toast-' + Date.now();
    const html = '<div id="' + id + '" class="toast align-items-center text-white ' + bgClass + ' border-0" role="alert">' +
        '<div class="d-flex"><div class="toast-body">' + escapeHtml(message) + '</div>' +
        '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div></div>';
    container.insertAdjacentHTML('beforeend', html);
    const el = document.getElementById(id);
    const toast = new bootstrap.Toast(el, { delay: 5000 });
    toast.show();
    el.addEventListener('hidden.bs.toast', () => el.remove());
}

// ===== Progress bar update =====
function updateProgressBar(containerId, data) {
    const el = document.getElementById(containerId);
    if (!el) return;
    const bar = el.querySelector('.progress-bar');
    const text = el.querySelector('.progress-text');
    if (data.percentComplete != null) {
        const pct = data.percentComplete.toFixed(1);
        bar.style.width = pct + '%';
        bar.setAttribute('aria-valuenow', pct);
        bar.textContent = pct + '%';
    } else {
        const mb = (data.bytesDownloaded / 1048576).toFixed(1);
        bar.style.width = '100%';
        bar.classList.add('progress-bar-striped', 'progress-bar-animated');
        bar.textContent = mb + ' MB';
    }
    if (text) {
        const speed = data.bytesPerSecond > 0 ? (data.bytesPerSecond / 1048576).toFixed(1) + ' MB/s' : '';
        text.textContent = data.phase + (speed ? ' — ' + speed : '');
    }
}

// ===== Confirm dialog =====
function confirmAction(message) {
    return window.confirm(message);
}

// ===== Utilities =====
function escapeHtml(str) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}

// ===== Client-side table filter =====
function setupTableFilter(inputId, tableId) {
    const input = document.getElementById(inputId);
    const table = document.getElementById(tableId);
    if (!input || !table) return;
    input.addEventListener('input', function () {
        const filter = this.value.toLowerCase();
        const rows = table.querySelectorAll('tbody tr');
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(filter) ? '' : 'none';
        });
    });
}
