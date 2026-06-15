'use client'

import { useState, useEffect, FormEvent } from 'react'
import { useRouter } from 'next/navigation'
import { api } from '@/lib/api'
import { User } from '@/types'
import { useAuth } from '@/lib/auth'

export default function SettingsPage() {
  const userId = useAuth((s) => s.userId)
  const router = useRouter()
  const [saving, setSaving] = useState(false)
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
    api.get<User>(`/api/user/get/${userId}`).then((r) => {
      setForm({
        first_name: r.data.first_name,
        second_name: r.data.second_name,
        birthdate: r.data.birthdate,
        biography: r.data.biography,
        city: r.data.city,
        who_can_message: r.data.who_can_message ?? 0,
      })
    })
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

  const set = (key: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) =>
    setForm((prev) => ({ ...prev, [key]: e.target.value }))

  return (
    <div className="max-w-lg mx-auto space-y-6">
      <h1 className="text-xl font-bold">Настройки профиля</h1>

      {success && <p className="text-green-600 text-sm">Сохранено</p>}
      {error && <p className="text-red-500 text-sm">{error}</p>}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="bg-white rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Основная информация</h2>
          <input className="w-full border rounded px-3 py-2" placeholder="Имя" value={form.first_name} onChange={set('first_name')} required />
          <input className="w-full border rounded px-3 py-2" placeholder="Фамилия" value={form.second_name} onChange={set('second_name')} required />
          <input className="w-full border rounded px-3 py-2" type="date" value={form.birthdate} onChange={set('birthdate')} required />
          <textarea className="w-full border rounded px-3 py-2" placeholder="О себе" value={form.biography} onChange={set('biography')} />
          <input className="w-full border rounded px-3 py-2" placeholder="Город" value={form.city} onChange={set('city')} />
        </div>

        <div className="bg-white rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">Конфиденциальность</h2>
          <label className="block text-sm text-gray-600">Кто может писать личные сообщения</label>
          <select className="w-full border rounded px-3 py-2" value={form.who_can_message} onChange={set('who_can_message')}>
            <option value={0}>Все пользователи</option>
            <option value={1}>Только подписчики</option>
          </select>
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
