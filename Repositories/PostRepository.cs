using Giahuy0790.Data;
using Giahuy0790.Models;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Repositories;

/// <summary>
/// Triển khai Repository cho Bài đăng (Post) kế thừa từ Generic Repository
/// </summary>
public class PostRepository(AppDbContext context) : Repository<Post>(context), IPostRepository
{
    public override async Task<IEnumerable<Post>> GetAllAsync()
    {
        return await GetFeedAsync();
    }

    public async Task<IEnumerable<Post>> GetFeedAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<int?> LikePostAsync(int id)
    {
        var post = await _dbSet.FindAsync(id);
        if (post == null)
        {
            return null;
        }

        post.LikesCount += 1;
        await _context.SaveChangesAsync();

        return post.LikesCount;
    }
}
