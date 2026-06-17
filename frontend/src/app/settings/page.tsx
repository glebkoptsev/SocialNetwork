'use client'

import { useState, useEffect, FormEvent } from 'react'
import { api } from '@/lib/api'
import { User } from '@/types'
import { useAuth } from '@/lib/auth'

export default function SettingsPage() {
  const userId = useAuth((s) => s.userId)
  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState('')
  const [form, setForm] = useState({
    first_name: '',
    second_name: '',
    birthdate: '',
    biography: '',
    city: '',
    who_can_message: 0,
  })

  useEffect(() => {
    if (!userId) return
    api.get<User>(`/api/user/get/${userId}`)
      .then((r) => {
        setForm({
          first_name: r.data.first_name,
          second_name: r.data.second_name,
          birthdate: r.data.birthdate,
          biography: r.data.biography,
          city: r.data.city,
          who_can_message: r.data.who_can_message ?? 0,
        })
      })
      .catch(() => setError('Не удалось загрузить профиль'))
      .finally(() => setLoading(false))
  }, [userId])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setSuccess(false)
    setError('')
    try {
      await api.put('/api/user/profile', form)
      setSuccess(true)
    } catch {
      setError('Ошибка сохранения')
    }
    setSaving(false)
  }

  const set = (key: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const value = key === 'who_can_message' ? Number(e.target.value) : e.target.value
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  if (loading) return <p className="text-gray-400 text-center">Загрузка...</p>

  return (
    <div className="max-w-lg mx-auto space-y-6">
      <h1 className="text-xl font-bold">Настройки профиля</h1>

      {success && <p className="text-green-600 text-sm">Сохранено</p>}
      {error && <p className="text-red-500 text-sm">{error}</p>}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="bg-white rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Основная информация</h2>
          <label className="block">
            <span className="text-xs text-gray-500">Имя</span>
            <input className="w-full border rounded px-3 py-2 mt-1" value={form.first_name} onChange={set('first_name')} required />
          </label>
          <label className="block">
            <span className="text-xs text-gray-500">Фамилия</span>
            <input className="w-full border rounded px-3 py-2 mt-1" value={form.second_name} onChange={set('second_name')} required />
          </label>
          <label className="block">
            <span className="text-xs text-gray-500">Дата рождения</span>
            <input className="w-full border rounded px-3 py-2 mt-1" type="date" max={new Date().toISOString().split('T')[0]} value={form.birthdate} onChange={set('birthdate')} required />
          </label>
          <label className="block">
            <span className="text-xs text-gray-500">О себе</span>
            <textarea className="w-full border rounded px-3 py-2 mt-1" value={form.biography} onChange={set('biography')} />
          </label>
          <label className="block">
            <span className="text-xs text-gray-500">Город</span>
            <input className="w-full border rounded px-3 py-2 mt-1" value={form.city} onChange={set('city')} />
          </label>
        </div>

        <div className="bg-white rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Конфиденциальность</h2>
          <label className="block">
            <span className="text-sm text-gray-600">Кто может писать личные сообщения</span>
            <select className="w-full border rounded px-3 py-2 mt-1" value={form.who_can_message} onChange={set('who_can_message')}>
              <option value={0}>Все пользователи</option>
              <option value={1}>Только подписчики</option>
            </select>
          </label>
        </div>

        <button
          type="submit"
          disabled={saving}
          className="w-full bg-blue-600 text-white rounded py-2 hover:bg-blue-700 disabled:opacity-50"
        >
          {saving ? 'Сохранение...' : 'Сохранить'}
        </button>
      </form>
    </div>
  )
}
