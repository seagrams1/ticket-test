<script setup lang="ts">
import { ref, reactive, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { authApi } from '@/api/auth'
import Button from 'primevue/button'
import Password from 'primevue/password'
import Divider from 'primevue/divider'
import { useToast } from 'primevue/usetoast'
import AppNav from '@/components/AppNav.vue'

const auth = useAuthStore()
const toast = useToast()

const changingPassword = ref(false)

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const passwordErrors = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

function roleBadgeClass(role: string | null): string {
  switch (role) {
    case 'Admin':     return 'bg-red-500/20 text-red-400 border border-red-500/30'
    case 'Agent':     return 'bg-indigo-500/20 text-indigo-400 border border-indigo-500/30'
    case 'Submitter': return 'bg-slate-500/20 text-slate-400 border border-slate-500/30'
    default:          return 'bg-slate-500/20 text-slate-400 border border-slate-500/30'
  }
}

// Decode JWT to get iat (issued at) as proxy for member since
const memberSince = computed(() => {
  try {
    const token = auth.token
    if (!token) return null
    const payload = JSON.parse(atob(token.split('.')[1]))
    if (payload.iat) {
      return new Date(payload.iat * 1000).toLocaleDateString(undefined, {
        year: 'numeric', month: 'long', day: 'numeric'
      })
    }
  } catch {
    // ignore
  }
  return null
})

function validatePasswordForm(): boolean {
  let valid = true
  passwordErrors.currentPassword = ''
  passwordErrors.newPassword = ''
  passwordErrors.confirmPassword = ''

  if (!passwordForm.currentPassword) {
    passwordErrors.currentPassword = 'Current password is required.'
    valid = false
  }
  if (!passwordForm.newPassword) {
    passwordErrors.newPassword = 'New password is required.'
    valid = false
  } else if (passwordForm.newPassword.length < 6) {
    passwordErrors.newPassword = 'New password must be at least 6 characters.'
    valid = false
  }
  if (!passwordForm.confirmPassword) {
    passwordErrors.confirmPassword = 'Please confirm your new password.'
    valid = false
  } else if (passwordForm.newPassword !== passwordForm.confirmPassword) {
    passwordErrors.confirmPassword = 'Passwords do not match.'
    valid = false
  }

  return valid
}

async function submitChangePassword() {
  if (!validatePasswordForm()) return

  changingPassword.value = true
  try {
    await authApi.changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
    })
    toast.add({ severity: 'success', summary: 'Success', detail: 'Password changed successfully!', life: 3000 })
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmPassword = ''
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to change password'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 4000 })
  } finally {
    changingPassword.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <AppNav />

    <main class="max-w-2xl mx-auto px-4 sm:px-6 py-8 sm:py-10">
      <!-- Breadcrumb -->
      <nav class="flex items-center gap-2 text-sm text-slate-400 mb-6">
        <router-link to="/" class="hover:text-white transition-colors">Dashboard</router-link>
        <i class="pi pi-chevron-right text-xs"></i>
        <span class="text-white font-medium">Profile</span>
      </nav>

      <!-- Profile Card -->
      <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-6 sm:p-8 mb-6">
        <div class="flex items-start gap-5">
          <!-- Avatar -->
          <div class="w-16 h-16 rounded-full bg-indigo-600/30 flex items-center justify-center text-2xl font-bold text-indigo-400 flex-shrink-0">
            {{ auth.username?.charAt(0).toUpperCase() }}
          </div>
          <div class="flex-1 min-w-0">
            <h2 class="text-2xl font-bold">{{ auth.username }}</h2>
            <div class="flex items-center gap-2 mt-2 flex-wrap">
              <span :class="['inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium', roleBadgeClass(auth.role)]">
                {{ auth.role }}
              </span>
              <span v-if="memberSince" class="text-slate-500 text-sm">
                <i class="pi pi-calendar mr-1"></i>Member since {{ memberSince }}
              </span>
            </div>
          </div>
        </div>

        <Divider class="border-slate-700 my-5" />

        <!-- Account info -->
        <div class="space-y-4">
          <div>
            <p class="text-slate-500 text-xs uppercase tracking-wider mb-1">Username</p>
            <p class="font-medium">{{ auth.username }}</p>
          </div>
          <div>
            <p class="text-slate-500 text-xs uppercase tracking-wider mb-1">Role</p>
            <p class="font-medium">{{ auth.role }}</p>
          </div>
          <div>
            <p class="text-slate-500 text-xs uppercase tracking-wider mb-1">User ID</p>
            <p class="font-medium font-mono text-slate-400">#{{ auth.userId }}</p>
          </div>
        </div>
      </div>

      <!-- Change Password Card -->
      <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-6 sm:p-8">
        <h3 class="text-lg font-semibold mb-1">Change Password</h3>
        <p class="text-slate-400 text-sm mb-5">Make sure your new password is at least 6 characters long.</p>

        <form @submit.prevent="submitChangePassword" class="space-y-4">
          <!-- Current Password -->
          <div class="space-y-1.5">
            <label class="block text-sm font-medium">Current Password <span class="text-red-400">*</span></label>
            <Password
              v-model="passwordForm.currentPassword"
              :feedback="false"
              toggleMask
              inputClass="w-full"
              class="w-full"
              placeholder="Enter current password"
              @input="passwordErrors.currentPassword = ''"
            />
            <p v-if="passwordErrors.currentPassword" class="text-red-400 text-xs">
              <i class="pi pi-exclamation-circle mr-1"></i>{{ passwordErrors.currentPassword }}
            </p>
          </div>

          <!-- New Password -->
          <div class="space-y-1.5">
            <label class="block text-sm font-medium">New Password <span class="text-red-400">*</span></label>
            <Password
              v-model="passwordForm.newPassword"
              toggleMask
              inputClass="w-full"
              class="w-full"
              placeholder="At least 6 characters"
              @input="passwordErrors.newPassword = ''"
            />
            <p v-if="passwordErrors.newPassword" class="text-red-400 text-xs">
              <i class="pi pi-exclamation-circle mr-1"></i>{{ passwordErrors.newPassword }}
            </p>
          </div>

          <!-- Confirm Password -->
          <div class="space-y-1.5">
            <label class="block text-sm font-medium">Confirm New Password <span class="text-red-400">*</span></label>
            <Password
              v-model="passwordForm.confirmPassword"
              :feedback="false"
              toggleMask
              inputClass="w-full"
              class="w-full"
              placeholder="Repeat new password"
              @input="passwordErrors.confirmPassword = ''"
            />
            <p v-if="passwordErrors.confirmPassword" class="text-red-400 text-xs">
              <i class="pi pi-exclamation-circle mr-1"></i>{{ passwordErrors.confirmPassword }}
            </p>
          </div>

          <div class="pt-1">
            <Button
              type="submit"
              label="Change Password"
              icon="pi pi-lock"
              :loading="changingPassword"
              class="bg-indigo-600 hover:bg-indigo-700 border-indigo-600 font-semibold"
            />
          </div>
        </form>
      </div>
    </main>
  </div>
</template>
