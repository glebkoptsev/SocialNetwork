'use client'

import { useState, FormEvent } from 'react'
import { api } from '@/lib/api'
import { useQueryClient } from '@tanstack/react-query'

export default function PostComposer() {
  const [text, setText] = useState('')
  const queryClient = useQueryClient()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!text.trim()) return
    await api.post('/api/post/create', { text })
    setText('')
    queryClient.invalidateQueries({ queryKey: ['feed'] })
  }

  return (
    <form onSubmit={handleSubmit} className="bg-white rounded-lg border p-4 space-y-2">
      <textarea
        className="w-full border rounded px-3 py-2 resize-none"
        rows={3}
        placeholder="Что у вас на уме?"
        value={text}
        onChange={(e) => setText(e.target.value)}
      />
      <div className="flex justify-end">
        <button className="bg-blue-600 text-white rounded px-4 py-1.5 text-sm hover:bg-blue-700">
          Опубликовать
        </button>
      </div>
    </form>
  )
}
