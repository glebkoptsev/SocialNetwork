'use client'

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'next/navigation'
import { dialogApi } from '@/lib/api'
import { Message } from '@/types'
import { useState, FormEvent, useEffect, useRef } from 'react'
import { useAuth } from '@/lib/auth'

const statusIcon = (status: number) => {
  if (status === 0) return <span className="text-blue-500 text-[11px] ml-1.5">Sent</span>
  if (status === 1) return <span className="text-blue-500 text-[11px] ml-1.5">Delivered</span>
  return <span className="text-blue-500 text-[11px] ml-1.5 font-bold">Read</span>
}

export default function ChatPage() {
  const { chatId } = useParams<{ chatId: string }>()
  const userId = useAuth((s) => s.userId)
  const queryClient = useQueryClient()
  const [text, setText] = useState('')
  const [error, setError] = useState('')
  const bottomRef = useRef<HTMLDivElement>(null)
  const messageCountRef = useRef(0)
  const readRef = useRef(false)

  const { data: messages, isLoading } = useQuery({
    queryKey: ['messages', chatId],
    queryFn: () =>
      dialogApi.get<Message[]>(`/api/dialog/${chatId}/messages`).then((r) => r.data),
  })

  const sendMutation = useMutation({
    mutationFn: (message: string) =>
      dialogApi.post(`/api/dialog/${chatId}/send`, { message }),
    onSuccess: () => {
      setText('')
      setError('')
      queryClient.invalidateQueries({ queryKey: ['messages', chatId] })
    },
    onError: (err: any) => {
      setError(err?.response?.data?.error || 'Ошибка отправки')
    },
  })

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    if (!text.trim()) return
    sendMutation.mutate(text)
  }

  // Auto-scroll on new messages
  useEffect(() => {
    if (!messages?.length) return
    const isNewMessage = messages.length !== messageCountRef.current
    messageCountRef.current = messages.length
    if (isNewMessage) {
      setTimeout(() => bottomRef.current?.scrollIntoView({ behavior: 'smooth' }), 50)
    }
  }, [messages])

  // Mark as read once per chat open, not on every refetch
  useEffect(() => {
    if (readRef.current || !messages?.length) return
    const hasUnread = messages.some((m) => m.user_id !== userId && m.status < 2)
    if (!hasUnread) return
    readRef.current = true
    const timer = setTimeout(() => {
      dialogApi.post(`/api/dialog/${chatId}/read`).catch(() => {})
    }, 1000)
    return () => clearTimeout(timer)
  }, [messages, chatId, userId])

  if (isLoading) return <p className="text-gray-400 text-center">Загрузка...</p>

  return (
    <div className="max-w-2xl mx-auto flex flex-col h-[calc(100vh-8rem)]">
      <div className="flex-1 overflow-y-auto flex flex-col p-4 bg-white border-x border-t rounded-t-lg">
        <div className="mt-auto space-y-2">
          {messages?.map((msg) => (
            <div
              key={msg.message_id}
              className={`max-w-[75%] rounded-lg px-3 py-2 text-sm bg-gray-100 ${
                msg.user_id === userId ? 'ml-auto rounded-br-sm' : 'rounded-bl-sm'
              }`}
            >
              {msg.user_id !== userId && (
                <p className="text-xs font-semibold mb-1">{msg.user_name}</p>
              )}
              <p>{msg.message}</p>
              <div className="flex items-center gap-1 mt-1">
                <p className="text-xs opacity-70">
                  {new Date(msg.creation_datetime).toLocaleTimeString()}
                </p>
                {msg.user_id === userId && statusIcon(msg.status)}
              </div>
            </div>
          ))}
        </div>
        <div ref={bottomRef} />
      </div>
      {error && <p className="text-red-500 text-xs text-center py-1">{error}</p>}
      <form onSubmit={handleSubmit} className="flex gap-2 border p-2 bg-white rounded-b-lg">
        <input
          className="flex-1 border rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Напишите сообщение..."
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
        <button
          className="bg-blue-600 text-white rounded-lg px-5 py-2 text-sm hover:bg-blue-700 disabled:opacity-50 whitespace-nowrap"
          disabled={sendMutation.isPending}
        >
          {sendMutation.isPending ? '...' : 'Отправить'}
        </button>
      </form>
    </div>
  )
}
