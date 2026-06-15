'use client'

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'next/navigation'
import { dialogApi } from '@/lib/api'
import { Message } from '@/types'
import { useState, FormEvent, useEffect, useRef } from 'react'
import { useAuth } from '@/lib/auth'

export default function ChatPage() {
  const { chatId } = useParams<{ chatId: string }>()
  const userId = useAuth((s) => s.userId)
  const queryClient = useQueryClient()
  const [text, setText] = useState('')
  const bottomRef = useRef<HTMLDivElement>(null)
  const scrolled = useRef(false)

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
    if (scrolled.current || !messages?.length) return
    scrolled.current = true
    setTimeout(() => bottomRef.current?.scrollIntoView(), 0)
  }, [messages])

  return (
    <div className="max-w-2xl mx-auto flex flex-col h-[calc(100vh-8rem)]">
      <div className="flex-1 overflow-y-auto flex flex-col p-4 bg-white border-x border-t rounded-t-lg">
        <div className="mt-auto space-y-2">
          {messages?.map((msg) => (
            <div
              key={msg.message_id}
              className={`max-w-[75%] rounded-lg px-3 py-2 text-sm ${
                msg.user_id === userId
                  ? 'bg-blue-600 text-white ml-auto rounded-br-sm'
                  : 'bg-gray-100 rounded-bl-sm'
              }`}
            >
              {msg.user_id !== userId && (
                <p className="text-xs font-semibold mb-1">{msg.user_name}</p>
              )}
              <p>{msg.message}</p>
              <p className="text-xs opacity-70 mt-1">
                {new Date(msg.creation_datetime).toLocaleTimeString()}
              </p>
            </div>
          ))}
        </div>
        <div ref={bottomRef} />
      </div>
      <form onSubmit={handleSubmit} className="flex gap-2 border p-2 bg-white rounded-b-lg">
        <input
          className="flex-1 border rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Напишите сообщение..."
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
        <button className="bg-blue-600 text-white rounded-lg px-5 py-2 text-sm hover:bg-blue-700 whitespace-nowrap">
          Отправить
        </button>
      </form>
    </div>
  )
}
