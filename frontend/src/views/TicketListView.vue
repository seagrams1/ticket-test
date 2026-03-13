<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import InputText from 'primevue/inputtext'
import Dialog from 'primevue/dialog'
import Textarea from 'primevue/textarea'

const router = useRouter()
const auth = useAuthStore()

const loading = ref(false)
const searchQuery = ref('')
const showCreateDialog = ref(false)

// Placeholder data — replace with real API call
const tickets = ref([
  { id: 1, title: 'Login page not loading', status: 'Open', priority: 'High', createdAt: '2026-03-12', assignee: 'agent1' },
  { id: 2, title: 'Email notifications delayed', status: 'In Progress', priority: 'Medium', createdAt: '2026-03-11', assignee: 'agent2' },
  { id: 3, title: 'Export to CSV broken', status: 'Resolved', priority: 'Low', createdAt: '2026-03-10', assignee: 'agent1' },
])

const newTicket = ref({ title: '', description: '' })

function getStatusSeverity(status: string) {
  switch (status) {
    case 'Open': return 'warn'
    case 'In Progress': return 'info'
    case 'Resolved': return 'success'
    case 'Closed': return 'secondary'
    default: return 'secondary'
  }
}

function getPrioritySeverity(priority: string) {
  switch (priority) {
    case 'High': return 'danger'
    case 'Medium': return 'warn'
    case 'Low': return 'info'
    default: return 'secondary'
  }
}

function viewTicket(ticketId: number) {
  router.push({ name: 'ticket-detail', params: { id: ticketId } })
}

async function createTicket() {
  // TODO: call tickets API
  console.log('Create ticket:', newTicket.value)
  showCreateDialog.value = false
  newTicket.value = { title: '', description: '' }
}

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <!-- Navbar -->
    <header class="bg-slate-800/80 backdrop-blur border-b border-slate-700/50 sticky top-0 z-10">
      <div class="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center">
            <i class="pi pi-ticket text-white text-sm"></i>
          </div>
          <span class="font-bold text-lg tracking-tight">TicketSystem</span>
        </div>
        <nav class="hidden md:flex items-center gap-6 text-sm text-slate-400">
          <router-link to="/" class="hover:text-white transition-colors">Dashboard</router-link>
          <router-link to="/tickets" class="text-white font-medium">Tickets</router-link>
        </nav>
        <div class="flex items-center gap-3">
          <span class="text-slate-400 text-sm hidden sm:block">
            <i class="pi pi-user mr-1"></i>{{ auth.username }}
          </span>
          <Button icon="pi pi-sign-out" severity="secondary" text rounded @click="logout" />
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto px-6 py-10">
      <!-- Page header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h2 class="text-3xl font-bold tracking-tight">Tickets</h2>
          <p class="text-slate-400 mt-1">Manage and track all support tickets</p>
        </div>
        <Button
          label="Create Ticket"
          icon="pi pi-plus"
          class="bg-indigo-600 hover:bg-indigo-700 border-indigo-600 font-semibold"
          @click="showCreateDialog = true"
        />
      </div>

      <!-- Search -->
      <div class="mb-4">
        <div class="relative max-w-sm">
          <span class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
            <i class="pi pi-search text-sm"></i>
          </span>
          <InputText
            v-model="searchQuery"
            placeholder="Search tickets..."
            class="w-full pl-9 bg-slate-800 border-slate-600 text-white placeholder:text-slate-500"
          />
        </div>
      </div>

      <!-- DataTable -->
      <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl overflow-hidden">
        <DataTable
          :value="tickets"
          :loading="loading"
          stripedRows
          class="w-full"
          :pt="{
            root: { class: 'bg-transparent' },
            header: { class: 'bg-slate-700/40 border-b border-slate-700' },
            thead: { class: 'bg-slate-700/40' },
            tbody: { class: 'bg-transparent' },
          }"
        >
          <Column field="id" header="ID" style="width: 4rem">
            <template #body="{ data }">
              <span class="text-slate-400 text-sm font-mono">#{{ data.id }}</span>
            </template>
          </Column>

          <Column field="title" header="Title">
            <template #body="{ data }">
              <button
                class="text-left hover:text-indigo-400 transition-colors font-medium"
                @click="viewTicket(data.id)"
              >
                {{ data.title }}
              </button>
            </template>
          </Column>

          <Column field="status" header="Status" style="width: 8rem">
            <template #body="{ data }">
              <Tag :value="data.status" :severity="getStatusSeverity(data.status)" />
            </template>
          </Column>

          <Column field="priority" header="Priority" style="width: 7rem">
            <template #body="{ data }">
              <Tag :value="data.priority" :severity="getPrioritySeverity(data.priority)" />
            </template>
          </Column>

          <Column field="assignee" header="Assignee" style="width: 9rem">
            <template #body="{ data }">
              <span class="text-slate-400 text-sm">{{ data.assignee }}</span>
            </template>
          </Column>

          <Column field="createdAt" header="Created" style="width: 9rem">
            <template #body="{ data }">
              <span class="text-slate-400 text-sm">{{ data.createdAt }}</span>
            </template>
          </Column>

          <Column header="" style="width: 5rem">
            <template #body="{ data }">
              <Button
                icon="pi pi-eye"
                severity="secondary"
                text
                rounded
                size="small"
                @click="viewTicket(data.id)"
                v-tooltip="'View ticket'"
              />
            </template>
          </Column>

          <template #empty>
            <div class="text-center py-12 text-slate-400">
              <i class="pi pi-inbox text-4xl mb-3 block"></i>
              <p>No tickets found</p>
            </div>
          </template>
        </DataTable>
      </div>
    </main>

    <!-- Create Ticket Dialog -->
    <Dialog
      v-model:visible="showCreateDialog"
      header="Create New Ticket"
      modal
      class="w-full max-w-lg"
      :pt="{ root: { class: 'bg-slate-800 border border-slate-700' } }"
    >
      <div class="space-y-4 py-2">
        <div class="space-y-2">
          <label class="block text-sm font-medium text-slate-300">Title</label>
          <InputText
            v-model="newTicket.title"
            placeholder="Brief description of the issue"
            class="w-full"
          />
        </div>
        <div class="space-y-2">
          <label class="block text-sm font-medium text-slate-300">Description</label>
          <Textarea
            v-model="newTicket.description"
            placeholder="Describe the issue in detail..."
            rows="4"
            class="w-full"
          />
        </div>
      </div>
      <template #footer>
        <div class="flex gap-2 justify-end">
          <Button label="Cancel" severity="secondary" outlined @click="showCreateDialog = false" />
          <Button label="Create Ticket" icon="pi pi-check" @click="createTicket" />
        </div>
      </template>
    </Dialog>
  </div>
</template>
