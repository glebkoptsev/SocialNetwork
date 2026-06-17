'use client'

import { useEffect } from 'react'
import Link from 'next/link'

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  useEffect(() => {
    console.error(error)
  }, [error])

  return (
    <div className="flex flex-col items-center justify-center min-h-[50vh] space-y-4">
      <h2 className="text-xl font-bold text-red-500">Что-то пошло не так</h2>
      <p className="text-gray-500 text-sm">{error.message}</p>
      <div className="flex gap-3">
        <button
          onClick={reset}
          className="bg-blue-600 text-white rounded px-4 py-2 text-sm hover:bg-blue-700"
        >
          Попробовать снова
        </button>
        <Link href="/feed" className="text-blue-600 hover:underline self-center">
          В ленту
        </Link>
      </div>
    </div>
  )
}
