<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Button from 'primevue/button'
import AppNav from '@/components/AppNav.vue'

const router = useRouter()
const auth = useAuthStore()
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <AppNav v-if="auth.isAuthenticated" />

    <div class="flex flex-col items-center justify-center min-h-[80vh] px-6 text-center">
      <div class="w-24 h-24 rounded-full bg-slate-800 flex items-center justify-center mb-6">
        <i class="pi pi-search text-4xl text-slate-400"></i>
      </div>
      <h1 class="text-6xl font-black text-slate-700 mb-2">404</h1>
      <h2 class="text-2xl font-bold mb-3">Page Not Found</h2>
      <p class="text-slate-400 max-w-md mb-8">
        The page you're looking for doesn't exist or may have been moved.
      </p>
      <div class="flex flex-wrap gap-3 justify-center">
        <Button
          v-if="auth.isAuthenticated"
          label="Go to Dashboard"
          icon="pi pi-home"
          @click="router.push({ name: 'dashboard' })"
        />
        <Button
          label="Go Back"
          icon="pi pi-arrow-left"
          severity="secondary"
          outlined
          @click="router.back()"
        />
      </div>
    </div>
  </div>
</template>
