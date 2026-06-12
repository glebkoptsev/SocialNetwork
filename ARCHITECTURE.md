# Архитектура SocialNetwork

## Стек

| Компонент | Технология | Роль |
|-----------|-----------|------|
| Frontend | Next.js 16 (Turbopack), Tailwind | UI, `localhost:8080` |
| user_api | ASP.NET Core 10 | Auth, Users, Posts, Friends, Search |
| dialog_api | ASP.NET Core 10 | Чаты (messages) + SignalR Hub |
| livefeed | ASP.NET Core 10 | SignalR Hub для уведомлений ленты |
| cache_update | ASP.NET Core 10 | Kafka consumer → обновление Redis-кеша |
| feed_client | ASP.NET Core 10 | Kafka consumer → отправка SignalR событий |
| PostgreSQL 16 | single DB `socialnetwork`, schema `app_users` | Все данные |
| Redis 7 | IDistributedCache + RedLock | Кеш ленты (`feed-{userId}`), distributed locks |
| Kafka 4.0.1 (KRaft mode) | topic: `feed-posts` | Асинхронные уведомления (outbox pattern) |

> ZooKeeper удалён — Kafka работает в KRaft mode (без внешнего координатора).

## Схема

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           FRONTEND (Next.js)                                │
│  localhost:8080                                                              │
│                                                                              │
│  ┌──────────┐  ┌───────────┐  ┌───────────┐  ┌──────────────────────┐      │
│  │  Search   │  │  Feed     │  │  Profile  │  │  Dialog (Chat)       │      │
│  │  (REST)   │  │  (REST +  │  │  (REST)   │  │  (REST / SignalR)   │      │
│  │           │  │  SignalR) │  │           │  │                      │      │
│  └─────┬─────┘  └───┬───────┘  └─────┬─────┘  └──────────┬───────────┘      │
└────────┼────────────┼────────────────┼───────────────────┼──────────────────┘
         │            │                │                   │
    HTTP │       HTTP │+WS        HTTP │              HTTP │
         │            │                │                   │
┌────────┼────────────┼────────────────┼───────────────────┼──────────────────┐
│  ▼     │      ▼     │     ▼          │        ▼          │                  │
│  ┌─────┴────────────┴─────────────────┴──────────────────┴──────────────┐  │
│  │                    user_api (ASP.NET :5000 / :5001)                   │  │
│  │  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌────────────────────────┐ │  │
│  │  │Security  │ │ UserCtrl │ │PostCtrl   │ │ FriendCtrl             │ │  │
│  │  │/login    │ │/register │ │/feed      │ │ /subscribe /status     │ │  │
│  │  │          │ │/get/{id} │ │/create    │ │ /subscriptions /folws  │ │  │
│  │  │          │ │/search   │ │/delete    │ │                        │ │  │
│  │  └──────────┘ └──────────┘ └───────────┘ └────────────────────────┘ │  │
│  │  ┌────────────────────────────────────────────────────────────────┐ │  │
│  │  │ Services: UsersService, PostService, FriendService              │ │  │
│  │  │ DB: EF Core + Npgsql │ Redis: IDistributedCache                │ │  │
│  │  │ Kafka: IProducer (feed_posts topic, outbox pattern)            │ │  │
│  │  └────────────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌─────────────────┐  ┌────────────────────────┐  ┌──────────────────────┐  │
│  │  livefeed       │  │  cache_update          │  │  feed_client         │  │
│  │  (ASP.NET :5003)│  │  (ASP.NET)              │  │  (ASP.NET)           │  │
│  │  SignalR Hub    │  │  Kafka consumer         │  │  Kafka consumer      │  │
│  │  /post/feed/    │  │  → обновляет            │  │  → отправляет        │  │
│  │  posted         │  │    Redis-кеш ленты      │  │    SignalR событие   │  │
│  └────────┬────────┘  └──────────┬──────────────┘  └──────────┬───────────┘  │
│           │                      │                            │              │
│  ┌────────┴──────────────────────┴────────────────────────────┴──────────┐  │
│  │                    Kafka (topic: feed-posts)                           │  │
│  │                    ZooKeeper (координация)                            │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌──────────────────────────────┐  ┌─────────────────────────────────┐     │
│  │  postgres:16                 │  │  redis:7-alpine                  │     │
│  │  DB: socialnetwork           │  │  Keys: feed-{userId} (кеш ленты)│     │
│  │  Schema: app_users           │  │  Keys: redlock:* (distr. locks) │     │
│  │  ┌─ users (100k)             │  │                                 │     │
│  │  ├─ posts (3.5M)            │  │                                 │     │
│  │  ├─ friends (140k)          │  │                                 │     │
│  │  ├─ feed_outbox             │  │                                 │     │
│  │  ├─ dialogs (messages)      │  │                                 │     │
│  │  └─ EF Migrations           │  │                                 │     │
│  │  Indexes: pg_trgm GIN на    │  │                                 │     │
│  │  (first_name||second_name   │  │                                 │     │
│  │   ||login) для поиска       │  │                                 │     │
│  └──────────────────────────────┘  └─────────────────────────────────┘     │
│                                                                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Flow создания поста

```
POST /api/post/create (user_api)
       │
       ├── INSERT post ─────────────────────► postgres
       │
       ├── INSERT feed_outbox (по 1/follower)
       │
       ▼
   Kafka topic: feed-posts
       │
       ├──────► cache_update ──UPDATE──► Redis (feed-{followerId})
       │
       └──────► feed_client ──SignalR──► livefeed (Hub)
                                              │
                                         Receive event
                                              │
                                              ▼
                                         frontend (refetch feed)
```

## Flow подписки

```
POST /api/friend/set/{targetId} (user_api)
       │
       ├── INSERT friend (user_id → friend_id) ──► postgres
       │
       ├── Kafka (feed-posts) ──► cache_update
       │                            └──► Redis (создать ключ feed-{targetId}
       │                                      с постами нового друга)
       │
       └── Response: {"status": "friend_added", "direction": "outgoing"}
```

## Пагинация (конвенция)

Все GET-эндпоинты, возвращающие списки, принимают:
- `offset` (int, default = 0)
- `limit` (int, default = 20)

Фронт использует `IntersectionObserver` с `rootMargin: 200px` для дозагрузки при скролле. Кнопки "Загрузить ещё" / пагинация страниц не используются.
