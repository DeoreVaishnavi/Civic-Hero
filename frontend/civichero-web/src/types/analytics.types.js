export const ANALYTICS_REPORTS = Object.freeze([
  { key: 'overview', label: 'Executive overview', description: 'Top-level complaints, SLA, satisfaction and user metrics' },
  { key: 'complaints', label: 'Complaint register', description: 'Complaint-level operational export' },
  { key: 'departments', label: 'Department performance', description: 'Resolution, SLA and satisfaction ranking' },
  { key: 'officers', label: 'Officer performance', description: 'Assignments, completion and revisit metrics' },
  { key: 'wards', label: 'Ward analysis', description: 'Density, priority and heat-score data' },
  { key: 'sla', label: 'SLA compliance', description: 'Assignment and resolution compliance summary' },
  { key: 'satisfaction', label: 'Citizen satisfaction', description: 'Ratings, approval and dispute metrics' },
  { key: 'citizen-engagement', label: 'Citizen engagement', description: 'Complaints, supports, comments, verification, rewards, redemptions and initiative activity' },
]);

export const ANALYTICS_EXPORT_FORMATS = Object.freeze([
  { key: 'csv', label: 'CSV', description: 'Universal data file' },
  { key: 'xlsx', label: 'Excel', description: 'Formatted workbook' },
  { key: 'pdf', label: 'PDF', description: 'Printable report' },
]);
