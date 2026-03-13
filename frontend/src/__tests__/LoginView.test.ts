import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'

// Mock PrimeVue components used in LoginView
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: vi.fn(),
  }),
}))

vi.mock('primevue/inputtext', () => ({
  default: {
    name: 'InputText',
    template: '<input data-test="input-text" v-bind="$attrs" />',
  },
}))

vi.mock('primevue/password', () => ({
  default: {
    name: 'Password',
    template: '<input data-test="password-input" type="password" v-bind="$attrs" />',
  },
}))

vi.mock('primevue/button', () => ({
  default: {
    name: 'Button',
    template: '<button data-test="button" v-bind="$attrs" @click="$emit(\'click\')">{{ label }}</button>',
    props: ['label', 'loading', 'icon'],
  },
}))

vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({
    login: vi.fn().mockResolvedValue(undefined),
    isAuthenticated: false,
  }),
}))

import LoginView from '@/views/LoginView.vue'

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div>Home</div>' } },
      { path: '/login', component: LoginView },
      { path: '/access-denied', name: 'access-denied', component: { template: '<div>Access Denied</div>' } },
    ],
  })
}

describe('LoginView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders the login form with username and password fields', () => {
    const router = createTestRouter()
    const wrapper = mount(LoginView, {
      global: {
        plugins: [router, createPinia()],
        stubs: {
          InputText: { template: '<input data-test="input-text" v-bind="$attrs" />' },
          Password: { template: '<input data-test="password-input" type="password" v-bind="$attrs" />' },
          Button: { template: '<button data-test="button" type="submit">Sign In</button>', props: ['label', 'loading'] },
        },
      },
    })

    expect(wrapper.find('form').exists()).toBe(true)
    expect(wrapper.find('[data-test="input-text"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="password-input"]').exists()).toBe(true)
  })

  it('renders the TicketSystem brand name', () => {
    const router = createTestRouter()
    const wrapper = mount(LoginView, {
      global: {
        plugins: [router, createPinia()],
        stubs: {
          InputText: { template: '<input />' },
          Password: { template: '<input type="password" />' },
          Button: { template: '<button>Sign In</button>', props: ['label', 'loading'] },
        },
      },
    })

    expect(wrapper.text()).toContain('TicketSystem')
  })

  it('shows an error when submitting an empty form', async () => {
    const router = createTestRouter()
    const wrapper = mount(LoginView, {
      global: {
        plugins: [router, createPinia()],
        stubs: {
          InputText: { template: '<input v-bind="$attrs" />' },
          Password: { template: '<input type="password" v-bind="$attrs" />' },
          Button: { template: '<button data-test="submit-btn" type="submit">Sign In</button>', props: ['label', 'loading'] },
        },
      },
    })

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    // Should show validation error
    expect(wrapper.text()).toContain('Please enter')
  })

  it('shows the Sign In button', () => {
    const router = createTestRouter()
    const wrapper = mount(LoginView, {
      global: {
        plugins: [router, createPinia()],
        stubs: {
          InputText: { template: '<input />' },
          Password: { template: '<input type="password" />' },
          Button: { template: '<button data-test="submit-btn">{{ label }}</button>', props: ['label', 'loading'] },
        },
      },
    })

    expect(wrapper.find('[data-test="submit-btn"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Sign In')
  })
})
