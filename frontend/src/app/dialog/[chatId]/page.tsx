'use client'

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'next/navigation'
import { dialogApi } from '@/lib/api'
import { Message } from '@/types'
import { useState, FormEvent, useEffect, useRef } from 'react'
import { useAuth } from '@/lib/auth'

export default function ChatPage() {
  const { chatId } = useParams()
  const userId = useAuth((s) => s.userId)
  const queryClient = useQueryClient()
  const [text, setText] = useState('')
  const bottomRef = useRef<HTMLDivElement>(null)

  const { data: messages } = useQuery({
    queryKey: ['messages', chatId],
    queryFn: () =>
      dialogApi.get<Message[]>(`/api/dialog/${chatId}/messages`).then((r) => r.data),
  })

  const sendMutation = useMutation({
    mutationFn: (message: string) =>
      dialogApi.post(`/api/dialog/${chatId}/send`, { message }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['messages', chatId] })
    },
  })

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    if (!text.trim()) return
    sendMutation.mutate(text)
    setText('')
  }

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  return (
    <div className="flex flex-col h-[calc(100vh-8rem)]">
      <div className="flex-1 overflow-y-auto space-y-2 p-4 bg-white rounded-t-lg border">
        {messages?.map((msg) => (
          <div
            key={msg.message_id}
            className={`max-w-[70%] rounded-lg px-3 py-2 text-sm ${
              msg.user_id === userId
                ? 'bg-blue-600 text-white ml-auto'
                : 'bg-gray-100'
            }`}
          >
            <p>{msg.message}</p>
            <p className="text-xs opacity-70 mt-1">
              {new Date(msg.creation_datetime).toLocaleTimeString()}
            </p>
          </div>
        ))}
        <div ref={bottomRef} />
      </div>
      <form onSubmit={handleSubmit} className="flex gap-2 border-t p-2 bg-white rounded-b-lg">
        <input
          className="flex-1 border rounded px-3 py-2"
          placeholder="Напишите сообщение..."
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
        <button className="bg-blue-600 text-white rounded px-4 py-2 text-sm hover:bg-blue-700">
          Отправить
        </button>
      </form>
    </div>
  )
}
