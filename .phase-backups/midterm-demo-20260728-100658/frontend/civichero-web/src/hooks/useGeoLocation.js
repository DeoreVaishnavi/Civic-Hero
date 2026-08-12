import { useState } from 'react';

export function useGeoLocation() {
  const [state, setState] = useState({ loading: false, error: '', coordinates: null });

  const locate = () => new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      const message = 'Geolocation is not supported by this browser.';
      setState({ loading: false, error: message, coordinates: null });
      reject(new Error(message));
      return;
    }

    setState((current) => ({ ...current, loading: true, error: '' }));
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const coordinates = {
          latitude: Number(position.coords.latitude.toFixed(7)),
          longitude: Number(position.coords.longitude.toFixed(7)),
          accuracy: Math.round(position.coords.accuracy),
        };
        setState({ loading: false, error: '', coordinates });
        resolve(coordinates);
      },
      (error) => {
        const message = error.code === 1
          ? 'Location permission was denied. Enter the coordinates manually.'
          : 'Unable to obtain your current location.';
        setState({ loading: false, error: message, coordinates: null });
        reject(new Error(message));
      },
      { enableHighAccuracy: true, timeout: 12000, maximumAge: 30000 },
    );
  });

  return { ...state, locate };
}
