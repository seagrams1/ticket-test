<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ticketsApi, usersApi, type TicketSummary, type AgentDto } from '@/api/tickets'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import InputText from 'primevue/inputtext'
import Dialog from 'primevue/dialog'
import Select from 'primevue/select'
import Paginator from 'primevue/paginator'
import Skeleton from 'primevue/skeleton'
import { useToast } from 'primevue/usetoast'
import { timeAgo } from '@/utils/time'
import AppNav from '@/components/AppNav.vue'

const router = useRouter()
const auth = useAuthStore()
const toast = useToast()

const loading = ref(false)
const searchQuery = ref('')
const selectedStatus = ref<string>('')
const selectedPriority = ref<string>('')
const showAssignDialog = ref(false)
const assigningTicketId = ref<number | null>(null)
const selectedAgentId = ref<number | null>(null)
const assigningLoading = ref(false)

// Pagination
const currentPage = ref(0) // PrimeVue Paginator is 0-indexed
const pageSize = ref(20)
const totalCount = ref(0)

// Debounce timer
let searchDebounce: ReturnType<typeof setTimeout> | null = null

const tickets = ref<TicketSummary[]>([])
const agents = ref<AgentDto[]>([])

const statusOptions = [
  { label: 'All Statuses', value: '' },
  { label: 'Open',        value: 'Open' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Paused',      value: 'Paused' },
  { label: 'Resolved',    value: 'Resolved' },
  { label: 'Unresolved',  value: 'Unresolved' },
]

const priorityOptions = [
  { label: 'All Priorities', value: '' },
  { label: 'Low',      value: 'Low' },
  { label: 'Medium',   value: 'Medium' },
  { label: 'High',     value: 'High' },
  { label: 'Critical', value: 'Critical' },
]

// ─── Lifecycle ─────────────────────────────────────────────────────────────────

onMounted(async () => {
  await loadTickets()
  if (auth.isAdmin) {
    await loadAgents()
  }
})

// Debounced search watcher
watch(searchQuery, () => {
  if (searchDebounce) clearTimeout(searchDebounce)
  searchDebounce = setTimeout(() => {
    currentPage.value = 0
    loadTickets()
  }, 300)
})

// Immediate watchers for filter/pagination
watch([selectedStatus, selectedPriority], () => {
  currentPage.value = 0
  loadTickets()
})

// ─── Data fetching ─────────────────────────────────────────────────────────────

async function loadTickets() {
  loading.value = true
  try {
    const res = await ticketsApi.getAll({
      search: searchQuery.value || undefined,
      status: selectedStatus.value || undefined,
      page: currentPage.value + 1, // API is 1-indexed
      pageSize: pageSize.value,
    })
    tickets.value = res.data.items
    totalCount.value = res.data.totalCount
  } catch {
    // Error handled globally
  } finally {
    loading.value = false
  }
}

async function loadAgents() {
  try {
    const res = await usersApi.getAgents()
    agents.value = res.data
  } catch {
    // Admin only
  }
}

function onPageChange(event: { first: number; rows: number; page: number }) {
  currentPage.value = event.page
  pageSize.value = event.rows
  loadTickets()
}

// ─── Actions ───────────────────────────────────────────────────────────────────

function viewTicket(ticketId: number) {
  router.push({ name: 'ticket-detail', params: { id: ticketId } })
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
  assigningLoading.value = true
  try {
    await ticketsApi.assignTicket(assigningTicketId.value, selectedAgentId.value)
    showAssignDialog.value = false
    await loadTickets()
    toast.add({ severity: 'success', summary: 'Assigned', detail: 'Ticket assigned', life: 3000 })
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to assign ticket'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 3000 })
  } finally {
    assigningLoading.value = false
  }
}

// ─── Helpers ───────────────────────────────────────────────────────────────────

function getStatusSeverity(status: string): string {
  switch (status) {
    case 'Open':       return 'info'
    case 'InProgress': return 'warn'
    case 'Resolved':   return 'success'
    case 'Paused':     return 'secondary'
    case 'Unresolved': return 'danger'
    default:           return 'secondary'
  }
}

function getPrioritySeverity(priority: string): string {
  switch (priority) {
    case 'Low':      return 'secondary'
    case 'Medium':   return 'info'
    case 'High':     return 'warn'
    case 'Critical': return 'danger'
    default:         return 'secondary'
  }
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <AppNav />

    <main class="max-w-7xl mx-auto px-4 sm:px-6 py-8 sm:py-10">
      <!-- Page header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h2 class="text-2xl sm:text-3xl font-bold tracking-tight">Tickets</h2>
          <p class="text-slate-400 mt-1">
            {{ totalCount }} ticket{{ totalCount !== 1 ? 's' : '' }} found
          </p>
        </div>
        <Button
          label="Create Ticket"
          icon="pi pi-plus"
          class="bg-indigo-600 hover:bg-indigo-700 border-indigo-600 font-semibold w-full sm:w-auto"
          @click="router.push({ name: 'create-ticket' })"
        />
      </div>

      <!-- Search + filter bar -->
      <div class="flex flex-col sm:flex-row gap-3 mb-4 flex-wrap">
        <!-- Search -->
        <div class="relative flex-1 min-w-0 sm:max-w-xs">
          <span class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
            <i class="pi pi-search text-sm"></i>
          </span>
          <InputText
            v-model="searchQuery"
            placeholder="Search title or description..."
            class="w-full pl-9 bg-slate-800 border-slate-600 text-white placeholder:text-slate-500"
          />
        </div>

        <!-- Status filter -->
        <Select
          v-model="selectedStatus"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          class="min-w-36"
        />

        <!-- Priority filter -->
        <Select
          v-model="selectedPriority"
          :options="priorityOptions"
          optionLabel="label"
          optionValue="value"
          class="min-w-36"
        />
      </div>

      <!-- Loading skeleton -->
      <template v-if="loading">
        <div class="space-y-3">
          <Skeleton v-for="i in 5" :key="i" height="3.5rem" class="bg-slate-700 rounded-xl" />
        </div>
      </template>

      <!-- DataTable -->
      <template v-else>
        <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl overflow-x-auto">
          <DataTable
            :value="tickets"
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

            <Column field="priority" header="Priority" style="width: 7rem">
              <template #body="{ data }">
                <Tag :value="data.priority" :severity="getPrioritySeverity(data.priority)" />
              </template>
            </Column>

            <Column field="assignedTo" header="Assignee" style="width: 9rem" class="hidden sm:table-cell">
              <template #body="{ data }">
                <span v-if="data.assignedTo" class="text-slate-300 text-sm">{{ data.assignedTo }}</span>
                <span v-else class="text-slate-500 text-sm italic">Unassigned</span>
              </template>
            </Column>

            <Column field="createdBy" header="Created By" style="width: 9rem" class="hidden md:table-cell">
              <template #body="{ data }">
                <span class="text-slate-400 text-sm">{{ data.createdBy }}</span>
              </template>
            </Column>

            <Column field="createdAt" header="Date" style="width: 8rem" class="hidden lg:table-cell">
              <template #body="{ data }">
                <span class="text-slate-400 text-sm" :title="new Date(data.createdAt).toLocaleString()">
                  {{ timeAgo(data.createdAt) }}
                </span>
              </template>
            </Column>

            <!-- Actions column -->
            <Column header="" style="width: 10rem">
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
              <div class="text-center py-16 text-slate-400">
                <i class="pi pi-inbox text-5xl mb-4 block opacity-50"></i>
                <p class="font-medium text-lg">No tickets found</p>
                <p v-if="searchQuery || selectedStatus || selectedPriority" class="text-sm mt-1">
                  Try adjusting your search or filters
                </p>
                <p v-else class="text-sm mt-1">
                  Get started by
                  <button class="text-indigo-400 hover:text-indigo-300 underline" @click="router.push({ name: 'create-ticket' })">creating a ticket</button>
                </p>
              </div>
            </template>
          </DataTable>
        </div>

        <!-- Paginator -->
        <Paginator
          v-if="totalCount > pageSize"
          :first="currentPage * pageSize"
          :rows="pageSize"
          :totalRecords="totalCount"
          :rowsPerPageOptions="[10, 20, 50]"
          class="mt-4 bg-slate-800/40 border border-slate-700/50 rounded-xl"
          @page="onPageChange"
        />
      </template>
    </main>

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
            :loading="assigningLoading"
            @click="confirmAssign"
          />
        </div>
      </template>
    </Dialog>
  </div>
</template>
