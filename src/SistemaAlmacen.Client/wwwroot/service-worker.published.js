// Service Worker for Sistema POS - Production
// Cache-first for static assets (.dll, .wasm, .css, .js)
// Network-first for API requests

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'sistema-pos-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

// Static assets to cache (cache-first strategy)
const offlineAssetsInclude = [
    /\.dll$/,
    /\.pdb$/,
    /\.wasm/,
    /\.html/,
    /\.js$/,
    /\.json$/,
    /\.css$/,
    /\.woff$/,
    /\.woff2$/,
    /\.png$/,
    /\.jpe?g$/,
    /\.gif$/,
    /\.ico$/,
    /\.blat$/,
    /\.dat$/,
    /\.webmanifest$/,
    /\.svg$/
];

const offlineAssetsExclude = [/^service-worker\.js$/];

// API paths that use network-first strategy
const apiPaths = ['/api/'];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest (precache)
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    if (event.request.method !== 'GET') {
        return fetch(event.request);
    }

    const requestUrl = new URL(event.request.url);

    // Network-first strategy for API requests
    if (apiPaths.some(path => requestUrl.pathname.startsWith(path))) {
        return networkFirst(event.request);
    }

    // Cache-first strategy for static assets
    return cacheFirst(event.request);
}

// Cache-first: serve from cache, fallback to network
async function cacheFirst(request) {
    // For navigation requests, try to serve index.html from cache
    const shouldServeIndexHtml = request.mode === 'navigate'
        && !manifestUrlList.some(url => url === request.url);

    const cacheRequest = shouldServeIndexHtml ? 'index.html' : request;
    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(cacheRequest);

    if (cachedResponse) {
        return cachedResponse;
    }

    try {
        const networkResponse = await fetch(request);
        // Cache successful responses for static assets
        if (networkResponse.ok && !request.url.includes('/api/')) {
            const responseClone = networkResponse.clone();
            cache.put(request, responseClone);
        }
        return networkResponse;
    } catch (error) {
        // If offline and not in cache, return a basic offline page
        if (request.mode === 'navigate') {
            return cache.match('index.html');
        }
        throw error;
    }
}

// Network-first: try network, fallback to cache (for API requests)
async function networkFirst(request) {
    const cache = await caches.open(cacheName);

    try {
        const networkResponse = await fetch(request);
        // Cache successful GET API responses for offline access
        if (networkResponse.ok) {
            const responseClone = networkResponse.clone();
            cache.put(request, responseClone);
        }
        return networkResponse;
    } catch (error) {
        // Network failed, try to serve from cache
        const cachedResponse = await cache.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        // Return a JSON error response indicating offline status
        return new Response(
            JSON.stringify({ error: 'Sin conexión', offline: true }),
            {
                status: 503,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
}
