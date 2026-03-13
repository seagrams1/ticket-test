import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'

// Mock the auth API module so we don't make real HTTP requests
vi.mock('@/api/auth', () => ({
  authApi: {
    login: vi.fn(),
    register: vi.fn(),
    changePassword: vi.fn(),
  },
}))

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: (key: string) => store[key] ?? null,
    setItem: (key: string, value: string) => { store[key] = value },
    removeItem: (key: string) => { delete store[key] },
    clear: () => { store = {} },
  }
})()

Object.defineProperty(global, 'localStorage', { value: localStorageMock, writable: true })

describe('useAuthStore', () => {
  beforeEach(() => {
    localStorageMock.clear()
    setActivePinia(createPinia())
  })

  // ─── Initial state ──────────────────────────────────────────────────────────

  it('starts unauthenticated when localStorage is empty', () => {
    const auth = useAuthStore()
    expect(auth.isAuthenticated).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.username).toBeNull()
    expect(auth.role).toBeNull()
    expect(auth.userId).toBeNull()
    expect(auth.user).toBeNull()
  })

  it('restores auth state from localStorage on init', () => {
    localStorageMock.setItem('token', 'existing-token')
    localStorageMock.setItem('username', 'testuser')
    localStorageMock.setItem('role', 'Agent')
    localStorageMock.setItem('userId', '42')

    // Re-create store after setting localStorage
    const auth = useAuthStore()
    expect(auth.isAuthenticated).toBe(true)
    expect(auth.token).toBe('existing-token')
    expect(auth.username).toBe('testuser')
    expect(auth.role).toBe('Agent')
    expect(auth.userId).toBe(42)
  })

  // ─── login() ───────────────────────────────────────────────────────────────

  it('login() sets token and user data from API response', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 'jwt-token', username: 'alice', role: 'Admin', userId: 1 },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'alice', password: 'pass' })

    expect(auth.token).toBe('jwt-token')
    expect(auth.username).toBe('alice')
    expect(auth.role).toBe('Admin')
    expect(auth.userId).toBe(1)
    expect(auth.isAuthenticated).toBe(true)
  })

  it('login() persists token to localStorage', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 'stored-token', username: 'bob', role: 'Agent', userId: 2 },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'bob', password: 'pass' })

    expect(localStorageMock.getItem('token')).toBe('stored-token')
    expect(localStorageMock.getItem('username')).toBe('bob')
    expect(localStorageMock.getItem('role')).toBe('Agent')
    expect(localStorageMock.getItem('userId')).toBe('2')
  })

  // ─── logout() ──────────────────────────────────────────────────────────────

  it('logout() clears all auth state', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 'temp-token', username: 'carol', role: 'Submitter', userId: 3 },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'carol', password: 'pass' })
    expect(auth.isAuthenticated).toBe(true)

    auth.logout()

    expect(auth.isAuthenticated).toBe(false)
    expect(auth.token).toBeNull()
    expect(auth.username).toBeNull()
    expect(auth.role).toBeNull()
    expect(auth.userId).toBeNull()
    expect(auth.user).toBeNull()
  })

  it('logout() removes all keys from localStorage', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 'temp', username: 'dave', role: 'Agent', userId: 4 },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'dave', password: 'pass' })
    auth.logout()

    expect(localStorageMock.getItem('token')).toBeNull()
    expect(localStorageMock.getItem('username')).toBeNull()
    expect(localStorageMock.getItem('role')).toBeNull()
    expect(localStorageMock.getItem('userId')).toBeNull()
  })

  // ─── Role computed properties ───────────────────────────────────────────────

  it('isAdmin is true only for Admin role', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 't', username: 'admin', role: 'Admin', userId: 1 },
      status: 200, statusText: 'OK', headers: {}, config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'admin', password: 'pass' })

    expect(auth.isAdmin).toBe(true)
    expect(auth.isAgent).toBe(true)   // Admin also has agent-level access
    expect(auth.isSubmitter).toBe(false)
  })

  it('isAgent is true for Agent role (but not admin-only features)', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 't', username: 'agent', role: 'Agent', userId: 2 },
      status: 200, statusText: 'OK', headers: {}, config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'agent', password: 'pass' })

    expect(auth.isAgent).toBe(true)
    expect(auth.isAdmin).toBe(false)
    expect(auth.isSubmitter).toBe(false)
  })

  it('isSubmitter is true only for Submitter role', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 't', username: 'sub', role: 'Submitter', userId: 3 },
      status: 200, statusText: 'OK', headers: {}, config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'sub', password: 'pass' })

    expect(auth.isSubmitter).toBe(true)
    expect(auth.isAdmin).toBe(false)
    expect(auth.isAgent).toBe(false)
  })

  // ─── user computed ──────────────────────────────────────────────────────────

  it('user computed returns combined identity object when logged in', async () => {
    const { authApi } = await import('@/api/auth')
    vi.mocked(authApi.login).mockResolvedValueOnce({
      data: { token: 'tok', username: 'eve', role: 'Agent', userId: 99 },
      status: 200, statusText: 'OK', headers: {}, config: {} as any,
    })

    const auth = useAuthStore()
    await auth.login({ username: 'eve', password: 'pass' })

    expect(auth.user).toEqual({ id: 99, username: 'eve', role: 'Agent' })
  })

  it('user computed returns null when not logged in', () => {
    const auth = useAuthStore()
    expect(auth.user).toBeNull()
  })
})
