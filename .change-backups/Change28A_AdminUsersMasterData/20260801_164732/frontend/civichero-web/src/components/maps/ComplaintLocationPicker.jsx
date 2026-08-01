import { useEffect, useRef, useState } from 'react';

const LEAFLET_VERSION = '1.9.4';
const LEAFLET_SCRIPT = `https://unpkg.com/leaflet@${LEAFLET_VERSION}/dist/leaflet.js`;
const LEAFLET_CSS = `https://unpkg.com/leaflet@${LEAFLET_VERSION}/dist/leaflet.css`;
let leafletPromise;

function loadLeaflet() {
  if (globalThis.L) return Promise.resolve(globalThis.L);
  if (leafletPromise) return leafletPromise;

  if (!document.querySelector(`link[href="${LEAFLET_CSS}"]`)) {
    const stylesheet = document.createElement('link');
    stylesheet.rel = 'stylesheet';
    stylesheet.href = LEAFLET_CSS;
    document.head.appendChild(stylesheet);
  }

  leafletPromise = new Promise((resolve, reject) => {
    const existing = document.querySelector(`script[src="${LEAFLET_SCRIPT}"]`);
    if (existing) {
      existing.addEventListener('load', () => resolve(globalThis.L), { once: true });
      existing.addEventListener('error', () => reject(new Error('OpenStreetMap could not be loaded.')), { once: true });
      return;
    }
    const script = document.createElement('script');
    script.src = LEAFLET_SCRIPT;
    script.async = true;
    script.onload = () => resolve(globalThis.L);
    script.onerror = () => reject(new Error('OpenStreetMap could not be loaded.'));
    document.head.appendChild(script);
  });
  return leafletPromise;
}

export default function ComplaintLocationPicker({ latitude, longitude, onChange }) {
  const containerRef = useRef(null);
  const mapRef = useRef(null);
  const markerRef = useRef(null);
  const leafletRef = useRef(null);
  const onChangeRef = useRef(onChange);
  const [error, setError] = useState('');

  useEffect(() => { onChangeRef.current = onChange; }, [onChange]);

  useEffect(() => {
    if (!containerRef.current) return undefined;
    let cancelled = false;

    loadLeaflet().then((L) => {
      if (cancelled || !containerRef.current) return;
      const suppliedLatitude = Number(latitude);
      const suppliedLongitude = Number(longitude);
      const hasCoordinates = Number.isFinite(suppliedLatitude) && Number.isFinite(suppliedLongitude)
        && String(latitude) !== '' && String(longitude) !== '';
      const center = hasCoordinates ? [suppliedLatitude, suppliedLongitude] : [19.076, 72.8777];

      const map = L.map(containerRef.current, { center, zoom: hasCoordinates ? 16 : 11, zoomControl: true });
      mapRef.current = map;
      leafletRef.current = L;
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors',
      }).addTo(map);

      const placeMarker = (lat, lng, notify = true) => {
        if (!markerRef.current) {
          markerRef.current = L.marker([lat, lng], { draggable: true }).addTo(map);
          markerRef.current.on('dragend', (event) => {
            const point = event.target.getLatLng();
            onChangeRef.current?.(point.lat, point.lng);
          });
        } else {
          markerRef.current.setLatLng([lat, lng]);
        }
        if (notify) onChangeRef.current?.(lat, lng);
      };

      if (hasCoordinates) placeMarker(suppliedLatitude, suppliedLongitude, false);
      map.on('click', (event) => placeMarker(event.latlng.lat, event.latlng.lng));
      window.setTimeout(() => map.invalidateSize(), 0);
      setError('');
    }).catch((reason) => setError(reason?.message || 'Map could not be loaded.'));

    return () => {
      cancelled = true;
      markerRef.current = null;
      leafletRef.current = null;
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    const L = leafletRef.current;
    const lat = Number(latitude);
    const lng = Number(longitude);
    if (!map || !L || !Number.isFinite(lat) || !Number.isFinite(lng) || String(latitude) === '' || String(longitude) === '') return;
    if (!markerRef.current) {
      markerRef.current = L.marker([lat, lng], { draggable: true }).addTo(map);
      markerRef.current.on('dragend', (event) => {
        const point = event.target.getLatLng();
        onChangeRef.current?.(point.lat, point.lng);
      });
    } else {
      markerRef.current.setLatLng([lat, lng]);
    }
    map.panTo([lat, lng], { animate: true });
  }, [latitude, longitude]);

  return (
    <div style={{ position: 'relative' }}>
      <div ref={containerRef} style={{ minHeight: 390, borderRadius: 18, overflow: 'hidden', background: '#e5e7eb' }} />
      <div className="map-overlay-card" style={{ position: 'absolute', left: 12, bottom: 12, zIndex: 500, maxWidth: 300 }}>
        <strong>Click or drag the marker</strong>
        <p>Choose the exact issue location. You can also use GPS or enter coordinates manually.</p>
      </div>
      {error && <div className="alert error" style={{ position: 'absolute', left: 12, right: 12, top: 12, zIndex: 500 }}>{error}</div>}
    </div>
  );
}
