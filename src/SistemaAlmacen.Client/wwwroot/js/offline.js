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
