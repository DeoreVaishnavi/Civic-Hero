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

function markerColor(item, selected) {
  if (selected) return '#2563eb';
  if (String(item?.slaState).toLowerCase() === 'breached') return '#b91c1c';
  if (String(item?.slaState).toLowerCase() === 'atrisk') return '#d97706';
  if (String(item?.priority).toLowerCase() === 'critical') return '#7f1d1d';
  if (String(item?.priority).toLowerCase() === 'high') return '#dc2626';
  return '#15966f';
}

export default function OfficerAssignmentMap({ assignments = [], selectedId, onSelect }) {
  const containerRef = useRef(null);
  const mapRef = useRef(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!containerRef.current) return undefined;
    let cancelled = false;

    loadLeaflet().then((L) => {
      if (cancelled || !containerRef.current) return;
      if (mapRef.current) mapRef.current.remove();

      const usable = assignments.filter((item) => Number.isFinite(Number(item?.latitude)) && Number.isFinite(Number(item?.longitude)));
      const map = L.map(containerRef.current, { center: [19.076, 72.8777], zoom: 11, zoomControl: true });
      mapRef.current = map;
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors',
      }).addTo(map);

      const bounds = L.latLngBounds([]);
      usable.forEach((item) => {
        const latLng = [Number(item.latitude), Number(item.longitude)];
        bounds.extend(latLng);
        const selected = Number(item.complaintId) === Number(selectedId);
        const marker = L.circleMarker(latLng, {
          radius: selected ? 12 : 9,
          color: '#ffffff',
          weight: selected ? 4 : 3,
          fillColor: markerColor(item, selected),
          fillOpacity: 0.95,
        }).addTo(map);

        const tooltip = document.createElement('div');
        const title = document.createElement('strong');
        title.textContent = item.title || item.referenceNumber || 'Assignment';
        const details = document.createElement('div');
        details.textContent = `${item.priority || '—'} · ${item.slaState || '—'}`;
        tooltip.append(title, details);
        marker.bindTooltip(tooltip, { direction: 'top' });
        marker.on('click', () => onSelect?.(item));
        if (selected) marker.openTooltip();
      });

      if (bounds.isValid()) map.fitBounds(bounds, { padding: [45, 45], maxZoom: 15 });
      window.setTimeout(() => map.invalidateSize(), 0);
      setError('');
    }).catch((reason) => setError(reason?.message || 'Map could not be loaded.'));

    return () => {
      cancelled = true;
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
    };
  }, [assignments, selectedId, onSelect]);

  return (
    <div style={{ position: 'relative' }}>
      <div ref={containerRef} style={{ minHeight: 520, borderRadius: 18, overflow: 'hidden', background: '#e5e7eb' }} />
      {error && <div className="alert error" style={{ position: 'absolute', left: 12, right: 12, top: 12, zIndex: 500 }}>{error}</div>}
    </div>
  );
}
