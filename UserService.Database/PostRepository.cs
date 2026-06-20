using Microsoft.EntityFrameworkCore;
using UserService.Database.Entities;

namespace UserService.Database
{
    public class PostRepository(UserDbContext writeDb, UserReadDbContext readDb) : IPostRepository
    {
        public async Task<Guid> AddPostAsync(Guid user_id, string post, Guid postId, OutboxEntry? outboxEntry = null)
        {
            var postEntity = new Post
            {
                Post_id = postId,
                User_id = user_id,
                Text = post,
                Creation_datetime = DateTime.UtcNow
            };

            if (outboxEntry is null)
            {
                writeDb.Posts.Add(postEntity);
                await writeDb.SaveChangesAsync();
                return postId;
            }

            await using var transaction = await writeDb.Database.BeginTransactionAsync();
            writeDb.Posts.Add(postEntity);
            writeDb.FeedOutbox.Add(new FeedOutboxEntity
            {
                Kafka_key = outboxEntry.KafkaKey,
                Kafka_value = outboxEntry.KafkaValue,
                Created_at = DateTime.UtcNow
            });
            await writeDb.SaveChangesAsync();
            await transaction.CommitAsync();
            return postId;
        }

        public async Task UpdatePostAsync(Guid post_id, string post, Guid user_id, OutboxEntry? outboxEntry = null)
        {
            var postEntity = await writeDb.Posts.FirstOrDefaultAsync(p => p.Post_id == post_id && p.User_id == user_id)
                ?? throw new KeyNotFoundException();
            postEntity.Text = post;

            if (outboxEntry is not null)
            {
                writeDb.FeedOutbox.Add(new FeedOutboxEntity
                {
                    Kafka_key = outboxEntry.KafkaKey,
                    Kafka_value = outboxEntry.KafkaValue,
                    Created_at = DateTime.UtcNow
                });
            }

            await writeDb.SaveChangesAsync();
        }

        public async Task DeletePostAsync(Guid post_id, Guid user_id, OutboxEntry? outboxEntry = null)
        {
            var postEntity = await writeDb.Posts.FirstOrDefaultAsync(p => p.Post_id == post_id && p.User_id == user_id);
            if (postEntity is null) return;

            writeDb.Posts.Remove(postEntity);

            if (outboxEntry is not null)
            {
                writeDb.FeedOutbox.Add(new FeedOutboxEntity
                {
                    Kafka_key = outboxEntry.KafkaKey,
                    Kafka_value = outboxEntry.KafkaValue,
                    Created_at = DateTime.UtcNow
                });
            }

            await writeDb.SaveChangesAsync();
        }

        public async Task<Post?> GetPostAsync(Guid post_id)
        {
            var post = await writeDb.Posts
                .Where(p => p.Post_id == post_id)
                .Join(writeDb.Users, p => p.User_id, u => u.User_id, (p, u) => new Post
                {
                    Post_id = p.Post_id,
                    User_id = p.User_id,
                    Text = p.Text,
                    Creation_datetime = p.Creation_datetime,
                    AuthorFirstName = u.First_name,
                    AuthorSecondName = u.Second_name,
                    LikeCount = p.LikeCount
                })
                .FirstOrDefaultAsync();
            return post;
        }

        public async Task<List<Post>> GetUserPostsAsync(Guid author_id, int offset, int limit)
        {
            var userPosts = from p in readDb.Posts
                            join u in readDb.Users on p.User_id equals u.User_id
                            where p.User_id == author_id
                            select new Post
                            {
                                Post_id = p.Post_id,
                                User_id = p.User_id,
                                Text = p.Text,
                                Creation_datetime = p.Creation_datetime,
                                AuthorFirstName = u.First_name,
                                AuthorSecondName = u.Second_name,
                    LikeCount = p.LikeCount
                            };

            return await userPosts
                .OrderByDescending(p => p.Creation_datetime)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Post>> GetFeedAsync(Guid user_id, int offset, int limit)
        {
            var friendPosts = from f in readDb.Friends
                              join p in readDb.Posts on f.Friend_id equals p.User_id
                              join u in readDb.Users on p.User_id equals u.User_id
                              where f.User_id == user_id
                              select new Post
                              {
                                  Post_id = p.Post_id,
                                  User_id = p.User_id,
                                  Text = p.Text,
                                  Creation_datetime = p.Creation_datetime,
                                  AuthorFirstName = u.First_name,
                                  AuthorSecondName = u.Second_name,
                    LikeCount = p.LikeCount
                              };

            var ownPosts = from p in readDb.Posts
                           join u in readDb.Users on p.User_id equals u.User_id
                           where p.User_id == user_id
                           select new Post
                           {
                               Post_id = p.Post_id,
                               User_id = p.User_id,
                               Text = p.Text,
                               Creation_datetime = p.Creation_datetime,
                               AuthorFirstName = u.First_name,
                               AuthorSecondName = u.Second_name,
                    LikeCount = p.LikeCount
                           };

            var posts = await friendPosts
                .Union(ownPosts)
                .OrderByDescending(p => p.Creation_datetime)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            return posts;
        }

        public async Task<(bool Liked, int LikeCount)> ToggleLikeAsync(Guid post_id, Guid user_id)
        {
            await using var tx = await writeDb.Database.BeginTransactionAsync();
            var existing = await writeDb.Likes.FirstOrDefaultAsync(l => l.Post_id == post_id && l.User_id == user_id);
            if (existing is not null)
            {
                writeDb.Likes.Remove(existing);
                await writeDb.Posts.Where(p => p.Post_id == post_id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, p => p.LikeCount - 1));
                await writeDb.SaveChangesAsync();
                await tx.CommitAsync();
                var count = await writeDb.Posts.Where(p => p.Post_id == post_id).Select(p => p.LikeCount).FirstOrDefaultAsync();
                return (false, count);
            }
            else
            {
                writeDb.Likes.Add(new LikeEntity { Post_id = post_id, User_id = user_id, Created_at = DateTime.UtcNow });
                await writeDb.Posts.Where(p => p.Post_id == post_id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, p => p.LikeCount + 1));
                await writeDb.SaveChangesAsync();
                await tx.CommitAsync();
                var count = await writeDb.Posts.Where(p => p.Post_id == post_id).Select(p => p.LikeCount).FirstOrDefaultAsync();
                return (true, count);
            }
        }
    }
}
