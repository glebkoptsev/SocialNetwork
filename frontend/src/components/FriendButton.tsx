'use client'

import { useState } from 'react'
import { api } from '@/lib/api'
import { useQuery, useQueryClient } from '@tanstack/react-query'

export default function FriendButton({ userId, friendId }: { userId: string; friendId: string }) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const queryClient = useQueryClient()

  const { data: isSubscribed } = useQuery({
    queryKey: ['subscription-status', userId, friendId],
    queryFn: () => api.get<boolean>(`/api/friend/status/${friendId}`).then((r) => r.data),
    enabled: userId !== friendId,
  })

  if (userId === friendId) return null

  const handleToggle = async () => {
    setLoading(true)
    setError('')
    try {
      if (isSubscribed) {
        await api.put(`/api/friend/delete/${friendId}`)
      } else {
        await api.put(`/api/friend/set/${friendId}`)
      }
      queryClient.invalidateQueries({ queryKey: ['subscription-status', userId, friendId] })
    } catch {
      setError('Ошибка')
    }
    setLoading(false)
  }

  return (
    <div className="flex items-center gap-2">
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
      {error && <span className="text-red-500 text-xs">{error}</span>}
    </div>
  )
}
