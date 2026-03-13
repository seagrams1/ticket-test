<script setup lang="ts">
import { useRouter } from 'vue-router'
import Button from 'primevue/button'

const router = useRouter()
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white flex items-center justify-center">
    <div class="text-center max-w-md px-6">
      <div class="w-20 h-20 rounded-full bg-red-500/20 flex items-center justify-center mx-auto mb-6">
        <i class="pi pi-ban text-red-400 text-4xl"></i>
      </div>
      <h1 class="text-3xl font-bold mb-3">Access Denied</h1>
      <p class="text-slate-400 mb-8 leading-relaxed">
        You don't have permission to access this resource. If you believe this is a mistake, contact your administrator.
      </p>
      <div class="flex gap-3 justify-center">
        <Button
          label="Go Back"
          icon="pi pi-arrow-left"
          severity="secondary"
          outlined
          @click="router.back()"
        />
        <Button
          label="Dashboard"
          icon="pi pi-home"
          @click="router.push({ name: 'dashboard' })"
        />
      </div>
    </div>
  </div>
</template>
