'use client'

import { useState, FormEvent } from 'react'
import { useAuth } from '@/lib/auth'
import { useRouter } from 'next/navigation'
import Link from 'next/link'

export default function RegisterPage() {
  const [form, setForm] = useState({
    first_name: '',
    second_name: '',
    birthdate: '',
    biography: '',
    city: '',
    password: '',
  })
  const [error, setError] = useState('')
  const register = useAuth((s) => s.register)
  const login = useAuth((s) => s.login)
  const router = useRouter()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      const userId = await register(form)
      await login(userId, form.password)
      router.push('/feed')
    } catch {
      setError('Ошибка регистрации')
    }
  }

  const set = (key: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm((prev) => ({ ...prev, [key]: e.target.value }))

  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-3">
        <h1 className="text-2xl font-bold text-center">Регистрация</h1>
        {error && <p className="text-red-500 text-sm text-center">{error}</p>}
        <input className="w-full border rounded px-3 py-2" placeholder="Имя" value={form.first_name} onChange={set('first_name')} required />
        <input className="w-full border rounded px-3 py-2" placeholder="Фамилия" value={form.second_name} onChange={set('second_name')} required />
        <input className="w-full border rounded px-3 py-2" type="date" value={form.birthdate} onChange={set('birthdate')} required />
        <textarea className="w-full border rounded px-3 py-2" placeholder="О себе" value={form.biography} onChange={set('biography')} />
        <input className="w-full border rounded px-3 py-2" placeholder="Город" value={form.city} onChange={set('city')} />
        <input className="w-full border rounded px-3 py-2" type="password" placeholder="Пароль" value={form.password} onChange={set('password')} required />
        <button className="w-full bg-blue-600 text-white rounded py-2 hover:bg-blue-700">Зарегистрироваться</button>
        <p className="text-sm text-center text-gray-500">
          Уже есть аккаунт?{' '}
          <Link href="/login" className="text-blue-600 underline">Войти</Link>
        </p>
      </form>
    </div>
  )
}
