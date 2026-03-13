import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'

// Mock PrimeVue Badge
vi.mock('primevue/badge', () => ({
  default: {
    name: 'Badge',
    template: '<span data-test="badge">{{ value }}</span>',
    props: ['value', 'severity'],
  },
}))

// Provide a mock auth store for testing
vi.mock('@/stores/auth', () => ({
  useAuthStore: () => mockAuthStore,
}))

let mockAuthStore = {
  username: 'testuser',
  role: 'Agent',
  isAuthenticated: true,
  isAdmin: false,
  isAgent: true,
  isSubmitter: false,
  logout: vi.fn(),
}

import AppNav from '@/components/AppNav.vue'

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'dashboard', component: { template: '<div>' } },
      { path: '/tickets', name: 'tickets', component: { template: '<div>' } },
      { path: '/tickets/new', name: 'create-ticket', component: { template: '<div>' } },
      { path: '/profile', name: 'profile', component: { template: '<div>' } },
      { path: '/login', name: 'login', component: { template: '<div>' } },
    ],
  })
}

describe('AppNav', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mockAuthStore = {
      username: 'testuser',
      role: 'Agent',
      isAuthenticated: true,
      isAdmin: false,
      isAgent: true,
      isSubmitter: false,
      logout: vi.fn(),
    }
  })

  it('renders the navigation bar', () => {
    const router = createTestRouter()
    const wrapper = mount(AppNav, {
      global: { plugins: [router] },
    })
    expect(wrapper.find('nav').exists()).toBe(true)
  })

  it('displays the username when logged in', () => {
    const router = createTestRouter()
    const wrapper = mount(AppNav, {
      global: { plugins: [router] },
    })
    expect(wrapper.text()).toContain('testuser')
  })

  it('displays the role badge', () => {
    const router = createTestRouter()
    const wrapper = mount(AppNav, {
      global: { plugins: [router] },
    })
    // Role should appear somewhere in the nav (as a badge or text)
    expect(wrapper.text()).toContain('Agent')
  })
})
