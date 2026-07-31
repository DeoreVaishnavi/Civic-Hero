import { useEffect, useMemo, useRef, useState } from 'react';
import './CivicHeatmapMap.css';

const MAPBOX_VERSION = '3.25.0';
const MAPBOX_SCRIPT = `https://api.mapbox.com/mapbox-gl-js/v${MAPBOX_VERSION}/mapbox-gl.js`;
const MAPBOX_CSS = `https://api.mapbox.com/mapbox-gl-js/v${MAPBOX_VERSION}/mapbox-gl.css`;
const LEAFLET_VERSION = '1.9.4';
const LEAFLET_SCRIPT = `https://unpkg.com/leaflet@${LEAFLET_VERSION}/dist/leaflet.js`;
const LEAFLET_CSS = `https://unpkg.com/leaflet@${LEAFLET_VERSION}/dist/leaflet.css`;
const OPEN_STREET_MAP_TILES = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
let mapboxPromise;
let leafletPromise;

function loadExternalLibrary({ globalName, scriptUrl, stylesheetUrl, errorMessage }) {
  if (globalThis[globalName]) return Promise.resolve(globalThis[globalName]);

  if (!document.querySelector(`link[href="${stylesheetUrl}"]`)) {
    const stylesheet = document.createElement('link');
    stylesheet.rel = 'stylesheet';
    stylesheet.href = stylesheetUrl;
    document.head.appendChild(stylesheet);
  }

  return new Promise((resolve, reject) => {
    const existing = document.querySelector(`script[src="${scriptUrl}"]`);
    if (existing) {
      existing.addEventListener('load', () => resolve(globalThis[globalName]), { once: true });
      existing.addEventListener('error', () => reject(new Error(errorMessage)), { once: true });
      return;
    }

    const script = document.createElement('script');
    script.src = scriptUrl;
    script.async = true;
    script.onload = () => resolve(globalThis[globalName]);
    script.onerror = () => reject(new Error(errorMessage));
    document.head.appendChild(script);
  });
}

function loadMapbox() {
  if (!mapboxPromise) {
    mapboxPromise = loadExternalLibrary({
      globalName: 'mapboxgl',
      scriptUrl: MAPBOX_SCRIPT,
      stylesheetUrl: MAPBOX_CSS,
      errorMessage: 'Mapbox could not be loaded.',
    });
  }
  return mapboxPromise;
}

function loadLeaflet() {
  if (!leafletPromise) {
    leafletPromise = loadExternalLibrary({
      globalName: 'L',
      scriptUrl: LEAFLET_SCRIPT,
      stylesheetUrl: LEAFLET_CSS,
      errorMessage: 'OpenStreetMap could not be loaded.',
    });
  }
  return leafletPromise;
}

function pointKey(point) {
  return `${point.latitude}:${point.longitude}:${point.wardId ?? point.id ?? 'none'}`;
}

function riskColor(riskLevel) {
  switch (String(riskLevel || '').toLowerCase()) {
    case 'critical': return '#991b1b';
    case 'high': return '#ef4444';
    case 'medium': return '#f59e0b';
    default: return '#15966f';
  }
}

function toGeoJson(points) {
  return {
    type: 'FeatureCollection',
    features: points.map((point, index) => ({
      type: 'Feature',
      id: index,
      geometry: {
        type: 'Point',
        coordinates: [Number(point.longitude), Number(point.latitude)],
      },
      properties: {
        pointIndex: index,
        complaintCount: Number(point.complaintCount || 0),
        activeCount: Number(point.activeCount || 0),
        solvedCount: Number(point.solvedCount || 0),
        resolutionRate: Number(point.resolutionRate || 0),
        riskLevel: point.riskLevel || 'Low',
        wardName: point.wardName || 'Unassigned ward',
        dominantCategory: point.dominantCategory || 'Other',
      },
    })),
  };
}

function createPopupContent(point) {
  const wrapper = document.createElement('div');
  wrapper.className = 'civic-map-popup';

  const heading = document.createElement('strong');
  heading.textContent = point.wardName || 'Selected area';
  wrapper.appendChild(heading);

  const category = document.createElement('span');
  category.textContent = `Dominant issue: ${point.dominantCategory || 'Other'}`;
  wrapper.appendChild(category);

  const counts = document.createElement('span');
  counts.textContent = `${Number(point.complaintCount || 0)} complaints · ${Number(point.solvedCount || 0)} solved`;
  wrapper.appendChild(counts);

  return wrapper;
}

function addMapboxLayers(map, mapboxgl, points, onSelect) {
  const data = toGeoJson(points);
  map.addSource('civichero-complaints', { type: 'geojson', data });

  map.addLayer({
    id: 'complaint-density',
    type: 'heatmap',
    source: 'civichero-complaints',
    maxzoom: 16,
    paint: {
      'heatmap-weight': ['interpolate', ['linear'], ['get', 'complaintCount'], 0, 0, 10, 0.45, 40, 1],
      'heatmap-intensity': ['interpolate', ['linear'], ['zoom'], 8, 0.8, 15, 2.4],
      'heatmap-radius': ['interpolate', ['linear'], ['zoom'], 8, 18, 15, 42],
      'heatmap-opacity': ['interpolate', ['linear'], ['zoom'], 8, 0.82, 16, 0.28],
      'heatmap-color': [
        'interpolate', ['linear'], ['heatmap-density'],
        0, 'rgba(21,150,111,0)',
        0.18, 'rgba(21,150,111,0.55)',
        0.38, 'rgba(250,204,21,0.72)',
        0.62, 'rgba(245,158,11,0.82)',
        0.82, 'rgba(239,68,68,0.88)',
        1, 'rgba(153,27,27,0.95)',
      ],
    },
  });

  map.addLayer({
    id: 'heatmap-circles',
    type: 'circle',
    source: 'civichero-complaints',
    minzoom: 9,
    paint: {
      'circle-radius': ['interpolate', ['linear'], ['get', 'complaintCount'], 1, 12, 10, 18, 40, 27],
      'circle-color': [
        'match', ['get', 'riskLevel'],
        'Critical', '#991b1b',
        'High', '#ef4444',
        'Medium', '#f59e0b',
        '#15966f',
      ],
      'circle-opacity': 0.88,
      'circle-stroke-color': '#ffffff',
      'circle-stroke-width': 2,
    },
  });

  map.addLayer({
    id: 'heatmap-counts',
    type: 'symbol',
    source: 'civichero-complaints',
    minzoom: 9,
    layout: {
      'text-field': ['to-string', ['get', 'complaintCount']],
      'text-size': 11,
      'text-font': ['Open Sans Bold', 'Arial Unicode MS Bold'],
      'text-allow-overlap': true,
    },
    paint: { 'text-color': '#ffffff' },
  });

  const bounds = new mapboxgl.LngLatBounds();
  points.forEach((point) => bounds.extend([Number(point.longitude), Number(point.latitude)]));
  if (!bounds.isEmpty()) map.fitBounds(bounds, { padding: 70, maxZoom: 14, duration: 600 });

  const choosePoint = (event) => {
    const feature = event.features?.[0];
    const index = Number(feature?.properties?.pointIndex);
    if (Number.isInteger(index) && points[index]) onSelect?.(points[index]);
  };
  map.on('click', 'heatmap-circles', choosePoint);
  map.on('mouseenter', 'heatmap-circles', () => { map.getCanvas().style.cursor = 'pointer'; });
  map.on('mouseleave', 'heatmap-circles', () => { map.getCanvas().style.cursor = ''; });
}

function addLeafletLayers(map, L, points, onSelect) {
  L.tileLayer(OPEN_STREET_MAP_TILES, {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors',
  }).addTo(map);

  const bounds = L.latLngBounds([]);
  points.forEach((point) => {
    const latitude = Number(point.latitude);
    const longitude = Number(point.longitude);
    const complaintCount = Number(point.complaintCount || 0);
    const color = riskColor(point.riskLevel);
    const latLng = [latitude, longitude];
    bounds.extend(latLng);

    L.circle(latLng, {
      radius: Math.min(2400, 350 + complaintCount * 55),
      color,
      weight: 1,
      opacity: 0.32,
      fillColor: color,
      fillOpacity: 0.16,
      interactive: false,
    }).addTo(map);

    const marker = L.circleMarker(latLng, {
      radius: Math.min(25, 10 + Math.sqrt(Math.max(1, complaintCount)) * 3),
      color: '#ffffff',
      weight: 3,
      opacity: 0.95,
      fillColor: color,
      fillOpacity: 0.9,
    }).addTo(map);

    marker.bindTooltip(String(complaintCount), {
      permanent: true,
      direction: 'center',
      className: 'civic-leaflet-count',
    });
    marker.bindPopup(createPopupContent(point));
    marker.on('click', () => onSelect?.(point));
  });

  if (bounds.isValid()) {
    map.fitBounds(bounds, { padding: [55, 55], maxZoom: 14 });
  }
}

export default function CivicHeatmapMap({ points = [], config, selectedPoint, onSelect, className = '' }) {
  const containerRef = useRef(null);
  const mapRef = useRef(null);
  const providerRef = useRef('');
  const onSelectRef = useRef(onSelect);
  const [provider, setProvider] = useState('');
  const [mapError, setMapError] = useState('');
  const selectedKey = selectedPoint ? pointKey(selectedPoint) : '';
  useEffect(() => { onSelectRef.current = onSelect; }, [onSelect]);

  const usablePoints = useMemo(
    () => points.filter((point) => Number.isFinite(Number(point.latitude)) && Number.isFinite(Number(point.longitude))),
    [points],
  );

  useEffect(() => {
    if (!containerRef.current || usablePoints.length === 0) return undefined;

    let cancelled = false;
    let activeMap;
    const defaultLatitude = Number(config?.defaultCenter?.latitude ?? 19.0760);
    const defaultLongitude = Number(config?.defaultCenter?.longitude ?? 72.8777);
    const defaultZoom = Number(config?.defaultZoom || 10.5);

    setMapError('');
    setProvider('');

    const startLeaflet = async (reason = '') => {
      const L = await loadLeaflet();
      if (cancelled || !containerRef.current) return;
      activeMap = L.map(containerRef.current, {
        center: [defaultLatitude, defaultLongitude],
        zoom: defaultZoom,
        zoomControl: true,
        attributionControl: true,
      });
      mapRef.current = activeMap;
      providerRef.current = 'OpenStreetMap';
      setProvider('OpenStreetMap');
      if (reason) setMapError(reason);
      addLeafletLayers(activeMap, L, usablePoints, (point) => onSelectRef.current?.(point));
      window.setTimeout(() => activeMap?.invalidateSize(), 0);
    };

    const start = async () => {
      const canUseMapbox = Boolean(config?.enabled && config?.accessToken);
      if (!canUseMapbox) {
        await startLeaflet();
        return;
      }

      try {
        const mapboxgl = await loadMapbox();
        if (cancelled || !mapboxgl || !containerRef.current) return;
        mapboxgl.accessToken = config.accessToken;
        activeMap = new mapboxgl.Map({
          container: containerRef.current,
          style: config.styleUrl || 'mapbox://styles/mapbox/streets-v12',
          center: [defaultLongitude, defaultLatitude],
          zoom: defaultZoom,
          attributionControl: true,
        });
        mapRef.current = activeMap;
        providerRef.current = 'Mapbox';
        setProvider('Mapbox');
        activeMap.addControl(new mapboxgl.NavigationControl({ showCompass: true }), 'top-right');
        activeMap.on('load', () => {
          if (!cancelled) addMapboxLayers(activeMap, mapboxgl, usablePoints, (point) => onSelectRef.current?.(point));
        });
      } catch (error) {
        if (!cancelled) await startLeaflet(error.message || 'Mapbox was unavailable; OpenStreetMap is being used.');
      }
    };

    start().catch((error) => {
      if (!cancelled) setMapError(error.message || 'The street map could not be loaded.');
    });

    return () => {
      cancelled = true;
      if (activeMap) activeMap.remove();
      mapRef.current = null;
      providerRef.current = '';
    };
  }, [config, usablePoints]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map || !selectedPoint) return;
    const latitude = Number(selectedPoint.latitude);
    const longitude = Number(selectedPoint.longitude);

    if (providerRef.current === 'Mapbox') {
      map.flyTo({ center: [longitude, latitude], zoom: Math.max(map.getZoom(), 13), duration: 500 });
      return;
    }

    if (providerRef.current === 'OpenStreetMap') {
      map.flyTo([latitude, longitude], Math.max(map.getZoom(), 13), { duration: 0.5 });
    }
  }, [selectedKey, selectedPoint]);

  if (usablePoints.length === 0) {
    return <div className="heatmap-empty">No complaint locations match these filters.</div>;
  }

  return (
    <div className={`civic-real-map-shell ${className}`.trim()}>
      <div ref={containerRef} className="civic-mapbox civic-real-map" aria-label="Interactive city complaint heatmap on a real street map" />
      {provider && <span className="civic-map-provider">Map data: {provider}</span>}
      {mapError && provider === 'OpenStreetMap' && <p className="heatmap-map-note">{mapError}</p>}
      {mapError && !provider && <div className="heatmap-empty">{mapError}</div>}
    </div>
  );
}
