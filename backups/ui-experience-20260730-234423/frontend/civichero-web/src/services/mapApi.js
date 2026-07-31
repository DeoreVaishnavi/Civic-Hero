import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const mapApi = {
  async config() {
    const response = await axiosInstance.get('/maps/config');
    return unwrap(response);
  },
};
