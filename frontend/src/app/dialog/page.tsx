'use client'

import { useState, useEffect, useRef, useCallback } from 'react'
import { dialogApi } from '@/lib/api'
import { Chat } from '@/types'
import Link from 'next/link'

const PAGE_SIZE = 20

export default function DialogListPage() {
  const [chats, setChats] = useState<Chat[]>([])
  const [offset, setOffset] = useState(0)
  const [hasMore, setHasMore] = useState(true)
  const [loading, setLoading] = useState(false)
  const [initialLoading, setInitialLoading] = useState(true)
  const sentinelRef = useRef<HTMLDivElement>(null)
  const loadingRef = useRef(false)

  const loadChats = useCallback(async (off: number, append: boolean) => {
    loadingRef.current = true
    setLoading(true)
    try {
      const { data } = await dialogApi.get<Chat[]>('/api/dialog/list', {
        params: { offset: off, limit: PAGE_SIZE },
      })
      if (append) {
        setChats(prev => [...prev, ...data])
      } else {
        setChats(data)
      }
      setHasMore(data.length === PAGE_SIZE)
      setOffset(off + data.length)
    } catch {
      if (!append) setChats([])
      setHasMore(false)
    }
    loadingRef.current = false
    setLoading(false)
    setInitialLoading(false)
  }, [])

  useEffect(() => { loadChats(0, false) }, [loadChats])

  useEffect(() => {
    const el = sentinelRef.current
    if (!el) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loadingRef.current) {
          loadChats(offset, true)
        }
      },
      { rootMargin: '200px' }
    )
    observer.observe(el)
    return () => observer.disconnect()
  }, [hasMore, offset, loadChats])

  return (
    <div className="space-y-2">
      <h1 className="text-xl font-bold">Сообщения</h1>
      {initialLoading && <p className="text-gray-400">Загрузка...</p>}
      {chats.map((chat) => (
        <Link
          key={chat.chat_id}
          href={`/dialog/${chat.chat_id}`}
          className="block bg-white rounded-lg border p-4 hover:shadow-sm"
        >
          <p className="font-medium">{chat.chat_name}</p>
        </Link>
      ))}
      {loading && !initialLoading && <p className="text-gray-400 text-center">Загрузка...</p>}
      {!loading && hasMore && <div ref={sentinelRef} className="h-4" />}
      {!initialLoading && chats.length === 0 && (
        <p className="text-gray-400">Чатов пока нет.</p>
      )}
    </div>
  )
}
