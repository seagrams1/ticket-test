import api from './axios'

export interface TicketSummary {
  id: number
  title: string
  status: string
  createdBy: string
  assignedTo: string | null
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
  createdById: number
  assignedToId: number | null
  comments: TicketComment[]
  history: TicketHistory[]
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

export const ticketsApi = {
  getAll: () => api.get<TicketSummary[]>('/tickets'),
  getById: (id: number) => api.get<TicketDetail>(`/tickets/${id}`),
  create: (data: CreateTicketRequest) => api.post<TicketDetail>('/tickets', data),
  update: (id: number, data: UpdateTicketRequest) => api.put<TicketDetail>(`/tickets/${id}`, data),
  addComment: (id: number, content: string) =>
    api.post<TicketComment>(`/tickets/${id}/comments`, { content }),
}
