'use client'

import { create } from 'zustand'
import { api } from './api'

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
    const userId = document.cookie
      .split('; ')
      .find((row) => row.startsWith('userId='))
      ?.split('=')[1]
    set({ token: token ?? null, userId: userId ?? null, loading: false })
  },

  login: async (id, password) => {
    const { data } = await api.post('/api/security/login', { login, password })
    document.cookie = `token=${data.access_token}; path=/; max-age=${data.expiresIn}`
    document.cookie = `userId=${id}; path=/; max-age=${data.expiresIn}`
    set({ token: data.access_token, userId: id })
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
