import { test, expect } from '@playwright/test'

async function login(page: any) {
  await page.goto('/login')
  await page.waitForSelector('h1:has-text("Вход")')
  await page.getByLabel('Логин').fill('test')
  await page.getByLabel('Пароль').fill('12345')
  await page.getByRole('button', { name: 'Войти' }).click()
  await page.waitForURL('/feed', { timeout: 10000 })
}

test.describe('SocialNetwork E2E', () => {
  test('login and navigate feed', async ({ page }) => {
    await login(page)
    await expect(page.locator('header')).toContainText('Лента')
    await expect(page.locator('textarea')).toBeVisible()
  })

  test('search for users', async ({ page }) => {
    await login(page)
    await page.getByRole('link', { name: 'Поиск' }).click()
    await page.waitForURL('/search')
    await page.getByPlaceholder('Имя, фамилия или логин').fill('admin')
    await page.waitForTimeout(500)
    await expect(page.getByText('Admin User').first()).toBeVisible({ timeout: 5000 })
  })

  test('create post and verify via API', async ({ page, request }) => {
    await login(page)
    const postText = `PW ${Date.now()}`
    await page.getByPlaceholder('Что у вас на уме?').fill(postText)
    await page.getByRole('button', { name: 'Опубликовать' }).click()
    await page.waitForTimeout(500)

    const token = await page.evaluate(() =>
      document.cookie.split('; ').find(r => r.startsWith('token='))?.split('=')[1]
    )

    // Retry loop: pipeline may take a moment
    let found = false
    let postId = ''
    for (let i = 0; i < 10; i++) {
      const feedRes = await request.get('http://localhost:5000/api/post/feed?limit=50', {
        headers: { Authorization: `Bearer ${token}` }
      })
      const posts = await feedRes.json()
      const p = posts.find((p: any) => p.text === postText)
      if (p) { found = true; postId = p.post_id; break }
      await new Promise(r => setTimeout(r, 1000))
    }
    expect(found).toBeTruthy()

    // Like toggle test
    let likeRes = await request.post(`http://localhost:5000/api/post/${postId}/like`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    let like = await likeRes.json()
    expect(like.liked).toBe(true)
    expect(like.like_count).toBe(1)

    // Toggle off
    likeRes = await request.post(`http://localhost:5000/api/post/${postId}/like`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    like = await likeRes.json()
    expect(like.liked).toBe(false)
    expect(like.like_count).toBe(0)

    // Verify like_count via feed
    const feedAgain = await request.get('http://localhost:5000/api/post/feed?limit=50', {
      headers: { Authorization: `Bearer ${token}` }
    })
    const postsAgain = await feedAgain.json()
    const updatedPost = postsAgain.find((p: any) => p.post_id === postId)
    expect(updatedPost.like_count).toBe(0)
  })

  test('chat: create personal chat and send message', async ({ page }) => {
    await login(page)
    await page.goto('/user/admin')
    await page.waitForSelector('text=Admin User')

    const msgBtn = page.getByRole('button', { name: 'Написать сообщение' })
    if (await msgBtn.isVisible()) {
      await msgBtn.click()
      await page.waitForURL(/\/dialog\//)

      const msgText = `PW ${Date.now()}`
      await page.getByPlaceholder('Напишите сообщение...').fill(msgText)
      await page.getByRole('button', { name: 'Отправить' }).click()
      await page.waitForTimeout(1000)
      await expect(page.getByText(msgText).first()).toBeVisible()
    }
  })

  test('404 page shows', async ({ page }) => {
    await login(page)
    await page.goto('/nonexistent-route')
    await expect(page.getByRole('heading', { name: '404' })).toBeVisible({ timeout: 5000 })
    await expect(page.getByText('Страница не найдена')).toBeVisible()
  })
})
