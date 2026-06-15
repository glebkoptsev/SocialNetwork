'use client'

import { create } from 'zustand'
import { api } from './api'

function parseJwtUserId(token: string): string | null {
  try {
    const base64url = token.split('.')[1]
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/')
    const payload = JSON.parse(atob(base64))
    return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? payload['nameid'] ?? null
  } catch {
    return null
  }
}

interface AuthState {
  token: string | null
  userId: string | null
  loading: boolean
  login: (login: string, password: string) => Promise<void>
  register: (data: {
    login: string
    first_name: string
    second_name: string
    birthdate: string
    biography: string
    city: string
    password: string
  }) => Promise<string>
  logout: () => void
  hydrate: () => void
}

export const useAuth = create<AuthState>((set) => ({
  token: null,
  userId: null,
  loading: true,

  hydrate: () => {
    const token = document.cookie
      .split('; ')
      .find((row) => row.startsWith('token='))
      ?.split('=')[1]
    const login = document.cookie
      .split('; ')
      .find((row) => row.startsWith('userId='))
      ?.split('=')[1]
    const userId = token ? (parseJwtUserId(token) ?? login) : null
    set({ token: token ?? null, userId, loading: false })
  },

  login: async (login, password) => {
    const { data } = await api.post('/api/security/login', { login, password })
    const userId = parseJwtUserId(data.access_token)
    document.cookie = `token=${data.access_token}; path=/; max-age=${data.expiresIn}`
    document.cookie = `userId=${userId}; path=/; max-age=${data.expiresIn}`
    set({ token: data.access_token, userId })
  },

  register: async (body) => {
    const { data } = await api.post('/api/user/register', body)
    return data.user_id
  },

  logout: () => {
    document.cookie = 'token=; path=/; max-age=0'
    document.cookie = 'userId=; path=/; max-age=0'
    set({ token: null, userId: null })
  },
}))
