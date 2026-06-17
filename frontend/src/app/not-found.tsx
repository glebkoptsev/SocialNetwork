import Link from 'next/link'

export default function NotFound() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[50vh] space-y-4">
      <h1 className="text-3xl font-bold text-gray-400">404</h1>
      <p className="text-gray-500">Страница не найдена</p>
      <Link href="/feed" className="text-blue-600 hover:underline">
        Вернуться в ленту
      </Link>
    </div>
  )
}
