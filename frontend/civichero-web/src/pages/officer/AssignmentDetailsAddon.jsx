import { useParams } from 'react-router-dom';
import ComplaintComments from '../../components/complaints/ComplaintComments.jsx';
import AssignmentDetails from './AssignmentDetails.jsx';
export default function AssignmentDetailsAddon() { const { complaintId } = useParams(); return <><AssignmentDetails /><div className="px-6 pb-10 lg:px-10"><div className="mx-auto max-w-7xl"><ComplaintComments complaintId={complaintId} /></div></div></>; }
