<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Button from 'primevue/button'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const menuOpen = ref(false)

function logout() {
  auth.logout()
  router.push({ name: 'login' })
}

function isActive(path: string) {
  if (path === '/') return route.path === '/'
  return route.path.startsWith(path)
}
</script>

<template>
  <header class="bg-slate-800/80 backdrop-blur border-b border-slate-700/50 sticky top-0 z-20">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
      <!-- Logo -->
      <div class="flex items-center gap-3">
        <div class="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center flex-shrink-0">
          <i class="pi pi-ticket text-white text-sm"></i>
        </div>
        <span class="font-bold text-lg tracking-tight">TicketSystem</span>
      </div>

      <!-- Desktop Nav -->
      <nav class="hidden md:flex items-center gap-6 text-sm text-slate-400">
        <router-link
          to="/"
          :class="['transition-colors', isActive('/') && route.path === '/' ? 'text-white font-medium' : 'hover:text-white']"
        >Dashboard</router-link>
        <router-link
          to="/tickets"
          :class="['transition-colors', isActive('/tickets') ? 'text-white font-medium' : 'hover:text-white']"
        >Tickets</router-link>
        <router-link
          to="/profile"
          :class="['transition-colors', isActive('/profile') ? 'text-white font-medium' : 'hover:text-white']"
        >Profile</router-link>
      </nav>

      <!-- Desktop right side -->
      <div class="hidden md:flex items-center gap-3">
        <span class="text-slate-400 text-sm">
          <i class="pi pi-user mr-1"></i>{{ auth.username }}
          <span v-if="auth.role" class="ml-1 text-xs text-indigo-400">({{ auth.role }})</span>
        </span>
        <Button icon="pi pi-sign-out" severity="secondary" text rounded @click="logout" v-tooltip="'Sign out'" />
      </div>

      <!-- Mobile hamburger -->
      <button
        class="md:hidden p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-700/50 transition-colors"
        @click="menuOpen = !menuOpen"
        aria-label="Toggle menu"
      >
        <i :class="menuOpen ? 'pi pi-times' : 'pi pi-bars'" class="text-lg"></i>
      </button>
    </div>

    <!-- Mobile Menu -->
    <transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 -translate-y-2"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-150 ease-in"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 -translate-y-2"
    >
      <div v-if="menuOpen" class="md:hidden border-t border-slate-700/50 bg-slate-800/95 backdrop-blur">
        <nav class="px-4 py-3 space-y-1">
          <router-link
            to="/"
            :class="['flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors', isActive('/') && route.path === '/' ? 'bg-slate-700 text-white font-medium' : 'text-slate-400 hover:bg-slate-700/50 hover:text-white']"
            @click="menuOpen = false"
          >
            <i class="pi pi-home"></i>Dashboard
          </router-link>
          <router-link
            to="/tickets"
            :class="['flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors', isActive('/tickets') ? 'bg-slate-700 text-white font-medium' : 'text-slate-400 hover:bg-slate-700/50 hover:text-white']"
            @click="menuOpen = false"
          >
            <i class="pi pi-list"></i>Tickets
          </router-link>
          <router-link
            to="/profile"
            :class="['flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors', isActive('/profile') ? 'bg-slate-700 text-white font-medium' : 'text-slate-400 hover:bg-slate-700/50 hover:text-white']"
            @click="menuOpen = false"
          >
            <i class="pi pi-user"></i>Profile
          </router-link>
          <div class="border-t border-slate-700/50 pt-2 mt-2">
            <div class="px-3 py-1 text-xs text-slate-500">
              {{ auth.username }}
              <span v-if="auth.role" class="ml-1 text-indigo-400">({{ auth.role }})</span>
            </div>
            <button
              class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-400 hover:bg-slate-700/50 hover:text-white transition-colors w-full"
              @click="logout"
            >
              <i class="pi pi-sign-out"></i>Sign out
            </button>
          </div>
        </nav>
      </div>
    </transition>
  </header>
</template>
