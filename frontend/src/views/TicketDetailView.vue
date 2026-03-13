<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ticketsApi, usersApi, type TicketDetail, type AgentDto } from '@/api/tickets'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import Divider from 'primevue/divider'
import Skeleton from 'primevue/skeleton'
import Select from 'primevue/select'
import { useToast } from 'primevue/usetoast'
import Toast from 'primevue/toast'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const toast = useToast()

const loading = ref(true)
const ticketId = Number(route.params.id)

const ticket = ref<TicketDetail | null>(null)
const agents = ref<AgentDto[]>([])

const newComment = ref('')
const submittingComment = ref(false)

// Edit state for status/assignee (agents & admins only)
const editingStatus = ref(false)
const selectedStatus = ref('')
const selectedAssigneeId = ref<number | null>(null)
const saving = ref(false)

// ─── Status options ────────────────────────────────────────────────────────────

const statusOptions = [
  { label: 'Open',        value: 'Open' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Paused',      value: 'Paused' },
  { label: 'Resolved',    value: 'Resolved' },
  { label: 'Unresolved',  value: 'Unresolved' },
]

// ─── Computed permissions ──────────────────────────────────────────────────────

const isSubmitter = computed(() => auth.role === 'Submitter')
const isAgent     = computed(() => auth.role === 'Agent')
const isAdmin     = computed(() => auth.role === 'Admin')

const isAssignedToMe = computed(() => ticket.value?.assignedToId === auth.userId)
const isUnassigned   = computed(() => ticket.value?.assignedToId === null)

// Agent can edit status if ticket is assigned to them
const canEditStatus = computed(() =>
  isAdmin.value || (isAgent.value && isAssignedToMe.value)
)

// (Assignment is rendered conditionally via isAgent/isAdmin + isUnassigned in the template)

// ─── Lifecycle ─────────────────────────────────────────────────────────────────

onMounted(async () => {
  await loadTicket()
  if (isAdmin.value) {
    await loadAgents()
  }
})

// ─── Data fetching ─────────────────────────────────────────────────────────────

async function loadTicket() {
  loading.value = true
  try {
    const res = await ticketsApi.getById(ticketId)
    ticket.value = res.data
    selectedStatus.value = res.data.status
    selectedAssigneeId.value = res.data.assignedToId ?? null
  } catch (err: any) {
    if (err?.response?.status === 403) {
      toast.add({ severity: 'error', summary: 'Access Denied', detail: 'You do not have permission to view this ticket', life: 4000 })
      router.push({ name: 'tickets' })
    } else {
      toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load ticket', life: 3000 })
    }
  } finally {
    loading.value = false
  }
}

async function loadAgents() {
  try {
    const res = await usersApi.getAgents()
    agents.value = res.data
  } catch {
    // ignore
  }
}

// ─── Actions ───────────────────────────────────────────────────────────────────

async function saveChanges() {
  if (!ticket.value) return
  saving.value = true
  try {
    const res = await ticketsApi.update(ticketId, {
      status: selectedStatus.value !== ticket.value.status ? selectedStatus.value : undefined,
      assignedToId: isAdmin.value && selectedAssigneeId.value !== ticket.value.assignedToId
        ? selectedAssigneeId.value
        : undefined,
    })
    ticket.value = res.data
    editingStatus.value = false
    toast.add({ severity: 'success', summary: 'Saved', detail: 'Ticket updated', life: 3000 })
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to update ticket'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 3000 })
  } finally {
    saving.value = false
  }
}

async function assignToMe() {
  try {
    const res = await ticketsApi.assignTicket(ticketId)
    ticket.value = res.data
    toast.add({ severity: 'success', summary: 'Assigned', detail: 'Ticket assigned to you', life: 3000 })
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to assign ticket'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 3000 })
  }
}

async function assignToAgent() {
  if (!selectedAssigneeId.value) return
  try {
    const res = await ticketsApi.assignTicket(ticketId, selectedAssigneeId.value)
    ticket.value = res.data
    toast.add({ severity: 'success', summary: 'Assigned', detail: 'Ticket assigned', life: 3000 })
  } catch (err: any) {
    const msg = err?.response?.data?.message || 'Failed to assign ticket'
    toast.add({ severity: 'error', summary: 'Error', detail: msg, life: 3000 })
  }
}

async function addComment() {
  if (!newComment.value.trim()) return
  submittingComment.value = true
  try {
    await ticketsApi.addComment(ticketId, newComment.value.trim())
    newComment.value = ''
    // Reload to get fresh comments + history
    await loadTicket()
    toast.add({ severity: 'success', summary: 'Posted', detail: 'Comment added', life: 2000 })
  } catch {
    toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to post comment', life: 3000 })
  } finally {
    submittingComment.value = false
  }
}

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}

// ─── Helpers ───────────────────────────────────────────────────────────────────

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
  return new Date(iso).toLocaleString()
}

function formatHistoryEntry(entry: { fieldChanged: string; oldValue: string | null; newValue: string | null }) {
  const field = entry.fieldChanged
  if (field === 'AssignedToId') {
    if (!entry.oldValue && entry.newValue) return `Assigned (agent ID ${entry.newValue})`
    if (entry.oldValue && !entry.newValue) return `Unassigned (was ID ${entry.oldValue})`
    return `Reassigned from ID ${entry.oldValue} to ID ${entry.newValue}`
  }
  if (field === 'Status') return `Status: ${entry.oldValue} → ${entry.newValue}`
  if (field === 'Title')  return `Title updated`
  if (field === 'Description') return `Description updated`
  return `${field} changed`
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
          <router-link to="/tickets" class="hover:text-white transition-colors">Tickets</router-link>
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

    <main class="max-w-5xl mx-auto px-6 py-10">
      <!-- Back button -->
      <Button
        icon="pi pi-arrow-left"
        label="Back to Tickets"
        text severity="secondary"
        class="mb-6"
        @click="router.push({ name: 'tickets' })"
      />

      <!-- Loading skeleton -->
      <template v-if="loading">
        <div class="space-y-4">
          <Skeleton height="2rem" class="bg-slate-700" />
          <Skeleton height="1rem" width="60%" class="bg-slate-700" />
          <Skeleton height="8rem" class="bg-slate-700" />
        </div>
      </template>

      <template v-else-if="ticket">
        <!-- Ticket header card -->
        <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-6 mb-6">
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 text-slate-400 text-sm mb-2">
                <span class="font-mono">#{{ ticket.id }}</span>
                <span>·</span>
                <span>Created {{ formatDate(ticket.createdAt) }}</span>
              </div>
              <h2 class="text-2xl font-bold leading-tight">{{ ticket.title }}</h2>
            </div>
            <Tag :value="ticket.status" :severity="getStatusSeverity(ticket.status)" />
          </div>

          <Divider class="border-slate-700 my-4" />

          <!-- Meta info -->
          <div class="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm mb-4">
            <div>
              <p class="text-slate-500 mb-1">Created by</p>
              <p class="font-medium">{{ ticket.createdBy }}</p>
            </div>
            <div>
              <p class="text-slate-500 mb-1">Assignee</p>
              <p class="font-medium">{{ ticket.assignedTo || 'Unassigned' }}</p>
            </div>
            <div>
              <p class="text-slate-500 mb-1">Created</p>
              <p class="font-medium">{{ formatDate(ticket.createdAt) }}</p>
            </div>
            <div>
              <p class="text-slate-500 mb-1">Last updated</p>
              <p class="font-medium">{{ formatDate(ticket.updatedAt) }}</p>
            </div>
          </div>

          <Divider class="border-slate-700 my-4" />

          <!-- Description -->
          <div class="mb-4">
            <h3 class="text-sm font-semibold text-slate-400 uppercase tracking-wider mb-3">Description</h3>
            <p class="text-slate-300 leading-relaxed whitespace-pre-wrap">{{ ticket.description || '(no description)' }}</p>
          </div>

          <!-- ── Role-based action area ── -->
          <div v-if="!isSubmitter" class="pt-4 border-t border-slate-700">

            <!-- Agent: Assign to me (if unassigned) -->
            <div v-if="isAgent && isUnassigned" class="flex items-center gap-3 mb-4">
              <Button
                label="Assign to me"
                icon="pi pi-user-plus"
                severity="info"
                outlined
                size="small"
                @click="assignToMe"
              />
            </div>

            <!-- Agent: Status editing (if assigned to them) -->
            <div v-if="canEditStatus" class="flex items-center gap-3 flex-wrap">
              <div class="flex items-center gap-2">
                <label class="text-sm text-slate-400">Status:</label>
                <Select
                  v-model="selectedStatus"
                  :options="statusOptions"
                  optionLabel="label"
                  optionValue="value"
                  class="min-w-36"
                  size="small"
                />
              </div>

              <!-- Admin: Assignee dropdown (any unassigned → assign, or current assignment) -->
              <div v-if="isAdmin" class="flex items-center gap-2">
                <label class="text-sm text-slate-400">Assignee:</label>
                <Select
                  v-model="selectedAssigneeId"
                  :options="[{ id: null, username: 'Unassigned' }, ...agents]"
                  optionLabel="username"
                  optionValue="id"
                  class="min-w-36"
                  size="small"
                  :disabled="ticket.assignedToId !== null && selectedAssigneeId === ticket.assignedToId"
                />
              </div>

              <Button
                label="Save"
                icon="pi pi-check"
                size="small"
                :loading="saving"
                @click="saveChanges"
              />
            </div>

            <!-- Admin: Assign to agent button when currently unassigned -->
            <div v-if="isAdmin && isUnassigned" class="flex items-center gap-3 mt-3">
              <Select
                v-model="selectedAssigneeId"
                :options="agents"
                optionLabel="username"
                optionValue="id"
                placeholder="Select agent to assign..."
                class="min-w-44"
                size="small"
              />
              <Button
                label="Assign"
                icon="pi pi-user-plus"
                severity="info"
                size="small"
                :disabled="!selectedAssigneeId"
                @click="assignToAgent"
              />
            </div>
          </div>

          <!-- Submitter: read-only notice -->
          <div v-if="isSubmitter" class="pt-4 border-t border-slate-700">
            <p class="text-sm text-slate-500 italic">
              <i class="pi pi-lock mr-1"></i>You submitted this ticket. An agent will update it.
            </p>
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- Comments section -->
          <div class="lg:col-span-2 space-y-4">
            <h3 class="text-lg font-semibold">
              Comments
              <span class="text-slate-400 font-normal text-base">({{ ticket.comments.length }})</span>
            </h3>

            <div
              v-for="comment in ticket.comments"
              :key="comment.id"
              class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-5"
            >
              <div class="flex items-center justify-between mb-3">
                <div class="flex items-center gap-2">
                  <div class="w-7 h-7 rounded-full bg-indigo-600/30 flex items-center justify-center text-indigo-400 text-xs font-bold">
                    {{ comment.author.charAt(0).toUpperCase() }}
                  </div>
                  <span class="font-medium text-sm">{{ comment.author }}</span>
                </div>
                <span class="text-slate-500 text-xs">{{ formatDate(comment.createdAt) }}</span>
              </div>
              <p class="text-slate-300 text-sm leading-relaxed">{{ comment.content }}</p>
            </div>

            <div v-if="ticket.comments.length === 0" class="text-slate-500 text-sm italic">
              No comments yet.
            </div>

            <!-- Add comment — all roles -->
            <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-5">
              <h4 class="text-sm font-semibold mb-3">Add a comment</h4>
              <Textarea
                v-model="newComment"
                placeholder="Write your comment..."
                rows="3"
                class="w-full mb-3"
              />
              <Button
                label="Post Comment"
                icon="pi pi-send"
                size="small"
                :loading="submittingComment"
                :disabled="!newComment.trim()"
                @click="addComment"
              />
            </div>
          </div>

          <!-- History section — always visible -->
          <div>
            <h3 class="text-lg font-semibold mb-4">History</h3>
            <div v-if="ticket.history.length === 0" class="text-slate-500 text-sm italic">
              No history yet.
            </div>
            <div class="space-y-3">
              <div
                v-for="event in ticket.history"
                :key="event.id"
                class="relative pl-5 border-l-2 border-slate-700 pb-4 last:pb-0"
              >
                <div class="absolute left-[-5px] top-1 w-2 h-2 rounded-full bg-indigo-500"></div>
                <p class="text-sm font-medium">{{ formatHistoryEntry(event) }}</p>
                <p class="text-xs text-slate-500 mt-0.5">
                  by {{ event.changedBy }} · {{ formatDate(event.createdAt) }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </template>
    </main>
  </div>
</template>
