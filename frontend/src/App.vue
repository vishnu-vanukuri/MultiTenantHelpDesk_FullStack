<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from './api'

const token = ref(localStorage.getItem('helpdesk_token') || '')
const user = ref(JSON.parse(localStorage.getItem('helpdesk_user') || 'null'))
const tenant = ref(localStorage.getItem('helpdesk_tenant') || '')
const mode = ref('login')
const error = ref('')
const busy = ref(false)
const tickets = ref([])
const summary = ref({ total: 0, open: 0, inProgress: 0, resolved: 0, critical: 0 })
const search = ref('')
const statusFilter = ref('')
const showForm = ref(false)
const editingId = ref(null)
const authForm = reactive({ companyName: '', fullName: '', email: '', password: '' })
const ticketForm = reactive({ title: '', description: '', priority: 'Medium', status: 'Open', assignedTo: '' })
const isAdmin = computed(() => user.value?.role === 'Admin')
const visibleTickets = computed(() => tickets.value.filter(t => {
  const q = search.value.toLowerCase()
  return (!q || `${t.title} ${t.description} ${t.assignedTo || ''}`.toLowerCase().includes(q)) &&
    (!statusFilter.value || t.status === statusFilter.value)
}))

function setSession(data) {
  token.value = data.token; user.value = data.user; tenant.value = data.tenant
  localStorage.setItem('helpdesk_token', data.token)
  localStorage.setItem('helpdesk_user', JSON.stringify(data.user))
  localStorage.setItem('helpdesk_tenant', data.tenant)
}
function logout() {
  token.value = ''; user.value = null; tenant.value = ''
  localStorage.removeItem('helpdesk_token'); localStorage.removeItem('helpdesk_user'); localStorage.removeItem('helpdesk_tenant')
  tickets.value = []
}
async function authenticate() {
  error.value = ''; busy.value = true
  try {
    const endpoint = mode.value === 'register' ? '/auth/register' : '/auth/login'
    const payload = mode.value === 'register'
      ? { ...authForm }
      : { email: authForm.email, password: authForm.password }
    const { data } = await api.post(endpoint, payload)
    setSession(data); await loadData()
  } catch (e) { error.value = e.response?.data?.message || 'Could not sign in. Check the API and your details.' }
  finally { busy.value = false }
}
async function loadData() {
  if (!token.value) return
  try {
    const [t, s] = await Promise.all([api.get('/tickets'), api.get('/dashboard/summary')])
    tickets.value = t.data; summary.value = s.data
  } catch (e) {
    if (e.response?.status === 401) logout()
    else error.value = 'Unable to load workspace data. Is the API running?'
  }
}
function resetTicket() {
  Object.assign(ticketForm, { title: '', description: '', priority: 'Medium', status: 'Open', assignedTo: '' })
  editingId.value = null
}
function editTicket(t) {
  Object.assign(ticketForm, { title: t.title, description: t.description, priority: t.priority, status: t.status, assignedTo: t.assignedTo || '' })
  editingId.value = t.id; showForm.value = true
}
async function saveTicket() {
  error.value = ''
  try {
    if (editingId.value) await api.put(`/tickets/${editingId.value}`, ticketForm)
    else await api.post('/tickets', ticketForm)
    showForm.value = false; resetTicket(); await loadData()
  } catch (e) { error.value = e.response?.data?.message || 'Could not save ticket.' }
}
async function deleteTicket(id) {
  if (!confirm('Delete this ticket?')) return
  try { await api.delete(`/tickets/${id}`); await loadData() }
  catch { error.value = 'Could not delete ticket.' }
}
function newTicket() { resetTicket(); showForm.value = true }
onMounted(loadData)
</script>

<template>
  <main v-if="!token" class="auth-shell">
    <section class="auth-card">
      <div class="brand"><span class="brand-mark">H</span><span>HelpDesk<span class="dot">.</span></span></div>
      <p class="eyebrow">SUPPORT OPERATIONS PLATFORM</p>
      <h1>{{ mode === 'login' ? 'Welcome back' : 'Create your workspace' }}</h1>
      <p class="muted">{{ mode === 'login' ? 'Sign in to manage your support queue.' : 'Register your company and create its admin account.' }}</p>
      <form @submit.prevent="authenticate">
        <label v-if="mode === 'register'">Company name<input v-model="authForm.companyName" required minlength="2" placeholder="Acme Technologies"></label>
        <label v-if="mode === 'register'">Full name<input v-model="authForm.fullName" required placeholder="Your name"></label>
        <label>Email address<input v-model="authForm.email" type="email" required placeholder="you@company.com"></label>
        <label>Password<input v-model="authForm.password" type="password" required minlength="8" placeholder="At least 8 characters"></label>
        <p v-if="error" class="error">{{ error }}</p>
        <button class="primary full" :disabled="busy">{{ busy ? 'Please wait…' : mode === 'login' ? 'Sign in' : 'Create workspace' }}</button>
      </form>
      <p class="switch">{{ mode === 'login' ? 'New to HelpDesk?' : 'Already have a workspace?' }}
        <a @click="mode = mode === 'login' ? 'register' : 'login'; error = ''">{{ mode === 'login' ? 'Create an account' : 'Sign in' }}</a>
      </p>
    </section>
  </main>

  <div v-else class="app-shell">
    <aside class="sidebar">
      <div class="brand"><span class="brand-mark">H</span><span>HelpDesk<span class="dot">.</span></span></div>
      <div class="workspace"><span class="workspace-icon">{{ tenant?.charAt(0)?.toUpperCase() }}</span><div><b>{{ tenant }}</b><small>Workspace</small></div></div>
      <p class="nav-label">WORKSPACE</p>
      <a class="nav-item active"><span>▦</span> Overview</a>
      <a class="nav-item"><span>▤</span> All tickets <em>{{ summary.total }}</em></a>
      <div class="sidebar-bottom"><div class="user-chip"><div class="avatar">{{ user?.fullName?.charAt(0) }}</div><div><b>{{ user?.fullName }}</b><small>{{ user?.role }}</small></div></div><button class="ghost full" @click="logout">Sign out ↗</button></div>
    </aside>
    <section class="content">
      <header class="topbar"><div><span class="breadcrumb">Workspace /</span> Overview</div><div class="top-user"><span class="online"></span> {{ user?.email }}</div></header>
      <div class="page">
        <div class="page-heading"><div><p class="eyebrow">YOUR SUPPORT WORKSPACE</p><h1>Good day, {{ user?.fullName?.split(' ')[0] }} 👋</h1><p class="muted">Here’s what’s happening with your support tickets.</p></div><button class="primary" @click="newTicket">＋ Create ticket</button></div>
        <p v-if="error" class="error banner">{{ error }}</p>
        <div class="stats">
          <article class="stat-card"><span class="stat-icon blue">▤</span><span class="stat-label">Total tickets</span><strong>{{ summary.total }}</strong><small>Across this workspace</small></article>
          <article class="stat-card"><span class="stat-icon amber">◷</span><span class="stat-label">Open</span><strong>{{ summary.open }}</strong><small>Awaiting action</small></article>
          <article class="stat-card"><span class="stat-icon purple">↻</span><span class="stat-label">In progress</span><strong>{{ summary.inProgress }}</strong><small>Being worked on</small></article>
          <article class="stat-card"><span class="stat-icon green">✓</span><span class="stat-label">Resolved</span><strong>{{ summary.resolved }}</strong><small>Completed tickets</small></article>
        </div>
        <section class="panel">
          <div class="panel-heading"><div><h2>Recent tickets</h2><p class="muted">Manage and track your team's support requests.</p></div><span class="count-pill">{{ visibleTickets.length }} tickets</span></div>
          <div class="filters"><input v-model="search" placeholder="⌕  Search tickets…" /><select v-model="statusFilter"><option value="">All statuses</option><option>Open</option><option>In Progress</option><option>Resolved</option><option>Closed</option></select></div>
          <div class="table-wrap">
            <table><thead><tr><th>TICKET</th><th>ASSIGNEE</th><th>PRIORITY</th><th>STATUS</th><th>CREATED</th><th>ACTIONS</th></tr></thead>
              <tbody><tr v-for="t in visibleTickets" :key="t.id"><td><b>{{ t.title }}</b><small class="description">{{ t.description }}</small></td><td>{{ t.assignedTo || 'Unassigned' }}</td><td><span :class="['priority', t.priority.toLowerCase()]">{{ t.priority }}</span></td><td><span :class="['status', t.status.toLowerCase().replace(' ', '-')]">{{ t.status }}</span></td><td>{{ new Date(t.createdAt).toLocaleDateString() }}</td><td><button class="text-btn" @click="editTicket(t)">Edit</button><button v-if="isAdmin" class="text-btn danger-text" @click="deleteTicket(t.id)">Delete</button></td></tr>
                <tr v-if="visibleTickets.length === 0"><td colspan="6" class="empty">No tickets found. Create your first support ticket.</td></tr></tbody>
            </table>
          </div>
        </section>
        <footer>HelpDesk Workspace <span>•</span> Tenant-isolated support operations</footer>
      </div>
    </section>
    <div v-if="showForm" class="modal-backdrop" @click.self="showForm = false">
      <section class="modal"><div class="modal-head"><div><p class="eyebrow">TICKET MANAGEMENT</p><h2>{{ editingId ? 'Update ticket' : 'Create a ticket' }}</h2></div><button class="close" @click="showForm = false">×</button></div>
        <form @submit.prevent="saveTicket"><label>Title<input v-model="ticketForm.title" required minlength="3" maxlength="160" placeholder="Briefly describe the issue"></label><label>Description<textarea v-model="ticketForm.description" rows="4" placeholder="Add details to help resolve this issue"></textarea></label>
          <div class="form-row"><label>Priority<select v-model="ticketForm.priority"><option>Low</option><option>Medium</option><option>High</option><option>Critical</option></select></label><label>Status<select v-model="ticketForm.status"><option>Open</option><option>In Progress</option><option>Resolved</option><option>Closed</option></select></label></div>
          <label>Assigned to<input v-model="ticketForm.assignedTo" placeholder="Employee or agent name"></label>
          <div class="modal-actions"><button type="button" class="secondary" @click="showForm = false">Cancel</button><button class="primary">{{ editingId ? 'Save changes' : 'Create ticket' }}</button></div>
        </form>
      </section>
    </div>
  </div>
</template>

