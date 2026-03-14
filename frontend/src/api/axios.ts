import axios from 'axios'

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
      // Only redirect to login if this wasn't the login request itself
      const isLoginRequest = error.config?.url?.includes('/auth/login')
      if (!isLoginRequest) {
        localStorage.removeItem('token')
        localStorage.removeItem('username')
        localStorage.removeItem('role')
        localStorage.removeItem('userId')
        // Use router lazily to avoid circular dependency
        import('@/router').then(({ default: router }) => {
          router.push({ name: 'login' })
        })
      }
      return Promise.reject(error)
    }

    if (status === 403) {
      import('@/router').then(({ default: router }) => {
        router.push({ name: 'access-denied' })
      })
      return Promise.reject(error)
    }

    // Show toast for other 4xx/5xx errors
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
