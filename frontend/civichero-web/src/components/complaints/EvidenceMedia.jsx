import { useEffect, useState } from 'react';
import axiosInstance from '../../api/axiosInstance.js';

export default function EvidenceMedia({ image }) {
  const [objectUrl, setObjectUrl] = useState('');
  const publicUrl = image?.publicUrl || '';
  const mimeType = image?.mimeType || '';
  const sourceUrl = publicUrl || objectUrl;
  const isImage = mimeType.startsWith('image/');
  const isVideo = mimeType.startsWith('video/');

  useEffect(() => {
    if (!image?.downloadPath || publicUrl || (!isImage && !isVideo)) return undefined;
    let active = true;
    let createdUrl = '';

    axiosInstance.get(image.downloadPath, { responseType: 'blob', timeout: 30000 })
      .then((response) => {
        createdUrl = URL.createObjectURL(response.data);
        if (active) setObjectUrl(createdUrl);
      })
      .catch(() => {});

    return () => {
      active = false;
      if (createdUrl) URL.revokeObjectURL(createdUrl);
    };
  }, [image?.downloadPath, publicUrl, isImage, isVideo]);

  if (!isImage && !isVideo) {
    return (
      <div className="grid h-48 w-full place-items-center rounded-2xl bg-slate-100 p-4 text-center">
        <div>
          <div className="text-3xl">📄</div>
          <strong className="mt-2 block text-slate-800">Supporting document</strong>
          <p className="mt-1 break-all text-xs text-slate-500">{image?.fileName || mimeType || 'Document evidence'}</p>
        </div>
      </div>
    );
  }

  if (!sourceUrl) {
    return <div className="issue-photo grid min-h-48 w-full place-items-center">Loading evidence…</div>;
  }

  if (isVideo) {
    return (
      <video controls preload="metadata" className="h-48 w-full rounded-2xl bg-slate-950 object-cover" aria-label={image.fileName || 'Complaint evidence video'}>
        <source src={sourceUrl} type={mimeType} />
        Your browser does not support this video.
      </video>
    );
  }

  return <img src={sourceUrl} alt={image.fileName || 'Complaint evidence'} className="h-48 w-full rounded-2xl object-cover" loading="lazy" />;
}
