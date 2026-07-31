import { useEffect, useMemo, useRef, useState } from 'react';

const MAPBOX_VERSION = '3.25.0';
const MAPBOX_SCRIPT = `https://api.mapbox.com/mapbox-gl-js/v${MAPBOX_VERSION}/mapbox-gl.js`;
const MAPBOX_CSS = `https://api.mapbox.com/mapbox-gl-js/v${MAPBOX_VERSION}/mapbox-gl.css`;
let mapboxPromise;

function loadMapbox() {
  if (globalThis.mapboxgl) return Promise.resolve(globalThis.mapboxgl);
  if (mapboxPromise) return mapboxPromise;

  mapboxPromise = new Promise((resolve, reject) => {
    if (!document.querySelector(`link[href="${MAPBOX_CSS}"]`)) {
      const stylesheet = document.createElement('link');
      stylesheet.rel = 'stylesheet';
      stylesheet.href = MAPBOX_CSS;
      document.head.appendChild(stylesheet);
    }

    const existing = document.querySelector(`script[src="${MAPBOX_SCRIPT}"]`);
    if (existing) {
      existing.addEventListener('load', () => resolve(globalThis.mapboxgl), { once: true });
      existing.addEventListener('error', () => reject(new Error('Mapbox library could not be loaded.')), { once: true });
      return;
    }

    const script = document.createElement('script');
    script.src = MAPBOX_SCRIPT;
    script.async = true;
    script.onload = () => resolve(globalThis.mapboxgl);
    script.onerror = () => reject(new Error('Mapbox library could not be loaded.'));
    document.head.appendChild(script);
  });

  return mapboxPromise;
}

function pointKey(point) {
  return `${point.latitude}:${point.longitude}:${point.wardId ?? 'none'}`;
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

export default function CivicHeatmapMap({ points = [], config, selectedPoint, onSelect }) {
  const containerRef = useRef(null);
  const mapRef = useRef(null);
  const [mapError, setMapError] = useState('');
  const selectedKey = selectedPoint ? pointKey(selectedPoint) : '';
  const usablePoints = useMemo(
    () => points.filter((point) => Number.isFinite(Number(point.latitude)) && Number.isFinite(Number(point.longitude))),
    [points],
  );

  useEffect(() => {
    if (!config?.enabled || !config?.accessToken || !containerRef.current || usablePoints.length === 0) return undefined;
    let cancelled = false;
    let map;

    setMapError('');
    loadMapbox()
      .then((mapboxgl) => {
        if (cancelled || !mapboxgl || !containerRef.current) return;
        mapboxgl.accessToken = config.accessToken;

        const defaultCenter = [
          Number(config.defaultCenter?.longitude ?? 72.8777),
          Number(config.defaultCenter?.latitude ?? 19.0760),
        ];
        map = new mapboxgl.Map({
          container: containerRef.current,
          style: config.styleUrl || 'mapbox://styles/mapbox/streets-v12',
          center: defaultCenter,
          zoom: Number(config.defaultZoom || 10.5),
          attributionControl: true,
        });
        mapRef.current = map;
        map.addControl(new mapboxgl.NavigationControl({ showCompass: true }), 'top-right');

        map.on('load', () => {
          if (cancelled) return;
          const data = toGeoJson(usablePoints);
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
                'Critical', '#b91c1c',
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
          usablePoints.forEach((point) => bounds.extend([Number(point.longitude), Number(point.latitude)]));
          if (!bounds.isEmpty()) {
            map.fitBounds(bounds, { padding: 70, maxZoom: 14, duration: 600 });
          }

          const choosePoint = (event) => {
            const feature = event.features?.[0];
            const index = Number(feature?.properties?.pointIndex);
            if (Number.isInteger(index) && usablePoints[index]) onSelect?.(usablePoints[index]);
          };
          map.on('click', 'heatmap-circles', choosePoint);
          map.on('mouseenter', 'heatmap-circles', () => { map.getCanvas().style.cursor = 'pointer'; });
          map.on('mouseleave', 'heatmap-circles', () => { map.getCanvas().style.cursor = ''; });
        });

        map.on('error', (event) => {
          if (!cancelled) setMapError(event?.error?.message || 'The street map could not be displayed.');
        });
      })
      .catch((error) => {
        if (!cancelled) setMapError(error.message || 'The street map could not be loaded.');
      });

    return () => {
      cancelled = true;
      if (map) map.remove();
      mapRef.current = null;
    };
  }, [config, usablePoints, onSelect]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map || !selectedPoint) return;
    map.flyTo({ center: [Number(selectedPoint.longitude), Number(selectedPoint.latitude)], zoom: Math.max(map.getZoom(), 13), duration: 500 });
  }, [selectedKey, selectedPoint]);

  useEffect(() => {
    if (!mapError || !mapRef.current) return;
    mapRef.current.remove();
    mapRef.current = null;
  }, [mapError]);

  if (usablePoints.length === 0) {
    return <div className="heatmap-empty">No complaint locations match these filters.</div>;
  }

  if (!config?.enabled || !config?.accessToken || mapError) {
    return (
      <div>
        <FallbackMap points={usablePoints} selectedPoint={selectedPoint} onSelect={onSelect} />
        <p className="heatmap-map-note">
          {mapError || 'Mapbox token is not configured. Live complaint data is shown in the built-in geographic view.'}
        </p>
      </div>
    );
  }

  return <div ref={containerRef} className="civic-mapbox" aria-label="Interactive city complaint heatmap" />;
}

function FallbackMap({ points, selectedPoint, onSelect }) {
  const latitudes = points.map((point) => Number(point.latitude));
  const longitudes = points.map((point) => Number(point.longitude));
  const minLat = Math.min(...latitudes);
  const maxLat = Math.max(...latitudes);
  const minLon = Math.min(...longitudes);
  const maxLon = Math.max(...longitudes);
  const position = (value, min, max) => (max === min ? 50 : 7 + ((value - min) * 86) / (max - min));

  return (
    <div className="heatmap-fallback" role="img" aria-label="Complaint locations plotted by latitude and longitude">
      <div className="heatmap-fallback-grid" />
      {points.map((point) => {
        const selected = selectedPoint && pointKey(point) === pointKey(selectedPoint);
        return (
          <button
            key={pointKey(point)}
            type="button"
            className={`heatmap-fallback-point risk-${String(point.riskLevel || 'low').toLowerCase()}${selected ? ' selected' : ''}`}
            style={{
              left: `${position(Number(point.longitude), minLon, maxLon)}%`,
              bottom: `${position(Number(point.latitude), minLat, maxLat)}%`,
              '--point-size': `${Math.min(54, 22 + Number(point.complaintCount || 0) * 2)}px`,
            }}
            onClick={() => onSelect?.(point)}
            title={`${point.wardName || 'Area'}: ${point.complaintCount} complaints, ${point.solvedCount} solved`}
          >
            {point.complaintCount}
          </button>
        );
      })}
      <span className="heatmap-axis north">N</span>
      <span className="heatmap-axis south">S</span>
      <span className="heatmap-axis east">E</span>
      <span className="heatmap-axis west">W</span>
    </div>
  );
}
