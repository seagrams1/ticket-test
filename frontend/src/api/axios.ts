import axios from 'axios'
import router from '@/router'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor — attach JWT token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor — handle errors globally
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status

    if (status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
      return Promise.reject(error)
    }

    if (status === 403) {
      router.push({ name: 'access-denied' })
      return Promise.reject(error)
    }

    // Show toast for other 4xx/5xx errors
    // We use a custom event because we can't use composables outside Vue components
    if (status >= 400) {
      const detail = error.response?.data?.message
        || error.response?.data?.title
        || `Request failed (${status})`
      window.dispatchEvent(new CustomEvent('api-error', { detail: { status, message: detail } }))
    }

    return Promise.reject(error)
  }
)

export default api
