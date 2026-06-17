import axios from 'axios'

function authInterceptor(config: any) {
  if (typeof window !== 'undefined') {
    const token = document.cookie
      .split('; ')
      .find((row) => row.startsWith('token='))
      ?.split('=')[1]
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
  }
  return config
}

function unauthorizedInterceptor() {
  if (typeof window === 'undefined') return
  document.cookie = 'token=; path=/; max-age=0'
  document.cookie = 'userId=; path=/; max-age=0'
  window.location.href = '/login'
}

export const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000',
})

api.interceptors.request.use(authInterceptor)
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err?.response?.status === 401 && typeof window !== 'undefined') {
      unauthorizedInterceptor()
    }
    return Promise.reject(err)
  }
)

export const dialogApi = axios.create({
  baseURL: process.env.NEXT_PUBLIC_DIALOG_API_URL || 'http://localhost:5002',
})

dialogApi.interceptors.request.use(authInterceptor)
dialogApi.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err?.response?.status === 401 && typeof window !== 'undefined') {
      unauthorizedInterceptor()
    }
    return Promise.reject(err)
  }
)
