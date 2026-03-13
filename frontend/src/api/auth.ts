import api from './axios'

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  password: string
  role?: string
}

export interface AuthResponse {
  token: string
  username: string
  role: string
  userId: number
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export const authApi = {
  login: (data: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', data),

  register: (data: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', data),

  changePassword: (data: ChangePasswordRequest) =>
    api.post<{ message: string }>('/auth/change-password', data),
}
