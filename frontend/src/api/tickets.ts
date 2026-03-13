import api from './axios'

export interface TicketSummary {
  id: number
  title: string
  status: string
  createdBy: string
  createdById: number
  assignedTo: string | null
  assignedToId: number | null
  createdAt: string
  updatedAt: string
}

export interface TicketComment {
  id: number
  ticketId: number
  authorId: number
  author: string
  content: string
  createdAt: string
  updatedAt: string | null
}

export interface TicketHistory {
  id: number
  ticketId: number
  fieldChanged: string
  oldValue: string | null
  newValue: string | null
  changedById: number
  changedBy: string
  createdAt: string
}

export interface TicketDetail extends TicketSummary {
  description: string
  comments: TicketComment[]
  history: TicketHistory[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CreateTicketRequest {
  title: string
  description?: string
}

export interface UpdateTicketRequest {
  title?: string
  description?: string
  status?: string
  assignedToId?: number | null
}

export interface TicketListParams {
  search?: string
  status?: string
  assignedToMe?: boolean
  page?: number
  pageSize?: number
}

export interface AgentDto {
  id: number
  username: string
}

export interface TicketStats {
  openCount: number
  inProgressCount: number
  resolvedTodayCount: number
  totalVisible: number
}

export const ticketsApi = {
  getAll: (params?: TicketListParams) =>
    api.get<PagedResult<TicketSummary>>('/tickets', { params }),
  getById: (id: number) => api.get<TicketDetail>(`/tickets/${id}`),
  create: (data: CreateTicketRequest) => api.post<TicketDetail>('/tickets', data),
  update: (id: number, data: UpdateTicketRequest) => api.put<TicketDetail>(`/tickets/${id}`, data),
  addComment: (id: number, content: string) =>
    api.post<TicketComment>(`/tickets/${id}/comments`, { content }),
  editComment: (ticketId: number, commentId: number, content: string) =>
    api.put<TicketComment>(`/tickets/${ticketId}/comments/${commentId}`, { content }),
  deleteComment: (ticketId: number, commentId: number) =>
    api.delete(`/tickets/${ticketId}/comments/${commentId}`),
  assignTicket: (id: number, assignedToId?: number) =>
    api.post<TicketDetail>(`/tickets/${id}/assign`, { assignedToId }),
  getStats: () => api.get<TicketStats>('/tickets/stats'),
}

export const usersApi = {
  getAgents: () => api.get<AgentDto[]>('/users/agents'),
}
