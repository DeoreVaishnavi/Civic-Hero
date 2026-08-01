import { useEffect, useState } from 'react';
import axiosInstance from '../../api/axiosInstance.js';

export default function EvidenceMedia({ image }) {
  const [objectUrl, setObjectUrl] = useState('');
  const publicUrl = image?.publicUrl || '';
  const mimeType = image?.mimeType || '';
  const sourceUrl = publicUrl || objectUrl;

  useEffect(() => {
    if (!image?.downloadPath || publicUrl) return undefined;

    let active = true;
    let createdUrl = '';

    axiosInstance
      .get(image.downloadPath, { responseType: 'blob', timeout: 30000 })
      .then((response) => {
        createdUrl = URL.createObjectURL(response.data);
        if (active) setObjectUrl(createdUrl);
      })
      .catch(() => {});

    return () => {
      active = false;
      if (createdUrl) URL.revokeObjectURL(createdUrl);
    };
  }, [image?.downloadPath, publicUrl]);

  if (!sourceUrl) {
    return (
      <div className="issue-photo" style={{ width: '100%', minHeight: 180 }}>
        Loading evidence…
      </div>
    );
  }

  if (mimeType.startsWith('video/')) {
    return (
      <video
        controls
        preload="metadata"
        className="h-48 w-full rounded-2xl bg-slate-950 object-cover"
        aria-label={image.fileName || 'Complaint evidence video'}
      >
        <source src={sourceUrl} type={mimeType} />
        Your browser does not support this video.
      </video>
    );
  }

  return (
    <img
      src={sourceUrl}
      alt={image.fileName || 'Complaint evidence'}
      className="h-48 w-full rounded-2xl object-cover"
      loading="lazy"
    />
  );
}
