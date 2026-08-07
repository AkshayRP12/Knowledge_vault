import axios from 'axios'

const BASE_URL = 'http://localhost:5000/api'

const api = axios.create({ baseURL: BASE_URL })

api.interceptors.request.use(config => {
  const token = localStorage.getItem('kv_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  res => res,
  err => {
    // Only redirect to /login if 401 happens on non-auth requests
    if (err.response?.status === 401 && !err.config?.url?.includes('/auth/')) {
      localStorage.removeItem('kv_token')
      localStorage.removeItem('kv_user')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

// ─── Auth ───────────────────────────────────────────
export const authApi = {
  login: (data) => api.post('/auth/login', data),
  register: (data) => api.post('/auth/register', data),
}

// ─── Articles ────────────────────────────────────────
export const articlesApi = {
  getAll: (params) => api.get('/articles', { params }),
  getById: (id) => api.get(`/articles/${id}`),
  create: (data) => api.post('/articles', data),
  update: (id, data) => api.put(`/articles/${id}`, data),
  delete: (id) => api.delete(`/articles/${id}`),
  approve: (id, status) => api.patch(`/articles/${id}/approve`, { status }),
  getPending: () => api.get('/articles/pending'),
}

// ─── Categories ──────────────────────────────────────
export const categoriesApi = {
  getAll: () => api.get('/categories'),
  create: (data) => api.post('/categories', data),
  update: (id, data) => api.put(`/categories/${id}`, data),
  delete: (id) => api.delete(`/categories/${id}`),
}

// ─── Comments ────────────────────────────────────────
export const commentsApi = {
  getByArticle: (articleId) => api.get(`/articles/${articleId}/comments`),
  create: (articleId, data) => api.post(`/articles/${articleId}/comments`, data),
  delete: (articleId, commentId) => api.delete(`/articles/${articleId}/comments/${commentId}`),
}

// ─── Likes ───────────────────────────────────────────
export const likesApi = {
  toggle: (articleId) => api.post(`/articles/${articleId}/like`),
}

// ─── Bookmarks ───────────────────────────────────────
export const bookmarksApi = {
  getAll: () => api.get('/bookmarks'),
  toggle: (articleId) => api.post(`/bookmarks/${articleId}`),
}

// ─── Users ───────────────────────────────────────────
export const usersApi = {
  getAll: () => api.get('/users'),
  delete: (id) => api.delete(`/users/${id}`),
}

export default api
