import { useParams } from 'react-router-dom';
import ComplaintComments from '../../components/complaints/ComplaintComments.jsx';
import ManageAssignment from './ManageAssignment.jsx';
export default function ManageAssignmentAddon() { const { complaintId } = useParams(); return <><ManageAssignment /><div className="px-6 pb-10 lg:px-10"><div className="mx-auto max-w-7xl"><ComplaintComments complaintId={complaintId} /></div></div></>; }
