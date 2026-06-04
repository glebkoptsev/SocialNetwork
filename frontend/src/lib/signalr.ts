import * as signalR from '@microsoft/signalr'

let connection: signalR.HubConnection | null = null

const signalrUrl = process.env.NEXT_PUBLIC_SIGNALR_URL || 'http://localhost:5003'

export function getFeedConnection(token: string) {
  if (connection?.state === signalR.HubConnectionState.Connected) {
    return connection
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${signalrUrl}/post/feed/posted`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .build()

  return connection
}
