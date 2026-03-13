<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import { ticketsApi, type TicketStats } from '@/api/tickets'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import AppNav from '@/components/AppNav.vue'

const auth = useAuthStore()
const router = useRouter()

const stats = ref<TicketStats | null>(null)
const loadingStats = ref(false)

onMounted(async () => {
  await loadStats()
})

async function loadStats() {
  loadingStats.value = true
  try {
    const res = await ticketsApi.getStats()
    stats.value = res.data
  } catch {
    // Stats unavailable — not critical
  } finally {
    loadingStats.value = false
  }
}

function statCards() {
  return [
    {
      label: 'Open Tickets',
      value: stats.value !== null ? String(stats.value.openCount) : '—',
      icon: 'pi pi-inbox',
      color: 'text-blue-400',
      bg: 'bg-blue-500/10',
    },
    {
      label: 'In Progress',
      value: stats.value !== null ? String(stats.value.inProgressCount) : '—',
      icon: 'pi pi-spinner',
      color: 'text-amber-400',
      bg: 'bg-amber-500/10',
    },
    {
      label: 'Resolved Today',
      value: stats.value !== null ? String(stats.value.resolvedTodayCount) : '—',
      icon: 'pi pi-check-circle',
      color: 'text-green-400',
      bg: 'bg-green-500/10',
    },
    {
      label: 'Visible Tickets',
      value: stats.value !== null ? String(stats.value.totalVisible) : '—',
      icon: 'pi pi-list',
      color: 'text-violet-400',
      bg: 'bg-violet-500/10',
    },
  ]
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <AppNav />

    <main class="max-w-7xl mx-auto px-4 sm:px-6 py-8 sm:py-10">
      <!-- Welcome -->
      <div class="mb-8 sm:mb-10">
        <h2 class="text-2xl sm:text-3xl font-bold tracking-tight">
          Welcome back, <span class="text-indigo-400">{{ auth.username }}</span> 👋
        </h2>
        <p class="text-slate-400 mt-2">Here's what's going on with your support system today.</p>
      </div>

      <!-- Stats cards -->
      <div v-if="loadingStats" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-10">
        <Skeleton v-for="i in 4" :key="i" height="6rem" class="bg-slate-700 rounded-xl" />
      </div>
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-10">
        <div
          v-for="stat in statCards()"
          :key="stat.label"
          class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-5 flex items-start gap-4 hover:border-slate-600 transition-colors"
        >
          <div :class="[stat.bg, 'p-3 rounded-xl flex-shrink-0']">
            <i :class="[stat.icon, stat.color, 'text-xl']"></i>
          </div>
          <div>
            <p class="text-slate-400 text-sm">{{ stat.label }}</p>
            <p class="text-2xl font-bold mt-0.5">{{ stat.value }}</p>
          </div>
        </div>
      </div>

      <!-- Quick actions -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-6">
          <h3 class="text-lg font-semibold mb-4">Quick Actions</h3>
          <div class="space-y-3">
            <Button
              label="View All Tickets"
              icon="pi pi-list"
              class="w-full justify-start"
              severity="secondary"
              outlined
              @click="router.push({ name: 'tickets' })"
            />
            <Button
              label="Create New Ticket"
              icon="pi pi-plus"
              class="w-full justify-start"
              severity="secondary"
              outlined
              @click="router.push({ name: 'create-ticket' })"
            />
            <Button
              label="My Profile"
              icon="pi pi-user"
              class="w-full justify-start"
              severity="secondary"
              outlined
              @click="router.push({ name: 'profile' })"
            />
          </div>
        </div>

        <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-6">
          <h3 class="text-lg font-semibold mb-4">Your Role</h3>
          <div class="space-y-2 text-sm text-slate-400">
            <div v-if="auth.role === 'Admin'" class="space-y-1">
              <p><i class="pi pi-check text-green-400 mr-2"></i>View all tickets</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>Assign tickets to agents</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>Update status on any ticket</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>Create and edit tickets</p>
            </div>
            <div v-else-if="auth.role === 'Agent'" class="space-y-1">
              <p><i class="pi pi-check text-green-400 mr-2"></i>View assigned + unassigned tickets</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>Self-assign unassigned tickets</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>Update status on your tickets</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>Create and edit tickets</p>
            </div>
            <div v-else class="space-y-1">
              <p><i class="pi pi-check text-green-400 mr-2"></i>Create tickets</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>View your own tickets</p>
              <p><i class="pi pi-check text-green-400 mr-2"></i>Add comments</p>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
