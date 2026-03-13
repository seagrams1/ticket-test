<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const toast = useToast()

const username = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')

async function handleLogin() {
  if (!username.value || !password.value) {
    error.value = 'Please enter your username and password.'
    return
  }

  loading.value = true
  error.value = ''

  try {
    await auth.login({ username: username.value, password: password.value })
    const redirect = (route.query.redirect as string) || '/'
    router.push(redirect)
  } catch (err: any) {
    error.value =
      err?.response?.data?.message || err?.response?.data || 'Invalid credentials. Please try again.'
    toast.add({ severity: 'error', summary: 'Login Failed', detail: error.value, life: 4000 })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
    <!-- Background decorative elements -->
    <div class="absolute inset-0 overflow-hidden pointer-events-none">
      <div class="absolute -top-40 -right-40 w-96 h-96 rounded-full bg-indigo-600/10 blur-3xl"></div>
      <div class="absolute -bottom-40 -left-40 w-96 h-96 rounded-full bg-violet-600/10 blur-3xl"></div>
    </div>

    <div class="relative w-full max-w-md px-4">
      <!-- Logo / Brand -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-indigo-600 shadow-lg shadow-indigo-500/30 mb-4">
          <i class="pi pi-ticket text-white text-3xl"></i>
        </div>
        <h1 class="text-3xl font-bold text-white tracking-tight">TicketSystem</h1>
        <p class="text-slate-400 mt-1 text-sm">Sign in to your workspace</p>
      </div>

      <!-- Card -->
      <div class="bg-slate-800/60 backdrop-blur-xl border border-slate-700/50 rounded-2xl shadow-2xl p-8">
        <form @submit.prevent="handleLogin" class="space-y-5">
          <!-- Error message -->
          <div v-if="error" class="flex items-center gap-2 p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-sm">
            <i class="pi pi-exclamation-circle flex-shrink-0"></i>
            <span>{{ error }}</span>
          </div>

          <!-- Username field -->
          <div class="space-y-2">
            <label class="block text-sm font-medium text-slate-300">Username</label>
            <div class="relative">
              <span class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
                <i class="pi pi-user text-sm"></i>
              </span>
              <InputText
                v-model="username"
                placeholder="Enter your username"
                class="w-full pl-9 bg-slate-700/50 border-slate-600 text-white placeholder:text-slate-500 focus:border-indigo-500"
                :disabled="loading"
                autocomplete="username"
              />
            </div>
          </div>

          <!-- Password field -->
          <div class="space-y-2">
            <label class="block text-sm font-medium text-slate-300">Password</label>
            <Password
              v-model="password"
              placeholder="Enter your password"
              :feedback="false"
              toggleMask
              class="w-full"
              inputClass="w-full bg-slate-700/50 border-slate-600 text-white placeholder:text-slate-500 focus:border-indigo-500"
              :disabled="loading"
              autocomplete="current-password"
            />
          </div>

          <!-- Submit button -->
          <Button
            type="submit"
            label="Sign In"
            icon="pi pi-sign-in"
            :loading="loading"
            class="w-full bg-indigo-600 hover:bg-indigo-700 border-indigo-600 hover:border-indigo-700 font-semibold py-3 mt-2"
          />
        </form>

        <div class="mt-6 pt-6 border-t border-slate-700/50 text-center text-slate-500 text-xs">
          &copy; {{ new Date().getFullYear() }} TicketSystem. All rights reserved.
        </div>
      </div>
    </div>
  </div>
</template>
