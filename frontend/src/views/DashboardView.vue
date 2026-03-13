<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Card from 'primevue/card'

const auth = useAuthStore()
const router = useRouter()

const stats = [
  { label: 'Open Tickets', value: '—', icon: 'pi pi-inbox', color: 'text-blue-400', bg: 'bg-blue-500/10' },
  { label: 'In Progress', value: '—', icon: 'pi pi-spin pi-spinner', color: 'text-amber-400', bg: 'bg-amber-500/10' },
  { label: 'Resolved Today', value: '—', icon: 'pi pi-check-circle', color: 'text-green-400', bg: 'bg-green-500/10' },
  { label: 'Total Users', value: '—', icon: 'pi pi-users', color: 'text-violet-400', bg: 'bg-violet-500/10' },
]

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
          <router-link to="/" class="text-white font-medium">Dashboard</router-link>
          <router-link to="/tickets" class="hover:text-white transition-colors">Tickets</router-link>
        </nav>
        <div class="flex items-center gap-3">
          <span class="text-slate-400 text-sm hidden sm:block">
            <i class="pi pi-user mr-1"></i>{{ auth.username }}
            <span v-if="auth.role" class="ml-1 text-xs text-indigo-400">({{ auth.role }})</span>
          </span>
          <Button icon="pi pi-sign-out" severity="secondary" text rounded @click="logout" v-tooltip="'Sign out'" />
        </div>
      </div>
    </header>

    <!-- Main content -->
    <main class="max-w-7xl mx-auto px-6 py-10">
      <!-- Welcome -->
      <div class="mb-10">
        <h2 class="text-3xl font-bold tracking-tight">
          Welcome back, <span class="text-indigo-400">{{ auth.username }}</span> 👋
        </h2>
        <p class="text-slate-400 mt-2">Here's what's going on with your support system today.</p>
      </div>

      <!-- Stats cards -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-10">
        <div
          v-for="stat in stats"
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
              @click="router.push({ name: 'tickets' })"
            />
          </div>
        </div>

        <div class="bg-slate-800/60 border border-slate-700/50 rounded-xl p-6">
          <h3 class="text-lg font-semibold mb-4">Recent Activity</h3>
          <div class="space-y-3">
            <div class="flex items-center gap-3 text-slate-400 text-sm">
              <i class="pi pi-info-circle text-indigo-400"></i>
              <span>No recent activity yet.</span>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
