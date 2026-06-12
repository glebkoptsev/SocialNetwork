'use client'

import { useState, useEffect, useRef, useCallback } from 'react'
import { api } from '@/lib/api'
import { User } from '@/types'
import UserCard from '@/components/UserCard'

const PAGE_SIZE = 20

export default function SearchPage() {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<User[]>([])
  const [offset, setOffset] = useState(0)
  const [hasMore, setHasMore] = useState(false)
  const [loading, setLoading] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)

  const activeQueryRef = useRef('')
  const loadingRef = useRef(false)

  const loadResults = useCallback(async (q: string, off: number, append: boolean) => {
    if (!q.trim()) {
      setResults([])
      setHasMore(false)
      return
    }
    loadingRef.current = true
    setLoading(true)
    try {
      const { data } = await api.get<User[]>('/api/user/search', {
        params: { query: q, offset: off, limit: PAGE_SIZE },
      })
      if (append) {
        setResults(prev => [...prev, ...data])
      } else {
        setResults(data)
      }
      setHasMore(data.length === PAGE_SIZE)
      setOffset(off + data.length)
    } catch {
      if (!append) setResults([])
      setHasMore(false)
    }
    loadingRef.current = false
    setLoading(false)
  }, [])

  // Debounced search on query change
  useEffect(() => {
    const timer = setTimeout(() => {
      if (query !== activeQueryRef.current) {
        activeQueryRef.current = query
        loadResults(query, 0, false)
      }
    }, 300)
    return () => clearTimeout(timer)
  }, [query, loadResults])

  // Infinite scroll
  useEffect(() => {
    const el = sentinelRef.current
    if (!el) return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loadingRef.current && activeQueryRef.current.trim()) {
          loadResults(activeQueryRef.current, offset, true)
        }
      },
      { rootMargin: '200px' }
    )
    observer.observe(el)
    return () => observer.disconnect()
  }, [hasMore, offset, loadResults])

  return (
    <div className="space-y-4">
      <input
        className="w-full border rounded px-3 py-2"
        placeholder="Имя, фамилия или логин"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        autoFocus
      />
      {results.map((user) => (
        <UserCard key={user.user_id} user={user} />
      ))}
      {loading && <p className="text-gray-400 text-center">Поиск...</p>}
      {!loading && hasMore && <div ref={sentinelRef} className="h-4" />}
      {!loading && results.length === 0 && query.trim() && (
        <p className="text-gray-400 text-center">Пользователи не найдены.</p>
      )}
    </div>
  )
}
