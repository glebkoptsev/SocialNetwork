using Microsoft.EntityFrameworkCore;
using UserService.Database.Entities;

namespace UserService.Database
{
    public class PostRepository(UserDbContext context) : IPostRepository
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
                context.Posts.Add(postEntity);
                await context.SaveChangesAsync();
                return postId;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Posts.Add(postEntity);
            context.FeedOutbox.Add(new FeedOutboxEntity
            {
                Kafka_key = outboxEntry.KafkaKey,
                Kafka_value = outboxEntry.KafkaValue,
                Created_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return postId;
        }

        public async Task UpdatePostAsync(Guid post_id, string post, Guid user_id, OutboxEntry? outboxEntry = null)
        {
            var postEntity = await context.Posts
                .FirstOrDefaultAsync(p => p.Post_id == post_id && p.User_id == user_id)
                ?? throw new KeyNotFoundException($"Post {post_id} not found");

            postEntity.Text = post;

            if (outboxEntry is null)
            {
                await context.SaveChangesAsync();
                return;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            context.FeedOutbox.Add(new FeedOutboxEntity
            {
                Kafka_key = outboxEntry.KafkaKey,
                Kafka_value = outboxEntry.KafkaValue,
                Created_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task DeletePostAsync(Guid post_id, Guid user_id, OutboxEntry? outboxEntry = null)
        {
            var postEntity = await context.Posts
                .FirstOrDefaultAsync(p => p.Post_id == post_id && p.User_id == user_id)
                ?? throw new KeyNotFoundException($"Post {post_id} not found");

            context.Posts.Remove(postEntity);

            if (outboxEntry is null)
            {
                await context.SaveChangesAsync();
                return;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            context.FeedOutbox.Add(new FeedOutboxEntity
            {
                Kafka_key = outboxEntry.KafkaKey,
                Kafka_value = outboxEntry.KafkaValue,
                Created_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<Post?> GetPostAsync(Guid post_id)
        {
            var post = await context.Posts
                .Where(p => p.Post_id == post_id)
                .Join(context.Users, p => p.User_id, u => u.User_id, (p, u) => new Post
                {
                    Post_id = p.Post_id,
                    User_id = p.User_id,
                    Text = p.Text,
                    Creation_datetime = p.Creation_datetime,
                    AuthorFirstName = u.First_name,
                    AuthorSecondName = u.Second_name
                })
                .FirstOrDefaultAsync();
            return post;
        }

        public async Task<List<Post>> GetUserPostsAsync(Guid author_id, int offset, int limit)
        {
            var userPosts = from p in context.Posts
                            join u in context.Users on p.User_id equals u.User_id
                            where p.User_id == author_id
                            select new Post
                            {
                                Post_id = p.Post_id,
                                User_id = p.User_id,
                                Text = p.Text,
                                Creation_datetime = p.Creation_datetime,
                                AuthorFirstName = u.First_name,
                                AuthorSecondName = u.Second_name
                            };

            return await userPosts
                .OrderByDescending(p => p.Creation_datetime)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Post>> GetFeedAsync(Guid user_id, int offset, int limit)
        {
            var friendPosts = from f in context.Friends
                              join p in context.Posts on f.Friend_id equals p.User_id
                              join u in context.Users on p.User_id equals u.User_id
                              where f.User_id == user_id
                              select new Post
                              {
                                  Post_id = p.Post_id,
                                  User_id = p.User_id,
                                  Text = p.Text,
                                  Creation_datetime = p.Creation_datetime,
                                  AuthorFirstName = u.First_name,
                                  AuthorSecondName = u.Second_name
                              };

            var ownPosts = from p in context.Posts
                           join u in context.Users on p.User_id equals u.User_id
                           where p.User_id == user_id
                           select new Post
                           {
                               Post_id = p.Post_id,
                               User_id = p.User_id,
                               Text = p.Text,
                               Creation_datetime = p.Creation_datetime,
                               AuthorFirstName = u.First_name,
                               AuthorSecondName = u.Second_name
                           };

            var posts = await friendPosts
                .Union(ownPosts)
                .OrderByDescending(p => p.Creation_datetime)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            return posts;
        }
    }
}
