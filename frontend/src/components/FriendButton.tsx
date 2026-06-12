'use client'

import { useState } from 'react'
import { api } from '@/lib/api'
import { useQuery } from '@tanstack/react-query'

export default function FriendButton({ userId, friendId }: { userId: string; friendId: string }) {
  const [loading, setLoading] = useState(false)

  const { data: isSubscribed, refetch } = useQuery({
    queryKey: ['subscription-status', friendId],
    queryFn: () => api.get<boolean>(`/api/friend/status/${friendId}`).then((r) => r.data),
    enabled: userId !== friendId,
  })

  if (userId === friendId) return null

  const handleToggle = async () => {
    setLoading(true)
    try {
      if (isSubscribed) {
        await api.put(`/api/friend/delete/${friendId}`)
      } else {
        await api.put(`/api/friend/set/${friendId}`)
      }
      refetch()
    } catch {
      /* ignore */
    }
    setLoading(false)
  }

  return (
    <button
      onClick={handleToggle}
      disabled={loading}
      className={`rounded px-3 py-1 text-sm disabled:opacity-50 ${
        isSubscribed
          ? 'bg-gray-200 text-gray-700 hover:bg-gray-300'
          : 'bg-blue-600 text-white hover:bg-blue-700'
      }`}
    >
      {loading ? '...' : isSubscribed ? 'Отписаться' : 'Подписаться'}
    </button>
  )
}