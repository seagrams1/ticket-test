import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type LoginRequest, type RegisterRequest } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const username = ref<string | null>(localStorage.getItem('username'))
  const role = ref<string | null>(localStorage.getItem('role'))
  const userId = ref<number | null>(
    localStorage.getItem('userId') ? parseInt(localStorage.getItem('userId')!) : null
  )

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => role.value === 'Admin')
  const isAgent = computed(() => role.value === 'Agent' || role.value === 'Admin')
  const isSubmitter = computed(() => role.value === 'Submitter')

  // Convenience object combining user identity fields
  const user = computed(() =>
    username.value && userId.value != null
      ? { id: userId.value, username: username.value, role: role.value }
      : null
  )

  function setAuth(data: { token: string; username: string; role: string; userId: number }) {
    token.value = data.token
    username.value = data.username
    role.value = data.role
    userId.value = data.userId

    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
    localStorage.setItem('role', data.role)
    localStorage.setItem('userId', data.userId.toString())
  }

  function clearAuth() {
    token.value = null
    username.value = null
    role.value = null
    userId.value = null

    localStorage.removeItem('token')
    localStorage.removeItem('username')
    localStorage.removeItem('role')
    localStorage.removeItem('userId')
  }

  async function login(credentials: LoginRequest) {
    const response = await authApi.login(credentials)
    setAuth(response.data)
    return response.data
  }

  async function register(data: RegisterRequest) {
    const response = await authApi.register(data)
    setAuth(response.data)
    return response.data
  }

  function logout() {
    clearAuth()
  }

  return {
    token,
    username,
    role,
    userId,
    user,
    isAuthenticated,
    isAdmin,
    isAgent,
    isSubmitter,
    login,
    register,
    logout,
  }
})
