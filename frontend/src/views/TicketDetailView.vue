<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Textarea from 'primevue/textarea'
import Divider from 'primevue/divider'
import Skeleton from 'primevue/skeleton'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const loading = ref(true)
const ticketId = route.params.id

// Placeholder ticket data — replace with real API call
const ticket = ref({
  id: ticketId,
  title: 'Login page not loading',
  description: 'Users are reporting that the login page fails to load after the latest deployment. The page shows a blank white screen.',
  status: 'Open',
  priority: 'High',
  createdAt: '2026-03-12T10:23:00Z',
  updatedAt: '2026-03-12T14:45:00Z',
  createdBy: 'user1',
  assignee: 'agent1',
})

// Placeholder comments
const comments = ref([
  {
    id: 1,
    author: 'agent1',
    content: 'Looking into this now. Could be related to the new auth middleware.',
    createdAt: '2026-03-12T11:00:00Z',
    isInternal: false,
  },
  {
    id: 2,
    author: 'user1',
    content: 'It started happening right after the 14:00 deploy yesterday.',
    createdAt: '2026-03-12T12:30:00Z',
    isInternal: false,
  },
])

// Placeholder history
const history = ref([
  { id: 1, action: 'Ticket created', actor: 'user1', at: '2026-03-12T10:23:00Z' },
  { id: 2, action: 'Status changed to Open', actor: 'agent1', at: '2026-03-12T10:30:00Z' },
  { id: 3, action: 'Assigned to agent1', actor: 'admin', at: '2026-03-12T10:35:00Z' },
])

const newComment = ref('')
const submittingComment = ref(false)

onMounted(() => {
  // TODO: fetch ticket from API using ticketId
  setTimeout(() => { loading.value = false }, 500)
})

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

function formatDate(iso: string) {
  return new Date(iso).toLocaleString()
}

async function addComment() {
  if (!newComment.value.trim()) return
  submittingComment.value = true
  try {
    // TODO: POST to API
    comments.value.push({
      id: Date.now(),
      author: auth.username || 'me',
      content: newComment.value.trim(),
      createdAt: new Date().toISOString(),
      isInternal: false,
    })
    newComment.value = ''
  } finally {
    submittingComment.value = false
  }
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
          <router-link to="/tickets" class="hover:text-white transition-colors">Tickets</router-link>
        </nav>
        <div class="flex items-center gap-3">
          <span class="text-slate-400 text-sm hidden sm:block">
            <i class="pi pi-user mr-1"></i>{{ auth.username }}
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
        text
        severity="secondary"
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

      <template v-else>
        <!-- Ticket header -->
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
            <div class="flex items-center gap-2 flex-shrink-0">
              <Tag :value="ticket.priority" :severity="getPrioritySeverity(ticket.priority)" />
              <Tag :value="ticket.status" :severity="getStatusSeverity(ticket.status)" />
            </div>
          </div>

          <Divider class="border-slate-700 my-4" />

          <!-- Meta info grid -->
          <div class="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <p class="text-slate-500 mb-1">Created by</p>
              <p class="font-medium">{{ ticket.createdBy }}</p>
            </div>
            <div>
              <p class="text-slate-500 mb-1">Assignee</p>
              <p class="font-medium">{{ ticket.assignee || 'Unassigned' }}</p>
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
          <div>
            <h3 class="text-sm font-semibold text-slate-400 uppercase tracking-wider mb-3">Description</h3>
            <p class="text-slate-300 leading-relaxed whitespace-pre-wrap">{{ ticket.description }}</p>
          </div>

          <!-- Actions -->
          <div class="flex gap-2 mt-6 pt-4 border-t border-slate-700">
            <Button label="Assign to me" icon="pi pi-user-plus" severity="secondary" outlined size="small" />
            <Button label="Close Ticket" icon="pi pi-times-circle" severity="danger" outlined size="small" />
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- Comments section -->
          <div class="lg:col-span-2 space-y-4">
            <h3 class="text-lg font-semibold">Comments <span class="text-slate-400 font-normal text-base">({{ comments.length }})</span></h3>

            <div v-for="comment in comments" :key="comment.id" class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-5">
              <div class="flex items-center justify-between mb-3">
                <div class="flex items-center gap-2">
                  <div class="w-7 h-7 rounded-full bg-indigo-600/30 flex items-center justify-center text-indigo-400 text-xs font-bold">
                    {{ comment.author.charAt(0).toUpperCase() }}
                  </div>
                  <span class="font-medium text-sm">{{ comment.author }}</span>
                  <span v-if="comment.isInternal" class="text-xs text-amber-400 bg-amber-500/10 px-2 py-0.5 rounded">Internal</span>
                </div>
                <span class="text-slate-500 text-xs">{{ formatDate(comment.createdAt) }}</span>
              </div>
              <p class="text-slate-300 text-sm leading-relaxed">{{ comment.content }}</p>
            </div>

            <!-- Add comment -->
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

          <!-- History section -->
          <div>
            <h3 class="text-lg font-semibold mb-4">History</h3>
            <div class="space-y-3">
              <div
                v-for="event in history"
                :key="event.id"
                class="relative pl-5 border-l-2 border-slate-700 pb-4 last:pb-0"
              >
                <div class="absolute left-[-5px] top-1 w-2 h-2 rounded-full bg-indigo-500"></div>
                <p class="text-sm font-medium">{{ event.action }}</p>
                <p class="text-xs text-slate-500 mt-0.5">
                  by {{ event.actor }} · {{ formatDate(event.at) }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </template>
    </main>
  </div>
</template>
