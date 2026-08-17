import api from './axios';

export const authApi = {
  login: (data) => api.post('/auth/login', data),
  register: (data) => api.post('/auth/register', data),
};

export const profileApi = {
  get: () => api.get('/profile'),
  update: (data) => api.put('/profile', data),
  changePassword: (data) => api.put('/profile/password', data),
};

export const usersApi = {
  getAll: (params) => api.get('/users', { params }),
  getById: (id) => api.get(`/users/${id}`),
  create: (data) => api.post('/users', data),
  update: (id, data) => api.put(`/users/${id}`, data),
  delete: (id) => api.delete(`/users/${id}`),
};

export const packagesApi = {
  getAll: (activeOnly = true) => api.get('/packages', { params: { activeOnly } }),
  getById: (id) => api.get(`/packages/${id}`),
  create: (data) => api.post('/packages', data),
  update: (id, data) => api.put(`/packages/${id}`, data),
  delete: (id) => api.delete(`/packages/${id}`),
};

export const bookingsApi = {
  getAll: (params) => api.get('/bookings', { params }),
  getMy: () => api.get('/bookings/my'),
  getById: (id) => api.get(`/bookings/${id}`),
  getAvailableSlots: (packageId, date) =>
    api.get('/bookings/available-slots', { params: { packageId, date } }),
  create: (data) => api.post('/bookings', data),
  updateStatus: (id, status) => api.patch(`/bookings/${id}/status`, { status }),
  cancel: (id) => api.delete(`/bookings/${id}`),
};

export const inventoryApi = {
  getAll: (params) => api.get('/inventory', { params }),
  getAlerts: () => api.get('/inventory/alerts'),
  getById: (id) => api.get(`/inventory/${id}`),
  create: (data) => api.post('/inventory', data),
  update: (id, data) => api.put(`/inventory/${id}`, data),
  delete: (id) => api.delete(`/inventory/${id}`),
};

export const reviewsApi = {
  getByEmployee: (employeeId) => api.get(`/reviews/employee/${employeeId}`),
  getByBooking: (bookingId) => api.get(`/reviews/booking/${bookingId}`),
  create: (data) => api.post('/reviews', data),
  delete: (id) => api.delete(`/reviews/${id}`),
};

export const scheduleApi = {
  getAllTemplates: () => api.get('/schedule/templates'),
  getTemplatesByEmployee: (empId) => api.get(`/schedule/templates/employee/${empId}`),
  createTemplate: (data) => api.post('/schedule/templates', data),
  deleteTemplate: (id) => api.delete(`/schedule/templates/${id}`),
  getAllLeaves: () => api.get('/schedule/leaves'),
  getLeavesByEmployee: (empId) => api.get(`/schedule/leaves/employee/${empId}`),
  createLeave: (data) => api.post('/schedule/leaves', data),
  deleteLeave: (id) => api.delete(`/schedule/leaves/${id}`),
};
