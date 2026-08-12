export const ROLE_CONFIG = {
  Citizen: { name: 'Vaishnavi Patil', initials: 'VP', department: 'Citizen Portal', ward: 'Ward 12' },
  Officer: { name: 'Rahul Deshmukh', initials: 'RD', department: 'Roads & Infrastructure', ward: 'Ward 12' },
  Supervisor: { name: 'Meera Kulkarni', initials: 'MK', department: 'Civic Operations', ward: 'Mumbai Central Zone' },
  Admin: { name: 'System Administrator', initials: 'SA', department: 'Platform Administration', ward: 'All wards' },
};

export const initialComplaints = [
  {
    id: 'CH-2026-1048', title: 'Large pothole near college gate', category: 'Roads',
    description: 'A deep pothole is creating traffic congestion and is unsafe for two-wheelers, especially after rain.',
    location: 'Sion College Road, Ward 12', ward: 'Ward 12', department: 'Roads & Infrastructure',
    status: 'In Progress', priority: 'High', created: '27 Jul 2026', updated: '28 Jul 2026, 8:45 AM',
    reporter: 'Vaishnavi Patil', officer: 'Rahul Deshmukh', votes: 148, sla: '18 hours left',
    image: 'road', latitude: 19.0434, longitude: 72.8619,
    timeline: [
      ['Complaint submitted', 'Citizen', '27 Jul, 9:15 AM'],
      ['Verified and routed to Roads Department', 'CivicHero AI', '27 Jul, 9:18 AM'],
      ['Assigned to Rahul Deshmukh', 'Supervisor', '27 Jul, 10:05 AM'],
      ['Repair team dispatched', 'Officer', '28 Jul, 8:45 AM'],
    ],
  },
  {
    id: 'CH-2026-1031', title: 'Streetlight not working', category: 'Streetlights',
    description: 'Three lights have stopped working, leaving the lane dark after 8 PM.',
    location: 'Tilak Nagar Lane 4, Ward 12', ward: 'Ward 12', department: 'Electrical',
    status: 'Resolved', priority: 'Medium', created: '25 Jul 2026', updated: '27 Jul 2026, 6:20 PM',
    reporter: 'Aarav Shah', officer: 'Sneha Joshi', votes: 65, sla: 'Resolved in 31h', image: 'light',
    latitude: 19.0688, longitude: 72.8972,
    timeline: [
      ['Complaint submitted', 'Citizen', '25 Jul, 11:42 AM'],
      ['Assigned to Electrical Department', 'System', '25 Jul, 11:44 AM'],
      ['Replacement parts installed', 'Officer', '27 Jul, 5:55 PM'],
      ['Resolved with photo proof', 'Officer', '27 Jul, 6:20 PM'],
    ],
  },
  {
    id: 'CH-2026-1022', title: 'Garbage accumulation beside market', category: 'Sanitation',
    description: 'Waste has not been collected for two days and is blocking the footpath.',
    location: 'Central Market, Ward 9', ward: 'Ward 9', department: 'Solid Waste Management',
    status: 'Assigned', priority: 'High', created: '24 Jul 2026', updated: '27 Jul 2026, 4:10 PM',
    reporter: 'Neha Singh', officer: 'Amit More', votes: 92, sla: '6 hours left', image: 'waste',
    latitude: 19.0178, longitude: 72.8478,
    timeline: [
      ['Complaint submitted', 'Citizen', '24 Jul, 7:30 PM'],
      ['Community verification completed', 'Citizens', '25 Jul, 9:05 AM'],
      ['Assigned to Amit More', 'Supervisor', '27 Jul, 4:10 PM'],
    ],
  },
  {
    id: 'CH-2026-1015', title: 'Water leakage from main pipeline', category: 'Water',
    description: 'Continuous leakage is wasting water and damaging the side of the road.',
    location: 'LBS Road Junction, Ward 18', ward: 'Ward 18', department: 'Water Supply',
    status: 'Under Review', priority: 'Critical', created: '23 Jul 2026', updated: '28 Jul 2026, 7:30 AM',
    reporter: 'Ishaan Mehta', officer: 'Unassigned', votes: 211, sla: '2 hours left', image: 'water',
    latitude: 19.1075, longitude: 72.9255,
    timeline: [
      ['Complaint submitted', 'Citizen', '23 Jul, 6:05 AM'],
      ['Marked critical by AI triage', 'CivicHero AI', '23 Jul, 6:06 AM'],
      ['Escalated to supervisor', 'System', '28 Jul, 7:30 AM'],
    ],
  },
  {
    id: 'CH-2026-0998', title: 'Broken footpath tiles', category: 'Footpaths',
    description: 'Several broken tiles are dangerous for senior citizens and school children.',
    location: 'Station East Exit, Ward 12', ward: 'Ward 12', department: 'Roads & Infrastructure',
    status: 'Submitted', priority: 'Low', created: '22 Jul 2026', updated: '22 Jul 2026, 2:12 PM',
    reporter: 'Vaishnavi Patil', officer: 'Unassigned', votes: 34, sla: '42 hours left', image: 'footpath',
    latitude: 19.0759, longitude: 72.8776,
    timeline: [['Complaint submitted', 'Citizen', '22 Jul, 2:12 PM']],
  },
  {
    id: 'CH-2026-0976', title: 'Open drain causing foul smell', category: 'Drainage',
    description: 'The drain cover is missing and the smell is affecting nearby residents.',
    location: 'Ganesh Nagar, Ward 7', ward: 'Ward 7', department: 'Storm Water Drains',
    status: 'In Progress', priority: 'High', created: '20 Jul 2026', updated: '27 Jul 2026, 3:00 PM',
    reporter: 'Kabir Khan', officer: 'Pooja Nair', votes: 117, sla: '14 hours left', image: 'drain',
    latitude: 19.1223, longitude: 72.9062,
    timeline: [
      ['Complaint submitted', 'Citizen', '20 Jul, 10:10 AM'],
      ['Assigned to Pooja Nair', 'Supervisor', '21 Jul, 9:15 AM'],
      ['Safety barricade installed', 'Officer', '27 Jul, 3:00 PM'],
    ],
  },
];

export const users = [
  ['Vaishnavi Patil', 'Citizen', 'Ward 12', 'Active', '8 complaints'],
  ['Rahul Deshmukh', 'Officer', 'Roads · Ward 12', 'Active', '14 assigned'],
  ['Meera Kulkarni', 'Supervisor', 'Central Zone', 'Active', '5 teams'],
  ['Sneha Joshi', 'Officer', 'Electrical · Ward 12', 'Active', '9 assigned'],
  ['Amit More', 'Officer', 'Sanitation · Ward 9', 'Active', '11 assigned'],
  ['Rohan Verma', 'Citizen', 'Ward 18', 'Review', '3 complaints'],
];

export const notifications = [
  ['Repair team dispatched', 'Your complaint CH-2026-1048 is now In Progress.', '8 min ago', 'success'],
  ['148 citizens supported your issue', 'The pothole report is trending in Ward 12.', '32 min ago', 'info'],
  ['Resolution uploaded', 'Streetlight complaint CH-2026-1031 was resolved.', 'Yesterday', 'success'],
  ['SLA warning', 'Water leakage complaint needs assignment within 2 hours.', 'Today, 7:30 AM', 'warning'],
];

export const auditRows = [
  ['08:45:12', 'Rahul Deshmukh', 'Updated complaint status', 'CH-2026-1048', 'Success'],
  ['08:31:04', 'System', 'Refreshed SLA escalation queue', '18 records', 'Success'],
  ['08:15:49', 'Meera Kulkarni', 'Assigned officer', 'CH-2026-1022', 'Success'],
  ['07:58:10', 'Admin', 'Updated user ward scope', 'USR-0082', 'Success'],
  ['07:30:00', 'System', 'Escalated critical complaint', 'CH-2026-1015', 'Warning'],
];

export const monthlyTrend = [42, 58, 51, 76, 83, 94, 88, 112, 126, 118, 142, 156];
export const departmentPerformance = [
  ['Roads', 88, '4.1 h'], ['Electrical', 94, '3.2 h'], ['Sanitation', 81, '5.5 h'], ['Water', 76, '6.8 h'], ['Drainage', 84, '4.9 h'],
];
