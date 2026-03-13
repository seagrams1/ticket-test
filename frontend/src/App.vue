<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { RouterView } from 'vue-router'
import Toast from 'primevue/toast'
import { useToast } from 'primevue/usetoast'
import ConfirmDialog from 'primevue/confirmdialog'

const toast = useToast()

function handleApiError(event: Event) {
  const { message } = (event as CustomEvent).detail
  toast.add({ severity: 'error', summary: 'Error', detail: message, life: 4000 })
}

onMounted(() => {
  window.addEventListener('api-error', handleApiError)
})

onUnmounted(() => {
  window.removeEventListener('api-error', handleApiError)
})
</script>

<template>
  <Toast position="top-right" />
  <ConfirmDialog />
  <RouterView />
</template>
