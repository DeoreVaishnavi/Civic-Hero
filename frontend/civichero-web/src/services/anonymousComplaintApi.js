import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data;

function toFormData(request) {
  const data = new FormData();
  const fields = [
    'title',
    'description',
    'category',
    'departmentId',
    'wardId',
    'latitude',
    'longitude',
    'address',
    'contactEmail',
    'contactPhone',
    'captchaToken',
    'consentToLimitedContactStorage',
    'possibleEmergency',
    'emergencyReason',
  ];

  fields.forEach((key) => data.append(key, request[key] ?? ''));
  (request.images || []).forEach((file) => data.append('images', file, file.name));
  return data;
}

export const anonymousComplaintApi = {
  create: async (request) => unwrap(await axiosInstance.post(
    '/anonymous-complaints',
    toFormData(request),
    {
      timeout: 60000,
      civicTimeoutMessage: 'Anonymous submission is taking longer than expected. Check the backend and storage connection, then retry.',
    },
  )),
  track: async (referenceNumber, trackingToken) => unwrap(await axiosInstance.post(
    '/anonymous-complaints/track',
    { referenceNumber, trackingToken },
  )),
};
