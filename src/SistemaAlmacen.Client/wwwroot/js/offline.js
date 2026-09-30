// Offline interop functions for Blazor WASM
// Provides connectivity detection and localStorage access for offline support.

window.offlineInterop = {
    _dotNetRef: null,
    _onlineHandler: null,
    _offlineHandler: null,

    // Returns current online status
    isOnline: function () {
        return navigator.onLine;
    },

    // Registers a .NET object reference to receive connectivity change callbacks
    registerConnectivityHandler: function (dotNetRef) {
        this._dotNetRef = dotNetRef;

        this._onlineHandler = function () {
            dotNetRef.invokeMethodAsync('OnConnectivityChanged', true);
        };
        this._offlineHandler = function () {
            dotNetRef.invokeMethodAsync('OnConnectivityChanged', false);
        };

        window.addEventListener('online', this._onlineHandler);
        window.addEventListener('offline', this._offlineHandler);
    },

    // Unregisters connectivity event listeners
    unregisterConnectivityHandler: function () {
        if (this._onlineHandler) {
            window.removeEventListener('online', this._onlineHandler);
            this._onlineHandler = null;
        }
        if (this._offlineHandler) {
            window.removeEventListener('offline', this._offlineHandler);
            this._offlineHandler = null;
        }
        this._dotNetRef = null;
    },

    // localStorage wrapper: get item
    getItem: function (key) {
        return localStorage.getItem(key);
    },

    // localStorage wrapper: set item
    setItem: function (key, value) {
        localStorage.setItem(key, value);
    },

    // localStorage wrapper: remove item
    removeItem: function (key) {
        localStorage.removeItem(key);
    }
};

// Descarga un archivo en el navegador a partir de un arreglo de bytes.
// Usado para descargar PDFs de comprobantes y exportaciones de reportes desde Blazor.
// fileName: nombre del archivo | contentType: tipo MIME | bytes: Uint8Array/array de bytes desde .NET
window.downloadFileFromBytes = function (fileName, contentType, bytes) {
    // Blazor puede pasar los bytes como Uint8Array o como array normal; normalizamos.
    const data = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
    const blob = new Blob([data], { type: contentType || 'application/octet-stream' });

    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName || 'archivo';
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);

    // Liberar el object URL luego de un breve lapso para asegurar que la descarga inició.
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
};
