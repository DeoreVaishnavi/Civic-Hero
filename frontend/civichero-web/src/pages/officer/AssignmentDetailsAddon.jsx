import { useParams } from 'react-router-dom';
import ComplaintComments from '../../components/complaints/ComplaintComments.jsx';
import OfficerPageErrorBoundary from '../../components/common/OfficerPageErrorBoundary.jsx';
import AssignmentDetails from './AssignmentDetails.jsx';

export default function AssignmentDetailsAddon() {
  const { complaintId } = useParams();

  return (
    <OfficerPageErrorBoundary resetKey={complaintId}>
      <AssignmentDetails />
      <div className="page-wrap" style={{ paddingTop: 0 }}>
        <ComplaintComments complaintId={complaintId} />
      </div>
    </OfficerPageErrorBoundary>
  );
}
