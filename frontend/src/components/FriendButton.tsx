'use client'

import { useState } from 'react'
import { api } from '@/lib/api'
import { useQueryClient } from '@tanstack/react-query'

export default function FriendButton({ userId, friendId }: { userId: string; friendId: string }) {
  const [loading, setLoading] = useState(false)
  const queryClient = useQueryClient()

  if (userId === friendId) return null

  const handleAdd = async () => {
    setLoading(true)
    await api.put(`/api/friend/set/${friendId}`)
    queryClient.invalidateQueries({ queryKey: ['user', friendId] })
    setLoading(false)
  }

  return (
    <button
      onClick={handleAdd}
      disabled={loading}
      className="bg-blue-600 text-white rounded px-3 py-1 text-sm hover:bg-blue-700 disabled:opacity-50"
    >
      {loading ? '...' : 'Добавить в друзья'}
    </button>
  )
}
