import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import ComplaintComments from '../../components/complaints/ComplaintComments.jsx';
import { complaintApi } from '../../services/complaintApi.js';
import ComplaintDetails from './ComplaintDetails.jsx';

export default function ComplaintDetailsAddon() {
  const { id } = useParams();
  const [complaint, setComplaint] = useState(null);
  useEffect(() => { complaintApi.getById(id).then((result) => setComplaint(result?.complaint || null)).catch(() => {}); }, [id]);
  return <>
    <ComplaintDetails />
    <div className="px-6 pb-10 lg:px-10">
      {complaint?.possibleEmergency && <div className="mx-auto mb-6 max-w-6xl rounded-2xl border border-amber-400/30 bg-amber-400/10 p-5 text-amber-100"><strong>Possible emergency review:</strong> {complaint.emergencyReviewStatus || 'Pending'}</div>}
      <div className="mx-auto max-w-6xl"><ComplaintComments complaintId={id} /></div>
    </div>
  </>;
}
