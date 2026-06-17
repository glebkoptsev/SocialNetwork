'use client'

import { useState, FormEvent } from 'react'
import { api } from '@/lib/api'

export default function PostComposer() {
  const [text, setText] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!text.trim()) return
    setLoading(true)
    setError('')
    try {
      await api.post('/api/post/create', { text })
      setText('')
    } catch {
      setError('Не удалось опубликовать пост')
    }
    setLoading(false)
  }

  return (
    <form onSubmit={handleSubmit} className="bg-white rounded-lg border p-4 space-y-2">
      <textarea
        className="w-full border rounded px-3 py-2 resize-none"
        rows={3}
        placeholder="Что у вас на уме?"
        value={text}
        onChange={(e) => setText(e.target.value)}
        maxLength={2000}
      />
      {error && <p className="text-red-500 text-xs">{error}</p>}
      <div className="flex justify-end">
        <button
          className="bg-blue-600 text-white rounded px-4 py-1.5 text-sm hover:bg-blue-700 disabled:opacity-50"
          disabled={loading || !text.trim()}
        >
          {loading ? 'Публикация...' : 'Опубликовать'}
        </button>
      </div>
    </form>
  )
}
