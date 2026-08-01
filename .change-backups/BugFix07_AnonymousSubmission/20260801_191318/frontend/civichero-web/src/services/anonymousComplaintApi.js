import axiosInstance from '../api/axiosInstance.js';
const unwrap = (response) => response.data?.data;
function formData(request) {
  const data = new FormData();
  ['title','description','category','departmentId','wardId','latitude','longitude','address','contactEmail','contactPhone','captchaToken','consentToLimitedContactStorage','possibleEmergency','emergencyReason']
    .forEach((key) => data.append(key, request[key] ?? ''));
  (request.images || []).forEach((file) => data.append('images', file));
  return data;
}
export const anonymousComplaintApi = {
  create: async (request) => unwrap(await axiosInstance.post('/anonymous-complaints', formData(request), { headers: { 'Content-Type': 'multipart/form-data' }, timeout: 45000 })),
  track: async (referenceNumber, trackingToken) => unwrap(await axiosInstance.post('/anonymous-complaints/track', { referenceNumber, trackingToken })),
};
