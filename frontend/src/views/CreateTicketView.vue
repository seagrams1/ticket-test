<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ticketsApi } from '@/api/tickets'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import { useToast } from 'primevue/usetoast'
import AppNav from '@/components/AppNav.vue'

const router = useRouter()
const toast = useToast()

const submitting = ref(false)

const form = reactive({
  title: '',
  description: '',
  priority: 'Medium',
})

const errors = reactive({
  title: '',
  description: '',
})

const priorityOptions = [
  { label: '🟢 Low',      value: 'Low' },
  { label: '🔵 Medium',   value: 'Medium' },
  { label: '🟡 High',     value: 'High' },
  { label: '🔴 Critical', value: 'Critical' },
]

function validate(): boolean {
  let valid = true
  errors.title = ''
  errors.description = ''

  if (!form.title.trim()) {
    errors.title = 'Title is required.'
    valid = false
  } else if (form.title.trim().length > 255) {
    errors.title = 'Title must be 255 characters or fewer.'
    valid = false
  }

  if (!form.description.trim()) {
    errors.description = 'Description is required.'
    valid = false
  }

  return valid
}

async function submit() {
  if (!validate()) return

  submitting.value = true
  try {
    const res = await ticketsApi.create({
      title: form.title.trim(),
      description: form.description.trim(),
      priority: form.priority,
    })
    toast.add({ severity: 'success', summary: 'Created', detail: 'Ticket created successfully!', life: 3000 })
    router.push({ name: 'ticket-detail', params: { id: res.data.id } })
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to create ticket'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 4000 })
  } finally {
    submitting.value = false
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
        <router-link to="/tickets" class="hover:text-white transition-colors">Tickets</router-link>
        <i class="pi pi-chevron-right text-xs"></i>
        <span class="text-white font-medium">New Ticket</span>
      </nav>

      <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-6 sm:p-8">
        <div class="mb-6">
          <h2 class="text-2xl font-bold tracking-tight">Create New Ticket</h2>
          <p class="text-slate-400 mt-1 text-sm">Fill out the form below to submit a support ticket.</p>
        </div>

        <form @submit.prevent="submit" class="space-y-5">
          <!-- Title -->
          <div class="space-y-1.5">
            <label class="block text-sm font-medium">
              Title <span class="text-red-400">*</span>
            </label>
            <InputText
              v-model="form.title"
              placeholder="Brief summary of the issue"
              class="w-full"
              :class="{ 'border-red-500': errors.title }"
              @input="errors.title = ''"
            />
            <p v-if="errors.title" class="text-red-400 text-xs mt-1">
              <i class="pi pi-exclamation-circle mr-1"></i>{{ errors.title }}
            </p>
          </div>

          <!-- Priority -->
          <div class="space-y-1.5">
            <label class="block text-sm font-medium">Priority</label>
            <Select
              v-model="form.priority"
              :options="priorityOptions"
              optionLabel="label"
              optionValue="value"
              class="w-full"
            />
          </div>

          <!-- Description -->
          <div class="space-y-1.5">
            <label class="block text-sm font-medium">
              Description <span class="text-red-400">*</span>
            </label>
            <Textarea
              v-model="form.description"
              placeholder="Describe the issue in detail — steps to reproduce, expected vs actual behavior, etc."
              rows="6"
              class="w-full"
              :class="{ 'border-red-500': errors.description }"
              @input="errors.description = ''"
            />
            <p v-if="errors.description" class="text-red-400 text-xs mt-1">
              <i class="pi pi-exclamation-circle mr-1"></i>{{ errors.description }}
            </p>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-3 pt-2">
            <Button
              type="submit"
              label="Create Ticket"
              icon="pi pi-plus"
              :loading="submitting"
              class="bg-indigo-600 hover:bg-indigo-700 border-indigo-600 font-semibold"
            />
            <Button
              type="button"
              label="Cancel"
              severity="secondary"
              outlined
              @click="router.push({ name: 'tickets' })"
            />
          </div>
        </form>
      </div>
    </main>
  </div>
</template>
