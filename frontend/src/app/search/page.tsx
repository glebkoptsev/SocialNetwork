'use client'

import { useState, FormEvent } from 'react'
import { api } from '@/lib/api'
import { User } from '@/types'
import UserCard from '@/components/UserCard'

export default function SearchPage() {
  const [firstName, setFirstName] = useState('')
  const [secondName, setSecondName] = useState('')
  const [results, setResults] = useState<User[] | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSearch = async (e: FormEvent) => {
    e.preventDefault()
    if (!firstName.trim() && !secondName.trim()) return
    setLoading(true)
    try {
      const { data } = await api.get<User[]>('/api/user/search', {
        params: { first_name: firstName, second_name: secondName },
      })
      setResults(data)
    } catch {
      setResults([])
    }
    setLoading(false)
  }

  return (
    <div className="space-y-4">
      <form onSubmit={handleSearch} className="flex gap-2">
        <input
          className="flex-1 border rounded px-3 py-2"
          placeholder="Имя"
          value={firstName}
          onChange={(e) => setFirstName(e.target.value)}
        />
        <input
          className="flex-1 border rounded px-3 py-2"
          placeholder="Фамилия"
          value={secondName}
          onChange={(e) => setSecondName(e.target.value)}
        />
        <button className="bg-blue-600 text-white rounded px-4 py-2 hover:bg-blue-700">
          Поиск
        </button>
      </form>
      {loading && <p className="text-gray-400 text-center">Поиск...</p>}
      {results?.map((user) => (
        <UserCard key={user.user_id} user={user} />
      ))}
      {results?.length === 0 && (
        <p className="text-gray-400 text-center">Пользователи не найдены.</p>
      )}
    </div>
  )
}
