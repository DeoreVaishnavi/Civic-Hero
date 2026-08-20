import { useEffect, useMemo, useRef, useState } from 'react';
import { userApi } from '../../services/userApi.js';

export const AVATAR_UPDATED_EVENT = 'civichero:avatar-updated';

const initialsFor = (fullName = '') => {
  const initials = fullName
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase();

  return initials || 'CH';
};

export default function UserAvatar({ user, className = '', title }) {
  const [source, setSource] = useState('');
  const objectUrl = useRef('');
  const initials = useMemo(() => initialsFor(user?.fullName), [user?.fullName]);

  useEffect(() => {
    let active = true;

    const releaseObjectUrl = () => {
      if (objectUrl.current) {
        URL.revokeObjectURL(objectUrl.current);
        objectUrl.current = '';
      }
    };

    const load = async (cacheKey = Date.now()) => {
      try {
        const blob = await userApi.getAvatar(cacheKey);
        if (!(blob instanceof Blob) || blob.size <= 0 || !blob.type.startsWith('image/')) {
          throw new Error('The avatar response was not a valid image.');
        }
        if (!active) return;
        releaseObjectUrl();
        objectUrl.current = URL.createObjectURL(blob);
        setSource(objectUrl.current);
      } catch {
        if (!active) return;
        releaseObjectUrl();
        setSource('');
      }
    };

    void load();
    const handleAvatarChanged = (event) => { void load(event?.detail?.version || Date.now()); };
    window.addEventListener(AVATAR_UPDATED_EVENT, handleAvatarChanged);

    return () => {
      active = false;
      window.removeEventListener(AVATAR_UPDATED_EVENT, handleAvatarChanged);
      releaseObjectUrl();
    };
  }, [user?.id]);

  const accessibleTitle = title || `${user?.fullName || 'CivicHero user'} profile avatar`;

  return (
    <span className={className} title={accessibleTitle} aria-label={accessibleTitle}>
      {source
        ? <img src={source} alt="" className="user-avatar-image" />
        : <span className="user-avatar-initials" aria-hidden="true">{initials}</span>}
    </span>
  );
}
