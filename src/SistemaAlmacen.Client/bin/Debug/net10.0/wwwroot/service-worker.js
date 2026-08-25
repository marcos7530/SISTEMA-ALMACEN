/* Manifest version: gdxbWU8w */
// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
// However, we still implement basic strategies so the PWA shell works during dev testing.

const CACHE_NAME = 'sistema-pos-dev-v1';

self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', event => {
    // In development, always go to network
    // This ensures changes are reflected immediately
    return;
});
