<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ticketsApi, usersApi, type TicketSummary, type AgentDto } from '@/api/tickets'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import InputText from 'primevue/inputtext'
import Dialog from 'primevue/dialog'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import { useToast } from 'primevue/usetoast'
import Toast from 'primevue/toast'

const router = useRouter()
const auth = useAuthStore()
const toast = useToast()

const loading = ref(false)
const searchQuery = ref('')
const showCreateDialog = ref(false)
const showAssignDialog = ref(false)
const assigningTicketId = ref<number | null>(null)
const selectedAgentId = ref<number | null>(null)
const activeFilter = ref<'all' | 'mine' | 'unassigned'>('all')

const tickets = ref<TicketSummary[]>([])
const agents = ref<AgentDto[]>([])
const newTicket = ref({ title: '', description: '' })
const creatingTicket = ref(false)

// ─── Computed ─────────────────────────────────────────────────────────────────

const filteredTickets = computed(() => {
  let list: TicketSummary[] = tickets.value

  // Apply search
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter((t: TicketSummary) =>
      t.title.toLowerCase().includes(q) ||
      t.status.toLowerCase().includes(q) ||
      t.createdBy.toLowerCase().includes(q)
    )
  }

  // Apply tab filter
  if (activeFilter.value === 'mine') {
    list = list.filter((t: TicketSummary) => t.assignedToId === auth.userId || t.createdById === auth.userId)
  } else if (activeFilter.value === 'unassigned') {
    list = list.filter((t: TicketSummary) => t.assignedToId === null)
  }

  return list
})

// Tabs available per role
const filterTabs = computed(() => {
  const tabs = [{ key: 'all', label: 'All' }]
  if (auth.isAgent || auth.isAdmin) {
    tabs.push({ key: 'mine', label: auth.isAdmin ? 'My Assignments' : 'Assigned to Me' })
    tabs.push({ key: 'unassigned', label: 'Unassigned' })
  }
  return tabs
})

// ─── Lifecycle ────────────────────────────────────────────────────────────────

onMounted(async () => {
  await loadTickets()
  if (auth.isAdmin) {
    await loadAgents()
  }
})

// ─── Data fetching ────────────────────────────────────────────────────────────

async function loadTickets() {
  loading.value = true
  try {
    const res = await ticketsApi.getAll()
    tickets.value = res.data
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load tickets', life: 3000 })
  } finally {
    loading.value = false
  }
}

async function loadAgents() {
  try {
    const res = await usersApi.getAgents()
    agents.value = res.data
  } catch {
    // Admin only — ignore if not admin
  }
}

// ─── Actions ──────────────────────────────────────────────────────────────────

function viewTicket(ticketId: number) {
  router.push({ name: 'ticket-detail', params: { id: ticketId } })
}

async function createTicket() {
  if (!newTicket.value.title.trim()) return
  creatingTicket.value = true
  try {
    const res = await ticketsApi.create({
      title: newTicket.value.title.trim(),
      description: newTicket.value.description,
    })
    tickets.value.unshift(res.data as unknown as TicketSummary)
    showCreateDialog.value = false
    newTicket.value = { title: '', description: '' }
    toast.add({ severity: 'success', summary: 'Created', detail: 'Ticket created successfully', life: 3000 })
    router.push({ name: 'ticket-detail', params: { id: res.data.id } })
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to create ticket', life: 3000 })
  } finally {
    creatingTicket.value = false
  }
}

async function assignToMe(ticketId: number) {
  try {
    await ticketsApi.assignTicket(ticketId)
    await loadTickets()
    toast.add({ severity: 'success', summary: 'Assigned', detail: 'Ticket assigned to you', life: 3000 })
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to assign ticket'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 3000 })
  }
}

function openAssignDialog(ticketId: number) {
  assigningTicketId.value = ticketId
  selectedAgentId.value = null
  showAssignDialog.value = true
}

async function confirmAssign() {
  if (!assigningTicketId.value || !selectedAgentId.value) return
  try {
    await ticketsApi.assignTicket(assigningTicketId.value, selectedAgentId.value)
    showAssignDialog.value = false
    await loadTickets()
    toast.add({ severity: 'success', summary: 'Assigned', detail: 'Ticket assigned', life: 3000 })
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to assign ticket'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 3000 })
  }
}

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getStatusSeverity(status: string): string {
  switch (status) {
    case 'Open':       return 'warn'
    case 'InProgress': return 'info'
    case 'Resolved':   return 'success'
    case 'Paused':     return 'secondary'
    case 'Unresolved': return 'danger'
    default:           return 'secondary'
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString()
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <Toast />

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
            <span v-if="auth.role" class="ml-1 text-xs text-indigo-400">({{ auth.role }})</span>
          </span>
          <Button icon="pi pi-sign-out" severity="secondary" text rounded @click="logout" />
        </div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto px-6 py-10">
      <!-- Page header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h2 class="text-3xl font-bold tracking-tight">Tickets</h2>
          <p class="text-slate-400 mt-1">Manage and track support tickets</p>
        </div>
        <!-- All roles can create tickets -->
        <Button
          label="Create Ticket"
          icon="pi pi-plus"
          class="bg-indigo-600 hover:bg-indigo-700 border-indigo-600 font-semibold"
          @click="showCreateDialog = true"
        />
      </div>

      <!-- Filter tabs -->
      <div v-if="filterTabs.length > 1" class="flex gap-1 mb-4 bg-slate-800/40 border border-slate-700/50 rounded-xl p-1 w-fit">
        <button
          v-for="tab in filterTabs"
          :key="tab.key"
          @click="activeFilter = tab.key as any"
          :class="[
            'px-4 py-2 rounded-lg text-sm font-medium transition-colors',
            activeFilter === tab.key
              ? 'bg-indigo-600 text-white'
              : 'text-slate-400 hover:text-white'
          ]"
        >
          {{ tab.label }}
        </button>
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
          :value="filteredTickets"
          :loading="loading"
          stripedRows
          class="w-full"
          :pt="{
            root: { class: 'bg-transparent' },
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

          <Column field="assignedTo" header="Assignee" style="width: 10rem">
            <template #body="{ data }">
              <span v-if="data.assignedTo" class="text-slate-300 text-sm">{{ data.assignedTo }}</span>
              <span v-else class="text-slate-500 text-sm italic">Unassigned</span>
            </template>
          </Column>

          <Column field="createdBy" header="Created By" style="width: 9rem">
            <template #body="{ data }">
              <span class="text-slate-400 text-sm">{{ data.createdBy }}</span>
            </template>
          </Column>

          <Column field="createdAt" header="Date" style="width: 8rem">
            <template #body="{ data }">
              <span class="text-slate-400 text-sm">{{ formatDate(data.createdAt) }}</span>
            </template>
          </Column>

          <!-- Actions column -->
          <Column header="" style="width: 12rem">
            <template #body="{ data }">
              <div class="flex items-center gap-1">
                <Button
                  icon="pi pi-eye"
                  severity="secondary"
                  text rounded size="small"
                  @click="viewTicket(data.id)"
                  v-tooltip="'View'"
                />
                <!-- Agent: Assign to me (unassigned only) -->
                <Button
                  v-if="auth.isAgent && !auth.isAdmin && data.assignedToId === null"
                  label="Assign to me"
                  icon="pi pi-user-plus"
                  severity="info"
                  text
                  size="small"
                  @click="assignToMe(data.id)"
                />
                <!-- Admin: Assign button -->
                <Button
                  v-if="auth.isAdmin && data.assignedToId === null"
                  label="Assign"
                  icon="pi pi-user-plus"
                  severity="info"
                  text
                  size="small"
                  @click="openAssignDialog(data.id)"
                />
              </div>
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
    >
      <div class="space-y-4 py-2">
        <div class="space-y-2">
          <label class="block text-sm font-medium">Title</label>
          <InputText
            v-model="newTicket.title"
            placeholder="Brief description of the issue"
            class="w-full"
          />
        </div>
        <div class="space-y-2">
          <label class="block text-sm font-medium">Description</label>
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
          <Button
            label="Create Ticket"
            icon="pi pi-check"
            :loading="creatingTicket"
            :disabled="!newTicket.title.trim()"
            @click="createTicket"
          />
        </div>
      </template>
    </Dialog>

    <!-- Admin Assign Dialog -->
    <Dialog
      v-model:visible="showAssignDialog"
      header="Assign Ticket"
      modal
      class="w-full max-w-sm"
    >
      <div class="space-y-3 py-2">
        <label class="block text-sm font-medium">Select Agent</label>
        <Select
          v-model="selectedAgentId"
          :options="agents"
          optionLabel="username"
          optionValue="id"
          placeholder="Choose an agent..."
          class="w-full"
        />
      </div>
      <template #footer>
        <div class="flex gap-2 justify-end">
          <Button label="Cancel" severity="secondary" outlined @click="showAssignDialog = false" />
          <Button
            label="Assign"
            icon="pi pi-check"
            :disabled="!selectedAgentId"
            @click="confirmAssign"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
