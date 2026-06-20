export interface User {
  user_id: string
  first_name: string
  second_name: string
  birthdate: string
  biography: string
  city: string
  login: string
  who_can_message: number
}

export interface Post {
  post_id: string
  user_id: string
  text: string
  creation_datetime: string
  authorFirstName?: string
  authorSecondName?: string
  like_count: number
}

export interface Chat {
  chat_id: string
  creator_id: string
  chat_name: string
  creation_datetime: string
}

export interface Message {
  message_id: string
  chat_id: string
  user_id: string
  user_name: string
  message: string
  creation_datetime: string
  status: number
}

export interface LoginResponse {
  access_token: string
  expiresIn: number
}
